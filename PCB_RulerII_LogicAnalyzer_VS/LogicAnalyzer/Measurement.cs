/*
********************************************************************************
* COPYRIGHT(c) ЗАО «ЧИП и ДИП», 2018
* 
* Программное обеспечение предоставляется на условиях «как есть» (as is).
* При распространении указание автора обязательно.
********************************************************************************
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogicAnalyzer
{
    class Measurement
    {
        private const int LEVEL = -2;
        private const int ONE_PULSE = -1;
        private const int MANY_PULSES = 0;

        private PointCollection InputData;
        private double TimeResolution;
        private int PulseHighLevel;


        public Measurement(PointCollection GraphSegment, double TimeRes, double ScaleFactor, int HighLevel)
        {
            Result = new StackPanel();

            if (GraphSegment.Count != 0)
            {
                InputData = new PointCollection();
                for (int PointIndex = 0; PointIndex < GraphSegment.Count; PointIndex++)
                {
                    Point NewPoint = new Point(GraphSegment[PointIndex].X / ScaleFactor, GraphSegment[PointIndex].Y);
                    InputData.Add(NewPoint);
                }

                TimeResolution = TimeRes;
                PulseHighLevel = HighLevel;
                
                Result.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFADFF2F"));
                Result.Name = "MeasureGrid";
                Result.Opacity = 0.65;// 0.75;
                Result.Orientation = Orientation.Vertical;
                Result.RenderTransform = new ScaleTransform(1, -1);
                double Duration = GetDuration(InputData[0].X, InputData[InputData.Count - 1].X);
                AddData("Ширина: " + Duration.ToString("0.000") + " мкс");

                int EdgeCount;
                int RisingEdgeCount;
                int FallingEdgeCount;
                int PulseCntResult = PulseCounter(out EdgeCount, out RisingEdgeCount, out FallingEdgeCount);

                if (PulseCntResult == ONE_PULSE)
                {
                    double DutyCycle;
                    double Frequency;
                    double Period;
                    double PulseWidth;

                    Period = Duration;
                    Frequency = 1000 / Period; //кГц
                    GetPulseWidth(out PulseWidth);
                    DutyCycle = 100 * PulseWidth / Period; //%
                                        
                    AddData("Период: " + Period.ToString("0.000") + " мкс");                    
                    AddData("Частота: " + Frequency.ToString("0.000") + " кГц");
                    AddData("Длительность импульса: " + DutyCycle.ToString("0.000") + " % / " + PulseWidth.ToString("0.000") + " мкс");
                }

                else if (PulseCntResult == MANY_PULSES)
                {
                    AddData("Количество фронтов: " + EdgeCount.ToString());
                    AddData("Передних фронтов: " + RisingEdgeCount.ToString());
                    AddData("Задних фронтов: " + FallingEdgeCount.ToString());
                }
            }
        }

        public StackPanel Result { get; set; }

        private double GetDuration(double X1, double X2)
        {
            return ((X2 - X1) * TimeResolution);
        }

        private int PulseCounter(out int Edges, out int Rising, out int Falling)
        {
            int ReturnCode = LEVEL;
            Edges = 0;
            Rising = 0;
            Falling = 0;

            for (int DataPoint = 0; DataPoint < (InputData.Count - 1); DataPoint++)
            {
                if (InputData[DataPoint].Y > InputData[DataPoint + 1].Y)
                    Falling++;
                else if (InputData[DataPoint].Y < InputData[DataPoint + 1].Y)
                    Rising++;
            }

            Edges = Rising + Falling;

            if (Edges == 0)
                return ReturnCode;

            if ((Edges == 3)
             && (InputData[0].X == InputData[1].X)
             && (InputData[InputData.Count - 2].X == InputData[InputData.Count - 1].X))
            {
                ReturnCode = ONE_PULSE;
                return ReturnCode;
            }
            
            ReturnCode = MANY_PULSES;

            return ReturnCode;
        }

        private void GetPulseWidth(out double PulseWidth)
        {
            PulseWidth = 0;

            bool StartPoint = false;
            double HighLevelStart = 0;

            for (int PointIndex = 0; PointIndex < (InputData.Count - 1); PointIndex++)
            {
                if ((InputData[PointIndex].Y == PulseHighLevel) && (InputData[PointIndex + 1].Y == PulseHighLevel))
                {
                    if (StartPoint == false)
                    {
                        StartPoint = true;
                        HighLevelStart = InputData[PointIndex].X;
                    }
                }

                else if ((InputData[PointIndex].Y == PulseHighLevel) && ((InputData[PointIndex + 1].Y != PulseHighLevel) || ((PointIndex + 1) == (InputData.Count - 1))))
                {
                    if (StartPoint == true)
                    {
                        StartPoint = false;
                        PulseWidth += InputData[PointIndex].X - HighLevelStart;
                        break;
                    }
                }
            }

            PulseWidth *= TimeResolution;
        }

        private void AddData(string DataString)
        {
            TextBlock NewDataTextBlock = new TextBlock();
            NewDataTextBlock.FontSize = 14;
            NewDataTextBlock.Text = DataString;
            NewDataTextBlock.VerticalAlignment = VerticalAlignment.Bottom;

            //FallingEdgeCountTextBlock.Margin = new Thickness(0, 2, 0, 0);
            Result.Children.Add(NewDataTextBlock);
        }
    }
}
