/*
********************************************************************************
* COPYRIGHT(c) ЗАО «ЧИП и ДИП», 2017
* 
* Программное обеспечение предоставляется на условиях «как есть» (as is).
* При распространении указание автора обязательно.
********************************************************************************
*/




#ifndef __ANALYZER_USB_H
#define __ANALYZER_USB_H

#include "Analyzer_board.h"


#define       USB_MESSAGE_LENGTH           0x40 //длина пакета USB, максимум 64 байта


#define       USB_REPORT_ID_POS            0
#define       USB_CMD_POS                  1 //индекс команды в посылке

#define       USB_CMD_ID                   1

//команды
#define       USB_CMD_START_CAPTURE        1


#define       CAPTURE_SYNC_OFFSET          (USB_CMD_POS + 1)
#define       CAPTURE_TIM_PSC_OFFSET       (CAPTURE_SYNC_OFFSET + 1)
#define       CAPTURE_TIM_ARR_OFFSET       (CAPTURE_TIM_PSC_OFFSET + 1)
#define       CAPTURE_SAMPLE_OFFSET        (CAPTURE_TIM_ARR_OFFSET + 1)
#define       CAPTURE_TRIG_ENABLE_OFFSET   (CAPTURE_SAMPLE_OFFSET + 2)
#define       CAPTURE_TRIG_MODE_OFFSET     (CAPTURE_TRIG_ENABLE_OFFSET + 1)
#define       CAPTURE_TRIG_SET_OFFSET      (CAPTURE_TRIG_MODE_OFFSET + 1)

#define       CAPTURE_SYNC_INTERNAL        0
#define       CAPTURE_SYNC_EXTERNAL        1
#define       CAPTURE_TRIG_BYTES_COUNT     4
#define       CAPTURE_TRIG_DISABLE         0
#define       CAPTURE_TRIG_ENABLE          1
#define       CAPTURE_TRIG_MODE_CHANNELS   0
#define       CAPTURE_TRIG_MODE_EXT_LINES  1
#define       CAPTURE_TRIG_EXT_BYTES_COUNT 1

#define       TRIGGER_NONE                 0
#define       TRIGGER_LOW_LEVEL            1
#define       TRIGGER_HIGH_LEVEL           2
#define       TRIGGER_RISING_EDGE          3
#define       TRIGGER_FALLING_EDGE         4
#define       TRIGGER_ANY_EDGE             5
        

//Status
#define       ANALYZER_USB_IDLE            0
#define       ANALYZER_USB_START_CAPTURE   (1 << 0)



void Analyzer_USB_Init();

void Analyzer_USB_RecPacket(uint8_t *Packet);

void Analyzer_USB_SendData(uint8_t *Data);

uint8_t* Analyzer_USB_GetStatus();

void Analyzer_USB_ClearStatus(uint8_t StatusFlag);

uint8_t* Analyzer_USB_GetPacket();

uint8_t* Analyzer_USB_IsReadyToSend();


#endif //__ANALYZER_USB_H


