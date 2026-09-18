#include "stm32f10x.h"
#include "iap_trigger.h"
#include "delay.h"
#include "wdg.h"
#include "string.h"

#define BOOT_METADATA_MAGIC       0x4D455441U
#define BOOT_METADATA_PAGE_A      ((uint32_t)0x08004000)
#define BOOT_METADATA_PAGE_B      ((uint32_t)0x08004400)
#define APP_SECTOR_ADDR           ((uint32_t)0x08005000)
#define APP_SLOT_SIZE             0x5400U
#define APP_IMAGE_MAX_SIZE        (APP_SLOT_SIZE - 4U)

#define BOOT_STATE_NORMAL         0x13572401U
#define BOOT_STATE_UPDATE_PENDING 0x13572402U
#define BOOT_STATE_BACKUP         0x13572403U
#define BOOT_STATE_RECEIVING      0x13572404U
#define BOOT_STATE_TRIAL          0x13572405U

#define LEGACY_BOOT_FLAG_MAGIC    0x5AA5A55AU
#define LEGACY_BOOT_FLAG_UPDATE   0x55AA5502U

typedef struct
{
    uint32_t magic;
    uint32_t sequence;
    uint32_t state;
    uint32_t active_size;
    uint32_t active_crc32;
    uint32_t backup_size;
    uint32_t backup_crc32;
    uint32_t trial_attempts;
    uint32_t record_crc32;
} boot_metadata_t;

typedef struct
{
    uint32_t magic;
    uint32_t boot_flag;
    uint32_t boot_count;
    uint32_t crc32;
} legacy_boot_flag_t;

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

static uint8_t state_valid(uint32_t state)
{
    return (state == BOOT_STATE_NORMAL ||
            state == BOOT_STATE_UPDATE_PENDING ||
            state == BOOT_STATE_BACKUP ||
            state == BOOT_STATE_RECEIVING ||
            state == BOOT_STATE_TRIAL) ? 1 : 0;
}

static uint8_t read_metadata_at(uint32_t address,
                                boot_metadata_t *metadata)
{
    if (metadata == 0)
    {
        return 0;
    }

    memcpy(metadata, (void *)address, sizeof(boot_metadata_t));
    if (metadata->magic != BOOT_METADATA_MAGIC ||
        !state_valid(metadata->state) ||
        metadata->active_size > APP_IMAGE_MAX_SIZE ||
        metadata->backup_size > APP_IMAGE_MAX_SIZE ||
        metadata->trial_attempts > 3U)
    {
        return 0;
    }

    return crc32_bytes((const uint8_t *)metadata, 32U)
         == metadata->record_crc32 ? 1 : 0;
}

static uint8_t read_metadata(boot_metadata_t *metadata)
{
    boot_metadata_t a;
    boot_metadata_t b;
    uint8_t valid_a;
    uint8_t valid_b;

    valid_a = read_metadata_at(BOOT_METADATA_PAGE_A, &a);
    valid_b = read_metadata_at(BOOT_METADATA_PAGE_B, &b);

    if (!valid_a && !valid_b)
    {
        return 0;
    }

    if (valid_a && valid_b)
    {
        if ((int32_t)(b.sequence - a.sequence) > 0)
        {
            memcpy(metadata, &b, sizeof(boot_metadata_t));
        }
        else
        {
            memcpy(metadata, &a, sizeof(boot_metadata_t));
        }
    }
    else if (valid_a)
    {
        memcpy(metadata, &a, sizeof(boot_metadata_t));
    }
    else
    {
        memcpy(metadata, &b, sizeof(boot_metadata_t));
    }

    return 1;
}

