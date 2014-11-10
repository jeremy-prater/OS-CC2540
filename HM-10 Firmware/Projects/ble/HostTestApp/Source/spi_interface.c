/*******************************************************************************
  Filename:       npicallback.c

  *******************************************************************************/

/*******************************************************************************
 * INCLUDES
 */

#include "hal_types.h"
#include "OSAL.h"
#include "OSAL_Tasks.h"
#include "string.h"

#include "hal_board.h"
#include "npi.h"
#include "spi_interface.h"

/*******************************************************************************
 * MACROS
 */

/*******************************************************************************
 * CONSTANTS
 */

/*******************************************************************************
 * TYPEDEFS
 */

/*******************************************************************************
 * LOCAL VARIABLES
 */

/*******************************************************************************
 * GLOBAL VARIABLES
 */

extern uint8 spiTxPkt[272]; /* Can be trimmed as per requirement*/
uint8 * spiTempData; /* Can be trimmed as per requirement*/
static uint8 SPI_Interface_TaskID;
extern uint8 hciExtApp_TaskID;

SPIPacket_t * MessageController[NUM_OUTGOING_MESSAGES];
SPIPacket_t * SPICurrentMessage;
uint8 SPICurrentMessageNumber;
uint8 SPINextMessageNumber;
uint8 SPINewMessage;
uint8 SPINumMessages;

/*******************************************************************************
 * PROTOTYPES
 */

static void SPI_Interface_ProcessOSALMsg(SPIPacket_t *pMsg);

/*******************************************************************************
 * FUNCTIONS
 */

void SPI_Interface_Init(uint8 task_id)
{
	SPI_Interface_TaskID = task_id;
	//NPI_InitTransport(npiCallBack);
}

static void SPI_Interface_ProcessOSALMsg(SPIPacket_t *pMsg)
{
  //HAL_TOGGLE_LED1();
  switch (pMsg->Command)
  {
  case 0x3000: // Stored Echo Data
    {
      AddOutgoingMessage (pMsg);
      //spiTempData = osal_mem_alloc(pMsg->Length);
      //(void)memcpy(spiTempData, pMsg, pMsg->Length);
    }
    break;
  case 0x3010:
    {
      uint8* hci_command = osal_msg_allocate (pMsg->Length - 4);
      osal_memcpy (hci_command, pMsg->Data, pMsg->Length - 4);
      osal_msg_send(hciExtApp_TaskID, hci_command);
    }
    break;
  case 0x3EFD: // Get Number of Messages Waiting
    {
      char buffer[5];
      SPIPacket_t * packet = (SPIPacket_t*)buffer;
      packet->Length = 0x0005;
      packet->Command = 0x3EFD;
      packet->Data[0] = SPINumMessages;
      HalUARTWrite( NPI_UART_PORT, (unsigned char*)packet, 5);
      break;
    }
  case 0x3EFE: // Get Next Message and Length
    {
      char buffer[6];
      SPIPacket_t * packet = (SPIPacket_t*)buffer;
      packet->Length = 0x0006;
      packet->Command = 0x3EFE;
      *((uint16*)(packet->Data)) = GetNextMessage()->Length;
      HalUARTWrite( NPI_UART_PORT, (unsigned char*)packet, 6);
      SPIPacket_t * newmsg = (SPIPacket_t*)osal_msg_allocate (4);
      newmsg->Command = 0x3EFF;
      newmsg->Length = 0x0004;
      osal_msg_send(SPI_Interface_TaskID, (uint8*)newmsg);
      break;
    }
  case 0x3EFF: // Return Stored Data
    {
      HalUARTWrite( NPI_UART_PORT, (unsigned char*)SPICurrentMessage, SPICurrentMessage->Length);
      DisposeMessage();
      //NPI_WriteTransport ((unsigned char *)pMsg, pMsg->Length);
      //osal_mem_free (spiTempData);
    }
    break;
  }
  
  osal_msg_deallocate ((uint8*)pMsg);
}


uint16 SPI_Interface_ProcessEvent(uint8 task_id, uint16 events)
{
	VOID task_id; // OSAL required parameter that isn't used in this function

	if (events & SYS_EVENT_MSG)
	{
		uint8 *pMsg;

		if ((pMsg = osal_msg_receive(SPI_Interface_TaskID)) != NULL)
		{
                        SPI_Interface_ProcessOSALMsg ((SPIPacket_t *)pMsg);	
			// Release the OSAL message
			VOID osal_msg_deallocate(pMsg);
		}

		// return unprocessed events
		return (events ^ SYS_EVENT_MSG);
	}
	// Discard unknown events
	return 0;
}

void Execute_SPI_Command (SPIPacket_t * packet)
{
  osal_mem_free (packet);
}

void sendSerialEvt(void)
{

}

void SPI_OSAL_RX_COMMAND(SPIPacket_t * packet)
{
  osal_msg_send(SPI_Interface_TaskID, (uint8*)packet);
  //osal_set_event(SPI_Interface_TaskID, SYS_EVENT_MSG | SPI_CMD_RX);
}

void SPI_QUEUE_OUTGOING_MESSAGE(SPIPacket_t * packet)
{
    
}
    
void SPI_OSAL_TX_COMMAND(SPIPacket_t * packet)
{
  
  packet->Command = 0x3EFF;
  osal_msg_send(SPI_Interface_TaskID, (uint8*)packet);
  //osal_set_event(SPI_Interface_TaskID, SYS_EVENT_MSG | SPI_CMD_RX);
}     

void AddOutgoingMessageBuffer (void * data, unsigned short length)
{
  uint8 nextid = SPINextMessageNumber++;
  if (SPINextMessageNumber == NUM_OUTGOING_MESSAGES)
  SPINextMessageNumber = 0;
  if (MessageController[nextid] != NULL)
  {
          // OOPS! The buffer is full, so sorry.. :(
  }
  MessageController[nextid] = osal_mem_alloc(length+4);
  MessageController[nextid]->Command = 0x3EFF;
  MessageController[nextid]->Length = length+4;
  memcpy (MessageController[nextid]->Data, data, length);
  SPINumMessages++;  
}

void AddOutgoingMessage(SPIPacket_t * packet)
{
  AddOutgoingMessageBuffer ((void*)packet, packet->Length);
}

SPIPacket_t * GetNextMessage()
{
  SPICurrentMessage = MessageController[SPICurrentMessageNumber];
  return (SPIPacket_t *)SPICurrentMessage;  
}

void DisposeMessage()
{
  SPINumMessages--;

  osal_mem_free ((void*)MessageController[SPICurrentMessageNumber]);
  MessageController[SPICurrentMessageNumber++] = NULL;
  if (SPICurrentMessageNumber == NUM_OUTGOING_MESSAGES)
          SPICurrentMessageNumber = 0;

  SPICurrentMessage = NULL;  
}

/*******************************************************************************
 ******************************************************************************/
