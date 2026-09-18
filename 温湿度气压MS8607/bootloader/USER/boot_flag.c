#include "bootloader.h"
#include "crc_check.h"
#include "string.h"

#define LEGACY_BOOT_FLAG_MAGIC 0x5AA5A55AU

typedef struct
{
    uint32_t magic;
    uint32_t boot_flag;
    uint32_t boot_count;
    uint32_t crc32;
} legacy_boot_flag_t;

static uint8_t copy_buffer[FLASH_SECTOR_SIZE];

static uint32_t crc32_bytes(const uint8_t *data, uint32_t length)
{
    uint32_t crc;
    uint32_t i;
    uint8_t bit;

    crc = 0xFFFFFFFFU;
    for (i = 0; i < length; i++)
    {
        crc ^= data[i];
        for (bit = 0; bit < 8; bit++)
        {
            if (crc & 1U)
            {
                crc = (crc >> 1) ^ 0xEDB88320U;
            }
            else
            {
                crc >>= 1;
            }
        }
    }
    return ~crc;
}

static uint8_t metadata_state_valid(uint32_t state)
{
    return (state == BOOT_STATE_NORMAL ||
            state == BOOT_STATE_UPDATE_PENDING ||
            state == BOOT_STATE_BACKUP ||
            state == BOOT_STATE_RECEIVING ||
            state == BOOT_STATE_TRIAL) ? 1 : 0;
}

static uint8_t metadata_record_valid(uint32_t address,
                                     boot_metadata_t *metadata)
{
    uint32_t crc;

    memcpy(metadata, (void *)address, sizeof(boot_metadata_t));

    if (metadata->magic != BOOT_METADATA_MAGIC ||
        !metadata_state_valid(metadata->state) ||
        metadata->active_size > APP_IMAGE_MAX_SIZE ||
        metadata->backup_size > APP_IMAGE_MAX_SIZE ||
        metadata->trial_attempts > BOOT_MAX_TRIAL_ATTEMPTS)
    {
        return 0;
    }

    crc = crc32_bytes((const uint8_t *)metadata, 32U);
    if (crc != metadata->record_crc32)
    {
        return 0;
    }
    return 1;
}

static uint8_t sequence_is_newer(uint32_t left, uint32_t right)
{
    return ((int32_t)(left - right) > 0) ? 1 : 0;
}

static uint8_t legacy_record_valid(legacy_boot_flag_t *legacy)
{
    uint32_t crc;

    memcpy(legacy, (void *)BOOT_FLAG_SECTOR_ADDR, sizeof(legacy_boot_flag_t));
    if (legacy->magic != LEGACY_BOOT_FLAG_MAGIC)
    {
        return 0;
    }

    crc = crc32_bytes((const uint8_t *)legacy, 12U);
    return crc == legacy->crc32 ? 1 : 0;
}

static uint8_t legacy_to_metadata(boot_metadata_t *metadata)
{
    legacy_boot_flag_t legacy;

    if (!legacy_record_valid(&legacy))
    {
        return 0;
    }

    memset(metadata, 0, sizeof(boot_metadata_t));
    metadata->magic = BOOT_METADATA_MAGIC;
    metadata->sequence = 0;
    metadata->active_size = APP_IMAGE_MAX_SIZE;

    /*
     * Old firmware may have been downloaded by a programmer or may use
     * the old tail CRC format. The new bootloader cannot safely infer that
     * old length, so keep the direct-programming compatibility sentinel.
     */
    if (!boot_image_is_valid(APP_SECTOR_ADDR,
                             APP_IMAGE_MAX_SIZE,
                             0xFFFFFFFFU))
    {
        metadata->active_size = 0;
        metadata->active_crc32 = 0;
    }
    else
    {
        metadata->active_crc32 = 0xFFFFFFFFU;
    }

    if (legacy.boot_flag == 0x55AA5502U ||
        legacy.boot_flag == 0x55AA5503U ||
        legacy.boot_flag == BOOT_FLAG_NEED_UPDATE ||
        legacy.boot_flag == BOOT_FLAG_FIRST_BOOT)
    {
        metadata->state = BOOT_STATE_UPDATE_PENDING;
    }
    else if (legacy.boot_flag == 0x55AA5501U ||
             legacy.boot_flag == BOOT_FLAG_NORMAL)
    {
        metadata->state = metadata->active_size != 0U
                        ? BOOT_STATE_NORMAL
                        : BOOT_STATE_UPDATE_PENDING;
    }
    else
    {
        metadata->state = BOOT_STATE_UPDATE_PENDING;
    }

    return 1;
}

uint8_t boot_metadata_read(boot_metadata_t *metadata)
{
    boot_metadata_t page_a;
    boot_metadata_t page_b;
    uint8_t valid_a;
    uint8_t valid_b;

    if (metadata == 0)
    {
        return 0;
    }

    valid_a = metadata_record_valid(BOOT_METADATA_PAGE_A, &page_a);
    valid_b = metadata_record_valid(BOOT_METADATA_PAGE_B, &page_b);

    if (!valid_a && !valid_b)
    {
        return legacy_to_metadata(metadata);
    }

    if (valid_a && valid_b)
    {
        if (sequence_is_newer(page_b.sequence, page_a.sequence))
        {
            memcpy(metadata, &page_b, sizeof(boot_metadata_t));
        }
        else
        {
            memcpy(metadata, &page_a, sizeof(boot_metadata_t));
        }
    }
    else if (valid_a)
    {
        memcpy(metadata, &page_a, sizeof(boot_metadata_t));
    }
    else
    {
        memcpy(metadata, &page_b, sizeof(boot_metadata_t));
    }

    return 1;
}

