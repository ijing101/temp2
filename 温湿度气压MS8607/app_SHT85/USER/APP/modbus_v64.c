#include "modbus.h"
#include "iap_trigger_v64.h"
#include "wdg.h"
#include "stdio.h"

#define APP_VERSION 60064U

char buffer[50];
MODBUS modbus;
char buffer1[50];
char buffer2[50];
char buffer3[50];

u16 Reg[128];

static void Modbus_Send_Exception(u8 func, u8 code)
{
    u16 crc;
    u8 i;

    modbus.sendbuf[0] = modbus.myadd;
    modbus.sendbuf[1] = func | 0x80;
    modbus.sendbuf[2] = code;
    crc = Modbus_CRC16(modbus.sendbuf, 3);
    modbus.sendbuf[3] = crc / 256;
    modbus.sendbuf[4] = crc % 256;

    RS485_TX_ENABLE;
    for (i = 0; i < 5; i++)
    {
        Modbus_Send_Byte(modbus.sendbuf[i]);
    }
    RS485_RX_ENABLE;
}

static void Update1(void)
{
    uint8_t slave_addr;
    uint32_t baudrate;
    int16_t temp_offset;
    int16_t humi_offset;

    if (Flash_Storage_Load_Config(&slave_addr, &baudrate,
                                  &temp_offset, &humi_offset) == 0)
    {
        Reg[20] = (uint16_t)temp_offset;
        Reg[21] = (uint16_t)humi_offset;
        Reg[22] = 0;
    }
    else
    {
        Reg[20] = 0;
        Reg[21] = 0;
        Reg[22] = 0;
    }
}

static void Update2(void)
{
    uint16_t temp_value;
    uint16_t humi_value;

    /* SHT85 is the only sensor-specific part retained from the old app. */
    temp_value = (uint16_t)(SHT85.temp * 10);
    humi_value = (uint16_t)(SHT85.humi * 10);

    Reg[0] = temp_value + Reg[20];
    Reg[1] = humi_value + Reg[21];
    Reg[2] = 0;

    Reg[5] = SHT85.temp_raw;
    Reg[6] = SHT85.humi_raw;
    Reg[7] = 0;
    Reg[8] = 0;
    Reg[9] = 0;
    Reg[10] = 0;
    Reg[11] = 0;
    Reg[12] = 0;
    Reg[13] = 0;
    Reg[14] = 0;
    Reg[15] = APP_VERSION;

    Reg[23] = modbus.myadd;
    if (modbus.bound == 19200)
    {
        Reg[24] = 1;
    }
    else if (modbus.bound == 115200)
    {
        Reg[24] = 2;
    }
    else
    {
        Reg[24] = 0;
    }
}

void Modbus_Init(void)
{
    uint8_t slave_addr;
    uint32_t baudrate;
    int16_t temp_offset;
    int16_t humi_offset;
    uint32_t baud_index;
    uint32_t actual_baudrate;

    if (Flash_Storage_Load_Config(&slave_addr, &baudrate,
                                  &temp_offset, &humi_offset) == 0)
    {
        modbus.myadd = slave_addr;
        modbus.myadd_cached = slave_addr;
        modbus.bound = baudrate;
        Reg[20] = (uint16_t)temp_offset;
        Reg[21] = (uint16_t)humi_offset;
        Reg[22] = 0;
    }
    else
    {
        modbus.myadd = 0x01;
        modbus.myadd_cached = 0x01;
        modbus.bound = 9600;
        Reg[20] = 0;
        Reg[21] = 0;
        Reg[22] = 0;
    }

    if (modbus.bound == 19200)
    {
        baud_index = 1;
        actual_baudrate = 19200;
    }
    else if (modbus.bound == 115200)
    {
        baud_index = 2;
        actual_baudrate = 115200;
    }
    else
    {
        baud_index = 0;
        actual_baudrate = 9600;
    }

    modbus.bound = actual_baudrate;
    Reg[15] = APP_VERSION;
    Reg[23] = modbus.myadd;
    Reg[24] = (uint16_t)baud_index;

    snprintf(buffer1, sizeof(buffer1), "Modbus slave address: %d\r\n",
             modbus.myadd);
    Usart2_SendString(buffer1);
    delay_ms(500);
    snprintf(buffer2, sizeof(buffer2),
             "Baud index: %d (0=9600, 1=19200, 2=115200)\r\n",
             baud_index);
    Usart2_SendString(buffer2);
    delay_ms(500);

    Modbus_uart2_init(actual_baudrate);
    delay_ms(100);
    modbus.timrun = 0;
}

