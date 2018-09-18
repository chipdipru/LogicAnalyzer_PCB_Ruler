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
using System.Threading.Tasks;
using System.Windows.Media;

namespace LogicAnalyzer
{
    class I2C : IHardwareInterface
    {
        private static readonly string StartItem = "S";
        private static readonly string StartRepItem = "Sr";
        private static readonly string AddressItem = "Адрес ";
        private static readonly string ACKItem = "ACK";
        private static readonly string NACKItem = "NACK";
        private static readonly string ReadItem = "R";
        private static readonly string WriteItem = "W";
        private static readonly string StopItem = "P";
        private static readonly string StartItemBackground = "Green";
        private static readonly string StartRepItemBackground = StartItemBackground;
        private static readonly string StopItemBackground = "Red";
        private static readonly int StartStopItemWidth = 3;
        private static readonly string DataBackground = "DarkGoldenrod";
        private static readonly string AddrBackground = "Blue";
        private static readonly double TimeCoef = 1;
        private static readonly int SCLLineIndex = 0;
        private static readonly int SDALineIndex = 1;


        private ObservableCollection<InterfaceItem> I2C_BitsObserv = new ObservableCollection<InterfaceItem>();
        private ObservableCollection<InterfaceItem> I2C_PartsObserv = new ObservableCollection<InterfaceItem>();
        private Collection<int> SearchCollection = new Collection<int>();
        private int MatchingItemsCounter = 0;
        private List<InterfaceLine> InterfaceLines = new List<InterfaceLine>();
        private List<int> datalinesnumbers = new List<int>();
        private List<int> alllinesnumbers = new List<int>();
        

        public string Name { get; set; }
        public string Type { get { return "I2C"; } }
        public List<int> DataLinesNumbers { get { return datalinesnumbers; } }
        public List<int> AllLinesNumbers { get { return alllinesnumbers; } }
        public ObservableCollection<InterfaceItem> InterfaceParts { get { return I2C_PartsObserv; } }
        public ObservableCollection<InterfaceItem> InterfaceBits { get { return I2C_BitsObserv; } }
        public int MatchedItemsCount { get { return SearchCollection.Count; } }
        public int CurrentMatchedItemIndex
        {            
            get
            {
                if (SearchCollection.Count > 0)
                    return SearchCollection[MatchingItemsCounter];
                else
                    return 0;
            }
        }
        public List<bool> InterfaceVisibility { get; set; }

        public void Initialize(List<InterfaceLine> InitParams)
        {
            InterfaceLines = InitParams;
            foreach(InterfaceLine line in InitParams)
            {
                alllinesnumbers.Add(line.Number);
                if (line.Type == InterfaceLine.DataLineType)
                    datalinesnumbers.Add(line.Number);
            }
        }

