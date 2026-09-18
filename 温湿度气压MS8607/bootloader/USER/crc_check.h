#ifndef __CRC_CHECK_H
#define __CRC_CHECK_H

#include "stm32f10x.h"

#define CRC16_CCITT_POLY    0x1021U
#define CRC16_CCITT_INIT    0x0000U

uint16_t crc16_ccitt(const uint8_t *data, uint16_t length);
uint16_t crc16_update(uint16_t crc, uint8_t data);
uint8_t verify_ymodem_packet(const uint8_t *packet, uint16_t data_len);

#endif