void Modbus_Func3(void)
{
    u16 Regadd;
    u16 Reglen;
    u16 crc;
    u8 i;
    u8 j;

    if (modbus.recount != 8)
    {
        Modbus_Send_Exception(0x03, 0x03);
        return;
    }

    Regadd = modbus.rcbuf[2] * 256 + modbus.rcbuf[3];
    Reglen = modbus.rcbuf[4] * 256 + modbus.rcbuf[5];
    if (Reglen == 0 || Regadd >= 128 || Reglen > (128 - Regadd))
    {
        Modbus_Send_Exception(0x03, 0x02);
        return;
    }
    if (Reglen > ((sizeof(modbus.sendbuf) - 5) / 2))
    {
        Modbus_Send_Exception(0x03, 0x03);
        return;
    }

    Update1();
    Update2();

    i = 0;
    modbus.sendbuf[i++] = modbus.myadd;
    modbus.sendbuf[i++] = 0x03;
    modbus.sendbuf[i++] = (Reglen * 2) % 256;
    for (j = 0; j < Reglen; j++)
    {
        modbus.sendbuf[i++] = Reg[Regadd + j] / 256;
        modbus.sendbuf[i++] = Reg[Regadd + j] % 256;
    }
    crc = Modbus_CRC16(modbus.sendbuf, i);
    modbus.sendbuf[i++] = crc / 256;
    modbus.sendbuf[i++] = crc % 256;

    RS485_TX_ENABLE;
    for (j = 0; j < i; j++)
    {
        Modbus_Send_Byte(modbus.sendbuf[j]);
    }
    RS485_RX_ENABLE;
}

void Modbus_Func6(void)
{
    u16 Regadd;
    u16 val;
    u16 i;
    u16 crc;
    u16 j;

    Regadd = modbus.rcbuf[2] * 256 + modbus.rcbuf[3];
    val = modbus.rcbuf[4] * 256 + modbus.rcbuf[5];

    if (Regadd == 15 || Regadd == 22 || Regadd >= 128)
    {
        Modbus_Send_Exception(0x06, 0x02);
        return;
    }
    Reg[Regadd] = val;

    i = 0;
    modbus.sendbuf[i++] = modbus.myadd;
    modbus.sendbuf[i++] = 0x06;
    modbus.sendbuf[i++] = Regadd / 256;
    modbus.sendbuf[i++] = Regadd % 256;
    modbus.sendbuf[i++] = val / 256;
    modbus.sendbuf[i++] = val % 256;
    crc = Modbus_CRC16(modbus.sendbuf, i);
    modbus.sendbuf[i++] = crc / 256;
    modbus.sendbuf[i++] = crc % 256;

    RS485_TX_ENABLE;
    for (j = 0; j < i; j++)
    {
        Modbus_Send_Byte(modbus.sendbuf[j]);
    }
    delay_ms(100);
    RS485_RX_ENABLE;

    if (Regadd == 20 || Regadd == 21)
    {
        int16_t signed_val = (int16_t)val;
        if (signed_val >= -999 && signed_val <= 999)
        {
            if (Regadd == 20)
            {
                Reg[20] = (uint16_t)signed_val;
            }
            else if (Regadd == 21)
            {
                Reg[21] = (uint16_t)signed_val;
            }
            Modbus_Save_Config(modbus.myadd, modbus.bound,
                               (int16_t)Reg[20], (int16_t)Reg[21]);
        }
    }
    else if (Regadd == 23 && val >= 1 && val <= 247)
    {
        modbus.myadd = (uint8_t)val;
        modbus.myadd_cached = (uint8_t)val;
        Modbus_Save_Config((uint8_t)val, modbus.bound,
                           (int16_t)Reg[20], (int16_t)Reg[21]);
        delay_ms(100);
    }
    else if (Regadd == 24)
    {
        uint32_t actual_baudrate;
        if (val == 1)
        {
            actual_baudrate = 19200;
        }
        else if (val == 2)
        {
            actual_baudrate = 115200;
        }
        else
        {
            actual_baudrate = 9600;
        }
        modbus.bound = actual_baudrate;
        Modbus_Save_Config(modbus.myadd, actual_baudrate,
                           (int16_t)Reg[20], (int16_t)Reg[21]);
        Modbus_uart2_reconfig(actual_baudrate);
        delay_ms(100);
    }
    else if (Regadd == 0x11 && val == 0x1234)
    {
        IWDG_ReloadCounter();
        trigger_iap_update();
    }
}

