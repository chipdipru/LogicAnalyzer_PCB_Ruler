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

namespace LogicAnalyzer
{
    public class InterfaceLine
    {
        public static readonly int CLKLineType = 0;
        public static readonly int DataLineType = 1;

        private int linenum;

        public InterfaceLine(int LineNumber)
        {
            linenum = LineNumber;
        }

        public int Type { get; set; }
        public int Number { get { return linenum; } }
    }
}
