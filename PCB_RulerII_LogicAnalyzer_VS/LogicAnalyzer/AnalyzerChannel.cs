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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LogicAnalyzer
{
    class AnalyzerChannel
    {        
        private PointCollection CapturedData = new PointCollection();
        private double XOffset = 0;
        private PointCollection graphpoints = new PointCollection();
       
        
        public PointCollection GraphPoints
        {
            get { return graphpoints; }            
        }

        public int Index { get; set; }
        public int Start_Y { get; set; }
        public int PulseHeight { get; set; }
        public string Trigger { get; set; }
        public string Name { get; set; }

        public void RedrawGraph(int StartPoint, int PointsCount)
        {
            PointCollection DataToPlot = new PointCollection();

            if ((StartPoint < 0) || (StartPoint >= CapturedData.Count))
                return;
                        
            XOffset = CapturedData[StartPoint].X;

            for (int PointIndex = 0; PointIndex < PointsCount; PointIndex++)
            {
                if ((PointIndex + StartPoint) == CapturedData.Count)
                    break;
                Point NewPoint = new Point(CapturedData[PointIndex + StartPoint].X - XOffset, CapturedData[PointIndex + StartPoint].Y);
                DataToPlot.Add(NewPoint);
            }

            for (int PointIndex = 0; PointIndex < (DataToPlot.Count - 1); PointIndex++)
            {
                if (DataToPlot[PointIndex].X != DataToPlot[PointIndex + 1].X)
                {
                    if (((DataToPlot[PointIndex].Y == Start_Y) && (DataToPlot[PointIndex + 1].Y == (Start_Y + PulseHeight)))
                     || ((DataToPlot[PointIndex].Y == (Start_Y + PulseHeight)) && (DataToPlot[PointIndex + 1].Y == Start_Y)))
                        DataToPlot.Insert(PointIndex + 1, new Point(DataToPlot[PointIndex + 1].X, DataToPlot[PointIndex].Y));
                }
            }

            graphpoints = DataToPlot;
        }

        public void CaptureData(PointCollection InputData)
        {
            CapturedData = InputData;
        }

        public PointCollection GetCapturedData()
        {
            return CapturedData;
        }

        public double Get_X_Offset()
        {
            return XOffset;
        }

        public void Scale(double NewScaleFactor)
        {
            double PrevScaleFactor = 1;

            for (int RawPoint = 0; RawPoint < CapturedData.Count; RawPoint++)
            {
                PrevScaleFactor = (CapturedData[RawPoint + 1].X - CapturedData[RawPoint].X);

                if (PrevScaleFactor != 0)
                    break;
            }

            for (int RawPoint = 0; RawPoint < CapturedData.Count; RawPoint++)
            {
                Point NewPoint = CapturedData[RawPoint];
                NewPoint.X /= PrevScaleFactor;
                NewPoint.X *= NewScaleFactor;
                CapturedData[RawPoint] = NewPoint;
            }
        }                
    }
}
