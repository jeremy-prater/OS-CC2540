/*
* spi.h
*
* Created: 3/29/2014 1:53:30 PM
*  Author: Administrator
*/


#ifndef SPI_H_
#define SPI_H_

#ifdef __cplusplus
extern "C"{
#endif

#define SR_2_WR 1U << 5
#define SR_2_PROC 1U << 6
#define SR_2_RBR 1U << 7

#define SR_3_WR 1U << 5
#define SR_3_PROC 1U << 6
#define SR_3_RBR 1U << 7

#define SR_4_RBR 1U << 7
#define SR_4_ROM 1U << 6

// Program only supports 256 byte buffers (0x100 length)

#define SPI_SLAVE_RX_BUFFER_SIZE 272
#define SPI_SLAVE_TX_BUFFER_SIZE 272
#define SPI_MASTER_RX_BUFFER_SIZE 272
#define SPI_MASTER_TX_BUFFER_SIZE 272

#define SPI_FLASH_0_BUFFER_SIZE 272
#define SPI_FLASH_1_BUFFER_SIZE 272
#define SPI_HM_10_BUFFER_SIZE 272

// create alias for the different SPI chip pins - code assumes all on port B
#if (defined(__AVR_ATmega328__) || defined(__AVR_ATmega328P__))
#define SPI_SS_PIN IOPORT_CREATE_PIN(PORTB, 2)
#define SPI_MOSI_PIN IOPORT_CREATE_PIN(PORTB, 3)
#define SPI_MISO_PIN IOPORT_CREATE_PIN(PORTB, 4)
#define SPI_SCK_PIN IOPORT_CREATE_PIN(PORTB, 5)

#define SPI_UART_SCK_PIN IOPORT_CREATE_PIN(PORTD, 4)
#define SPI_UART_MOSI_PIN IOPORT_CREATE_PIN(PORTD, 1)
#define SPI_UART_MISO_PIN IOPORT_CREATE_PIN(PORTD, 0)

#define SPI_UART_SS_FLASH_0 IOPORT_CREATE_PIN(PORTD, 2)
#define SPI_UART_SS_FLASH_1 IOPORT_CREATE_PIN(PORTD, 5)
#define SPI_UART_SS_HM_10	IOPORT_CREATE_PIN(PORTC, 3)

#else
	#error unknown processor - add to spi.h
#endif

// SPI clock modes
#define SPI_MODE_0 0x00 /* Sample (Rising) Setup (Falling) CPOL=0, CPHA=0 */
#define SPI_MODE_1 0x01 /* Setup (Rising) Sample (Falling) CPOL=0, CPHA=1 */
#define SPI_MODE_2 0x02 /* Sample (Falling) Setup (Rising) CPOL=1, CPHA=0 */
#define SPI_MODE_3 0x03 /* Setup (Falling) Sample (Rising) CPOL=1, CPHA=1 */

// data direction
#define SPI_LSB 1 /* send least significant bit (bit 0) first */
#define SPI_MSB 0 /* send most significant bit (bit 7) first */

// whether to raise interrupt when data received (SPIF bit received)
#define SPI_NO_INTERRUPT 0
#define SPI_INTERRUPT 1

// slave or master with clock divisor
#define SPI_SLAVE 0xF0
#define SPI_MSTR_CLK4 0x00 /* chip clock/4 */
#define SPI_MSTR_CLK16 0x01 /* chip clock/16 */
#define SPI_MSTR_CLK64 0x02 /* chip clock/64 */
#define SPI_MSTR_CLK128 0x03 /* chip clock/128 */
#define SPI_MSTR_CLK2 0x04 /* chip clock/2 */
#define SPI_MSTR_CLK8 0x05 /* chip clock/8 */
#define SPI_MSTR_CLK32 0x06 /* chip clock/32 */

#define SPI_UART_MSTR_CLK2   0x00 /* chip clock/2 */
#define SPI_UART_MSTR_CLK4   0x01 /* chip clock/4 */
#define SPI_UART_MSTR_CLK8   0x03 /* chip clock/8 */
#define SPI_UART_MSTR_CLK16  0x07 /* chip clock/16 */
#define SPI_UART_MSTR_CLK32  0x0F /* chip clock/32 */
#define SPI_UART_MSTR_CLK64  0x1F /* chip clock/64 */
#define SPI_UART_MSTR_CLK128 0x3F /* chip clock/128 */

// setup spi
void setup_spi(uint8_t mode,   // timing mode SPI_MODE[0-4]
int dord,             // data direction SPI_LSB|SPI_MSB
int interrupt,        // whether to raise interrupt on recieve
uint8_t clock); // clock diviser

// disable spi
void disable_spi(void);

// setup uart spi
void setup_uart_spi(uint8_t mode,   // timing mode SPI_MODE[0-4]
int dord,             // data direction SPI_LSB|SPI_MSB
int interrupt,        // whether to raise interrupt on recieve
uint8_t clock); // clock diviser

void LoadMasterSPI (unsigned char data);

void SendMasterSPI (void);

unsigned short GetMasterRxLength(void);
unsigned char GetMasterRxData(uint16_t address);

void LoadFlashBuffer (uint8_t data, uint8_t flashid);
void LoadSPISlaveTx (volatile uint8_t * data);

void GetNumHM10Message(void);

#ifdef __cplusplus
} // extern "C"
#endif


#endif /* SPI_H_ */