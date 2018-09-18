/*
********************************************************************************
* COPYRIGHT(c) ЗАО «ЧИП и ДИП», 2018
* 
* Программное обеспечение предоставляется на условиях «как есть» (as is).
* При распространении указание автора обязательно.
********************************************************************************
*/

#include "Analyzer_board.h"
#include "Analyzer_USB.h"


uint8_t Capture[CAPTURE_BUFFER_SIZE];
volatile uint8_t Status = 0;
uint16_t LevelTrigValue = 0;
uint16_t LevelTrigChnls = 0;
uint16_t EdgeTrigChnls = 0;


int main()
{
  AnalyzerInit();
  
  for(;;)
  {
    if ((*Analyzer_USB_GetStatus()) == ANALYZER_USB_START_CAPTURE)
    {
      uint8_t *NewData = (uint8_t *)Analyzer_USB_GetPacket();
      uint16_t SamplesToCapture = (*(NewData + CAPTURE_SAMPLE_OFFSET)) | ((*(NewData + CAPTURE_SAMPLE_OFFSET + 1)) << 8);  

      Status &= ~CAPTURE_COMPLETE;
      DMA_CNL_FOR_CAPTURE->CNDTR = SamplesToCapture;
      DMA_CNL_FOR_CAPTURE->CCR |= DMA_CCR_EN;
      
      CAPTURE_TIMER->SR = 0;
      CAPTURE_TIMER->PSC = (*(NewData + CAPTURE_TIM_PSC_OFFSET));
      CAPTURE_TIMER->ARR = (*(NewData + CAPTURE_TIM_ARR_OFFSET));
      CAPTURE_TIMER->CNT = CAPTURE_TIMER->ARR;
      CAPTURE_TIMER->DIER = TIM_DIER_UDE;
      
      if ((*(NewData + CAPTURE_TRIG_ENABLE_OFFSET)) == CAPTURE_TRIG_ENABLE)
      {    
        LevelTrigValue = 0;
        LevelTrigChnls = 0;
        EXTI->RTSR &= ~LINE_INTERRUPT_MASK;
        EXTI->FTSR &= ~LINE_INTERRUPT_MASK;
        uint8_t OnlyLevelTrigger = 1;
        uint8_t TrigSetBytes = CAPTURE_TRIG_BYTES_COUNT;
        uint8_t ChnlNumOffset = 0;
        uint16_t InterruptMask = CAPTURE_MASK;
                  
        for (uint8_t TrigByte = 0; TrigByte < TrigSetBytes; TrigByte++)
        {
          uint8_t TrigSettings = (*(NewData + CAPTURE_TRIG_SET_OFFSET + TrigByte));
                       
          for (uint8_t ChnlPart = 0; ChnlPart < 2; ChnlPart++)
          {
            uint8_t ChnlNum = 2 * TrigByte + ChnlPart + ChnlNumOffset;
            uint8_t ChnlTrigger = (TrigSettings >> (4 * ChnlPart)) & 0x0F;
              
            if (ChnlTrigger != TRIGGER_NONE)
            { 
              switch(ChnlTrigger)
              {
                case TRIGGER_LOW_LEVEL:
                  LevelTrigChnls |= 1 << ChnlNum;
                break;
                  
                case TRIGGER_HIGH_LEVEL:
                  LevelTrigValue |= 1 << ChnlNum;
                  LevelTrigChnls |= 1 << ChnlNum;
                break;
                  
                case TRIGGER_RISING_EDGE:
                  EXTI->RTSR |= 1 << ChnlNum;
                  OnlyLevelTrigger = 0;
                break;
                  
                case TRIGGER_FALLING_EDGE:
                  EXTI->FTSR |= 1 << ChnlNum;
                  OnlyLevelTrigger = 0;
                break;
                  
                case TRIGGER_ANY_EDGE:
                  EXTI->RTSR |= 1 << ChnlNum;
                  EXTI->FTSR |= 1 << ChnlNum;
                  OnlyLevelTrigger = 0;
                break;
                  
                default:
                break;
              }
            }
          }    
        }
          
        if (OnlyLevelTrigger == 1) //у всех каналов триггеры по уровню
        {
          while ((CAPTURE_GPIO->IDR & LevelTrigChnls) != LevelTrigValue);
          CAPTURE_TIMER->CR1 |= TIM_CR1_CEN;
        }
          
        else
        {
          EdgeTrigChnls = EXTI->RTSR;
          EdgeTrigChnls = (EdgeTrigChnls | EXTI->FTSR) & InterruptMask;
          EXTI->IMR |= EdgeTrigChnls;
        }
      }
  
      else
        CAPTURE_TIMER->CR1 |= TIM_CR1_CEN;

      while((Status & CAPTURE_COMPLETE) != CAPTURE_COMPLETE)
      {
        __WFI();
      }
      
      uint8_t PacketsToSend = SamplesToCapture / USB_MESSAGE_LENGTH;
            
      for (uint8_t Packet = 0; Packet < PacketsToSend; Packet++)
      {
        while((*Analyzer_USB_IsReadyToSend()) != 1);
        Analyzer_USB_SendData(&Capture[USB_MESSAGE_LENGTH * Packet]);
      }
      
      Analyzer_USB_ClearStatus(ANALYZER_USB_START_CAPTURE);
    }
  }
}
//------------------------------------------------------------------------------
void AnalyzerInit()
{
  //HSI, PLL, 48 MHz
  FLASH->ACR = FLASH_ACR_PRFTBE | (uint32_t)FLASH_ACR_LATENCY;
  // HCLK = SYSCLK / 1
  RCC->CFGR |= (uint32_t)RCC_CFGR_HPRE_DIV1;
  // PCLK2 = HCLK / 1
  RCC->CFGR |= (uint32_t)RCC_CFGR_PPRE_DIV1;
  // частота PLL: (HSI / 2) * 12 = (8 / 2) * 12 = 48 (МГц)
  //RCC->CFGR &= (uint32_t)((uint32_t)~(RCC_CFGR_PLLSRC | RCC_CFGR_PLLXTPRE | RCC_CFGR_PLLMUL));
  RCC->CFGR |= (uint32_t)(RCC_CFGR_PLLSRC_HSI_DIV2 | RCC_CFGR_PLLMUL12);
  RCC->CR |= RCC_CR_PLLON;
  while((RCC->CR & RCC_CR_PLLRDY) == 0);
  //RCC->CFGR &= (uint32_t)((uint32_t)~(RCC_CFGR_SW));
  RCC->CFGR |= (uint32_t)RCC_CFGR_SW_PLL;
  while ((RCC->CFGR & (uint32_t)RCC_CFGR_SWS) != (uint32_t)RCC_CFGR_SWS_PLL);

  RCC->AHBENR |= RCC_AHBENR_GPIOAEN;
  
  GPIOA->OSPEEDR |= (3 << (2 * 0)) | (3 << (2 * 1)) | (3 << (2 * 2)) | (3 << (2 * 3)) | \
                    (3 << (2 * 4)) | (3 << (2 * 5)) | (3 << (2 * 6)) | (3 << (2 * 7));
  
  GPIOA->PUPDR |= (2 << (2 * 0)) | (2 << (2 * 1)) | (2 << (2 * 2)) | (2 << (2 * 3)) | \
                  (2 << (2 * 4)) | (2 << (2 * 5)) | (2 << (2 * 6)) | (2 << (2 * 7));
  
  NVIC_EnableIRQ(EXTI0_1_IRQn);
  NVIC_SetPriority(EXTI0_1_IRQn,0);  
  NVIC_EnableIRQ(EXTI2_3_IRQn);
  NVIC_SetPriority(EXTI2_3_IRQn,0);
  NVIC_EnableIRQ(EXTI4_15_IRQn);
  NVIC_SetPriority(EXTI4_15_IRQn,0);
  
  //ПДП (DMA)
  RCC->DMA_FOR_CAPTURE_ENR |= DMA_FOR_CAPTURE_CLK_EN;
  DMA_CNL_FOR_CAPTURE->CPAR = (uint32_t) (&(GPIOA->IDR));
  DMA_CNL_FOR_CAPTURE->CMAR = (uint32_t)(Capture);
  DMA_CNL_FOR_CAPTURE->CCR = DMA_CCR_MINC | DMA_CCR_TEIE | DMA_CCR_TCIE;
  NVIC_EnableIRQ(DMA_CNL_FOR_CAPTURE_IRQ);
  NVIC_SetPriority(DMA_CNL_FOR_CAPTURE_IRQ,0);
  
  //таймер для захвата
  RCC->CAPTURE_TIMER_ENR |= CAPTURE_TIMER_CLK_EN;
  
  Analyzer_USB_Init();
}
//------------------------------------------------------------------------------
void DMA_FOR_CAPTURE_ISR(void)
{
  if ((DMA_FOR_CAPTURE->ISR & DMA_ISR_TEIF3) == DMA_ISR_TEIF3)
  {
    DMA_CNL_FOR_CAPTURE->CCR |= DMA_CCR_EN;
  }
  
  else if ((DMA_FOR_CAPTURE->ISR & DMA_ISR_TCIF3) == DMA_ISR_TCIF3)
  {
    CAPTURE_TIMER->CR1 &= ~TIM_CR1_CEN;
    DMA_FOR_CAPTURE->IFCR |= DMA_IFCR_CGIF3;
    DMA_CNL_FOR_CAPTURE->CCR &= ~DMA_CCR_EN;
    CAPTURE_TIMER->DIER &= ~TIM_DIER_UDE;
    Status |= CAPTURE_COMPLETE;      
  }
}