        public void Analyze(List<PointCollection> SignalPoints, double TimeRes)
        {
            Collection<InterfaceItem> I2C_Bits = new Collection<InterfaceItem>();
            Collection<InterfaceItem> I2C_Parts = new Collection<InterfaceItem>();

            bool AddrByte = true;
            I2C_BitsObserv.Clear();
            I2C_PartsObserv.Clear();
            
            for (int point = 0; point < (SignalPoints[SCLLineIndex].Count - 1); point++)
            {
                if ((SignalPoints[SCLLineIndex][point].Y == MainWindow.PULSE_HIGH) && (SignalPoints[SCLLineIndex][point + 1].Y == MainWindow.PULSE_HIGH) && (SignalPoints[SDALineIndex][point].Y > SignalPoints[SDALineIndex][point + 1].Y))
                {
                    I2C_Parts.Add(new InterfaceItem { Item = StartItem, StartPoint = point, Background = StartItemBackground, Width = StartStopItemWidth, CaptureTime = TimeRes * point * TimeCoef });
                    I2C_Bits.Add(I2C_Parts[I2C_Parts.Count - 1]);
                    point++;

                    if(WaitForLevelChange(SignalPoints[SCLLineIndex], ref point, MainWindow.PULSE_HIGH))
                        break;

                    if (WaitForLevelChange(SignalPoints[SCLLineIndex], ref point, MainWindow.PULSE_LOW))
                        break;
                    
                    while (point < SignalPoints[SCLLineIndex].Count)
                    {
                        string I2CItem = "0";
                        byte I2CByte = 0;
                        int I2CItemStartPoint = 0;


                        for (int pulse = 0; pulse < 8; pulse++)
                        {                            
                            if (SignalPoints[SDALineIndex][point].Y == MainWindow.PULSE_LOW)
                            {
                                I2CItem = "0";
                                if ((pulse == 7) && (AddrByte == true))
                                    I2CItem = WriteItem;
                            }

                            else
                            {
                                I2CItem = "1";                                
                                if ((pulse == 7) && (AddrByte == true))
                                {
                                    I2CItem = ReadItem;             
                                }                                    
                                else
                                    I2CByte |= (byte)(1 << (7 - pulse));
                            }
                                                                                        
                            I2C_Bits.Add(new InterfaceItem(I2CItem, point));
                            if (pulse == 0)
                                I2CItemStartPoint = point;
                            
                            if (pulse == 7)
                            {
                                I2C_Parts.Add(new InterfaceItem { Item = "0x" + I2CByte.ToString("X"), StartPoint = I2CItemStartPoint, Background = DataBackground, CaptureTime = TimeRes * I2CItemStartPoint * TimeCoef });
                                
                                if (AddrByte == true)
                                {
                                    I2CByte = (byte)(I2CByte >> 1);
                                    I2C_Parts[I2C_Parts.Count - 1].Item = AddressItem + "0x" + I2CByte.ToString("X") + "+" + I2CItem;
                                    I2C_Parts[I2C_Parts.Count - 1].Background = AddrBackground;
                                }                                  
                            }

                            if (WaitForLevelChange(SignalPoints[SCLLineIndex], ref point, MainWindow.PULSE_HIGH))
                                break;

                            if (WaitForLevelChange(SignalPoints[SCLLineIndex], ref point, MainWindow.PULSE_LOW))
                                break;
                        }

                        AddrByte = false;

                        if (point == SignalPoints[SCLLineIndex].Count)
                            break;

                        I2CItem = ACKItem;
                        if (SignalPoints[SDALineIndex][point].Y == MainWindow.PULSE_HIGH)
                            I2CItem = NACKItem;
                        I2C_Bits.Add(new InterfaceItem(I2CItem, point));
                        I2C_Parts[I2C_Parts.Count - 1].Item += "+" + I2CItem;

                        WaitForLevelChange(SignalPoints[SCLLineIndex], ref point, MainWindow.PULSE_HIGH);

                        I2C_Parts[I2C_Parts.Count - 1].Width = point - I2C_Parts[I2C_Parts.Count - 1].StartPoint;
                        
                        if (WaitForLevelChange(SignalPoints[SCLLineIndex], ref point, MainWindow.PULSE_LOW))
                            break;

                        while (SignalPoints[SCLLineIndex][point].Y == MainWindow.PULSE_HIGH)
                        {
                            point++;
                            if (point >= SignalPoints[SCLLineIndex].Count - 1)
                                break;

                            if ((SignalPoints[SCLLineIndex][point].Y == MainWindow.PULSE_HIGH) && (SignalPoints[SCLLineIndex][point + 1].Y == MainWindow.PULSE_HIGH) && (SignalPoints[SDALineIndex][point].Y < SignalPoints[SDALineIndex][point + 1].Y))
                            {                                
                                I2C_Parts.Add(new InterfaceItem { Item = StopItem, StartPoint = point, Background = StopItemBackground, Width = StartStopItemWidth, CaptureTime = TimeRes * point * TimeCoef });
                                I2C_Bits.Add(I2C_Parts[I2C_Parts.Count - 1]);
                                break;
                            }

                            else if ((SignalPoints[SCLLineIndex][point].Y == MainWindow.PULSE_HIGH) && (SignalPoints[SCLLineIndex][point + 1].Y == MainWindow.PULSE_HIGH) && (SignalPoints[SDALineIndex][point].Y > SignalPoints[SDALineIndex][point + 1].Y))
                            {
                                I2C_Parts.Add(new InterfaceItem { Item = StartRepItem, StartPoint = point, Background = StartRepItemBackground, Width = StartStopItemWidth, CaptureTime = TimeRes * point * TimeCoef });
                                I2C_Bits.Add(I2C_Parts[I2C_Parts.Count - 1]);
                                break;
                            }
                        }

                        if (I2C_Bits[I2C_Bits.Count - 1].Item == StopItem)
                        {
                            AddrByte = true;
                            break;
                        }

                        else if (I2C_Bits[I2C_Bits.Count - 1].Item == StartRepItem)
                        {
                            AddrByte = true;

                            if (WaitForLevelChange(SignalPoints[SCLLineIndex], ref point, MainWindow.PULSE_HIGH))
                                break;

                            if (WaitForLevelChange(SignalPoints[SCLLineIndex], ref point, MainWindow.PULSE_LOW))
                                break;
                        }

                        else
                        {
                            point--;
                            while (SignalPoints[SCLLineIndex][point].Y == MainWindow.PULSE_HIGH)
                                point--;
                            point++;
                        }    
                    }
                }                
            }
            
            I2C_BitsObserv = new ObservableCollection<InterfaceItem>(I2C_Bits);
            I2C_PartsObserv = new ObservableCollection<InterfaceItem>(I2C_Parts);
        }

        private bool WaitForLevelChange(PointCollection InputSignal, ref int StartPoint, int CurrentLevel)
        {
            bool IsSignalEnd = false;

            if (StartPoint >= InputSignal.Count)
                IsSignalEnd = true;
            else
            {
                while (InputSignal[StartPoint].Y == CurrentLevel)
                {
                    StartPoint++;
                    if (StartPoint == InputSignal.Count)
                    {
                        IsSignalEnd = true;
                        break;
                    }
                }
            }

            return IsSignalEnd;
        }

        public void SearchItem(string ItemToSearch)
        {
            MatchingItemsCounter = 0;
            SearchCollection.Clear();

            for (int i = 0; i < I2C_PartsObserv.Count; i++)
            {
                if (I2C_PartsObserv[i].Item.IndexOf(ItemToSearch) != -1)
                    SearchCollection.Add(i);
            }
        }

        public void NextMatchItem()
        {
            MatchingItemsCounter++;
            if (MatchingItemsCounter == SearchCollection.Count)
                MatchingItemsCounter = 0;
        }

        public void PreviousMatchItem()
        {
            MatchingItemsCounter--;
            if (MatchingItemsCounter == -1)
                MatchingItemsCounter = SearchCollection.Count - 1;
        }
    }
}