uint8_t boot_metadata_write(boot_metadata_t *metadata)
{
    boot_metadata_t page_a;
    boot_metadata_t page_b;
    boot_metadata_t verify;
    uint8_t valid_a;
    uint8_t valid_b;
    uint32_t current_address;
    uint32_t target_address;
    uint32_t sequence;
    uint32_t *words;
    uint32_t i;
    FLASH_Status status;

    if (metadata == 0 ||
        !metadata_state_valid(metadata->state) ||
        metadata->active_size > APP_IMAGE_MAX_SIZE ||
        metadata->backup_size > APP_IMAGE_MAX_SIZE ||
        metadata->trial_attempts > BOOT_MAX_TRIAL_ATTEMPTS)
    {
        return 0;
    }

    valid_a = metadata_record_valid(BOOT_METADATA_PAGE_A, &page_a);
    valid_b = metadata_record_valid(BOOT_METADATA_PAGE_B, &page_b);

    if (!valid_a && !valid_b)
    {
        current_address = 0;
        sequence = 1U;
        target_address = BOOT_METADATA_PAGE_A;
    }
    else if (valid_a && valid_b)
    {
        if (sequence_is_newer(page_b.sequence, page_a.sequence))
        {
            current_address = BOOT_METADATA_PAGE_B;
            sequence = page_b.sequence + 1U;
        }
        else
        {
            current_address = BOOT_METADATA_PAGE_A;
            sequence = page_a.sequence + 1U;
        }
        target_address = current_address == BOOT_METADATA_PAGE_A
                       ? BOOT_METADATA_PAGE_B
                       : BOOT_METADATA_PAGE_A;
    }
    else if (valid_a)
    {
        current_address = BOOT_METADATA_PAGE_A;
        sequence = page_a.sequence + 1U;
        target_address = BOOT_METADATA_PAGE_B;
    }
    else
    {
        current_address = BOOT_METADATA_PAGE_B;
        sequence = page_b.sequence + 1U;
        target_address = BOOT_METADATA_PAGE_A;
    }

    metadata->magic = BOOT_METADATA_MAGIC;
    metadata->sequence = sequence;
    metadata->record_crc32 = crc32_bytes((const uint8_t *)metadata, 32U);

    FLASH_Unlock();
    status = FLASH_ErasePage(target_address);
    if (status != FLASH_COMPLETE)
    {
        FLASH_Lock();
        return 0;
    }

    words = (uint32_t *)metadata;
    for (i = 0; i < sizeof(boot_metadata_t) / 4U; i++)
    {
        status = FLASH_ProgramWord(target_address + i * 4U, words[i]);
        if (status != FLASH_COMPLETE)
        {
            FLASH_Lock();
            return 0;
        }
    }
    FLASH_Lock();

    if (!metadata_record_valid(target_address, &verify))
    {
        return 0;
    }

    return memcmp(&verify, metadata, sizeof(boot_metadata_t)) == 0 ? 1 : 0;
}

uint8_t boot_copy_image(uint32_t source_addr, uint32_t destination_addr)
{
    uint32_t offset;
    uint32_t remaining;
    uint32_t chunk;

    if ((source_addr != APP_SECTOR_ADDR &&
         source_addr != APP_BACKUP_ADDR) ||
        (destination_addr != APP_SECTOR_ADDR &&
         destination_addr != APP_BACKUP_ADDR) ||
        source_addr == destination_addr)
    {
        return 0;
    }

    if (!mcu_flash_erase(destination_addr, APP_ERASE_SECTORS))
    {
        return 0;
    }

    offset = 0;
    remaining = APP_SLOT_SIZE;
    while (remaining != 0U)
    {
        chunk = remaining > FLASH_SECTOR_SIZE ? FLASH_SECTOR_SIZE : remaining;
        mcu_flash_read(source_addr + offset, copy_buffer, chunk);
        if (!mcu_flash_write(destination_addr + offset, copy_buffer, chunk))
        {
            return 0;
        }
        if (memcmp((void *)(destination_addr + offset),
                   copy_buffer,
                   chunk) != 0)
        {
            return 0;
        }

        offset += chunk;
        remaining -= chunk;
    }

    return 1;
}

uint8_t boot_restore_backup(boot_metadata_t *metadata)
{
    if (metadata == 0 ||
        metadata->backup_size == 0U ||
        !boot_backup_is_valid(metadata->backup_size,
                               metadata->backup_crc32))
    {
        return 0;
    }

    if (!boot_copy_image(APP_BACKUP_ADDR, APP_SECTOR_ADDR))
    {
        return 0;
    }

    metadata->active_size = metadata->backup_size;
    metadata->active_crc32 = metadata->backup_crc32;
    metadata->state = BOOT_STATE_NORMAL;
    metadata->trial_attempts = 0U;
    return boot_metadata_write(metadata);
}
