/*
* Copyright (c) 2009 Andrew Smallbone <andrew@rocketnumbernine.com>
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

#include <asf.h>
#include "gpio.h"
#include <spi.h>
#include "main.h"
#include <string.h>

volatile unsigned char FirstSPIByte;


volatile uint16_t SPISlaveRxMessagePtrLength;
volatile uint16_t SlaveMessageLength;
volatile uint16_t SlaveMessageCommand;
volatile uint8_t PacketProcessed;

volatile uint8_t MessageRead;

volatile uint16_t IncomingPacket_Length;

extern volatile uint8_t HM_10_MSG_AVAIL;

ISR(SPI_STC_vect)
{
	uint8_t temp = SPDR;
	SPISlaveRxMessagePtrLength++;
	
	if (SPISlaveRxMessagePtrLength == 1) // Receive Length
	{
		ioport_set_pin_level (LED_3 ,true); // Turn on RX LED
		SlaveMessageLength = temp;
		PacketProcessed = false;
	}
	
	if (SPISlaveRxMessagePtrLength == 2) // Receive Length
	SlaveMessageLength |= (uint16_t)(temp << 8);
	
	if (SPISlaveRxMessagePtrLength == 3) // Receive Length
	SlaveMessageCommand = temp;
	
	if (SPISlaveRxMessagePtrLength == 4) // Receive command
	{
		SlaveMessageCommand |= (uint16_t)(temp << 8);
		
		if (SlaveMessageCommand == 0x0002) // Some Commands need to send data while reading...
		{
			LoadSPISlaveTx(SR);
			PacketProcessed = true;
		}
		if (SlaveMessageCommand == 0x1FFF)
		{
			LoadSPISlaveTx(SPI_FLASH_0_BUFFER);
			PacketProcessed = true;
		}
		if (SlaveMessageCommand == 0x2FFF)
		{
			LoadSPISlaveTx(SPI_FLASH_1_BUFFER);
			PacketProcessed = true;
		}
		if (SlaveMessageCommand == 0x3EFF) // Get Next Message Content
		{
			LoadSPISlaveTx(SPI_HM_10_BUFFER);
			PacketProcessed = true;
		}
		if (SlaveMessageCommand == 0x3FFE) // HM-10 Flash Progress Address
		{
			MessageRead = true;
			LoadSPISlaveTx((uint8_t *)&HM_10_FLASH_PROGRESS);
			PacketProcessed = true;
		}
		if (SlaveMessageCommand == 0x3FFF) // HM-10 Debug Stats
		{
			LoadSPISlaveTx(HM_10_STAT);
			PacketProcessed = true;
		}
		if (PacketProcessed == false)
		{
			NewPacket = malloc (SlaveMessageLength);
			if (NewPacket == 0)
			{
				led_error(5);
			}
			else
			{
				NewPacket->Length = SlaveMessageLength;
				NewPacket->Command = SlaveMessageCommand;
			}
		}
	}
	if (SPISlaveRxMessagePtrLength > 4) // Add Data
	{
		if (NewPacket != 0)
		NewPacket->Data[SPISlaveRxMessagePtrLength - 5] = temp;
	}
	if (SPISlaveRxMessagePtrLength >= 4)
	{
		if (SPISlaveRxMessagePtrLength == SlaveMessageLength) // End of message, reset Counters
		{
			if (NewPacket != 0) // If there is a new command to process... Add it!
			{
				AddPacket((Packet *)NewPacket);
				NewPacket = 0;
			}
			
			SPISlaveRxMessagePtrLength = 0; // Reset Rx Length for next incoming message
			
			if (SlaveMessageCommand == 0x1FFF) // Data is sent, reset counters
			{
				SR[1] &= ~SR_2_RBR; // Clear SR[1].7 - Read Buffer Ready
				SPIMasterBufferFlash0Length = 0;
				SPIMasterBufferFlash0Ptr = 0;
			}
			if (SlaveMessageCommand == 0x2FFF) // Data is sent, reset counters
			{
				SR[2] &= ~SR_3_RBR; // Clear SR[2].7 - Read Buffer Ready
				SPIMasterBufferFlash1Length = 0;
				SPIMasterBufferFlash1Ptr = 0;
			}
			if (SlaveMessageCommand == 0x3EFF) // Data is sent, reset counters
			{
					SR[3] &= ~SR_4_RBR; // Clear SR[3].7 - Read Buffer Ready
					SPIMasterBufferHM10Length = 0;
					SPIMasterBufferHM10Ptr = 0;
					SR[5] = 0x00;
					SR[6] = 0x00;
			}			
			LoadSPISlaveTx(0); // Clear TX Slave Buffer
			ioport_set_pin_level (LED_3 ,false); // Turn off RX LED
		}
	}

	if (SPISlaveTxBuffer != 0)
	{
		SPDR = SPISlaveTxBuffer[SPISlaveTxBufferPtr];
		SPISlaveTxBufferPtr++; // Load Next Byte to Send
		if (SPISlaveTxBufferPtr == SPI_SLAVE_TX_BUFFER_SIZE)
		SPISlaveTxBufferPtr = 0;
	}
	else
	SPDR = 0xFF;
}
ISR(USART_RX_vect)
{
	SPIMasterRxBuffer[SPIMasterRxBufferPtr++] = UDR0;
	if (SPIMasterRxBufferPtr == SPI_MASTER_RX_BUFFER_SIZE)
	SPIMasterRxBufferPtr = 0;	
}
ISR(USART_TX_vect)
{
	if (SPIMasterTxBufferPtr == SPIMasterTxBufferLength)
	{
		SPIMasterTxBufferPtr = 0; // Reset Counters and do not load UDR0, end of transmission
		SPIMasterTxBufferLength = 0;		
	}
	else
	UDR0 = SPIMasterTxBuffer[SPIMasterTxBufferPtr++];
}

void GetNumHM10Message(void)
{
	Packet * NewMessage;
	ioport_set_pin_level(SPI_UART_SS_HM_10, false);
	LoadMasterSPI(0x00); // Not sure why... First SPI is blank data. (DMA LOAD?)
	LoadMasterSPI(0x00);
	LoadMasterSPI(0x00);
	ioport_set_pin_level(LED_2, true);
	SendMasterSPI();
	ioport_set_pin_level(LED_3, true);
	uint16_t length = GetMasterRxData(0x01);
	length |= (GetMasterRxData(0x02) << 8);
	//NewMessage->Length = length;
	for (uint16_t temp=0;temp < length - 2;temp++)
	{
		LoadMasterSPI (temp + 0x03);
	}
	ioport_set_pin_level(LED_2, false);
	SendMasterSPI();
	ioport_set_pin_level(LED_3, false);
	NewMessage = (Packet*)(SPIMasterRxBuffer-2);
	if (NewMessage->Command == 0x3EFD)
	{
		SR[4] = NewMessage->Data[0];
	}
	if (NewMessage->Command == 0x3EFE)
	{
		SR[5] = NewMessage->Data[0];
		SR[6] = NewMessage->Data[1];
		SR[3] &= ~SR_4_RBR; // SET RBR NOT READY (LOADING DATA)
	}
	if (NewMessage->Command == 0x3EFF)
	{
		for (uint16_t temp=0;temp < NewMessage->Length-2; temp++)
			LoadFlashBuffer( GetMasterRxData (temp+2), 0x02);
		SR[3] |= SR_4_RBR; // SET RBR (DATA READY)
	}	
	ioport_set_pin_level(SPI_UART_SS_HM_10, true);
	HM_10_MSG_AVAIL = false;
}

inline unsigned short GetMasterRxLength(void)
{
	return SPIMasterRxBufferPtr;
}
inline unsigned char GetMasterRxData(uint16_t address)
{
	while (address >= SPI_MASTER_RX_BUFFER_SIZE)
	address -= SPI_MASTER_RX_BUFFER_SIZE;
	return SPIMasterRxBuffer[address];
}
void setup_spi(uint8_t mode, int dord, int interrupt, uint8_t clock)
{
	unsigned char temp;

	SPISlaveRxMessagePtrLength = 0;
	SlaveMessageLength = 0;
	SlaveMessageCommand = 0;


	// specify pin directions for SPI pins on port B
	ioport_enable_pin(SPI_MOSI_PIN);
	ioport_enable_pin(SPI_MISO_PIN);
	ioport_enable_pin(SPI_SS_PIN);
	ioport_enable_pin(SPI_SCK_PIN);
	
	if (clock == SPI_SLAVE) { // if slave SS and SCK is input
		ioport_set_pin_dir(SPI_SS_PIN, IOPORT_DIR_INPUT);
		ioport_set_pin_dir(SPI_MOSI_PIN, IOPORT_DIR_INPUT);
		ioport_set_pin_dir(SPI_MISO_PIN, IOPORT_DIR_OUTPUT);
		ioport_set_pin_dir(SPI_SCK_PIN, IOPORT_DIR_INPUT);
		} else {
		ioport_set_pin_dir(SPI_SS_PIN, IOPORT_DIR_OUTPUT);
		ioport_set_pin_dir(SPI_MOSI_PIN, IOPORT_DIR_OUTPUT);
		ioport_set_pin_dir(SPI_MISO_PIN, IOPORT_DIR_INPUT);
		ioport_set_pin_dir(SPI_SCK_PIN, IOPORT_DIR_OUTPUT);
	}
	SPCR = ((interrupt ? 1 : 0) << SPIE) // interrupt enabled
	| (1 << SPE) // enable SPI
	| (dord << DORD) // LSB or MSB
	| (((clock != SPI_SLAVE) ? 1 : 0) << MSTR) // Slave or Master
	| (((mode & 0x02) == 2) << CPOL) // clock timing mode CPOL
	| (((mode & 0x01)) << CPHA) // clock timing mode CPHA
	| (((clock & 0x02) == 2) << SPR1) // cpu clock divisor SPR1
	| ((clock & 0x01) << SPR0); // cpu clock divisor SPR0
	SPSR = (((clock & 0x04) == 4) << SPI2X); // clock divisor SPI2X
	temp = SPSR;
	temp = SPDR;
	temp++;
}
void setup_uart_spi(uint8_t mode, int dord, int interrupt, uint8_t clock)
{
	ioport_enable_pin(SPI_UART_MOSI_PIN);
	ioport_enable_pin(SPI_UART_MISO_PIN);
	ioport_enable_pin(SPI_UART_SCK_PIN);
	
	ioport_enable_pin(SPI_UART_SS_FLASH_0);
	ioport_enable_pin(SPI_UART_SS_FLASH_1);
	ioport_enable_pin(SPI_UART_SS_HM_10);
	
	ioport_set_pin_dir(SPI_UART_MOSI_PIN, IOPORT_DIR_OUTPUT);
	ioport_set_pin_dir(SPI_UART_MISO_PIN, IOPORT_DIR_INPUT);
	ioport_set_pin_dir(SPI_UART_SCK_PIN, IOPORT_DIR_OUTPUT);
	
	ioport_set_pin_dir(SPI_UART_SS_FLASH_0, IOPORT_DIR_OUTPUT);
	ioport_set_pin_dir(SPI_UART_SS_FLASH_1, IOPORT_DIR_OUTPUT);
	ioport_set_pin_dir(SPI_UART_SS_HM_10, IOPORT_DIR_OUTPUT);
	
	ioport_set_pin_level(SPI_UART_SS_FLASH_0, true);
	ioport_set_pin_level(SPI_UART_SS_FLASH_1, true);
	ioport_set_pin_level(SPI_UART_SS_HM_10, true);
	
	UBRR0 = 0;
	UCSR0C = (1 << UMSEL01) | (1 << UMSEL00) | (dord << UDORD0) | ((mode & 0x01) <<UCPHA0) | (((mode & 0x02) == 0x02) << UCPOL0);
	UCSR0B = (1<< RXCIE0) | (1<< TXCIE0) | (1 << RXEN0) | (1<< TXEN0) | (0 << UDRIE0);
	UBRR0 = clock;
	
	PCICR = 0x04;
	//PCIFR = 0x00;
	PCMSK2 = 0x40;
}
void disable_spi()
{
	SPCR = 0;
}

void LoadMasterSPI (unsigned char data)
{
	if (SPIMasterTxBufferLength < SPI_MASTER_TX_BUFFER_SIZE)
	SPIMasterTxBuffer[SPIMasterTxBufferLength++] = data;
}
void SendMasterSPI (void)
{
	SPIMasterRxBufferPtr = 0;
	if (SPIMasterTxBufferLength > 0)
	UDR0 = SPIMasterTxBuffer[SPIMasterTxBufferPtr++];
	while (SPIMasterTxBufferPtr > 0);
}
void LoadFlashBuffer (unsigned char data, unsigned char flashid)
{
	switch (flashid)
	{
		case 0: // FLASH 0
		SPI_FLASH_0_BUFFER[SPIMasterBufferFlash0Length++] = data;
		if (SPIMasterBufferFlash0Length == SPI_FLASH_0_BUFFER_SIZE)
		SPIMasterBufferFlash0Length = 0;
		break;
		case 1: // FLASH 1
		SPI_FLASH_1_BUFFER[SPIMasterBufferFlash1Length++] = data;
		if (SPIMasterBufferFlash1Length == SPI_FLASH_1_BUFFER_SIZE)
		SPIMasterBufferFlash1Length = 0;
		break;
		
		case 2: // HM-10
		SPI_HM_10_BUFFER[SPIMasterBufferHM10Length++] = data;
		if (SPIMasterBufferHM10Length == SPI_HM_10_BUFFER_SIZE)
		SPIMasterBufferHM10Length = 0;
		break;
		
		default:
		break;
	}
}

inline void LoadSPISlaveTx (volatile uint8_t * data)
{
	SPISlaveTxBuffer = data;
	SPISlaveTxBufferPtr = 0;
	ioport_set_pin_level(LED_2, SPISlaveTxBuffer == 0 ? false : true); // Light/Clear TX LED
}