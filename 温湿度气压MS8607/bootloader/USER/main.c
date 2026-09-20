#include "delay.h"
#include "ymodem.h"
#include "bsp_clkconfig.h"
#include "bootloader.h"
#include "string.h"

#define WAIT_TIMEOUT_SECONDS 1U
#define UPDATE_TIMEOUT_TICKS 200U
#define UPDATE_INACTIVITY_TICKS 10U

static uint8_t active_image_valid(boot_metadata_t *metadata)
{
    if (metadata == 0 || metadata->active_size == 0U)
    {
        return 0;
    }

    return boot_image_is_valid(APP_SECTOR_ADDR,
                               metadata->active_size,
                               metadata->active_crc32);
}

static uint8_t direct_image_valid(void)
{
    return boot_image_is_valid(APP_SECTOR_ADDR,
                                APP_IMAGE_MAX_SIZE,
                                0xFFFFFFFFU);
}

static uint8_t backup_image_valid(boot_metadata_t *metadata)
{
    if (metadata == 0 || metadata->backup_size == 0U)
    {
        return 0;
    }

    return boot_backup_is_valid(metadata->backup_size,
                                metadata->backup_crc32);
}

static uint8_t prepare_update(boot_metadata_t *metadata)
{
    if (metadata == 0)
    {
        return 0;
    }

    /*
     * BACKUP is committed before the active slot is erased. If power is
     * lost during the copy, the old active image is still available and
     * the next reset retries the copy.
     */
    if (active_image_valid(metadata))
    {
        metadata->state = BOOT_STATE_BACKUP;
        if (!boot_metadata_write(metadata))
        {
            return 0;
        }

        if (!boot_copy_image(APP_SECTOR_ADDR, APP_BACKUP_ADDR))
        {
            return 0;
        }

        metadata->backup_size = metadata->active_size;
        metadata->backup_crc32 = metadata->active_crc32;
        metadata->trial_attempts = 0U;
    }
    else if (!backup_image_valid(metadata))
    {
        metadata->active_size = 0U;
        metadata->active_crc32 = 0U;
        metadata->backup_size = 0U;
        metadata->backup_crc32 = 0U;
        metadata->trial_attempts = 0U;
    }

    metadata->state = BOOT_STATE_RECEIVING;
    return boot_metadata_write(metadata);
}

static uint8_t recover_receiving(boot_metadata_t *metadata)
{
    if (metadata == 0)
    {
        return 0;
    }

    /*
     * Keep BOOT available for a host retry. When the active slot was
     * interrupted and the backup is valid, restore it before any fallback
     * jump is attempted. The state remains RECEIVING until timeout.
     */
    if (!active_image_valid(metadata) && backup_image_valid(metadata))
    {
        if (!boot_copy_image(APP_BACKUP_ADDR, APP_SECTOR_ADDR))
        {
            return 0;
        }

        metadata->active_size = metadata->backup_size;
        metadata->active_crc32 = metadata->backup_crc32;
        return boot_metadata_write(metadata);
    }

    return active_image_valid(metadata) ? 1 : backup_image_valid(metadata);
}

static void enter_update_mode(boot_metadata_t *metadata)
{
    if (metadata != 0)
    {
        metadata->state = BOOT_STATE_RECEIVING;
        boot_metadata_write(metadata);
    }

    ymodem_reset_transfer();
    set_ymodem_status(UPDATE_PROGRAM);
}

static uint8_t fallback_to_confirmed_app(boot_metadata_t *metadata)
{
    if (metadata == 0)
    {
        return 0;
    }

    if (backup_image_valid(metadata) &&
        !active_image_valid(metadata))
    {
        return boot_restore_backup(metadata);
    }

    if (active_image_valid(metadata))
    {
        metadata->state = BOOT_STATE_NORMAL;
        metadata->trial_attempts = 0U;
        return boot_metadata_write(metadata);
    }

    return 0;
}

