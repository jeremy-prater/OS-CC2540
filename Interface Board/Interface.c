#include <ez8.h>
#include <STDLIB.H>
#include <STRING.H>
#include <STDIO.H>

#define UART_RX_SIZE 512
#define UART_TX_SIZE 512
#define SPI_RX_SIZE 512
#define SPI_TX_SIZE 512
#define SPI_BUFFER_SIZE 512

unsigned char RxBuffer		[UART_RX_SIZE];
unsigned char TxBuffer 		[UART_TX_SIZE];
unsigned char SPITxBuffer	[SPI_RX_SIZE];
unsigned char SPIRxBuffer	[SPI_TX_SIZE];
unsigned char SPIBuffer 	[SPI_BUFFER_SIZE];

volatile unsigned short RxBufferPtr;
volatile unsigned short TxBufferPtr;
volatile unsigned short TxBufferPtrTemp;
volatile unsigned short SPIRxBufferPtr;
volatile unsigned short SPITxBufferPtr;
volatile unsigned char  TxInProgress;
volatile unsigned short RxBufferPtrCurrent;

volatile unsigned char timer_ms_real;
volatile unsigned char * timer_ms;

volatile unsigned short PacketLength;
volatile unsigned short PacketCommand;
volatile unsigned short PacketCommandTemp;

volatile unsigned short Temp1;
volatile unsigned short Temp2;

volatile unsigned char flashid;

#pragma interrupt
void uart_tx_interrupt (void)
{
	SET_VECTOR (UART0_TX, uart_tx_interrupt);
	if (TxBufferPtr < TxBufferPtrTemp)
	{
		U0TXD = TxBuffer[TxBufferPtr++];
	}
	else
	{
		TxInProgress = FALSE;
		TxBufferPtr = 0;
		TxBufferPtrTemp = 0;
	}
	IRQ0 &= 0xF7; // Clear Interrupt
}

#pragma interrupt
void uart_rx_interrupt (void)
{
	SET_VECTOR (UART0_RX, uart_rx_interrupt);
	if (U0STAT0 & 0x80)
	{
		RxBuffer[RxBufferPtr++] = U0RXD;
		if (RxBufferPtr == UART_RX_SIZE)
		{
			RxBufferPtr = 0; // Reset Buffer Pointer to beginning
			PAOUT ^= 0x02; // Blink LED
		}
	}
	IRQ0 &= 0xEF; // Clear Interrupt
}

#pragma interrupt
void ms_timer_interrupt (void)
{
	SET_VECTOR (TIMER0, ms_timer_interrupt);
	timer_ms_real = 0; // End delay
	T0CTL1 &=0x7F; // Disable Timer
}

#pragma interrupt
void spi_interrupt (void)
{
	SET_VECTOR (SPI, spi_interrupt);
	SPISTAT |= 0x80;
	SPIRxBuffer[SPIRxBufferPtr++] = SPIDATA;
	SPITxBufferPtr &= (SPI_RX_SIZE - 1);
}

void setup_board()
{
	DI();
	
	open_PortA();
	setmodeOutput_PortA ( PORTPIN_ZERO | PORTPIN_ONE | PORTPIN_TWO );
	setmodeAltFunc_PortA( PORTPIN_FOUR|PORTPIN_FIVE ) ;
	PAOUT = 0x0;
	
	U0BR = 0x0005;
	U0CTL0 |= UART_CTL0_TEN | UART_CTL0_REN;
	
		
	PCADDR = 0x02;
	PCCTL = 0x3C;
		
	SPIBR = 0x0030;
	//SPIBR = 262;
	SPIMODE = 0x03;
	SPICTL = 0x83;
	
	IRQ0ENH |= 0x1A;
	IRQ0ENL |= 0x1A;
	
	T0CTL1 = 0x38;
	
	EI();
}

void delayMS (unsigned short ms)
{
	timer_ms_real = 1;
	T0 = 0x0000;
	T0R = (144 * ms);
	T0CTL1 |= 0x80;
	while (*timer_ms == 1)
	{
		
	}
}

unsigned char GetPacketData(unsigned short address)
{
	unsigned short realaddr = (address + RxBufferPtrCurrent + 4);
	while (realaddr >= UART_RX_SIZE)
		realaddr -= UART_RX_SIZE;
	return RxBuffer[realaddr];
}

unsigned char GetHeaderData(unsigned short address)
{
	unsigned short realaddr = (address + RxBufferPtrCurrent);
	while (realaddr >= UART_RX_SIZE)
		realaddr -= UART_RX_SIZE;
	return RxBuffer[realaddr];
}

