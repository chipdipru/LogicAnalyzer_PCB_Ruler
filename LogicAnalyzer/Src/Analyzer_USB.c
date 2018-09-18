/*
********************************************************************************
* COPYRIGHT(c) ЗАО «ЧИП и ДИП», 2018
* 
* Программное обеспечение предоставляется на условиях «как есть» (as is).
* При распространении указание автора обязательно.
********************************************************************************
*/


#include "Analyzer_USB.h"
#include "usbd_custom_hid_core.h"
#include "usbd_usr.h"


USB_CORE_HANDLE  USB_Device_dev;
uint8_t PrevXferDone = 1;
static uint8_t USBStatus = ANALYZER_USB_IDLE;
static uint8_t USBDataBuf[USB_MESSAGE_LENGTH];


void Analyzer_USB_Init()
{
  RCC->APB2ENR |= RCC_APB2ENR_SYSCFGCOMPEN;
  SYSCFG->CFGR1 |= 1 << 4;
  USBD_Init(&USB_Device_dev, &USR_desc, &USBD_HID_cb, &USR_cb);  
}
//------------------------------------------------------------------------------
void Analyzer_USB_RecPacket(uint8_t *Packet)
{
  for (uint8_t i = 0; i < USB_MESSAGE_LENGTH; i++)
      USBDataBuf[i] = *(Packet + i);
  
  switch(*(USBDataBuf + USB_CMD_POS))
  {
    case USB_CMD_START_CAPTURE:
      USBStatus |= ANALYZER_USB_START_CAPTURE;
    break;
    
    default:
    break;
  }
}
//------------------------------------------------------------------------------
void Analyzer_USB_SendData(uint8_t *Data)
{
  if ((PrevXferDone) && (USB_Device_dev.dev.device_status == USB_CONFIGURED))
  {    
    USBD_HID_SendReport(&USB_Device_dev, Data, USB_MESSAGE_LENGTH);
    PrevXferDone = 0;
  } 
}
//------------------------------------------------------------------------------
uint8_t* Analyzer_USB_GetStatus()
{
  return &USBStatus;
}
//------------------------------------------------------------------------------
void Analyzer_USB_ClearStatus(uint8_t StatusFlag)
{
  USBStatus &= ~StatusFlag;
}
//------------------------------------------------------------------------------
uint8_t* Analyzer_USB_GetPacket()
{
  return USBDataBuf;
}
//------------------------------------------------------------------------------
uint8_t* Analyzer_USB_IsReadyToSend()
{
  return &PrevXferDone;
}