//------------------------------------------------------------------------------
void EXTI0_1_IRQHandler(void)
{ 
  if (((EXTI->PR & EdgeTrigChnls) == EdgeTrigChnls)
   && ((CAPTURE_GPIO->IDR & LevelTrigChnls) == LevelTrigValue))
  {
    CAPTURE_TIMER->CR1 |= TIM_CR1_CEN;
    EXTI->IMR &= ~EdgeTrigChnls;
  }
  
  EXTI->PR |= EdgeTrigChnls;
}
//------------------------------------------------------------------------------
void EXTI2_3_IRQHandler(void)
{ 
  if (((EXTI->PR & EdgeTrigChnls) == EdgeTrigChnls)
   && ((CAPTURE_GPIO->IDR & LevelTrigChnls) == LevelTrigValue))
  {
    CAPTURE_TIMER->CR1 |= TIM_CR1_CEN;
    EXTI->IMR &= ~EdgeTrigChnls;
  }
  
  EXTI->PR |= EdgeTrigChnls;
}
//------------------------------------------------------------------------------
void EXTI4_15_IRQHandler(void)
{ 
  if (((EXTI->PR & EdgeTrigChnls) == EdgeTrigChnls)
   && ((CAPTURE_GPIO->IDR & LevelTrigChnls) == LevelTrigValue))
  {
    CAPTURE_TIMER->CR1 |= TIM_CR1_CEN;
    EXTI->IMR &= ~EdgeTrigChnls;
  }
  
  EXTI->PR |= EdgeTrigChnls;
}





