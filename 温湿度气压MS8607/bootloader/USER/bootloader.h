#ifndef __BOOTLOARDER_H
#define __BOOTLOARDER_H

#include "string.h"
#include "stm32f10x.h"
#include "main.h"

#define FLASH_SECTOR_SIZE           1024U
#define FLASH_SECTOR_NUM            64U
#define FLASH_START_ADDR            ((uint32_t)0x08000000)
#define FLASH_END_ADDR              ((uint32_t)0x08010000)

#define SRAM_START_ADDR             ((uint32_t)0x20000000)
#define SRAM_END_ADDR               ((uint32_t)0x20005000)

/*
 * STM32F103 64KB layout:
 * BOOT 16KB | metadata 4KB | active APP 21KB | backup APP 21KB | config 1KB | reserve 1KB
 */
#define BOOT_SECTOR_ADDR            ((uint32_t)0x08000000)
#define BOOT_SECTOR_SIZE            0x4000U
#define BOOT_FLAG_SECTOR_ADDR       ((uint32_t)0x08004000)
#define BOOT_FLAG_SECTOR_SIZE       0x1000U
#define BOOT_METADATA_PAGE_A        ((uint32_t)0x08004000)
#define BOOT_METADATA_PAGE_B        ((uint32_t)0x08004400)
#define BOOT_METADATA_PAGE_SIZE     0x0400U

#define APP_SECTOR_ADDR             ((uint32_t)0x08005000)
#define APP_SLOT_SIZE               0x5400U
#define APP_SECTOR_SIZE             APP_SLOT_SIZE
#define APP_BACKUP_ADDR             ((uint32_t)0x0800A400)
#define APP_IMAGE_MAX_SIZE          (APP_SLOT_SIZE - 4U)
#define APP_ERASE_SECTORS           (APP_SLOT_SIZE / FLASH_SECTOR_SIZE)
#define APP_WRITE_MAX_ADDR          (APP_SECTOR_ADDR + APP_SLOT_SIZE)

#define CONFIG_SECTOR_ADDR          ((uint32_t)0x0800F800)
#define CONFIG_SECTOR_SIZE          0x0400U
#define RESERVE_SECTOR_ADDR         ((uint32_t)0x0800FC00)
#define RESERVE_SECTOR_SIZE         0x0400U
#define MaxQueueSize                1200

#ifdef BOOT_DEBUG
#define BOOT_LOG printf
#else
#define BOOT_LOG
#endif

typedef enum
{
    NONE,
    WAIT_START_PROGRAM,
    START_PROGRAM,
    UPDATE_PROGRAM,
    UPDATE_SUCCESS,
    BUSY,
} process_status;

typedef void (*jump_callback)(void);

#define BOOT_METADATA_MAGIC       0x4D455441U
#define BOOT_STATE_NORMAL         0x13572401U
#define BOOT_STATE_UPDATE_PENDING 0x13572402U
#define BOOT_STATE_BACKUP         0x13572403U
#define BOOT_STATE_RECEIVING      0x13572404U
#define BOOT_STATE_TRIAL          0x13572405U
#define BOOT_MAX_TRIAL_ATTEMPTS   3U

/* Legacy values are retained for source compatibility. */
#define BOOT_FLAG_NORMAL          BOOT_STATE_NORMAL
#define BOOT_FLAG_NEED_UPDATE     BOOT_STATE_UPDATE_PENDING
#define BOOT_FLAG_FIRST_BOOT      BOOT_STATE_TRIAL

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

uint8_t jump_app(uint32_t appAddr);
void system_reboot(void);
uint8_t mcu_flash_erase(uint32_t addr, uint8_t sector_num);
uint8_t mcu_flash_write(uint32_t addr, uint8_t *buffer, uint32_t length);
void mcu_flash_read(uint32_t addr, uint8_t *buffer, uint32_t length);

uint8_t boot_metadata_read(boot_metadata_t *metadata);
uint8_t boot_metadata_write(boot_metadata_t *metadata);
uint32_t boot_image_crc32(uint32_t image_addr, uint32_t image_size);
uint8_t boot_image_is_valid(uint32_t image_addr, uint32_t image_size, uint32_t image_crc32);
uint8_t boot_backup_is_valid(uint32_t image_size, uint32_t image_crc32);
uint8_t boot_copy_image(uint32_t source_addr, uint32_t destination_addr);
uint8_t boot_restore_backup(boot_metadata_t *metadata);

void set_boot_state(process_status process);
process_status get_boot_state(void);

#endif
