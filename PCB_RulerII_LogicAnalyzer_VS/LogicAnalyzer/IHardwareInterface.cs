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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace LogicAnalyzer
{
    public interface IHardwareInterface
    {
        string Name { get; set; }
        string Type { get; }
        List<int> DataLinesNumbers { get; }
        List<int> AllLinesNumbers { get ; }
        ObservableCollection<InterfaceItem> InterfaceParts { get; }
        ObservableCollection<InterfaceItem> InterfaceBits { get; }
        int MatchedItemsCount { get; }
        int CurrentMatchedItemIndex { get; }
        List<bool> InterfaceVisibility { get; set; }

        void Initialize(List<InterfaceLine> InitParams);
        void Analyze(List<PointCollection> SignalPoints, double TimeRes);
        void SearchItem(string ItemToSearch);
        void NextMatchItem();
        void PreviousMatchItem();
    }
}