static uint8_t write_metadata(boot_metadata_t *metadata)
{
    boot_metadata_t a;
    boot_metadata_t b;
    boot_metadata_t verify;
    uint8_t valid_a;
    uint8_t valid_b;
    uint32_t target;
    uint32_t sequence;
    uint32_t i;
    FLASH_Status status;

    if (metadata == 0 || !state_valid(metadata->state))
    {
        return 0;
    }

    valid_a = read_metadata_at(BOOT_METADATA_PAGE_A, &a);
    valid_b = read_metadata_at(BOOT_METADATA_PAGE_B, &b);

    if (!valid_a && !valid_b)
    {
        target = BOOT_METADATA_PAGE_A;
        sequence = 1U;
    }
    else if (valid_a && valid_b)
    {
        if ((int32_t)(b.sequence - a.sequence) > 0)
        {
            target = BOOT_METADATA_PAGE_A;
            sequence = b.sequence + 1U;
        }
        else
        {
            target = BOOT_METADATA_PAGE_B;
            sequence = a.sequence + 1U;
        }
    }
    else if (valid_a)
    {
        target = BOOT_METADATA_PAGE_B;
        sequence = a.sequence + 1U;
    }
    else
    {
        target = BOOT_METADATA_PAGE_A;
        sequence = b.sequence + 1U;
    }

    metadata->magic = BOOT_METADATA_MAGIC;
    metadata->sequence = sequence;
    metadata->record_crc32 = crc32_bytes((const uint8_t *)metadata, 32U);

    IWDG_ReloadCounter();
    __disable_irq();
    FLASH_Unlock();
    status = FLASH_ErasePage(target);
    if (status != FLASH_COMPLETE)
    {
        FLASH_Lock();
        __enable_irq();
        return 0;
    }

    for (i = 0; i < sizeof(boot_metadata_t) / 4U; i++)
    {
        status = FLASH_ProgramWord(target + i * 4U,
                                   ((uint32_t *)metadata)[i]);
        if (status != FLASH_COMPLETE)
        {
            FLASH_Lock();
            __enable_irq();
            return 0;
        }
        IWDG_ReloadCounter();
    }
    FLASH_Lock();
    __enable_irq();

    if (!read_metadata_at(target, &verify))
    {
        return 0;
    }
    return memcmp(&verify, metadata, sizeof(boot_metadata_t)) == 0 ? 1 : 0;
}

static uint32_t legacy_crc32(const legacy_boot_flag_t *legacy)
{
    return crc32_bytes((const uint8_t *)legacy, 12U);
}

static uint8_t write_legacy_update_flag(void)
{
    legacy_boot_flag_t legacy;
    uint32_t i;
    FLASH_Status status;

    legacy.magic = LEGACY_BOOT_FLAG_MAGIC;
    legacy.boot_flag = LEGACY_BOOT_FLAG_UPDATE;
    legacy.boot_count = 0U;
    legacy.crc32 = legacy_crc32(&legacy);

    __disable_irq();
    FLASH_Unlock();
    status = FLASH_ErasePage(BOOT_METADATA_PAGE_A);
    if (status == FLASH_COMPLETE)
    {
        for (i = 0; i < 4U; i++)
        {
            status = FLASH_ProgramWord(BOOT_METADATA_PAGE_A + i * 4U,
                                       ((uint32_t *)&legacy)[i]);
            if (status != FLASH_COMPLETE)
            {
                break;
            }
        }
    }
    FLASH_Lock();
    __enable_irq();

    return status == FLASH_COMPLETE ? 1 : 0;
}

static uint8_t write_update_flag(void)
{
    boot_metadata_t metadata;

    if (!read_metadata(&metadata))
    {
        /*
         * This fallback keeps a new APP compatible with an old BOOT
         * until the BOOT itself is replaced.
         */
        return write_legacy_update_flag();
    }

    metadata.state = BOOT_STATE_UPDATE_PENDING;
    metadata.trial_attempts = 0U;
    return write_metadata(&metadata);
}

void trigger_iap_update(void)
{
    IWDG_ReloadCounter();

    if (!write_update_flag())
    {
        return;
    }

    IWDG_ReloadCounter();
    delay_ms(10);
    __disable_irq();
    NVIC_SystemReset();

    while (1)
    {
    }
}

void iap_confirm_app_boot(void)
{
    boot_metadata_t metadata;

    if (!read_metadata(&metadata))
    {
        return;
    }

    if (metadata.state != BOOT_STATE_TRIAL)
    {
        return;
    }

    metadata.state = BOOT_STATE_NORMAL;
    metadata.trial_attempts = 0U;
    write_metadata(&metadata);
}
