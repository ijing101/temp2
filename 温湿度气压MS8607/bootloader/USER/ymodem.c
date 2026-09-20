#include "ymodem.h"
#include "rs485.h"

ymodem_t ymodem =
{
    WAIT_START_PROGRAM,
    0,
    0,
    APP_SECTOR_ADDR,
    0,
    0,
    1,
    0,
    0
};

download_buf_t recvBuf;
static uint16_t rx_expected_len = 0;
static volatile uint8_t rx_idle_ticks = 0;
static volatile uint8_t rx_activity_since_check = 0;
seq_queue_t rx_queue;

void queue_initiate(seq_queue_t *Q)
{
    Q->rear = 0;
    Q->front = 0;
    Q->count = 0;
}

int queue_not_empty(seq_queue_t *Q)
{
    return Q->count != 0 ? 1 : 0;
}

int queue_append(seq_queue_t *Q, uint8_t value)
{
    if (Q->count >= MAX_QUEUE_SIZE)
    {
        return 0;
    }

    Q->queue[Q->rear] = value;
    Q->rear = (Q->rear + 1) % MAX_QUEUE_SIZE;
    Q->count++;
    return 1;
}

int queue_delete(seq_queue_t *Q, uint8_t *value)
{
    if (Q->count == 0)
    {
        return 0;
    }

    *value = Q->queue[Q->front];
    Q->front = (Q->front + 1) % MAX_QUEUE_SIZE;
    Q->count--;
    return 1;
}

int queue_get(seq_queue_t Q, uint8_t *value)
{
    if (Q.count == 0)
    {
        return 0;
    }

    *value = Q.queue[Q.front];
    return 1;
}

void ymodem_ack(void)
{
    uint8_t value = YMODEM_ACK;
    RS485_Send_Data(&value, 1);
}

void ymodem_nack(void)
{
    uint8_t value = YMODEM_NAK;
    RS485_Send_Data(&value, 1);
}

void ymodem_c(void)
{
    uint8_t value = YMODEM_C;
    RS485_Send_Data(&value, 1);
}

void set_ymodem_status(process_status process)
{
    ymodem.process = process;
}

process_status get_ymodem_status(void)
{
    return ymodem.process;
}

void ymodem_start(ymodem_callback cb)
{
    if (ymodem.status == 0)
    {
        ymodem.cb = cb;
    }
}

void ymodem_reset_transfer(void)
{
    ymodem.status = 0;
    ymodem.id = 0;
    ymodem.addr = APP_SECTOR_ADDR;
    ymodem.image_size = 0;
    ymodem.received_size = 0;
    ymodem.expected_packet = 1;
    ymodem.sectors_size = 0;
}

void ymodem_abort_transfer(void)
{
    /*
     * This is called from the main loop after a multi-second receive idle
     * timeout. Keep BOOT_STATE_RECEIVING and the rollback image intact; only
     * discard the incomplete YMODEM frame and return to the block-0 state.
     */
    __disable_irq();
    queue_initiate(&rx_queue);
    recvBuf.len = 0;
    rx_expected_len = 0;
    rx_idle_ticks = 0;
    rx_activity_since_check = 0;
    ymodem_reset_transfer();
    __enable_irq();
}

uint8_t ymodem_take_rx_activity(void)
{
    uint8_t activity;

    __disable_irq();
    activity = rx_activity_since_check;
    rx_activity_since_check = 0;
    __enable_irq();
    return activity;
}

static uint8_t header_packet_valid(download_buf_t *packet)
{
    if (packet == 0 ||
        packet->len != 133U ||
        packet->data[0] != YMODEM_SOH ||
        packet->data[1] != 0U ||
        ((uint8_t)(packet->data[1] ^ packet->data[2])) != 0xFFU)
    {
        return 0;
    }

    return verify_ymodem_packet(packet->data, 128U);
}

static uint8_t parse_image_size(const uint8_t *data, uint32_t *image_size)
{
    uint16_t i;
    uint32_t size;
    uint8_t digit_found;

    if (data == 0 || image_size == 0)
    {
        return 0;
    }

    i = 0;
    while (i < 128U && data[i] != 0U)
    {
        i++;
    }
    if (i >= 128U)
    {
        return 0;
    }

    i++;
    while (i < 128U && (data[i] == ' ' || data[i] == '	'))
    {
        i++;
    }

    size = 0;
    digit_found = 0;
    while (i < 128U && data[i] >= '0' && data[i] <= '9')
    {
        digit_found = 1;
        if (size > 0xFFFFFFFFU / 10U)
        {
            return 0;
        }
        size = size * 10U + (uint32_t)(data[i] - '0');
        i++;
    }

    if (!digit_found ||
        size == 0U ||
        size > APP_IMAGE_MAX_SIZE)
    {
        return 0;
    }

    *image_size = size;
    return 1;
}