void Modbus_Event(void)
{
    u16 crc;
    u16 rccrc;
    u8 func_code;

    if (modbus.reflag == 0)
    {
        return;
    }
    if (modbus.recount < 8)
    {
        modbus.reflag = 0;
        modbus.recount = 0;
        return;
    }

    crc = Modbus_CRC16(&modbus.rcbuf[0], modbus.recount - 2);
    rccrc = modbus.rcbuf[modbus.recount - 2] * 256 +
            modbus.rcbuf[modbus.recount - 1];
    if (crc == rccrc)
    {
        func_code = modbus.rcbuf[1];
        if (modbus.rcbuf[0] == modbus.myadd)
        {
            if (func_code == 3)
            {
                Modbus_Func3();
            }
            else if (func_code == 6)
            {
                Modbus_Func6();
            }
        }
        else if (modbus.rcbuf[0] == 0 && func_code == 6)
        {
            /* v6.4 accepts broadcast FC06 without returning a response. */
        }
    }

    modbus.recount = 0;
    modbus.reflag = 0;
}

void Modbus_Save_Config(uint8_t slave_addr, uint32_t baudrate,
                        int16_t temp_offset, int16_t humi_offset)
{
    if (Flash_Storage_Save_Config(slave_addr, baudrate, temp_offset,
                                  humi_offset) == 0)
    {
        modbus.myadd = slave_addr;
        modbus.myadd_cached = slave_addr;
        modbus.bound = baudrate;
        Reg[20] = (uint16_t)temp_offset;
        Reg[21] = (uint16_t)humi_offset;
        Reg[22] = 0;
        printf("Config saved: Slave=0x%02X, Baud=%d, Offset=%d/%d\r\n",
               slave_addr, baudrate, temp_offset, humi_offset);
    }
    else
    {
        printf("Failed to save config\r\n");
    }
}

void Modbus_Load_Config(void)
{
    uint8_t slave_addr;
    uint32_t baudrate;
    int16_t temp_offset;
    int16_t humi_offset;

    if (Flash_Storage_Load_Config(&slave_addr, &baudrate, &temp_offset,
                                  &humi_offset) == 0)
    {
        modbus.myadd = slave_addr;
        modbus.myadd_cached = slave_addr;
        modbus.bound = baudrate;
        Reg[20] = (uint16_t)temp_offset;
        Reg[21] = (uint16_t)humi_offset;
        Reg[22] = 0;
        printf("Config reloaded: Slave=0x%02X, Baud=%d, Offset=%d/%d\r\n",
               slave_addr, baudrate, temp_offset, humi_offset);
    }
    else
    {
        printf("Failed to load config\r\n");
    }
}
