/**
* \file
*
* \brief Empty user application template
*
*/

/**
* \mainpage User Application template doxygen documentation
*
* \par Empty user application template
*
* Bare minimum empty user application template
*
* \par Content
*
* -# Include the ASF header files (through asf.h)
* -# Minimal main function that starts with a call to board_init()
* -# "Insert application code here" comment
*
*/

/*
* Include header files for all drivers that have been imported from
* Atmel Software Framework (ASF).
*/
#include <asf.h>
#include "spi.h"
#include "gpio.h"
#include "main.h"

uint8_t SPIMasterRxBuffer[SPI_MASTER_RX_BUFFER_SIZE];
uint8_t SPIMasterTxBuffer[SPI_MASTER_TX_BUFFER_SIZE];

uint8_t SPI_FLASH_0_BUFFER[SPI_FLASH_0_BUFFER_SIZE];
uint8_t SPI_FLASH_1_BUFFER[SPI_FLASH_1_BUFFER_SIZE];
uint8_t SPI_HM_10_BUFFER[SPI_HM_10_BUFFER_SIZE];

volatile uint16_t SPISlaveTxBufferPtr;
volatile uint8_t * SPISlaveTxBuffer;

volatile uint16_t SPIMasterRxBufferPtr;

volatile uint16_t SPIMasterTxBufferPtr;
volatile uint16_t SPIMasterTxBufferLength;

uint16_t SPIMasterBufferFlash0Ptr;
uint16_t SPIMasterBufferFlash1Ptr;
uint16_t SPIMasterBufferHM10Ptr;

uint16_t SPIMasterBufferFlash0Length;
uint16_t SPIMasterBufferFlash1Length;
uint16_t SPIMasterBufferHM10Length;

uint16_t SlaveTempCommand;

volatile uint8_t SR[7];
volatile uint8_t HM_10_STAT[8];
volatile uint32_t HM_10_FLASH_PROGRESS;

volatile Packet * CurrentPacket;
volatile Packet * NewPacket;

uint32_t temp;

volatile uint8_t HM_10_MSG_AVAIL;

#define hm_10_flash_block_size 0x100
unsigned char * hm_10_flash_data = (unsigned char*)SPI_FLASH_0_BUFFER;

