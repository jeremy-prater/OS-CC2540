/*
 * main.h
 *
 * Created: 5/6/2014 3:01:12 PM
 *  Author: Administrator
 */ 


#ifndef MAIN_H_
#define MAIN_H_

extern uint8_t SPISlaveRxBuffer[SPI_SLAVE_RX_BUFFER_SIZE]; // .25 KB
extern uint8_t SPIMasterRxBuffer[SPI_MASTER_RX_BUFFER_SIZE]; // .75 KB
extern uint8_t SPIMasterTxBuffer[SPI_MASTER_TX_BUFFER_SIZE]; // 1 KB

extern uint8_t SPI_FLASH_0_BUFFER[SPI_FLASH_0_BUFFER_SIZE]; // 1.25 KB
extern uint8_t SPI_FLASH_1_BUFFER[SPI_FLASH_1_BUFFER_SIZE]; // 1.5 KB
extern uint8_t SPI_HM_10_BUFFER[SPI_HM_10_BUFFER_SIZE]; // 1.5 KB

extern volatile uint16_t SPISlaveTxBufferPtr;
extern volatile uint8_t * SPISlaveTxBuffer;

extern volatile uint16_t SPIMasterRxBufferPtr;

extern volatile uint16_t SPIMasterTxBufferPtr;
extern volatile uint16_t SPIMasterTxBufferLength;

extern uint16_t SPIMasterBufferFlash0Ptr;
extern uint16_t SPIMasterBufferFlash1Ptr;
extern uint16_t SPIMasterBufferHM10Ptr;

extern uint16_t SPIMasterBufferFlash0Length;
extern uint16_t SPIMasterBufferFlash1Length;
extern uint16_t SPIMasterBufferHM10Length;

extern volatile uint8_t SR[7];
extern volatile uint8_t HM_10_STAT[8];
extern volatile uint32_t HM_10_FLASH_PROGRESS;

typedef struct Packet
{
	uint16_t Length;
	uint16_t Command;
	uint8_t Data[];
} Packet;

#define Num_Packet_Commands 64

volatile Packet * PacketController[Num_Packet_Commands];
volatile uint8_t CurrentPacketNumber;
volatile uint8_t NextPacketNumber;

void AddPacket (Packet * packet);
Packet * GetNextPacket(void);
void DisposePacket(void);

extern volatile Packet * CurrentPacket;
extern volatile Packet * NewPacket;

#endif /* MAIN_H_ */