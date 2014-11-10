/*******************************************************************************
  Filename:       npi.h
  Revised:        $Date: 2007-10-28 09:35:41 -0700 (Sun, 28 Oct 2007) $
  Revision:       $Revision: 15796 $

  Description:    This file contains the Network Processor Interface (NPI),
                  which abstracts the physical link between the Application
                  Processor (AP) and the Network Processor (NP). The NPI
                  serves as the HAL's client for the SPI and UART drivers, and
                  provides API and callback services for its client.

  Copyright 2008-2012 Texas Instruments Incorporated. All rights reserved.

  IMPORTANT: Your use of this Software is limited to those specific rights
  granted under the terms of a software license agreement between the user
  who downloaded the software, his/her employer (which must be your employer)
  and Texas Instruments Incorporated (the "License").  You may not use this
  Software unless you agree to abide by the terms of the License. The License
  limits your use, and you acknowledge, that the Software may not be modified,
  copied or distributed unless embedded on a Texas Instruments microcontroller
  or used solely and exclusively in conjunction with a Texas Instruments radio
  frequency transceiver, which is integrated into your product.  Other than for
  the foregoing purpose, you may not use, reproduce, copy, prepare derivative
  works of, modify, distribute, perform, display or sell this Software and/or
  its documentation for any purpose.

  YOU FURTHER ACKNOWLEDGE AND AGREE THAT THE SOFTWARE AND DOCUMENTATION ARE
  PROVIDED “AS IS” WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED,
  INCLUDING WITHOUT LIMITATION, ANY WARRANTY OF MERCHANTABILITY, TITLE,
  NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE. IN NO EVENT SHALL
  TEXAS INSTRUMENTS OR ITS LICENSORS BE LIABLE OR OBLIGATED UNDER CONTRACT,
  NEGLIGENCE, STRICT LIABILITY, CONTRIBUTION, BREACH OF WARRANTY, OR OTHER
  LEGAL EQUITABLE THEORY ANY DIRECT OR INDIRECT DAMAGES OR EXPENSES
  INCLUDING BUT NOT LIMITED TO ANY INCIDENTAL, SPECIAL, INDIRECT, PUNITIVE
  OR CONSEQUENTIAL DAMAGES, LOST PROFITS OR LOST DATA, COST OF PROCUREMENT
  OF SUBSTITUTE GOODS, TECHNOLOGY, SERVICES, OR ANY CLAIMS BY THIRD PARTIES
  (INCLUDING BUT NOT LIMITED TO ANY DEFENSE THEREOF), OR OTHER SIMILAR COSTS.

  Should you have any questions regarding your right to use this Software,
  contact Texas Instruments Incorporated at www.TI.com.
*******************************************************************************/

#ifndef SPI_INTERFACE_H
#define SPI_INTERFACE_H

#ifdef __cplusplus
extern "C"
{
#endif

/*******************************************************************************
 * INCLUDES
 */

#include "hal_types.h"
#include "hal_board.h"
#include "hal_uart.h"
#include "OSAL.h"

/*******************************************************************************
 * MACROS
 */


/*******************************************************************************
 * CONSTANTS
 */

#define SPI_CMD_RX 0x02
#define SPI_EVT_TX 0x04

/*******************************************************************************
 * TYPEDEFS
 */
  
typedef struct _SPIPacket
{
uint16 Length;
uint16 Command;
uint8 Data[];
} SPIPacket_t;



typedef struct _HCICommandPacket
{
uint16 opCode;
uint8 ParamaterLength;
uint8 ParameterData[];
} HCICommandPacket_t;

typedef struct _HCIAsyncPacket
{
uint16 opCode;
uint16 DataLength;
uint8 Data[];
} HCIAsyncPacket_t;

typedef struct _HCIEventPacket
{
uint8 evCode;
uint8 ParamaterLength;
uint8 ParameterData[];
} HCIEventPacket_t;

/*******************************************************************************
 * LOCAL VARIABLES
 */

/*******************************************************************************
 * GLOBAL VARIABLES
 */


/*******************************************************************************
 * FUNCTIONS
 */

//
// SPI Interface
//

#define MESSAGE_BV                        BV(1)
#define MESSAGE_SBIT                      P2_1
#define MESSAGE_DDR                       P2DIR

#define NUM_OUTGOING_MESSAGES             64

void SPI_QUEUE_OUTGOING_MESSAGE(SPIPacket_t * packet);
void SPI_OSAL_RX_COMMAND(SPIPacket_t * packet);
void SPI_OSAL_TX_COMMAND(SPIPacket_t * packet);

void AddOutgoingMessage (SPIPacket_t * packet);
void AddOutgoingMessageBuffer (void * data, unsigned short length);
SPIPacket_t * GetNextMessage();
void DisposeMessage();

uint16 SPI_Interface_ProcessEvent( uint8 task_id, uint16 events );
void SPI_Interface_Init( uint8 task_id );

void Execute_SPI_Command (SPIPacket_t * packet);

void sendSerialEvt(void);


/*******************************************************************************
*/

#ifdef __cplusplus
}
#endif

#endif /* SPI_INTERFACE_H */