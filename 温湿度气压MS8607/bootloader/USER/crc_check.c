#include "crc_check.h"
#include "bootloader.h"

uint16_t crc16_ccitt(const uint8_t *data, uint16_t length)
{
    uint16_t crc;
    uint16_t i;
    uint8_t bit;

    if (data == 0)
    {
        return 0;
    }

    crc = CRC16_CCITT_INIT;
    for (i = 0; i < length; i++)
    {
        crc ^= (uint16_t)data[i] << 8;
        for (bit = 0; bit < 8; bit++)
        {
            if (crc & 0x8000U)
            {
                crc = (uint16_t)((crc << 1) ^ CRC16_CCITT_POLY);
            }
            else
            {
                crc = (uint16_t)(crc << 1);
            }
        }
    }
    return crc;
}

uint16_t crc16_update(uint16_t crc, uint8_t data)
{
    uint8_t bit;

    crc ^= (uint16_t)data << 8;
    for (bit = 0; bit < 8; bit++)
    {
        if (crc & 0x8000U)
        {
            crc = (uint16_t)((crc << 1) ^ CRC16_CCITT_POLY);
        }
        else
        {
            crc = (uint16_t)(crc << 1);
        }
    }
    return crc;
}

uint8_t verify_ymodem_packet(const uint8_t *packet, uint16_t data_len)
{
    uint16_t received_crc;
    uint16_t calculated_crc;

    if (packet == 0 || data_len == 0)
    {
        return 0;
    }

    received_crc = (uint16_t)(((uint16_t)packet[data_len + 3] << 8)
                              | packet[data_len + 4]);
    calculated_crc = crc16_ccitt(&packet[3], data_len);
    return received_crc == calculated_crc ? 1 : 0;
}

uint32_t boot_image_crc32(uint32_t image_addr, uint32_t image_size)
{
    uint32_t crc;
    uint32_t i;
    uint8_t bit;
    uint8_t value;

    crc = 0xFFFFFFFFU;
    for (i = 0; i < image_size; i++)
    {
        value = *(__IO uint8_t *)(image_addr + i);
        crc ^= value;
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

static uint8_t stack_pointer_valid(uint32_t stack_pointer)
{
    if (stack_pointer < SRAM_START_ADDR || stack_pointer > SRAM_END_ADDR)
    {
        return 0;
    }
    if ((stack_pointer & 3U) != 0)
    {
        return 0;
    }
    return 1;
}

static uint8_t reset_vector_valid(uint32_t reset_vector,
                                  uint32_t code_start,
                                  uint32_t code_end)
{
    uint32_t reset_address;

    if ((reset_vector & 1U) == 0)
    {
        return 0;
    }

    reset_address = reset_vector & ~1U;
    if (reset_address < code_start || reset_address >= code_end)
    {
        return 0;
    }
    return 1;
}

uint8_t boot_image_is_valid(uint32_t image_addr,
                            uint32_t image_size,
                            uint32_t image_crc32)
{
    uint32_t stack_pointer;
    uint32_t reset_vector;
    uint32_t image_end;

    if (image_addr != APP_SECTOR_ADDR ||
        image_size < 8U ||
        image_size > APP_IMAGE_MAX_SIZE)
    {
        return 0;
    }

    image_end = image_addr + image_size;
    stack_pointer = *(__IO uint32_t *)image_addr;
    reset_vector = *(__IO uint32_t *)(image_addr + 4U);

    if (!stack_pointer_valid(stack_pointer) ||
        !reset_vector_valid(reset_vector, image_addr, image_end))
    {
        return 0;
    }

    /*
     * 0xFFFFFFFF means the image was downloaded by a programmer and has
     * no transaction CRC. Vector validation is still mandatory.
     */
    if (image_crc32 != 0xFFFFFFFFU &&
        boot_image_crc32(image_addr, image_size) != image_crc32)
    {
        return 0;
    }

    return 1;
}

uint8_t boot_backup_is_valid(uint32_t image_size, uint32_t image_crc32)
{
    uint32_t stack_pointer;
    uint32_t reset_vector;

    if (image_size < 8U || image_size > APP_IMAGE_MAX_SIZE)
    {
        return 0;
    }

    stack_pointer = *(__IO uint32_t *)APP_BACKUP_ADDR;
    reset_vector = *(__IO uint32_t *)(APP_BACKUP_ADDR + 4U);

    if (!stack_pointer_valid(stack_pointer) ||
        !reset_vector_valid(reset_vector,
                            APP_SECTOR_ADDR,
                            APP_SECTOR_ADDR + image_size))
    {
        return 0;
    }

    if (image_crc32 != 0xFFFFFFFFU &&
        boot_image_crc32(APP_BACKUP_ADDR, image_size) != image_crc32)
    {
        return 0;
    }

    return 1;
}