int main (void)
{
	sysclk_init();
	board_init();
	ioport_init();
	init_gpio_pins();

	sysclk_enable_module(POWER_RED_REG0, PRSPI_bm);
	sysclk_enable_module(POWER_RED_REG0, PRTIM1_bm);
	sysclk_enable_module(POWER_RED_REG0, PRUSART0_bm);
	
	cpu_irq_disable();
	setup_spi(SPI_MODE_0, SPI_MSB, SPI_INTERRUPT, SPI_SLAVE); // Slave SPI
	setup_uart_spi(SPI_MODE_0, SPI_MSB, SPI_INTERRUPT, SPI_UART_MSTR_CLK4); // Master SPI
	cpu_irq_enable();

	SR[0] = 0x00;
	SR[1] = 0x00;
	SR[2] = 0x00;
	SR[3] = 0x00;
	SR[4] = 0x00;
	SR[5] = 0x00;
	SR[6] = 0x00;
	
	HM_10_MSG_AVAIL = 0;

	CurrentPacket = NULL;
	NewPacket = NULL;

	SPIMasterRxBufferPtr = 0;
	SPIMasterTxBufferPtr = 0;
	SPIMasterTxBufferLength = 0;
	
	SPIMasterBufferFlash0Ptr = 0;
	SPIMasterBufferFlash1Ptr = 0;
	SPIMasterBufferHM10Ptr = 0;
		
	SPIMasterBufferFlash0Length = 0;
	SPIMasterBufferFlash1Length = 0;
	SPIMasterBufferHM10Length = 0;
		
	HM_10_STAT[0] = 0x00;
	HM_10_STAT[1] = 0x00;
	HM_10_STAT[2] = 0x00;
	HM_10_STAT[3] = 0x00;
	HM_10_STAT[4] = 0x00;
	HM_10_STAT[5] = 0x00;
	HM_10_STAT[6] = 0x00;
	HM_10_STAT[7] = 0x00;
	
	/*
	0 HM_10_STAT_DBGSTAT = 0;
	1 HM_10_STAT_CONF = 0;
	2 HM_10_STAT_PC_LOW = 0;
	3 HM_10_STAT_PC_HIGH = 0;
	4 HM_10_STAT_ACC = 0;
	5 HM_10_STAT_CHIPID = 0;
	6 HM_10_STAT_CHIPVER = 0;
	7 HM_10_STAT_BM = 0;
	8 HM_10_FLASH_PROGRESS = 0;
	*/

	CurrentPacketNumber = 0;
	NextPacketNumber = 0;
	
	for (uint8_t r=0;r<Num_Packet_Commands;r++)
	PacketController[r] = NULL;
	
	while (1) // Main Loop
	{
		GetNextPacket();
		
		////////////////////////////////////////////////////////////////////
		//				FLASH 0 / 1 SPI Commands
		////////////////////////////////////////////////////////////////////
		
		if ((CurrentPacket->Command >= 0x1000) && (CurrentPacket->Command < 0x3000)) // SPI FLASH COMMANDS
		{
			unsigned char flashid = 0;
			if ((CurrentPacket->Command >= 0x2000) && (CurrentPacket->Command < 0x3000))
			flashid = 1;

			if (flashid == 0)
			ioport_set_pin_level(SPI_UART_SS_FLASH_0, false);
			else
			ioport_set_pin_level(SPI_UART_SS_FLASH_1, false);
			
			SlaveTempCommand = CurrentPacket->Command & 0x0FFF;
			
			if (SlaveTempCommand == 0x0001) // Write SR Flash
			{
				LoadMasterSPI(0x01);
				LoadMasterSPI(CurrentPacket->Data[0x00]);
				SendMasterSPI();
			}
			if ((SlaveTempCommand == 0x0002) || (SlaveTempCommand == 0x0008)) // Write Data Flash
			{
				if (flashid == 0)
				SR[1] |= SR_2_WR;
				else
				SR[2] |= SR_3_WR;
				if (SlaveTempCommand == 0x0008)
				{
					LoadMasterSPI(0x06);
					SendMasterSPI();
					if (flashid == 0)
					ioport_set_pin_level(SPI_UART_SS_FLASH_0, true);
					else
					ioport_set_pin_level(SPI_UART_SS_FLASH_1, true);
					timer_delay(SS_COMMAND_DELAY);
					if (flashid == 0)
					ioport_set_pin_level(SPI_UART_SS_FLASH_0, false);
					else
					ioport_set_pin_level(SPI_UART_SS_FLASH_1, false);
				}
				LoadMasterSPI(0x02);
				LoadMasterSPI(CurrentPacket->Data[0x00]); // Address 0 A23-A16
				LoadMasterSPI(CurrentPacket->Data[0x01]); // Address 1 A15-A8
				LoadMasterSPI(CurrentPacket->Data[0x02]); // Address 2  A7-A0
				temp=0;
				do
				{
					LoadMasterSPI(CurrentPacket->Data[0x03 + temp++]); // DATAn
				} while (temp != (CurrentPacket->Length - 7));
				SendMasterSPI();
				char holding = true;
				while (holding)
				{
					if (flashid == 0)
					ioport_set_pin_level(SPI_UART_SS_FLASH_0, true);
					else
					ioport_set_pin_level(SPI_UART_SS_FLASH_1, true);
					timer_delay(SS_COMMAND_DELAY);
					if (flashid == 0)
					ioport_set_pin_level(SPI_UART_SS_FLASH_0, false);
					else
					ioport_set_pin_level(SPI_UART_SS_FLASH_1, false);
					
					LoadMasterSPI(0x05);
					LoadMasterSPI(0x00);
					SendMasterSPI();
					if ((GetMasterRxData (0x01) & 0x01) != 0x01)
					holding = false;
				}
				if (flashid == 0)
				SR[1] &= ~SR_2_WR;
				else
				SR[2] &= ~SR_3_WR;

			}
			if (SlaveTempCommand == 0x0003) // Read Data Flash
			{
				uint16_t datalen;
				
				if (flashid == 0)
				{
					SR[1] &= ~SR_2_RBR; // Clear SR[1].7 - Read Buffer Ready
					SR[1] |= SR_2_PROC; // Set SR[1].6 - Read Buffer Processing
				}
				if (flashid == 1)
				{
					SR[2] &= ~SR_3_RBR; // Clear SR[2].7 - Read Buffer Ready
					SR[2] |= SR_3_PROC; // Set SR[2].6 - Read Buffer Processing
				}
				
				datalen = CurrentPacket->Data[0x03];
				datalen |= (CurrentPacket->Data[0x04] << 8);
				LoadMasterSPI(0x03);
				LoadMasterSPI(CurrentPacket->Data[0x00]); // Address 0 A23-A16
				LoadMasterSPI(CurrentPacket->Data[0x01]); // Address 1 A15-A8
				LoadMasterSPI(CurrentPacket->Data[0x02]); // Address 2  A7-A0
				temp=0;
				do
				{
					LoadMasterSPI(0); // DATAn
					temp++;
				} while (temp != datalen);
				SendMasterSPI();
				temp = 0;
				do
				{
					LoadFlashBuffer( GetMasterRxData (0x04 + temp++), flashid);
				} while (temp != datalen);

				if (flashid == 0)
				{
					SR[1] |= SR_2_RBR; // Set SR[1].7 - Read Buffer Ready
					SR[1] &= ~SR_2_PROC; // Clear SR[1].6 - Read Buffer Processing
				}
				if (flashid == 1)
				{
					SR[2] |= SR_3_RBR; // Set SR[2].7 - Read Buffer Ready
					SR[2] &= ~SR_3_PROC; // Clear SR[2].6 - Read Buffer Processing
				}
			}
			if (SlaveTempCommand == 0x0004) // WRDI Flash
			{
				LoadMasterSPI(0x04);
				SendMasterSPI();
			}
			if (SlaveTempCommand == 0x0005) // Read SR Flash
			{
				if (flashid == 0)
				{
					SR[1] &= ~SR_2_RBR; // Clear SR[1].7 - Read Buffer Ready
					SR[1] |= SR_2_PROC; // Set SR[1].6 - Read Buffer Processing
				}
				if (flashid == 1)
				{
					SR[2] &= ~SR_3_RBR; // Clear SR[2].7 - Read Buffer Ready
					SR[2] |= SR_3_PROC; // Set SR[2].6 - Read Buffer Processing
				}
				LoadMasterSPI(0x05);
				LoadMasterSPI(0x00);
				SendMasterSPI();
				LoadFlashBuffer( GetMasterRxData (0x01), flashid);
				if (flashid == 0)
				{
					SR[1] |= SR_2_RBR; // Set SR[1].7 - Read Buffer Ready
					SR[1] &= ~SR_2_PROC; // Clear SR[1].6 - Read Buffer Processing
				}
				if (flashid == 1)
				{
					SR[2] |= SR_3_RBR; // Set SR[2].7 - Read Buffer Ready
					SR[2] &= ~SR_3_PROC; // Clear SR[2].6 - Read Buffer Processing
				}
			}
			if (SlaveTempCommand == 0x0006) // WREN Flash
			{
				LoadMasterSPI(0x06);
				SendMasterSPI();
			}
			
			if ((SlaveTempCommand == 0x009E) || (SlaveTempCommand == 0x009F)) // Read ID
			{
				if (flashid == 0)
				{
					SR[1] &= ~SR_2_RBR; // Clear SR[1].7 - Read Buffer Ready
					SR[1] |= SR_2_PROC; // Set SR[1].6 - Read Buffer Processing
				}
				if (flashid == 1)
				{
					SR[2] &= ~SR_3_RBR; // Clear SR[2].7 - Read Buffer Ready
					SR[2] |= SR_3_PROC; // Set SR[2].6 - Read Buffer Processing
				}

				LoadMasterSPI(SlaveTempCommand & 0xFF);

				for (char t=0;t<20;t++)
				LoadMasterSPI(0x00);
				
				SendMasterSPI();
				
				for (char t=0;t<20;t++)
				LoadFlashBuffer( GetMasterRxData (0x01 + t), flashid);
				
				if (flashid == 0)
				{
					SR[1] |= SR_2_RBR; // Set SR[1].7 - Read Buffer Ready
					SR[1] &= ~SR_2_PROC; // Clear SR[1].6 - Read Buffer Processing
				}
				if (flashid == 1)
				{
					SR[2] |= SR_3_RBR; // Set SR[2].7 - Read Buffer Ready
					SR[2] &= ~SR_3_PROC; // Clear SR[2].6 - Read Buffer Processing
				}
				
			}
			
			
			if (SlaveTempCommand == 0x00D8) // Sector Erase Flash
			{
				LoadMasterSPI(0xD8);
				LoadMasterSPI(CurrentPacket->Data[0x00]); // Address 0 A23-A16
				LoadMasterSPI(CurrentPacket->Data[0x01]); // Address 1 A15-A8
				LoadMasterSPI(CurrentPacket->Data[0x02]); // Address 2  A7-A0
				
				SendMasterSPI();
			}
			
			if (SlaveTempCommand == 0x00C7) // Chip Erase Flash
			{
				LoadMasterSPI(0xC7);
				SendMasterSPI();
			}
			
			if (flashid == 0) // Reset SS Lines
			ioport_set_pin_level(SPI_UART_SS_FLASH_0, true);
			else
			ioport_set_pin_level(SPI_UART_SS_FLASH_1, true);
		}

		////////////////////////////////////////////////////////////////////
		//				CC 2540 BLE Commands
		////////////////////////////////////////////////////////////////////

		if ((CurrentPacket->Command >= 0x3000) && (CurrentPacket->Command <= 0x3EFE)) // Send HM-10 Data
		{
			uint8_t * data = (uint8_t*)CurrentPacket;
			for (uint16_t temp=0;temp < CurrentPacket->Length; temp++)
			{
				LoadMasterSPI(data[temp]); // DATAn
			}
					
			ioport_set_pin_level(SPI_UART_SS_HM_10, false);
			SendMasterSPI();
			ioport_set_pin_level(SPI_UART_SS_HM_10, true);			
		}

		////////////////////////////////////////////////////////////////////
		//				CC 2540 Debug/Flash Commands
		////////////////////////////////////////////////////////////////////
		
		if (CurrentPacket->Command == 0x3F00) // RESET HM-10
		{
			// SET DD to Interrupt / Input
			ioport_set_pin_dir(HM_10_DD, IOPORT_DIR_INPUT);
			ioport_set_pin_dir(HM_10_DC, IOPORT_DIR_INPUT);
			ioport_set_pin_dir(HM_10_RESET, IOPORT_DIR_OUTPUT);
					
			HM_10_RESET_DOWN();
			timer_delay (HM_10_CLOCK_RESET);
			HM_10_RESET_UP();
			timer_delay (HM_10_CLOCK_RESET);
			ioport_set_pin_dir(HM_10_RESET, IOPORT_DIR_INPUT);
		}
		
		if (CurrentPacket->Command == 0x3F01) // RESET HM-10 To Debug
		{
			// SET DD to Output
			ioport_set_pin_dir(HM_10_DD, IOPORT_DIR_OUTPUT);
			ioport_set_pin_dir(HM_10_DC, IOPORT_DIR_OUTPUT);
			ioport_set_pin_dir(HM_10_RESET, IOPORT_DIR_OUTPUT);
			
			HM_10_RESET_DOWN();
			timer_delay (HM_10_CLOCK_RESET);
			HM_10_DC_UP();
			timer_delay (HM_10_CLOCK_DATA);
			HM_10_DC_DOWN();
			timer_delay (HM_10_CLOCK_DATA);
			HM_10_DC_UP();
			timer_delay (HM_10_CLOCK_DATA);
			HM_10_DC_DOWN();
			timer_delay (HM_10_CLOCK_DATA);
			HM_10_RESET_UP();
			timer_delay (HM_10_CLOCK_RESET);
		}

		if (CurrentPacket->Command == 0x3F02) // Write Flash 0 to HM-10 ROM
		{
			SR[3] |= SR_4_ROM;
			uint8_t pagempty;
			for (unsigned char bank = 0; bank < 0x08; bank++)
			{
				for (uint32_t address = 0; address < 0x00008000; address += hm_10_flash_block_size)
				{
					LoadMasterSPI(0x03);
					HM_10_FLASH_PROGRESS = (bank * 0x00008000UL) + address;
					LoadMasterSPI((HM_10_FLASH_PROGRESS >> 16) & 0xFF);   // Address 2 A23-A16
					LoadMasterSPI((HM_10_FLASH_PROGRESS >>  8) & 0xFF);   // Address 1 A15-A8
					LoadMasterSPI((HM_10_FLASH_PROGRESS >>  0) & 0xFF);   // Address 0 A7 -A0
					for (temp=0;temp < hm_10_flash_block_size; temp++)
					{
						LoadMasterSPI(0); // DATAn
					}
					
					ioport_set_pin_level(SPI_UART_SS_FLASH_0, false);
					SendMasterSPI();
					ioport_set_pin_level(SPI_UART_SS_FLASH_0, true);

					pagempty = true;
					for (temp=0;temp < hm_10_flash_block_size; temp++)
					{
						uint8_t dtemp = GetMasterRxData (0x04 + temp);
						if (dtemp != 0xFF)
						pagempty = false;
						hm_10_flash_data[temp] = dtemp;
					}
					if (pagempty == false)
					write_flash_memory_block(hm_10_flash_data,HM_10_FLASH_PROGRESS,hm_10_flash_block_size);
				}
			}
			SR[3] &= ~SR_4_ROM;
		}

		if (CurrentPacket->Command == 0x3F03) // Read HM-10 ROM to Flash 0
		{
			uint8_t pageempty;
			SR[3] |= SR_4_ROM;
			for (unsigned char bank = 0; bank < 0x08; bank++)
			{
				for (uint32_t address = 0; address < 0x00008000; address += hm_10_flash_block_size)
				{
					pageempty = true;
					HM_10_FLASH_PROGRESS = (bank * 0x00008000UL) + address;
					read_flash_memory_block(bank,address,hm_10_flash_block_size,hm_10_flash_data);
					for (uint32_t pt = 0;pt < hm_10_flash_block_size;pt++)
					{
						if (hm_10_flash_data[pt] != 0xFF)
						pageempty = false;
					}
					if (pageempty == false)
					{
						LoadMasterSPI(0x06);
						ioport_set_pin_level(SPI_UART_SS_FLASH_0, false);
						SendMasterSPI();
						ioport_set_pin_level(SPI_UART_SS_FLASH_0, true);
						LoadMasterSPI(0x02);
						LoadMasterSPI((((bank * 0x00008000UL) + address) >> 16) & 0xFF);   // Address 2 A23-A16
						LoadMasterSPI((((bank * 0x00008000UL) + address) >>  8) & 0xFF);   // Address 1 A15-A8
						LoadMasterSPI((((bank * 0x00008000UL) + address) >>  0) & 0xFF);   // Address 0 A7 -A0
						for (temp=0;temp < hm_10_flash_block_size; temp++)
						{
							LoadMasterSPI(hm_10_flash_data[temp]); // DATAn
						}
						ioport_set_pin_level(SPI_UART_SS_FLASH_0, false);
						SendMasterSPI();
						ioport_set_pin_level(SPI_UART_SS_FLASH_0, true);
						char waiting = true;
						while (waiting)
						{
							LoadMasterSPI(0x05);
							LoadMasterSPI(0x00);
							ioport_set_pin_level(SPI_UART_SS_FLASH_0, false);
							SendMasterSPI();
							ioport_set_pin_level(SPI_UART_SS_FLASH_0, true);
							if ((GetMasterRxData (0x01) & 0x01) != 0x01)
							waiting = false;
						}
					}
				}
			}
			SR[3] &= ~SR_4_ROM;
		}

		if (CurrentPacket->Command == 0x3F04) // Verify HM-10 ROM to Flash 0
		{
			
		}

		if (CurrentPacket->Command == 0x3F05) // Verify HM-10 ROM to Flash 0
		{
			
		}
		
		if (CurrentPacket->Command == 0x3F06) // RESET HM-10 LOW
		{
			// SET DD to Interrupt / Input
			ioport_set_pin_dir(HM_10_DD, IOPORT_DIR_INPUT);
			
			HM_10_RESET_DOWN();
		}		

		if (CurrentPacket->Command == 0x3F07) // RESET HM-10 HIGH
		{
			// SET DD to Interrupt / Input
			ioport_set_pin_dir(HM_10_DD, IOPORT_DIR_INPUT);
			
			HM_10_RESET_UP();
		}

		if (CurrentPacket->Command == 0x3F10) // CC 2540 Chip Erase
		{
			hm_10_flash_data[0x00] = 0x10;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[0], NULL);
		}

		if (CurrentPacket->Command == 0x3F18) // CC 2540 WR_CONFIG
		{
			hm_10_flash_data[0x00] = 0x18;
			hm_10_flash_data[0x01] = CurrentPacket->Data[0x00];
			HM10SendDebugByte(hm_10_flash_data, 2);
			HM10ReadDebugByte(&HM_10_STAT[0], NULL);
		}

		if (CurrentPacket->Command == 0x3F20) // CC 2540 RD_CONFIG
		{
			hm_10_flash_data[0x00] = 0x20;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[1], NULL);
		}

		if (CurrentPacket->Command == 0x3F28) // CC 2540 GET PC
		{
			hm_10_flash_data[0x00] = 0x28;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[2], &HM_10_STAT[3]);
		}

		if (CurrentPacket->Command == 0x3F30) // CC 2540 Read Status
		{
			hm_10_flash_data[0x00] = 0x30;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[0], NULL);
		}

		if (CurrentPacket->Command == 0x3F38) // CC 2540 HW Breakpoint
		{
			hm_10_flash_data[0x00] = 0x38;
			hm_10_flash_data[0x01] = CurrentPacket->Data[0x00];
			hm_10_flash_data[0x02] = CurrentPacket->Data[0x01];
			hm_10_flash_data[0x03] = CurrentPacket->Data[0x02];
			HM10SendDebugByte(hm_10_flash_data, 4);
			HM10ReadDebugByte(&HM_10_STAT[0], NULL);
		}

		if (CurrentPacket->Command == 0x3F40) // CC 2540 Halt
		{
			hm_10_flash_data[0x00] = 0x40;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[0], NULL);
		}

		if (CurrentPacket->Command == 0x3F48) // CC 2540 Resume
		{
			hm_10_flash_data[0x00] = 0x48;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[0], NULL);
		}

		if (CurrentPacket->Command == 0x3F50) // CC 2540 Debug Instruction
		{
			HM_10_STAT[4] = HM10SendDebugCommand((uint8_t*)CurrentPacket->Data, CurrentPacket->Length - 4);
		}

		if (CurrentPacket->Command == 0x3F58) // CC 2540 Step Instruction
		{
			hm_10_flash_data[0x00] = 0x58;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[4], NULL);
		}

		if (CurrentPacket->Command == 0x3F60) // CC 2540 Get Memory Bank
		{
			hm_10_flash_data[0x00] = 0x60;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[7], NULL);
		}

		if (CurrentPacket->Command == 0x3F68) // CC 2540 Get Chip ID
		{
			hm_10_flash_data[0x00] = 0x68;
			HM10SendDebugByte(hm_10_flash_data, 1);
			HM10ReadDebugByte(&HM_10_STAT[5], &HM_10_STAT[6]);
		}

		if (CurrentPacket->Command == 0x3F80) // CC 2540 Burst Write
		{
			// Not implemented...
		}

		if (CurrentPacket->Command == 0x3FA0) // CC 2540 32 MHz XOSC Mode
		{
			write_xdata_memory(DUP_CLKCONCMD, 0x80);
			while (read_xdata_memory(DUP_CLKCONSTA) != 0x80);
		}
		////////////////////////////////////////////////////////////////////
		//				SPI Test Commands
		////////////////////////////////////////////////////////////////////

		if (CurrentPacket->Command == 0x4002) // Echo Test SPI Command
		{
			SR[1] &= ~SR_2_RBR; // Clear SR[1].7 - Read Buffer Ready
			SR[1] |= SR_2_PROC; // Set SR[1].6 - Read Buffer Processing
			uint16_t temp = 0;
			do
			{
				LoadFlashBuffer( ((uint8_t*)CurrentPacket)[temp++], 0);
			} while (temp != CurrentPacket->Length);
			SR[1] |= SR_2_RBR; // Set SR[1].7 - Read Buffer Ready
			SR[1] &= ~SR_2_PROC; // Clear SR[1].6 - Read Buffer Processing
		}
		DisposePacket();
	}
}

void AddPacket (Packet * packet)
{
	uint8_t nextid = NextPacketNumber++;
	if (NextPacketNumber == Num_Packet_Commands)
	NextPacketNumber = 0;
	if (PacketController[nextid] != NULL)
	{
		// OOPS! The buffer is full, so sorry.. :(
		led_error(3);
	}
	PacketController[nextid] = packet;
}

Packet * GetNextPacket(void)
{
	barrier();
	bool doingsomething = false;
	while (doingsomething == false)
	{
		if (PacketController[CurrentPacketNumber] != NULL)
		{
			doingsomething = true;
		}
		if (HM_10_MSG_AVAIL == true)
		{
			GetNumHM10Message();
		}
	}
	barrier();
	CurrentPacket = PacketController[CurrentPacketNumber];
	return (Packet *)CurrentPacket;
}

void DisposePacket(void)
{
	free ((void*)PacketController[CurrentPacketNumber]);
	PacketController[CurrentPacketNumber++] = NULL;
	if (CurrentPacketNumber == Num_Packet_Commands)
	CurrentPacketNumber = 0;
}