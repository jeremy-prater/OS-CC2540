;  zsldevinit.asm
;  Implementation file for opening peripheral devices.
; 
;  This file contains implementation for opening (by calling intialize routines)
;  peripheral devices required by ZiLOG Standard Library implementations for Z8
;  Encore! microcontrollers.
;
;  Copyright (C) 1999-2004 by  ZiLOG, Inc.
;  All Rights Reserved.
;

	segment	CODE


;---------------------------------------------------------------------------------------
	XDEF _open_periphdevice
	XDEF __open_periphdevice
;---------------------------------------------------------------------------------------
_open_periphdevice:
__open_periphdevice:


.ifdef _ZSL_DEVICE_PORTA
	XREF _open_PortA

    call _open_PortA					; initialize Port A.
.endif

.ifdef _ZSL_DEVICE_PORTB
	XREF _open_PortB

    call _open_PortB					; initialize Port B.
.endif

.ifdef _ZSL_DEVICE_PORTC
	XREF _open_PortC

    call _open_PortC					; initialize Port C.
.endif

.ifdef _ZSL_DEVICE_PORTD
	XREF _open_PortD

    call _open_PortD					; initialize Port D.
.endif

.ifdef _ZSL_DEVICE_PORTE
	XREF _open_PortE

    call _open_PortE					; initialize Port E.
.endif

.ifdef _ZSL_DEVICE_PORTF
	XREF _open_PortF

    call _open_PortF					; initialize Port F.
.endif

.ifdef _ZSL_DEVICE_PORTG
	XREF _open_PortG

    call _open_PortG					; initialize Port G.
.endif

.ifdef _ZSL_DEVICE_PORTH
	XREF _open_PortH

    call _open_PortH					; initialize Port H.
.endif


.ifdef _ZSL_DEVICE_UART0
	XREF _open_UART0

	call _open_UART0				; initialize UART0.
.endif



.ifdef _ZSL_DEVICE_UART1
	XREF _open_UART1

	call _open_UART1				; initialize UART1.
.endif


	ret


;---------------------------------------------------------------------------------------
	XDEF _close_periphdevice
	XDEF __close_periphdevice
;---------------------------------------------------------------------------------------
_close_periphdevice:
__close_periphdevice:



.ifdef _ZSL_DEVICE_PORTA
	XREF _close_PortA

    call _close_PortA					; close Port A.
.endif

.ifdef _ZSL_DEVICE_PORTB
	XREF _close_PortB

    call _close_PortB					; close Port B.
.endif

.ifdef _ZSL_DEVICE_PORTC
	XREF _close_PortC

	call _close_PortC				; close Port C.
.endif

.ifdef _ZSL_DEVICE_PORTD
	XREF _close_PortD

    call _close_PortD					; close Port D.
.endif

.ifdef _ZSL_DEVICE_PORTE
	XREF _close_PortE

    call _close_PortE					; close Port E.
.endif

.ifdef _ZSL_DEVICE_PORTF
	XREF _close_PortF

    call _close_PortF					; close Port F.
.endif

.ifdef _ZSL_DEVICE_PORTG
	XREF _close_PortG

    call _close_PortG					; close Port G.
.endif

.ifdef _ZSL_DEVICE_PORTH
	XREF _close_PortH

    call _close_PortH					; close Port H.
.endif

.ifdef _ZSL_DEVICE_UART0
	XREF _close_UART0

	call _close_UART0				; close UART0.
.endif

.ifdef _ZSL_DEVICE_UART1
	XREF _close_UART1

	call _close_UART1				; close UART1.
.endif

	ret


;---------------------------------------------------------------------------------------
.ifdef _ZSL_PORT_USED
	include "gpio.inc"

.ifdef _MODEL_LARGE
	segment far_data
.endif
.ifdef _MODEL_SMALL
	segment near_data
.endif

.ifdef _ZSL_DEVICE_PORTA
	XDEF _portamask
_portamask:
	db PORTAMASK
.endif
.ifdef _ZSL_DEVICE_PORTB
	XDEF _portbmask
_portbmask:
	db PORTBMASK
.endif
.ifdef _ZSL_DEVICE_PORTC
	XDEF _portcmask
_portcmask:
	db PORTCMASK
.endif
.ifdef _ZSL_DEVICE_PORTD
	XDEF _portdmask
_portdmask:
	db PORTDMASK
.endif
.ifdef _ZSL_DEVICE_PORTE
	XDEF _portemask
_portemask:
	db PORTEMASK
.endif
.ifdef _ZSL_DEVICE_PORTF
	XDEF _portfmask
_portfmask:
	db PORTFMASK
.endif
.ifdef _ZSL_DEVICE_PORTG
	XDEF _portgmask
_portgmask:
	db PORTGMASK
.endif
.ifdef _ZSL_DEVICE_PORTH
	XDEF _porthmask
_porthmask:
	db PORTHMASK
.endif


.endif ;if PORT used

;---------------------------------------------------------------------------------------
.ifdef _MODEL_LARGE
	segment far_data
.endif
.ifdef _MODEL_SMALL
	segment near_data
.endif

	XDEF _g_simulate
	XDEF _g_clock0
	XDEF _g_clock1
	XREF _zsl_g_clock_xdefine

_g_simulate:
.ifdef _SIMULATE
	db 1
.else
	db 0
.endif

_g_clock0:
_g_clock1:							; The clock value to be used in the UARTs.
	db _zsl_g_clock_xdefine>>24
	db _zsl_g_clock_xdefine>>16
	db _zsl_g_clock_xdefine>>8
	db _zsl_g_clock_xdefine



; End of File

