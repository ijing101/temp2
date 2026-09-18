#include "bootloader.h"
#include "crc_check.h"
#include "stm32f10x_tim.h"
#include "stm32f10x_usart.h"
#include "usart.h"
#include "delay.h"

static void cleanup_bootloader_environment(void)
{
    uint8_t i;

    __disable_irq();

    TIM_Cmd(TIM3, DISABLE);
    TIM_ITConfig(TIM3, TIM_IT_Update, DISABLE);

    USART_ITConfig(USART2, USART_IT_RXNE, DISABLE);
    USART_Cmd(USART2, DISABLE);
    USART_ITConfig(USART1, USART_IT_RXNE, DISABLE);

    SysTick->CTRL = 0;
    SysTick->LOAD = 0;
    SysTick->VAL = 0;

    for (i = 0; i < 8; i++)
    {
        NVIC->ICER[i] = 0xFFFFFFFFU;
        NVIC->ICPR[i] = 0xFFFFFFFFU;
    }

    SCB->VTOR = 0;
}

uint8_t jump_app(uint32_t app_addr)
{
    boot_metadata_t metadata;
    uint32_t stack_pointer;
    uint32_t reset_vector;
    jump_callback callback;

    if (app_addr != APP_SECTOR_ADDR)
    {
        return 0;
    }

    if (!boot_metadata_read(&metadata))
    {
        return 0;
    }

    if (metadata.state != BOOT_STATE_NORMAL &&
        metadata.state != BOOT_STATE_TRIAL)
    {
        return 0;
    }

    if (!boot_image_is_valid(app_addr,
                             metadata.active_size,
                             metadata.active_crc32))
    {
        return 0;
    }

    stack_pointer = *(__IO uint32_t *)app_addr;
    reset_vector = *(__IO uint32_t *)(app_addr + 4U);
    if ((reset_vector & 1U) == 0U)
    {
        return 0;
    }

    cleanup_bootloader_environment();

    SCB->VTOR = app_addr;
    callback = (jump_callback)reset_vector;
    __set_MSP(stack_pointer);
    callback();

    return 1;
}

void system_reboot(void)
{
    __set_FAULTMASK(1);
    NVIC_SystemReset();
}

static uint8_t flash_region_valid(uint32_t addr, uint32_t length)
{
    uint32_t region_start;
    uint32_t region_end;

    if (length == 0U)
    {
        return 0;
    }

    if (addr >= APP_SECTOR_ADDR &&
        addr < APP_SECTOR_ADDR + APP_SLOT_SIZE)
    {
        region_start = APP_SECTOR_ADDR;
        region_end = APP_SECTOR_ADDR + APP_SLOT_SIZE;
    }
    else if (addr >= APP_BACKUP_ADDR &&
             addr < APP_BACKUP_ADDR + APP_SLOT_SIZE)
    {
        region_start = APP_BACKUP_ADDR;
        region_end = APP_BACKUP_ADDR + APP_SLOT_SIZE;
    }
    else
    {
        return 0;
    }

    if (addr < region_start || addr + length > region_end)
    {
        return 0;
    }
    return 1;
}

uint8_t mcu_flash_erase(uint32_t addr, uint8_t sector_num)
{
    uint8_t i;
    uint32_t end;

    if ((addr != APP_SECTOR_ADDR && addr != APP_BACKUP_ADDR) ||
        sector_num == 0U ||
        sector_num > APP_ERASE_SECTORS)
    {
        return 0;
    }

    end = addr + ((uint32_t)sector_num * FLASH_SECTOR_SIZE);
    if (end > addr + APP_SLOT_SIZE)
    {
        return 0;
    }

    FLASH_Unlock();
    for (i = 0; i < sector_num; i++)
    {
        if (FLASH_ErasePage(addr + (uint32_t)i * FLASH_SECTOR_SIZE)
            != FLASH_COMPLETE)
        {
            FLASH_Lock();
            return 0;
        }
    }
    FLASH_Lock();
    return 1;
}

uint8_t mcu_flash_write(uint32_t addr, uint8_t *buffer, uint32_t length)
{
    uint32_t i;
    uint16_t data;
    FLASH_Status result;

    if (buffer == 0 ||
        length == 0U ||
        (length & 1U) != 0U ||
        !flash_region_valid(addr, length))
    {
        return 0;
    }

    FLASH_Unlock();
    for (i = 0; i < length; i += 2U)
    {
        data = (uint16_t)buffer[i] |
               (uint16_t)((uint16_t)buffer[i + 1U] << 8);
        result = FLASH_ProgramHalfWord(addr + i, data);
        if (result != FLASH_COMPLETE ||
            *(__IO uint16_t *)(addr + i) != data)
        {
            FLASH_Lock();
            return 0;
        }
    }
    FLASH_Lock();

    return 1;
}

void mcu_flash_read(uint32_t addr, uint8_t *buffer, uint32_t length)
{
    if (buffer != 0 && length != 0U)
    {
        memcpy(buffer, (void *)addr, length);
    }
}
