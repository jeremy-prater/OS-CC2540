/*
* gpio.c
*
* Created: 3/30/2014 8:48:06 AM
*  Author: Administrator
*/

#include "asf.h"
#include "gpio.h"
#include "spi.h"

extern volatile uint8_t HM_10_MSG_AVAIL;

unsigned char dma_desc_0[8] =
{
	// Debug Interface -> Buffer
	HIBYTE(DUP_DBGDATA),            // src[15:8]
	LOBYTE(DUP_DBGDATA),            // src[7:0]
	HIBYTE(ADDR_BUF0),              // dest[15:8]
	LOBYTE(ADDR_BUF0),              // dest[7:0]
	0,                              // len[12:8] - filled in later
	0,                              // len[7:0]
	31,                             // trigger: DBG_BW
	0x11                            // increment destination
};
//! DUP DMA descriptor
unsigned char dma_desc_1[8] =
{
	// Buffer -> Flash controller
	HIBYTE(ADDR_BUF0),              // src[15:8]
	LOBYTE(ADDR_BUF0),              // src[7:0]
	HIBYTE(DUP_FWDATA),             // dest[15:8]
	LOBYTE(DUP_FWDATA),             // dest[7:0]
	0,                              // len[12:8] - filled in later
	0,                              // len[7:0]
	18,                             // trigger: FLASH
	0x42,                           // increment source
};

void init_gpio_pins(void)
{
	ioport_enable_pin(LED_1);
	ioport_enable_pin(LED_2);
	ioport_enable_pin(LED_3);

	ioport_enable_pin(HM_10_RESET);
	ioport_enable_pin(HM_10_DD);
	ioport_enable_pin(HM_10_DC);
	ioport_enable_pin(HM_10_MSG);
	
	ioport_set_pin_dir(LED_1, IOPORT_DIR_OUTPUT);
	ioport_set_pin_dir(LED_2, IOPORT_DIR_OUTPUT);
	ioport_set_pin_dir(LED_3, IOPORT_DIR_OUTPUT);
	ioport_set_pin_dir(HM_10_RESET, IOPORT_DIR_INPUT);
	ioport_set_pin_dir(HM_10_DD, IOPORT_DIR_INPUT);
	ioport_set_pin_dir(HM_10_DC, IOPORT_DIR_INPUT);
	ioport_set_pin_dir(HM_10_MSG, IOPORT_DIR_INPUT);

	ioport_set_pin_level(LED_1, false);
	ioport_set_pin_level(LED_2, false);
	ioport_set_pin_level(LED_3, false); // LED's on
	
	
	/*ioport_set_pin_level(HM_10_DD, false); // DC,DD low
	ioport_set_pin_level(HM_10_DC, false);
	ioport_set_pin_level(HM_10_RESET, false); // HM-10 in reset (reset low)*/
}

//volatile uint8_t HM_10_MSG_RESET = true;

ISR(PCINT2_vect)
{
	if (ioport_get_pin_level(HM_10_MSG) == false)
	{
		HM_10_MSG_AVAIL = true;
	}
}

void SET_LED_VALUE(int led, int value)
{
	ioport_set_pin_level(led, value);
}
void HM_10_RESET_UP(void)
{
	ioport_set_pin_level(HM_10_RESET, true);
}
void HM_10_RESET_DOWN(void)
{
	ioport_set_pin_level(HM_10_RESET, false);
}
void HM_10_DC_UP(void)
{
	ioport_set_pin_level(HM_10_DC, true);
}
void HM_10_DC_DOWN(void)
{
	ioport_set_pin_level(HM_10_DC, false);
}
void HM_10_SET_DD(unsigned char data)
{
	ioport_set_pin_level(HM_10_DD, data == 0 ? false : true);
}
unsigned char HM10SendDebugCommand (unsigned char * data, unsigned char length)
{
	unsigned char command[4];
	command [0x00] = 0x50 + length;
	for (unsigned char temp=0;temp < length; temp++)
	{
		command [temp + 1] = data[temp];
	}
	HM10SendDebugByte (command, length + 1);
	HM10ReadDebugByte (command, NULL);
	return command[0];
}

