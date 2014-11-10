/*#include <ez8.h>
#include <STDLIB.H>
#include <STRING.H>
#include <STDIO.H>
#include <uart.h>

#define UART0_MODE MODE_INTERRUPT
#define DMA1_CTL DMA_UART0

unsigned char RxBuffer [1024];
unsigned char TxBuffer [1024];
unsigned short RxBufferPtr;
unsigned short RxLenTemp;

const unsigned int FLASH_BUFFER_SIZE = 256;

unsigned char InstBuffer[4];
unsigned char FlashBuffer [FLASH_BUFFER_SIZE];


unsigned short * PacketLength;
unsigned short * PacketCommand;
unsigned char  * PacketData;
unsigned char  * PacketChecksum;
unsigned char    CalcChecksum;

unsigned short   Temp1;

void setup_board()
{
	open_PortA();
	setmodeOutput_PortA(PORTPIN_ALL);
		
	open_UART0();
	PAOUT = 0xFF;
	
	EI();
}

void SendPacket (unsigned char command, unsigned char * data, unsigned short dlen)
{
	UINT16 length = 4 + dlen;
	UINT16 templen;
	unsigned char checksum = 0;
	
	memcpy (TxBuffer, &length, 2);
	TxBuffer[2] = command;
	for (templen = 0; templen < length; templen++)
	{
		TxBuffer[3 + templen] = data[templen];
	}
	
	for (templen = 0; templen < length - 1; templen++)
		checksum += TxBuffer[templen];
	checksum ^= 0xFF;
	checksum++;
	TxBuffer[length-1] = checksum;
	write_UART0 (TxBuffer, length);
	while(UART_IO_PENDING == get_txstatus_UART0());
	templen = 1;
	read_UART0 ((char*)&length, &templen); // Check result...
}

void ReadPacket()
{
	RxLenTemp = 2;
	read_UART0 (RxBuffer, &RxLenTemp);
	RxLenTemp = (RxBuffer[0] << 8) | RxBuffer[1];
	RxLenTemp -= 2;
	read_UART0 (RxBuffer + 2, &RxLenTemp);
	PacketLength = (unsigned short *)RxBuffer;
	PacketCommand = &RxBuffer[0x02];
	PacketData = &RxBuffer[0x04];
	PacketChecksum = &RxBuffer[(int)(*PacketLength - 1)];
}

void main()
{
	unsigned char cresult[2], temp, temp2, cmdtemp;
	unsigned short burstlen, burstemp;
	memset (&RxBuffer, 0, 1024);
	RxBufferPtr = 0;
	
	setup_board();
	
	while (1)
	{
		ReadPacket();
		CalcChecksum = 0;
		for (Temp1 = 0; Temp1 < *PacketLength - 1; Temp1++)
		{
			CalcChecksum += RxBuffer[Temp1];
		}
		CalcChecksum ^= 0xFF;
		CalcChecksum++;
		if (CalcChecksum != *PacketChecksum)
			// Send Error 0x55
			cresult[0] = 0x55;
		else
			// Send OK! 0xAA
			cresult[0] = 0xAA;
		write_UART0(cresult,1);
		
		temp = RxBuffer[3];
		RxBuffer[3] = RxBuffer[2];
		RxBuffer[2] = temp;
		
		if (cresult[0] == 0xAA)
		{
			// Process Packet
			switch (*PacketCommand)
			{
				case 0x02: // Enter Debug
					ResetDown();
					Pause (ResetTimeClock);
					
					ClockUp();
					Pause (ResetTimeClock);
					
					ClockDown();
					Pause (ResetTimeClock);
					
					ClockUp();
					Pause (ResetTimeClock);
					
					ClockDown();
					Pause (ResetTimeClock);
					
					ClockUp();
					Pause (ResetTimeClock);
					
					ResetUp();
					Pause (ResetTimeClock);
					
					
					break;
				case 0x03:
					cresult[0] = 0;
					cresult[1] = 0;
					cmdtemp = *PacketData & 0xF8;
					if (cmdtemp == 0x10) // CHIP_ERASE
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
					}
					if (cmdtemp == 0x18) // WR_CONFIG
					{
						SendDebugByte (PacketData,2);
						cresult[0] = ReadDebugByte();
						
					}
					if (cmdtemp == 0x20) // RD_CONFIG
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
					}
					if (cmdtemp == 0x28) // GET_PC
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
						cresult[1] = ReadDebugByte();
						
					}
					if (cmdtemp == 0x30) // READ_STATUS
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
					}
					if (cmdtemp == 0x38) // SET_HW_BRKPNT
					{
						SendDebugByte (PacketData,4);
						cresult[0] = ReadDebugByte();
						
					}
					if (cmdtemp == 0x40) // HALT
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
					}
					if (cmdtemp == 0x48) // RESUME
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
					}
					if (cmdtemp == 0x50) // DEBUG_INST
					{
						temp2 = PacketData[0] & 0x03;
						SendDebugByte (PacketData,temp2 + 1);
						cresult[0] = ReadDebugByte();
						
					}
					if (cmdtemp == 0x58) // STEP_INSTR
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
					}
					if (cmdtemp == 0x60) // GET_BM
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
					}
					if (cmdtemp == 0x68) // GET_CHIP_ID
					{
						SendDebugByte (PacketData,1);
						cresult[0] = ReadDebugByte();
						cresult[1] = ReadDebugByte();						

					}
					if (cmdtemp == 0x80)// BURST_WRITE
					{
						burstlen = PacketData[0] & 0x07;
						burstlen <<= 8;
						burstlen |= PacketData[1];
						if (burstlen == 0)
							burstlen = 2048;
						SendDebugByte (PacketData, burstlen + 1);
						cresult[0] = ReadDebugByte();						
					}
					SendPacket (0x04, cresult,2);
				break;
				case 0x10:
				{
					unsigned char bank;
					unsigned short daddr, dctr;
					for (bank=0;bank<8; bank++)
					{
						InstBuffer [0] = 0x53;
						InstBuffer [1] = 0x90;
						InstBuffer [2] = HIBYTE (DUP_MEMCTR);
						InstBuffer [3] = LOBYTE (DUP_MEMCTR);
						SendDebugByte (InstBuffer, 4); // MOV DPTR, DUP_MEMCTR
						ReadDebugByte();

						InstBuffer [0] = 0x52;
						InstBuffer [1] = 0x74;
						InstBuffer [2] = bank;
						SendDebugByte (InstBuffer, 3); // MOV A, bank
						ReadDebugByte();

						InstBuffer [0] = 0x51;
						InstBuffer [1] = 0xF0;
						SendDebugByte (InstBuffer, 2); // MOV @DPTR, A
						ReadDebugByte();
						
						InstBuffer [0] = 0x53;
						InstBuffer [1] = 0x90;
						InstBuffer [2] = 0x80;
						InstBuffer [3] = 0x00;
						SendDebugByte (InstBuffer, 4); // MOV DPTR, 0x8000
						ReadDebugByte();
						
						dctr = 0;
						
						for (daddr = 0; daddr < 0x8000; daddr++)
						{
							InstBuffer [0] = 0x51;
							InstBuffer [1] = 0xE0;
							SendDebugByte (InstBuffer, 2); // MOV A, @DPTR
							
							
							FlashBuffer[dctr++] = ReadDebugByte();
							
							if (dctr == FLASH_BUFFER_SIZE)
							{
								SendPacket (0x10,FlashBuffer, FLASH_BUFFER_SIZE);
								dctr = 0;
							}
						
							InstBuffer [0] = 0x51;
							InstBuffer [1] = 0xA3;
							SendDebugByte (InstBuffer, 2); // INC DPTR
							ReadDebugByte();
						}
					}
				}
				break;
				case 0x20:
				{
					unsigned char tmplen[2];
					unsigned short * address = (unsigned short*)PacketData;
					unsigned char addr1 = *(unsigned char*)address;
					unsigned char addr2 = *((unsigned char*)address+1);
					unsigned short * length = (unsigned short*)PacketData + 1;
					unsigned char * data = PacketData + 4;
					write_xdata_memory_block(ADDR_DMA_DESC_0, dma_desc_0, 8);
					write_xdata_memory_block(ADDR_DMA_DESC_1, dma_desc_1, 8);
					
					write_xdata_memory_block((ADDR_DMA_DESC_0+4),(unsigned char*)length, 2);
					write_xdata_memory_block((ADDR_DMA_DESC_1+4),(unsigned char*)length, 2);
					
					write_xdata_memory(DUP_DMA0CFGH, HIBYTE(ADDR_DMA_DESC_0));
					write_xdata_memory(DUP_DMA0CFGL, LOBYTE(ADDR_DMA_DESC_0));
					write_xdata_memory(DUP_DMA1CFGH, HIBYTE(ADDR_DMA_DESC_1));
					write_xdata_memory(DUP_DMA1CFGL, LOBYTE(ADDR_DMA_DESC_1));
					
					write_xdata_memory(DUP_FADDRH, addr1);
					write_xdata_memory(DUP_FADDRL, addr2);
					
					write_xdata_memory(DUP_DMAARM, CH_DBG_TO_BUF0);
					burst_write_block ( *length, data);
					
					write_xdata_memory(DUP_DMAARM, CH_BUF0_TO_FLASH);
					write_xdata_memory(DUP_FCTL, 0x06);
					
					while (read_xdata_memory(XREG_TO_INT(FCTL)) & 0x80);
					SendPacket (0x21,0,0);

				}
				break;
				default:
					RxBuffer[0] = 0x00;
				break;
			}
		}
		
		RxBufferPtr = 0;
	}
}*/