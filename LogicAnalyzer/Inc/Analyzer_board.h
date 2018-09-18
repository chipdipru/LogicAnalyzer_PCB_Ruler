/*
********************************************************************************
* COPYRIGHT(c) ЗАО «ЧИП и ДИП», 2017
* 
* Программное обеспечение предоставляется на условиях «как есть» (as is).
* При распространении указание автора обязательно.
********************************************************************************
*/


#ifndef __ANALYZER_BOARD_H
#define __ANALYZER_BOARD_H

#include "stm32f042x6.h"


#define       CAPTURE_GPIO                 GPIOA
#define       CAPTURE_MASK                 0xFF

#define       LINE_INTERRUPT_MASK          CAPTURE_MASK 

//ПДП (DMA)
#define       DMA_FOR_CAPTURE              DMA1
#define       DMA_FOR_CAPTURE_ENR          AHBENR
#define       DMA_FOR_CAPTURE_CLK_EN       RCC_AHBENR_DMAEN
#define       DMA_CNL_FOR_CAPTURE          DMA1_Channel3
#define       DMA_CNL_FOR_CAPTURE_IRQ      DMA1_Channel2_3_IRQn

//таймер для преобразований АЦП
#define       CAPTURE_TIMER                TIM3
#define       CAPTURE_TIMER_ENR            APB1ENR
#define       CAPTURE_TIMER_CLK_EN         RCC_APB1ENR_TIM3EN
#define       CAPTURE_TIMER_IRQ            TIM3_IRQn


#define       CAPTURE_BUFFER_SIZE          4096
#define       CHANNELS_COUNT               8

#define       CAPTURE_COMPLETE             (1 << 0)



void AnalyzerInit();

void DMA_FOR_CAPTURE_ISR(void);

void EXTI0_1_IRQHandler(void);

void EXTI2_3_IRQHandler(void);

void EXTI4_15_IRQHandler(void);


#endif //__ANALYZER_BOARD_H


