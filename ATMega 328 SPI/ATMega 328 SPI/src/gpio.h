/*
* gpio.h
*
* Created: 3/30/2014 8:48:21 AM
*  Author: Administrator
*/

#ifndef GPIO_H_
#define GPIO_H_

#ifdef __cplusplus
extern "C"{
#endif

//#if (defined(__AVR_ATmega48__) || defined(_AVR_ATmega88__) || defined(__AVR_ATmega168__) || defined(__AVR_ATmega328__) || defined(__AVR_ATmega328P__))
#define LED_1 IOPORT_CREATE_PIN(PORTC, 1) // LED 
#define LED_2 IOPORT_CREATE_PIN(PORTC, 0)
#define LED_3 IOPORT_CREATE_PIN(PORTB, 1)

#define HM_10_RESET IOPORT_CREATE_PIN(PORTD, 3)
#define HM_10_DD IOPORT_CREATE_PIN(PORTC, 4)
#define HM_10_DC IOPORT_CREATE_PIN(PORTC, 5)

#define HM_10_MSG IOPORT_CREATE_PIN(PORTD, 6)


#define SS_COMMAND_DELAY (2) // 1 uS = 1000 KHz (1 MHz)
#define HM_10_CLOCK_DATA  (1) // 1 uS = 1000 KHz (1 MHz)
#define HM_10_CLOCK_RESET (10 * 1000) // 10 mS

#define DUP_DBGDATA                 0x6260  // Debug interface data buffer
#define DUP_FCTL                    0x6270  // Flash controller
#define DUP_FADDRL                  0x6271  // Flash controller addr
#define DUP_FADDRH                  0x6272  // Flash controller addr
#define DUP_FWDATA                  0x6273  // Clash controller data buffer
#define DUP_CLKCONSTA               0x709E  // Sys clock status
#define DUP_CLKCONCMD               0x70C6  // Sys clock configuration
#define DUP_MEMCTR                  0x70C7  // Flash bank xdata mapping
#define DUP_DMA1CFGL                0x70D2  // Low byte, DMA config ch. 1
#define DUP_DMA1CFGH                0x70D3  // Hi byte , DMA config ch. 1
#define DUP_DMA0CFGL                0x70D4  // Low byte, DMA config ch. 0
#define DUP_DMA0CFGH                0x70D5  // Low byte, DMA config ch. 0
#define DUP_DMAARM                  0x70D6  // DMA arming register

#define CH_DBG_TO_BUF0               0x01   // Channel 0
#define CH_BUF0_TO_FLASH             0x02   // Channel 1

//! Low nibble of 16bit variable
#define LOBYTE(w)           ((unsigned char)(w & 0xFF))
//! High nibble of 16bit variable
#define HIBYTE(w)           ((unsigned char)(((unsigned short)(w) >> 8) & 0xFF))
//! Convert XREG register declaration to an XDATA integer address
#define XREG_TO_INT(a)      ((unsigned short)(&(a)))

#define ADDR_BUF0                    0x0000 // Buffer (512 bytes)
#define ADDR_DMA_DESC_0              0x0200 // DMA descriptors (8 bytes)
#define ADDR_DMA_DESC_1              (ADDR_DMA_DESC_0 + 8)

void init_gpio_pins(void);


void SET_LED_VALUE(int led, int value);

void timer_delay (unsigned short mS);
void led_error (uint8_t error);

void HM_10_RESET_UP(void);
void HM_10_RESET_DOWN(void);
void HM_10_DC_UP(void);
void HM_10_DC_DOWN(void);
void HM_10_SET_DD(unsigned char data);

void HM10SendDebugByte (unsigned char * data, unsigned short length);
unsigned char HM10SendDebugCommand (unsigned char * data, unsigned char length);
void HM10ReadDebugByte (volatile uint8_t * byte1, volatile uint8_t * byte2);

void write_xdata_memory_block(unsigned short address, unsigned char * data, unsigned short length);
void write_xdata_memory(unsigned short address, unsigned char data);
unsigned char read_xdata_memory(unsigned short address);
void burst_write_block (unsigned char * data, unsigned short len);
void write_flash_memory_block(unsigned char *src, uint32_t start_addr,unsigned short num_bytes);
void read_flash_memory_block(unsigned char bank,unsigned short flash_addr,unsigned short num_bytes, unsigned char *values);

#ifdef __cplusplus
} // extern "C"
#endif


#endif /* GPIO_H_ */