static uint8_t begin_transfer(download_buf_t *packet)
{
    boot_metadata_t metadata;
    uint32_t image_size;

    if (!header_packet_valid(packet) ||
        !parse_image_size(&packet->data[3], &image_size))
    {
        ymodem_nack();
        return 0;
    }

    if (!boot_metadata_read(&metadata) ||
        metadata.state != BOOT_STATE_RECEIVING)
    {
        ymodem_nack();
        return 0;
    }

    if (metadata.backup_size != 0U &&
        !boot_backup_is_valid(metadata.backup_size,
                              metadata.backup_crc32))
    {
        ymodem_nack();
        return 0;
    }

    if (!mcu_flash_erase(APP_SECTOR_ADDR, APP_ERASE_SECTORS))
    {
        ymodem_nack();
        return 0;
    }

    ymodem.addr = APP_SECTOR_ADDR;
    ymodem.image_size = image_size;
    ymodem.received_size = 0;
    ymodem.expected_packet = 1;
    ymodem.status = 1;
    ymodem.process = UPDATE_PROGRAM;

    ymodem_ack();
    ymodem_c();
    return 1;
}

static uint8_t is_empty_end_header(download_buf_t *packet)
{
    uint16_t i;

    for (i = 3U; i < 131U; i++)
    {
        if (packet->data[i] != 0U)
        {
            return 0;
        }
    }
    return 1;
}

void ymodem_recv(download_buf_t *packet)
{
    uint8_t type;
    uint8_t packet_num;
    uint8_t packet_inv;
    uint8_t previous_packet;
    uint16_t data_len;
    uint32_t logical_length;
    boot_metadata_t metadata;
    uint32_t active_crc;

    if (packet == 0 || packet->len == 0U)
    {
        return;
    }

    /* A host cancellation immediately returns BOOT to the block-0 state. */
    if (packet->len == 1U &&
        packet->data[0] == YMODEM_CA &&
        ymodem.status != 0U)
    {
        ymodem_reset_transfer();
        packet->len = 0;
        return;
    }

    /*
     * A new block 0 restarts a partial transfer. The old APP is already
     * protected in the backup slot, so erasing the active slot is safe.
     */
    if (ymodem.status != 3U &&
        header_packet_valid(packet))
    {
        begin_transfer(packet);
        packet->len = 0;
        return;
    }

    type = packet->data[0];
    packet_num = packet->len > 1U ? packet->data[1] : 0U;
    packet_inv = packet->len > 2U ? packet->data[2] : 0U;

    switch (ymodem.status)
    {
        case 0:
            break;

        case 1:
            if (type == YMODEM_SOH || type == YMODEM_STX)
            {
                if (type == YMODEM_SOH)
                {
                    data_len = 128U;
                }
                else
                {
                    data_len = 1024U;
                }

                if (packet->len != data_len + 5U ||
                    ((uint8_t)(packet_num ^ packet_inv)) != 0xFFU ||
                    !verify_ymodem_packet(packet->data, data_len))
                {
                    ymodem_nack();
                    break;
                }

                previous_packet = ymodem.expected_packet == 1U
                                 ? 255U
                                 : (uint8_t)(ymodem.expected_packet - 1U);

                if (packet_num == previous_packet)
                {
                    ymodem_ack();
                    break;
                }

                if (packet_num != ymodem.expected_packet ||
                    ymodem.received_size >= ymodem.image_size ||
                    ymodem.addr + data_len >
                    APP_SECTOR_ADDR + APP_SLOT_SIZE)
                {
                    ymodem_nack();
                    break;
                }

                logical_length = ymodem.image_size - ymodem.received_size;
                if (logical_length > data_len)
                {
                    logical_length = data_len;
                }

                if (!mcu_flash_write(ymodem.addr,
                                     &packet->data[3],
                                     data_len))
                {
                    ymodem_nack();
                    break;
                }

                ymodem.addr += data_len;
                ymodem.received_size += logical_length;
                ymodem.expected_packet = ymodem.expected_packet == 255U
                                        ? 1U
                                        : (uint8_t)(ymodem.expected_packet + 1U);
                ymodem_ack();
            }
            else if (type == YMODEM_EOT)
            {
                if (ymodem.received_size != ymodem.image_size)
                {
                    ymodem_nack();
                    break;
                }

                ymodem_nack();
                ymodem.status = 2;
            }
            break;

        case 2:
            if (type == YMODEM_EOT)
            {
                ymodem_ack();
                ymodem_c();
                ymodem.status = 3;
            }
            break;

        case 3:
            if (type == YMODEM_SOH &&
                packet->len == 133U &&
                packet_num == 0U &&
                ((uint8_t)(packet_num ^ packet_inv)) == 0xFFU &&
                verify_ymodem_packet(packet->data, 128U) &&
                is_empty_end_header(packet))
            {
                active_crc = boot_image_crc32(APP_SECTOR_ADDR,
                                              ymodem.image_size);
                if (!boot_image_is_valid(APP_SECTOR_ADDR,
                                         ymodem.image_size,
                                         active_crc) ||
                    !boot_metadata_read(&metadata) ||
                    metadata.state != BOOT_STATE_RECEIVING)
                {
                    ymodem_nack();
                    break;
                }

                metadata.active_size = ymodem.image_size;
                metadata.active_crc32 = active_crc;
                metadata.trial_attempts = 0;
                metadata.state = BOOT_STATE_TRIAL;

                if (!boot_metadata_write(&metadata))
                {
                    ymodem_nack();
                    break;
                }

                ymodem_ack();
                ymodem.status = 0;
                ymodem.process = UPDATE_SUCCESS;
            }
            else
            {
                ymodem_nack();
            }
            break;

        default:
            ymodem_reset_transfer();
            break;
    }

    packet->len = 0;
}