void SendPacket (unsigned short command, unsigned char * data, unsigned short dlen)
{
	unsigned short length = 4 + dlen;
	unsigned char * txtmp = &TxInProgress;

	while (*txtmp == TRUE);
	
	TxBuffer[0] = length & 0xFF;
	TxBuffer[1] = length >> 8;
	TxBuffer[2] = command & 0xFF;
	TxBuffer[3] = command >> 8;

	memcpy (TxBuffer+4, data, length);
	
	TxInProgress = TRUE;
	TxBufferPtrTemp = length;
	IRQ0 &= 0xF7; // Clear Interrupt
	U0TXD = TxBuffer[TxBufferPtr++];
}

unsigned short GetLength()
{
	unsigned short length = 0xFFFF;
	while (length > UART_RX_SIZE)
	{
		if (RxBufferPtr < RxBufferPtrCurrent)
			length = (RxBufferPtr + UART_RX_SIZE) - RxBufferPtrCurrent;
		else
			length = RxBufferPtr - RxBufferPtrCurrent;

	}
	return length;
}

void ReadPacket()
{
	while (GetLength() < 2); // Wait to receive packet length
	PacketLength = ((unsigned short)(GetHeaderData(1) << 8)) | GetHeaderData(0);
	while (GetLength() < PacketLength); // Wait to receive entire packet
	PacketCommand = ((unsigned short)(GetHeaderData(3) << 8)) | GetHeaderData(2);
}

void SendSPI (unsigned short len)
{
	unsigned short templen=0;
	SPIRxBufferPtr = 0; // RESET SPI Buffer Counters
	SPITxBufferPtr = 0;
	SPIMODE ^= 0x01; // ASSERT SS
	for (templen=0; templen < len ; templen++)
	{
		SPIDATA = SPITxBuffer[SPITxBufferPtr++];
		while ((SPISTAT & 0x02) == 0x02)
		{
			// Wait for Data to send
		}
		while ((SPISTAT & 0x80) == 0x80)
		{
			// INCOMING DATA IS SAVED TO SPIRxBuffer
		}
	}
	SPIMODE ^= 0x01; // CLEAR SS
}