void HM10SendDebugByte (unsigned char * data, unsigned short length)
{
	for (unsigned short temp=0;temp < length;temp++)
	{
		for (unsigned char bit = 0; bit < 8; bit++)
		{
			HM_10_DC_UP();
			HM_10_SET_DD((data[temp] & (1 << (7 - bit)))); // Sent Bits
			timer_delay(HM_10_CLOCK_DATA);
			HM_10_DC_DOWN();
			timer_delay(HM_10_CLOCK_DATA);
		}
	}
}
void HM10ReadDebugByte (volatile uint8_t * byte1, volatile uint8_t * byte2)
{
	unsigned char data = 0;
	ioport_set_pin_dir(HM_10_DD, IOPORT_DIR_INPUT);
	timer_delay (HM_10_CLOCK_DATA);
	while (ioport_get_pin_level(HM_10_DD) == true) // CC2540 is not ready to send, sample 8 bits and trash
	{
		for (unsigned char bit = 0; bit < 8; bit++)
		{
			HM_10_DC_UP();
			timer_delay (HM_10_CLOCK_DATA);
			HM_10_DC_DOWN();
			timer_delay (HM_10_CLOCK_DATA);
		}
	}
	data = 0;
	for (unsigned char bit = 0; bit < 8; bit++)
	{
		HM_10_DC_UP();
		timer_delay (HM_10_CLOCK_DATA);
		HM_10_DC_DOWN();
		data |= ioport_get_pin_level(HM_10_DD);
		if (bit < 7)
		data <<= 1;
		timer_delay (HM_10_CLOCK_DATA);
	}
	if (byte1 != NULL)
	*byte1 = data;
	if (byte2 != NULL)
	{
		data = 0;
		for (unsigned char bit = 0; bit < 8; bit++)
		{
			HM_10_DC_UP();
			timer_delay (HM_10_CLOCK_DATA);
			HM_10_DC_DOWN();
			data |= ioport_get_pin_level(HM_10_DD);
			if (bit < 7)
			data <<= 1;
			timer_delay (HM_10_CLOCK_DATA);
		}
		*byte2 = data;
	}
	ioport_set_pin_dir(HM_10_DD, IOPORT_DIR_OUTPUT);
	timer_delay (HM_10_CLOCK_DATA);
}