int main(void)
{
    boot_metadata_t metadata;
    uint8_t metadata_valid;
    uint8_t start_app;
    uint16_t wait_ticks;
    uint16_t update_timeout;
    uint8_t transfer_idle_ticks;
    uint8_t trial_write_ok;

    wait_ticks = WAIT_TIMEOUT_SECONDS;
    update_timeout = 0U;
    transfer_idle_ticks = 0U;
    start_app = 0U;

    HSE_SetSysClock(RCC_PLLMul_4);
    delay_init();
    ymodem_init();

    metadata_valid = boot_metadata_read(&metadata);
    if (!metadata_valid)
    {
        memset(&metadata, 0, sizeof(metadata));
        /*
         * A programmer may have placed a valid APP without metadata. Keep
         * that direct-download path using the 0xFFFFFFFF CRC sentinel.
         */
        if (boot_image_is_valid(APP_SECTOR_ADDR,
                                 APP_IMAGE_MAX_SIZE,
                                 0xFFFFFFFFU))
        {
            metadata.active_size = APP_IMAGE_MAX_SIZE;
            metadata.active_crc32 = 0xFFFFFFFFU;
            metadata.state = BOOT_STATE_NORMAL;
        }
        else
        {
            metadata.state = BOOT_STATE_UPDATE_PENDING;
        }
        boot_metadata_write(&metadata);
        metadata_valid = boot_metadata_read(&metadata);
    }
    else if (metadata.sequence == 0U)
    {
        /* Migrate one old-format record to redundant metadata. */
        boot_metadata_write(&metadata);
        metadata_valid = boot_metadata_read(&metadata);
    }

    if (!metadata_valid)
    {
        memset(&metadata, 0, sizeof(metadata));
        metadata.state = BOOT_STATE_RECEIVING;
        enter_update_mode(&metadata);
    }
    else
    {
        switch (metadata.state)
        {
            case BOOT_STATE_NORMAL:
                if (active_image_valid(&metadata))
                {
                    wait_ticks = WAIT_TIMEOUT_SECONDS;
                    set_ymodem_status(WAIT_START_PROGRAM);
                }
                else if (direct_image_valid())
                {
                    /* A programmer may have replaced APP directly. */
                    metadata.active_size = APP_IMAGE_MAX_SIZE;
                    metadata.active_crc32 = 0xFFFFFFFFU;
                    metadata.state = BOOT_STATE_NORMAL;
                    metadata.trial_attempts = 0U;
                    boot_metadata_write(&metadata);
                    wait_ticks = WAIT_TIMEOUT_SECONDS;
                    set_ymodem_status(WAIT_START_PROGRAM);
                }
                else if (fallback_to_confirmed_app(&metadata))
                {
                    wait_ticks = WAIT_TIMEOUT_SECONDS;
                    set_ymodem_status(WAIT_START_PROGRAM);
                }
                else
                {
                    prepare_update(&metadata);
                    enter_update_mode(&metadata);
                }
                break;

            case BOOT_STATE_UPDATE_PENDING:
            case BOOT_STATE_BACKUP:
                prepare_update(&metadata);
                enter_update_mode(&metadata);
                break;

            case BOOT_STATE_RECEIVING:
                recover_receiving(&metadata);
                enter_update_mode(&metadata);
                break;

            case BOOT_STATE_TRIAL:
                if (!active_image_valid(&metadata))
                {
                    if (!fallback_to_confirmed_app(&metadata))
                    {
                        metadata.state = BOOT_STATE_RECEIVING;
                        enter_update_mode(&metadata);
                    }
                    else
                    {
                        wait_ticks = WAIT_TIMEOUT_SECONDS;
                        set_ymodem_status(WAIT_START_PROGRAM);
                    }
                }
                else if (metadata.trial_attempts >= BOOT_MAX_TRIAL_ATTEMPTS)
                {
                    if (fallback_to_confirmed_app(&metadata))
                    {
                        wait_ticks = WAIT_TIMEOUT_SECONDS;
                        set_ymodem_status(WAIT_START_PROGRAM);
                    }
                    else
                    {
                        metadata.state = BOOT_STATE_RECEIVING;
                        enter_update_mode(&metadata);
                    }
                }
                else
                {
                    metadata.trial_attempts++;
                    trial_write_ok = boot_metadata_write(&metadata);
                    if (trial_write_ok)
                    {
                        wait_ticks = WAIT_TIMEOUT_SECONDS;
                        set_ymodem_status(WAIT_START_PROGRAM);
                    }
                    else
                    {
                        metadata.state = BOOT_STATE_RECEIVING;
                        enter_update_mode(&metadata);
                    }
                }
                break;

            default:
                metadata.state = BOOT_STATE_UPDATE_PENDING;
                prepare_update(&metadata);
                enter_update_mode(&metadata);
                break;
        }
    }

    if (wait_ticks == 0U)
    {
        wait_ticks = WAIT_TIMEOUT_SECONDS;
    }

    while (1)
    {
        switch (get_ymodem_status())
        {
            case WAIT_START_PROGRAM:
                delay_ms(1000);
                if (wait_ticks > 0U)
                {
                    wait_ticks--;
                }
                if (wait_ticks == 0U)
                {
                    set_ymodem_status(START_PROGRAM);
                }
                update_timeout = 0U;
                break;

            case START_PROGRAM:
                if (!jump_app(APP_SECTOR_ADDR))
                {
                    if (boot_metadata_read(&metadata) &&
                        fallback_to_confirmed_app(&metadata))
                    {
                        wait_ticks = WAIT_TIMEOUT_SECONDS;
                        set_ymodem_status(WAIT_START_PROGRAM);
                    }
                    else
                    {
                        if (boot_metadata_read(&metadata))
                        {
                            metadata.state = BOOT_STATE_RECEIVING;
                            boot_metadata_write(&metadata);
                        }
                        ymodem_reset_transfer();
                        set_ymodem_status(UPDATE_PROGRAM);
                        update_timeout = 0U;
                    }
                }
                else
                {
                    start_app = 1U;
                }
                break;

            case UPDATE_PROGRAM:
                /*
                 * Only advertise YMODEM while waiting for the file header.
                 * Once block 0 has been accepted, the host may send a 1K
                 * block which takes more than 500 ms at 9600 baud. Sending
                 * another 'C' during that block collides on the half-duplex
                 * RS485 bus and corrupts the packet.
                 */
                if (ymodem.status == 0U)
                {
                    ymodem_c();
                }

                /*
                 * A disconnected host can leave the receiver after block 0
                 * (ymodem.status == 1), where sending 'C' would otherwise be
                 * suppressed to protect a live 1K packet. After five seconds
                 * with no receive activity, abandon only the incomplete
                 * transfer. BOOT metadata and the confirmed backup remain
                 * untouched, then the next loop resumes advertising 'C'.
                 */
                if (ymodem.status != 0U)
                {
                    if (ymodem_take_rx_activity())
                    {
                        transfer_idle_ticks = 0U;
                        update_timeout = 0U;
                    }
                    else if (++transfer_idle_ticks >= UPDATE_INACTIVITY_TICKS)
                    {
                        ymodem_abort_transfer();
                        transfer_idle_ticks = 0U;
                        update_timeout = 0U;
                        ymodem_c();
                    }
                }
                else
                {
                    transfer_idle_ticks = 0U;
                    (void)ymodem_take_rx_activity();
                }
                delay_ms(500);
                update_timeout++;

                if (update_timeout >= UPDATE_TIMEOUT_TICKS)
                {
                    if (boot_metadata_read(&metadata) &&
                        fallback_to_confirmed_app(&metadata))
                    {
                        wait_ticks = WAIT_TIMEOUT_SECONDS;
                        set_ymodem_status(WAIT_START_PROGRAM);
                    }
                    else
                    {
                        /*
                         * No confirmed image exists. Continue advertising
                         * BOOT forever so a later host connection can repair
                         * the device instead of jumping to erased Flash.
                         */
                        ymodem_reset_transfer();
                        update_timeout = 0U;
                    }
                }
                break;

            case UPDATE_SUCCESS:
                delay_ms(1000);
                system_reboot();
                break;

            default:
                if (start_app != 0U)
                {
                    start_app = 0U;
                }
                break;
        }
    }
}
