#include "stm32f10x.h"
#include "usart.h"
#include "rs485.h"
#include "timer.h"
#include "modbus.h"
#include "eeprom.h"
#include "bsp_clkconfig.h"
#include "wdg.h"
#include "iap_trigger.h"
#include "ms8607.h"
#include "stdio.h"

int main(void)
 {
	SCB->VTOR = FLASH_BASE | 0x5000;
	__enable_irq(); 

	HSE_SetSysClock(RCC_PLLMul_4);  //8MHZ * 4 = 32MHZ
	delay_init();
	MS8607_IIC_Config();
	uart_init(115200);//usart1
	
	Flash_Storage_Init();
	IWDG_Init(6,2344); 
	delay_ms(500);
	NVIC_PriorityGroupConfig(NVIC_PriorityGroup_2);

	Modbus_uart2_init(9600);//485鍒濆鍖栵紙涓存椂浣跨敤9600锛?
	TIM3_Int_Init(1000-1,32-1);//modbus 1ms
	TIM4_Int_Init(1000-1,32-1);//MMsec 1ms
	Usart2_SendString("app V6.2\r\n");
	delay_ms(100);
	Modbus_Init();//初始化modbus
	
	while(1)
	{
		IWDG_Feed();
		MS8607_ReadDate();
		
		IWDG_Feed();
		Modbus_Event();

		delay_ms(50);
	}
 }

 