void write_xdata_memory_block(unsigned short address, unsigned char * data, unsigned short length)
{
    unsigned char instr[3];
    unsigned short i;

    // MOV DPTR, address
    instr[0] = 0x90;
    instr[1] = HIBYTE(address);
    instr[2] = LOBYTE(address);
    HM10SendDebugCommand(instr, 3);

    for (i = 0; i < length; i++)
    {
	    // MOV A, values[i]
	    instr[0] = 0x74;
	    instr[1] = data[i];
	    HM10SendDebugCommand(instr, 2);

	    // MOV @DPTR, A
	    instr[0] = 0xF0;
	    HM10SendDebugCommand(instr, 1);

	    // INC DPTR
	    instr[0] = 0xA3;
	    HM10SendDebugCommand(instr, 1);
    }
}
void write_xdata_memory(unsigned short address, unsigned char data)
{
    unsigned char instr[3];

    // MOV DPTR, address
    instr[0] = 0x90;
    instr[1] = HIBYTE(address);
    instr[2] = LOBYTE(address);
    HM10SendDebugCommand(instr, 3);

    // MOV A, values[i]
    instr[0] = 0x74;
    instr[1] = data;
    HM10SendDebugCommand(instr, 2);

    // MOV @DPTR, A
    instr[0] = 0xF0;
    HM10SendDebugCommand(instr, 1);
}
unsigned char read_xdata_memory(unsigned short address)
{
    unsigned char instr[3];

    // MOV DPTR, address
    instr[0] = 0x90;
    instr[1] = HIBYTE(address);
    instr[2] = LOBYTE(address);
    HM10SendDebugCommand(instr, 3);

    // MOVX A, @DPTR
    instr[0] = 0xE0;
    return HM10SendDebugCommand(instr, 1);
}
void burst_write_block (unsigned char * data, unsigned short len)
{
	unsigned char temp1,temp2;
	
	temp1 = 0x80 | HIBYTE(len);
	temp2 = LOBYTE(len);
	
	HM10SendDebugByte (&temp1, 1);
	HM10SendDebugByte (&temp2, 1);
	HM10SendDebugByte (data, len);
	HM10ReadDebugByte (&temp1, NULL);
}
void write_flash_memory_block(unsigned char *src, uint32_t start_addr,unsigned short num_bytes)
{
    // 1. Write the 2 DMA descriptors to RAM
    write_xdata_memory_block(ADDR_DMA_DESC_0, dma_desc_0, 8);
    write_xdata_memory_block(ADDR_DMA_DESC_1, dma_desc_1, 8);

    // 2. Update LEN value in DUP's DMA descriptors
    unsigned char len[2] = {HIBYTE(num_bytes), LOBYTE(num_bytes)};
    write_xdata_memory_block((ADDR_DMA_DESC_0+4), len, 2);  // LEN, DBG => ram
    write_xdata_memory_block((ADDR_DMA_DESC_1+4), len, 2);  // LEN, ram => flash

    // 3. Set DMA controller pointer to the DMA descriptors
    write_xdata_memory(DUP_DMA0CFGH, HIBYTE(ADDR_DMA_DESC_0));
    write_xdata_memory(DUP_DMA0CFGL, LOBYTE(ADDR_DMA_DESC_0));
    write_xdata_memory(DUP_DMA1CFGH, HIBYTE(ADDR_DMA_DESC_1));
    write_xdata_memory(DUP_DMA1CFGL, LOBYTE(ADDR_DMA_DESC_1));

    // 4. Set Flash controller start address (wants 16MSb of 18 bit address)
    start_addr >>= 2;
	write_xdata_memory(DUP_FADDRH, HIBYTE( start_addr ));
    write_xdata_memory(DUP_FADDRL, LOBYTE( start_addr ));

    // 5. Arm DBG=>buffer DMA channel and start burst write
    write_xdata_memory(DUP_DMAARM, CH_DBG_TO_BUF0);
    burst_write_block(src, num_bytes);

    // 6. Start programming: buffer to flash
    write_xdata_memory(DUP_DMAARM, CH_BUF0_TO_FLASH);
    write_xdata_memory(DUP_FCTL, 0x06);

    // 7. Wait until flash controller is done
    while (read_xdata_memory(DUP_FCTL) & 0x80);
}

void read_flash_memory_block(unsigned char bank,unsigned short flash_addr,unsigned short num_bytes, unsigned char *values)
{
    unsigned char instr[3];
    unsigned short i;
    unsigned short xdata_addr = (0x8000 + flash_addr);

    // 1. Map flash memory bank to XDATA address 0x8000-0xFFFF
    write_xdata_memory(DUP_MEMCTR, bank);

    // 2. Move data pointer to XDATA address (MOV DPTR, xdata_addr)
    instr[0] = 0x90;
    instr[1] = HIBYTE(xdata_addr);
    instr[2] = LOBYTE(xdata_addr);
    HM10SendDebugCommand(instr, 3);

    for (i = 0; i < num_bytes; i++)
    {
	    // 3. Move value pointed to by DPTR to accumulator (MOVX A, @DPTR)
	    instr[0] = 0xE0;
	    values[i] = HM10SendDebugCommand(instr, 1);

	    // 4. Increment data pointer (INC DPTR)
	    instr[0] = 0xA3;
	    HM10SendDebugCommand(instr, 1);
    }
}

volatile char holding;

ISR(TIMER1_OVF_vect)
{
	holding = false;
	TCCR1B = 0x00;
}

void timer_delay (unsigned short uS)
{
	TCCR1A = 0;
	TCCR1B = 0x02;
	TCCR1C = 0x00;
	TIMSK1 = 0x01;
	TCNT1 = 0xFFFF - (uS + (uS / 2));
	holding = true;
	while (holding) barrier();
}

void led_error (uint8_t error)
{
	cpu_irq_disable();
	ioport_set_pin_level(LED_1,true);
	ioport_set_pin_level(LED_2,true);
	ioport_set_pin_level(LED_3,true);
		
	while(1)
	{
		delay_ms(100);
		if ((error & 1) == 1)
			ioport_toggle_pin_level(LED_1);
		if ((error & 2) == 2)
			ioport_toggle_pin_level(LED_2);
		if ((error & 4) == 4)
			ioport_toggle_pin_level(LED_3);
	}
	
}