void ymodem_init(void)
{
    RS485_Init(9600);
    timer_init();
    queue_initiate(&rx_queue);
    recvBuf.len = 0;
    rx_expected_len = 0;
    rx_idle_ticks = 0;
    rx_activity_since_check = 0;
    ymodem_reset_transfer();
    ymodem.process = WAIT_START_PROGRAM;
}

static uint16_t get_expected_ymodem_frame_length(uint8_t first_byte)
{
    switch (ymodem.status)
    {
        case 0:
            return first_byte == YMODEM_SOH ? 133U : 0U;

        case 3:
            if (first_byte == YMODEM_CA)
            {
                return 1U;
            }
            return first_byte == YMODEM_SOH ? 133U : 0U;

        case 1:
            if (first_byte == YMODEM_SOH)
            {
                return 133U;
            }
            if (first_byte == YMODEM_STX)
            {
                return 1029U;
            }
            if (first_byte == YMODEM_EOT)
            {
                return 1U;
            }
            if (first_byte == YMODEM_CA)
            {
                return 1U;
            }
            return 0U;

        case 2:
            if (first_byte == YMODEM_EOT)
            {
                return 1U;
            }
            if (first_byte == YMODEM_SOH)
            {
                return 133U;
            }
            if (first_byte == YMODEM_CA)
            {
                return 1U;
            }
            return 0U;

        default:
            return 0U;
    }
}

void USART2_IRQHandler(void)
{
    uint8_t value;

    if (USART_GetITStatus(USART2, USART_IT_RXNE) != RESET)
    {
        value = USART_ReceiveData(USART2);
        queue_append(&rx_queue, value);
        rx_idle_ticks = 0;
        rx_activity_since_check = 1;
    }

    TIM3->CNT = 0;
    TIM_Cmd(TIM3, ENABLE);
}

void timer_init(void)
{
    TIM_TimeBaseInitTypeDef timer;
    NVIC_InitTypeDef nvic;

    RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM3, ENABLE);

    timer.TIM_Period = 999;
    timer.TIM_Prescaler = 71;
    timer.TIM_ClockDivision = TIM_CKD_DIV1;
    timer.TIM_CounterMode = TIM_CounterMode_Up;
    TIM_TimeBaseInit(TIM3, &timer);
    TIM_ITConfig(TIM3, TIM_IT_Update, ENABLE);

    nvic.NVIC_IRQChannel = TIM3_IRQn;
    nvic.NVIC_IRQChannelPreemptionPriority = 0;
    nvic.NVIC_IRQChannelSubPriority = 1;
    nvic.NVIC_IRQChannelCmd = ENABLE;
    NVIC_Init(&nvic);

    TIM_Cmd(TIM3, ENABLE);
}

void TIM3_IRQHandler(void)
{
    uint8_t value;

    if (TIM_GetITStatus(TIM3, TIM_IT_Update) == SET)
    {
        TIM_ClearITPendingBit(TIM3, TIM_IT_Update);
        TIM_Cmd(TIM3, DISABLE);

        while (queue_delete(&rx_queue, &value))
        {
            if (recvBuf.len == 0U)
            {
                rx_expected_len = get_expected_ymodem_frame_length(value);
                if (rx_expected_len == 0U)
                {
                    continue;
                }
            }

            if (recvBuf.len >= sizeof(recvBuf.data))
            {
                ymodem_nack();
                recvBuf.len = 0;
                rx_expected_len = 0;
                rx_idle_ticks = 0;
                continue;
            }

            recvBuf.data[recvBuf.len++] = value;
            if (recvBuf.len == rx_expected_len)
            {
                ymodem_recv(&recvBuf);
                rx_expected_len = 0;
                rx_idle_ticks = 0;
            }
        }

        if (recvBuf.len > 0U && rx_expected_len > recvBuf.len)
        {
            if (++rx_idle_ticks >= 10U)
            {
                ymodem_nack();
                recvBuf.len = 0;
                rx_expected_len = 0;
                rx_idle_ticks = 0;
            }
            else
            {
                TIM3->CNT = 0;
                TIM_Cmd(TIM3, ENABLE);
            }
        }
    }
}
