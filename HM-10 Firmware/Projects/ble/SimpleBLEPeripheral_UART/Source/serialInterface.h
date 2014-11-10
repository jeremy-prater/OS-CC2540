#include "hal_uart.h"
#include "OSAL.h"
#include "npi.h"

#define SI_CMD_RX                                 0x0001
#define SI_EVT_TX                                 0x0002

#define MAX_PKT_SIZE 128
#define CMD_RESPONSE 2

#define SERIAL_IDENTIFIER       0x77

#define APP_CMD_ADVERTISE       0x01
#define APP_CMD_DISCONNECT      0x02 // Not used
#define APP_CMD_PRINT_LCD       0x03

// Serial Event Mnemonic        Opcode
#define	APP_EVT_CMD_RESPONSE	0x81
#define APP_EVT_CONNECT	        0x82 // Not used
#define	APP_EVT_DISCONNECT	0x83 // Not used


typedef struct _BLEPacketHeader
{
uint8 identifier;	
uint8 opCode;
} BLEPacketHeader_t;


typedef struct _BLEPacket
{
BLEPacketHeader_t header;
// payload
uint8 length;  // opCode specific, 
uint8 data[10];  // command or event specific data, 10 Bytes currently
} BLEPacket_t;

typedef struct _SerialEventPacket
{
BLEPacketHeader_t header;
// payload
uint8 length;  // Length 
uint8 cmdCode; // Command Code
uint8 status;  // Status
} SerialEventPacket_t;


/* States for CRC parser */
typedef enum {
  NPI_SERIAL_STATE_ID,
  NPI_SERIAL_STATE_OPCODE,
  NPI_SERIAL_STATE_LEN,
  NPI_SERIAL_STATE_DATA,
} npi_serial_parse_state_t;

void cSerialPacketParser( uint8 port, uint8 events );
void parseCmd(void);
void sendSerialEvt(void);


/*******************************************************************************
 * MACROS
 */


/*********************************************************************
 * FUNCTIONS
 */

/*
 * Task Initialization for the BLE Application
 */
extern void SerialInterface_Init( uint8 task_id );

/*
 * Task Event Processor for the BLE Application
 */
extern uint16 SerialInterface_ProcessEvent( uint8 task_id, uint16 events );
