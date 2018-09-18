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

namespace LogicAnalyzer
{
    public class InterfaceItem
    {
        public string Item { get; set; }
        public int StartPoint { get; set; }
        public string Background { get; set; }
        public int Width { get; set; }
        public double CaptureTime { get; set; }

        public InterfaceItem(string TextItem, int PointIndex)
        {
            Item = TextItem;
            StartPoint = PointIndex;
        }

        public InterfaceItem()
        {

        }
    }
}