void main()
{
	unsigned char tempstuff[32];
	unsigned char temp, temp2, cmdtemp;
	unsigned short burstlen, burstemp;
	memset (&RxBuffer, 0, UART_TX_SIZE);
	memset (&TxBuffer, 0, UART_RX_SIZE);
	memset (&SPIRxBuffer, 0, SPI_RX_SIZE);
	memset (&SPITxBuffer, 0, SPI_TX_SIZE);
	
	RxBufferPtrCurrent = 0;
	RxBufferPtr = 0;
	SPIRxBufferPtr = 0;
	
	TxBufferPtrTemp = 0;
	TxBufferPtr = 0;
	TxInProgress = 0;
	
	PacketLength = 0;
	PacketCommand = 0;
	
	setup_board();
	
	PacketCommand = 0;
	PacketLength = 0;
	
	while (1)
	{	
		ReadPacket();
		// Process Packet
		
		///////////////////////////////////////////////////////////
		//		ATMega Access
		///////////////////////////////////////////////////////////
		
		if (PacketCommand == 0x0001) // Idle Response
		{
			SendPacket (0x0000, 0, 0);
		}
		
		if (PacketCommand == 0x0002) // Read ATMega SR
		{
			SPITxBuffer[0] = 0x0B;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0x02;
			SPITxBuffer[3] = 0x00;
			SPITxBuffer[4] = 0x00; // SR1
			SPITxBuffer[5] = 0x00; // SR2
			SPITxBuffer[6] = 0x00; // SR3
			SPITxBuffer[7] = 0x00; // SR4
			SPITxBuffer[8] = 0x00; // SR5
			SPITxBuffer[9] = 0x00; // SR6
			SPITxBuffer[10] = 0x00; // SR7
			SendSPI (0x0B);
 			SPIBuffer[0] = SPIRxBuffer[4];
			SPIBuffer[1] = SPIRxBuffer[5];
			SPIBuffer[2] = SPIRxBuffer[6];
			SPIBuffer[3] = SPIRxBuffer[7];
			SPIBuffer[4] = SPIRxBuffer[8];
			SPIBuffer[5] = SPIRxBuffer[9];
			SPIBuffer[6] = SPIRxBuffer[10];
			
			SendPacket (0x0002, SPIBuffer, 7);
			
		}

		///////////////////////////////////////////////////////////
		//		FLASH 0/1 Access
		///////////////////////////////////////////////////////////
		
		if ((PacketCommand >= 0x1000) && (PacketCommand < 0x3000))
		{
			if ((PacketCommand >= 0x1000) && (PacketCommand < 0x2000))
				flashid = 0;
			else
				flashid = 1;
			
			PacketCommandTemp = PacketCommand & 0x0FFF;
			
			if (PacketCommandTemp == 0x0001) // Write SR
			{
				SPITxBuffer[0] = 0x05;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0x01;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SPITxBuffer[4] = GetPacketData(0x00);
				SendSPI (5);
			}

			if ((PacketCommandTemp == 0x0002) || (PacketCommandTemp == 0x0008)) // Write Page
			{
				for (Temp1 = 0; Temp1 < PacketLength; Temp1++)
				{
					SPITxBuffer[Temp1] = GetHeaderData(Temp1);
				}
				SendSPI (PacketLength);
			}

			if (PacketCommandTemp == 0x0003) // Read Data
			{
				SPITxBuffer[0] = 0x09;
				SPITxBuffer[1] = 0x00;			
				SPITxBuffer[2] = 0x03;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SPITxBuffer[4] = GetPacketData(0x00); // A23 - A16
				SPITxBuffer[5] = GetPacketData(0x01); // A15 - A8
				SPITxBuffer[6] = GetPacketData(0x02); // A7  - A0
				SPITxBuffer[7] = GetPacketData(0x03);
				SPITxBuffer[8] = GetPacketData(0x04);
				SendSPI (9);
				
			}		
			
			if (PacketCommandTemp == 0x0004) // WRDI
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0x04;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SendSPI (4);
			}		


			if (PacketCommandTemp == 0x0005) // Read SR
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0x05;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SendSPI (4);
			}

			if (PacketCommandTemp == 0x0006) // WREN
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0x06;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SendSPI (4);
			}
			
			if (PacketCommandTemp == 0x00D8) // Sector Erase
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0xD8;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SPITxBuffer[4] = GetPacketData(0x00); // A23 - A16
				SPITxBuffer[5] = GetPacketData(0x01); // A15 - A8
				SPITxBuffer[6] = GetPacketData(0x02); // A7  - A0
				SendSPI (7);
			}
			

			if (PacketCommandTemp == 0x00C7) // Chip Erase
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0xC7;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SendSPI (4);
			}
			
			if (PacketCommandTemp == 0x009E) // Read ID 1
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0x9E;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SendSPI (4);
			}

			if (PacketCommandTemp == 0x009F) // Read ID 2
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0x9F;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SendSPI (4);
			}

			if (PacketCommandTemp == 0x00B9) // Power Down
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0xB9;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SendSPI (4);
			}
			
			if (PacketCommandTemp == 0x00AB) // Power UP
			{
				SPITxBuffer[0] = 0x04;
				SPITxBuffer[1] = 0x00;
				SPITxBuffer[2] = 0xAB;
				SPITxBuffer[3] = 0x10 + (flashid * 0x10);
				SendSPI (4);
			}

			if (PacketCommandTemp == 0x0FFF) // Read Buffer
			{
				unsigned short spilength = (GetPacketData(0x01) << 8) | GetPacketData(0x00) + 4;
				SPITxBuffer[0] = spilength & 0xFF;
				SPITxBuffer[1] = spilength >> 8;
				SPITxBuffer[2] = 0xFF;
				SPITxBuffer[3] = 0x1F + (flashid * 0x10);
				SendSPI (spilength);
				
				for (Temp1=0;Temp1<spilength - 4;Temp1++)
				{
					SPIBuffer[Temp1] = SPIRxBuffer[4 + Temp1];
				}
				
				SendPacket (PacketCommand, SPIBuffer, spilength - 4);
			}
		}
		
		///////////////////////////////////////////////////////////
		//		Basic ATMega Programming
		///////////////////////////////////////////////////////////
		
		if (PacketCommand == 0x0900) // Programming Enable
		{
			while (SPIRxBuffer[0x02] != 0x53)
			{
				
				PAOUT |= 0x01;
				delayMS (20);
				PAOUT &= 0xFE;
				delayMS (20);
				SPITxBuffer[0] = 0xAC;
				SPITxBuffer[1] = 0x53;
				SPITxBuffer[2] = 0x00;
				SPITxBuffer[3] = 0x00;
				SendSPI (4);
			}
			
		}
		if (PacketCommand == 0x0901) // Chip Erase
		{
			SPITxBuffer[0] = 0xAC;
			SPITxBuffer[1] = 0x80;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
		}
		if (PacketCommand == 0x0902) // Poll RDY/!BSY
		{
			SPITxBuffer[0] = 0xF0;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (0x0902, tempstuff, 1);
		}
		
		///////////////////////////////////////////////////////////
		//		Load Instructions ATMega Programming
		///////////////////////////////////////////////////////////

		if (PacketCommand == 0x0910) // Load Extended Adress Byte
		{
			SPITxBuffer[0] = 0x4D;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = GetPacketData(0x00);
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
		}
		if (PacketCommand == 0x0911) // Load Program Memory, High Byte
		{
			SPITxBuffer[0] = 0x48;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = GetPacketData(0x00);
			SPITxBuffer[3] = GetPacketData(0x01);
			SendSPI (4);
		}
		if (PacketCommand == 0x0912) // Load Program Memory, Low Byte
		{
			SPITxBuffer[0] = 0x40;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = GetPacketData(0x00);
			SPITxBuffer[3] = GetPacketData(0x01);
			SendSPI (4);
		}
		if (PacketCommand == 0x0913) // Load EEPROM Memory (page)
		{
			SPITxBuffer[0] = 0xC1;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = GetPacketData(0x00);
			SPITxBuffer[3] = GetPacketData(0x01);
			SendSPI (4);
		}

		///////////////////////////////////////////////////////////
		//		Read Instructions ATMega Programming
		///////////////////////////////////////////////////////////

		if (PacketCommand == 0x0920) // Read Program Memory, High Byte
		{
			SPITxBuffer[0] = 0x28;
			SPITxBuffer[1] = GetPacketData(0x00);
			SPITxBuffer[2] = GetPacketData(0x01);
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x01);
		}
		if (PacketCommand == 0x0921) // Read Program Memory, Low Byte
		{
			SPITxBuffer[0] = 0x20;
			SPITxBuffer[1] = GetPacketData(0x00);
			SPITxBuffer[2] = GetPacketData(0x01);
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x01);
		}
		if (PacketCommand == 0x0922) // Read EEPROM
		{
			SPITxBuffer[0] = 0xA0;
			SPITxBuffer[1] = GetPacketData(0x00);
			SPITxBuffer[2] = GetPacketData(0x01);
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x01);
		}
		if (PacketCommand == 0x0923) // Read Lock Bits
		{
			SPITxBuffer[0] = 0x58;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x01);
		}
		if (PacketCommand == 0x0924) // Read ATMega Signature Bytes
		{
			SPITxBuffer[0] = 0x30;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SPITxBuffer[0] = 0x30;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0x01;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[1] = SPIRxBuffer[0x03];
			SPITxBuffer[0] = 0x30;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0x02;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[2] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x03);
		}
		if (PacketCommand == 0x0925) // Read Fuse Bits
		{
			SPITxBuffer[0] = 0x50;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x01);
		}		
		if (PacketCommand == 0x0926) // Read Fuse High Bits
		{
			SPITxBuffer[0] = 0x58;
			SPITxBuffer[1] = 0x08;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x01);
		}		
		if (PacketCommand == 0x0927) // Read Extended Fuse Bits
		{
			SPITxBuffer[0] = 0x50;
			SPITxBuffer[1] = 0x08;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x01);
		}		
		if (PacketCommand == 0x0928) // Read Calibration Byte
		{
			SPITxBuffer[0] = 0x38;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
			tempstuff[0] = SPIRxBuffer[0x03];
			SendPacket (PacketCommand, tempstuff, 0x01);
		}
		///////////////////////////////////////////////////////////
		//		Write Instructions ATMega Programming
		///////////////////////////////////////////////////////////

		if (PacketCommand == 0x0930) // Write Program Memory Page
		{
			SPITxBuffer[0] = 0x4C;
			SPITxBuffer[1] = GetPacketData(0x00);
			SPITxBuffer[2] = GetPacketData(0x01);
			SPITxBuffer[3] = 0x00;
			SendSPI (4);
		}
		if (PacketCommand == 0x0931) // Write EEPROM Memory
		{
			SPITxBuffer[0] = 0xC0;
			SPITxBuffer[1] = GetPacketData(0x00);
			SPITxBuffer[2] = GetPacketData(0x01);
			SPITxBuffer[3] = GetPacketData(0x02);
			SendSPI (4);
		}
		if (PacketCommand == 0x0932) // Write EEPROM Memory (Page Access)
		{
			SPITxBuffer[0] = 0xC2;
			SPITxBuffer[1] = GetPacketData(0x00);
			SPITxBuffer[2] = GetPacketData(0x01);
			SPITxBuffer[3] = GetPacketData(0x02);
			SendSPI (4);
		}
		if (PacketCommand == 0x0933) // Write Lock Bits
		{
			SPITxBuffer[0] = 0xAC;
			SPITxBuffer[1] = 0xE0;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = GetPacketData(0x00);
			SendSPI (4);
		}		
		if (PacketCommand == 0x0934) // Write Fuse Bits
		{
			SPITxBuffer[0] = 0xAC;
			SPITxBuffer[1] = 0xA0;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = GetPacketData(0x00);
			SendSPI (4);
		}		
		if (PacketCommand == 0x0935) // Write Fuse High Bits
		{
			SPITxBuffer[0] = 0xAC;
			SPITxBuffer[1] = 0xA8;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = GetPacketData(0x00);
			SendSPI (4);
		}		
		if (PacketCommand == 0x0936) // Write Extended Fuse Bits
		{
			SPITxBuffer[0] = 0xAC;
			SPITxBuffer[1] = 0xA4;
			SPITxBuffer[2] = 0x00;
			SPITxBuffer[3] = GetPacketData(0x00);
			SendSPI (4);
		}

		///////////////////////////////////////////////////////////
		//		CC 2540 Commands
		///////////////////////////////////////////////////////////

		if ((PacketCommand >= 0x3000) && (PacketCommand < 0x3FF0) && (PacketCommand != 0x3EFF)) // Pass thru commmands
		{
			for (Temp1 = 0; Temp1 < PacketLength; Temp1++)
			{
				SPITxBuffer[Temp1] = GetHeaderData(Temp1);
			}
			SendSPI (PacketLength);
		}
		
		if (PacketCommand == 0x3EFF) // Send HM-10 Message
		{
			unsigned short spilength = (GetPacketData(0x01) << 8) | GetPacketData(0x00);
			SPITxBuffer[0] = spilength & 0xFF;
			SPITxBuffer[1] = spilength >> 8;
			SPITxBuffer[2] = 0xFF;
			SPITxBuffer[3] = 0x3E;
			SendSPI (spilength);
			
			for (Temp1=0;Temp1<spilength;Temp1++)
			{
				SPIBuffer[Temp1] = SPIRxBuffer[4 + Temp1];
			}
			
 			SendPacket (PacketCommand, SPIBuffer, spilength - 4);
		}
		
		if (PacketCommand == 0x3FFE) // Read Flash Pos
		{
			SPITxBuffer[0] = 0x08;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0xFE;
			SPITxBuffer[3] = 0x3F;
			SPITxBuffer[4] = 0x00;
			SPITxBuffer[5] = 0x00;
			SPITxBuffer[6] = 0x00;
			SPITxBuffer[7] = 0x00;
			SendSPI (8);
			
			for (Temp1=0;Temp1<4;Temp1++)
			{
				SPIBuffer[Temp1] = SPIRxBuffer[4 + Temp1];
			}
			
			SendPacket (PacketCommand, SPIBuffer, 4);
		}
		
		if (PacketCommand == 0x3FFF) // Read Status
		{
			SPITxBuffer[0] = 0x0C;
			SPITxBuffer[1] = 0x00;
			SPITxBuffer[2] = 0xFF;
			SPITxBuffer[3] = 0x3F;
			SPITxBuffer[4] = 0x00;
			SPITxBuffer[5] = 0x00;
			SPITxBuffer[6] = 0x00;
			SPITxBuffer[7] = 0x00;
			SPITxBuffer[8] = 0x00;
			SPITxBuffer[9] = 0x00;
			SPITxBuffer[10] = 0x00;
			SPITxBuffer[11] = 0x00;
			SendSPI (0xC);
			
			for (Temp1=0;Temp1<8;Temp1++)
			{
				SPIBuffer[Temp1] = SPIRxBuffer[4 + Temp1];
			}
			
			SendPacket (PacketCommand, SPIBuffer, 8);
		}
		
		///////////////////////////////////////////////////////////
		//		ATMega Control
		///////////////////////////////////////////////////////////

		if (PacketCommand == 0x4000) // RESET LOW
		{
			PAOUT &= 0xFE;
		}
		if (PacketCommand == 0x4001) // RESET HIGH
		{
			PAOUT |= 0x01;
		}
		if (PacketCommand == 0x4002) // Send SPI Data
		{
			for (Temp1 = 0; Temp1 < PacketLength - 4; Temp1++)
			{
				SPITxBuffer[Temp1] = GetPacketData(Temp1);
			}
			SendSPI (PacketLength - 4);
		}
		
		// Update Buffer Pointer position
		
		RxBufferPtrCurrent += PacketLength;
		if (RxBufferPtrCurrent >= UART_RX_SIZE)
		{
			RxBufferPtrCurrent -= UART_RX_SIZE;
			PAOUT ^= 0x04; // Toggle LED
		}
	}
}