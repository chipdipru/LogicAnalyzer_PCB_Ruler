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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LogicAnalyzer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const byte CHANNEL_COUNT = 8;
        private const int START_X_VALUE = 0;
        private const byte FRAME_UPDATE_PART = 3;
        private const int POINTS_COUNT = 512;
        private const byte GRAPHS_GAP = 55;
        private const byte ZOOM_OUT = 0;
        private const byte ZOOM_IN = 1;
        private static readonly double[] ScaleX = {0.5, 1, 3, 5, 10, 20, }; //шаги по оси х при мастабировании: 1, 3, 5, 10, 20
        private static readonly int[] MainTimeMarkStep = {50, 100, 90, 150, 200, 200, };
        private static readonly int[] SmallTimeMarkStep = {5, 10, 9, 15, 20, 20, };
        private static readonly double[] DescretTime = { 10, 2.5, 1.25, 1, 0.625, 0.5, 1.0 / 3.0, 0.25, 1.0 / 6.0, 0.125, }; //мкс
                                        //частота 100 кГц; 400 кГц; 800 кГц; 1 МГц; 1,6 МГц; 2 МГц; 3 МГц; 4 МГц; 6 МГц; 8 МГц; 
        private static readonly string[] TimeUnits = { "мкс", "мс", };
        private static readonly int[] SampleCount = { 128, 256, 512, 1024, 2048, 4096, };
        private static readonly string[] FreqStrings = { "100 кГц", "400 кГц", "800 кГц", "1 МГц", "1,6 МГц", "2 МГц", "3 МГц", "4 МГц", "6 МГц", "8 МГц", };
        private static readonly byte[] CaptureTimPsc = { 23, 11, 9, 7, 5, 5, 3, 3, 3, 2, };
        private static readonly byte[] CaptureTimArr = { 19, 9, 5, 5, 4, 3, 3, 2, 1, 1, };
        private static readonly string[] TriggersString = { "Х", "0", "1", "<", ">", "<>", };
        private const int TRIGGER_NONE = 0;
        private const int TRIGGER_LOW_LEVEL = 1;
        private const int TRIGGER_HIGH_LEVEL = 2;
        private const int TRIGGER_RISING_EDGE = 3;
        private const int TRIGGER_FALLING_EDGE = 4;
        private const int TRIGGER_ANY_EDGE = 5;

        private static readonly string TriggerHelp = "Установите триггеры для каналов. "
            + "Для активации выберите активировать триггеры. "
            + "Если триггеры установлены для нескольких каналов, захват начнется при одновременном детектировании всех активных триггеров. \n"
            + TriggersString[TRIGGER_NONE] +         " - триггер неактивен\n"
            + TriggersString[TRIGGER_LOW_LEVEL] +    " - триггер по низкому уровню\n"
            + TriggersString[TRIGGER_HIGH_LEVEL] +   " - триггер по высокому уровню\n"
            + TriggersString[TRIGGER_RISING_EDGE] +  " - триггер по переднему фронту\n"
            + TriggersString[TRIGGER_FALLING_EDGE] + " - триггер по заднему фронту\n"
            + TriggersString[TRIGGER_ANY_EDGE] +     " - триггер по любому фронту\n";

        private const int PULSE_HEIGHT = 30;
        private const int START_Y = 10;
        public const int PULSE_LOW = START_Y;
        public const int PULSE_HIGH = START_Y + PULSE_HEIGHT;
        private const Int32 MAIN_TIME_MARK_HEIGHT = 20;
        private const Int32 SMALL_TIME_MARK_HEIGHT = MAIN_TIME_MARK_HEIGHT / 2;
        private const double TOOLS_ELEMENT_OPACITY = 0.65;
        private static readonly string TOOLS_ELEMENT_COLOR = "#FFADFF2F";
        private static readonly string INTERFACE_TEXT_COLOR = "#FFFFFFFF";

        private const byte USB_CMD_PACKET_SIZE = 64;
        private const byte USB_REPORT_ID_POS = 0;
        private const byte USB_CMD_POS = 1;
        
        private const byte USB_CMD_ID = 1;
        private const byte USB_CMD_START_CAPTURE = 1;

        private const byte CAPTURE_SYNC_OFFSET = USB_CMD_POS + 1;
        private const byte CAPTURE_TIM_PSC_OFFSET = CAPTURE_SYNC_OFFSET + 1;
        private const byte CAPTURE_TIM_ARR_OFFSET = CAPTURE_TIM_PSC_OFFSET + 1;
        private const byte CAPTURE_SAMPLE_OFFSET = CAPTURE_TIM_ARR_OFFSET + 1;
        private const byte CAPTURE_TRIGGER_ENABLE_OFFSET = CAPTURE_SAMPLE_OFFSET + 2;
        private const byte CAPTURE_TRIGGER_MODE_OFFSET = CAPTURE_TRIGGER_ENABLE_OFFSET + 1;
        private const byte CAPTURE_TRIGGER_SET_OFFSET = CAPTURE_TRIGGER_MODE_OFFSET + 1;

        private const byte CAPTURE_SYNC_INTERNAL = 0;
        private const byte CAPTURE_MODE_EXTERNAL_CLK = 1;
        private const byte CAPTURE_TRIGGER_BYTES_COUNT = 4;
        private const byte CAPTURE_TRIGGER_DISABLE = 0;
        private const byte CAPTURE_TRIGGER_ENABLE = 1;
        private const byte CAPTURE_TRIGGER_MODE_CHANNELS = 0;
        private const byte CAPTURE_TRIGGER_MODE_EXT_LINES = 1;

        private const byte EXTRA_POINTS_COUNT_FOR_TRIGGER = 2;


        private int End_X_Value = 900;
        private int FrameStartPoint = 0;
        private int FramePoints = 0;
        private Int32 UpdatePoints = 0;
        private Int32 DescretTimeIndex = 0;
        private Int32 ScaleXIndex = 0;
        private int SamplesToCapture = SampleCount[2];
        private AnalyzerChannel[] Channels;
        
        private bool MeasuringActive = false;
        private bool LineActive = false;
        private Polyline PanPolyline = new Polyline();
        private Polyline ToolPolyline = null;
        private Boolean USBDevDetected;
        private ObservableCollection<IHardwareInterface> IAnalyzers;
        

        public MainWindow()
        {
            InitializeComponent();
     
            Channels = new AnalyzerChannel[CHANNEL_COUNT];
            IAnalyzers = new ObservableCollection<IHardwareInterface>();
            
            for (byte i = 0; i < CHANNEL_COUNT; i++)
            {
                Channels[i] = new AnalyzerChannel();
                Channels[i].Index = i;
                Channels[i].Name = "канал " + i.ToString();
                Channels[i].Trigger = TriggersString[0];
                Channels[i].Start_Y = START_Y;
                Channels[i].PulseHeight = PULSE_HEIGHT;
            }

            GraphsCanvas.MouseMove += GraphsCanvas_MouseMove_Panning;
            //demo starts
/*
            byte BitCnt = 0;
            for (byte Chnl = 1; Chnl < CHANNEL_COUNT; Chnl++)
            {
                BitCnt = Chnl;
                if (Chnl > 3)
                    BitCnt -= 4;

                PointCollection RawData = new PointCollection();

                for (UInt32 i = 0; i < POINTS_COUNT; i++)
                {
                    if (BitCnt <= 1)
                    {
                        Point NewPoint1 = new Point(i * ScaleX[ScaleXIndex], START_Y);
                        RawData.Add(NewPoint1);
                    }
                    else
                    {
                        Point NewPoint2 = new Point(i * ScaleX[ScaleXIndex], START_Y + PULSE_HEIGHT);
                        RawData.Add(NewPoint2);
                    }

                    BitCnt++;
                    if (BitCnt == 4)
                        BitCnt = 0;
                }

                Channels[Chnl].CaptureData(RawData);
            }

            PointCollection CapturedRawData = new PointCollection();
            for (UInt32 i = 0; i < POINTS_COUNT; i++)
                CapturedRawData.Add(new Point(i * ScaleX[ScaleXIndex], START_Y + PULSE_HEIGHT));
            CapturedRawData[120] = new Point(120 * ScaleX[ScaleXIndex], START_Y);
            Channels[0].CaptureData(CapturedRawData);
*/
            //demo ends
            
            ChnlInfo.ItemsSource = Channels;
            Samples.ItemsSource = SampleCount;
            Frequencies.ItemsSource = FreqStrings;
            InterfaceComboBox.ItemsSource = IAnalyzers;
            InterfaceComboBox.DisplayMemberPath = "Name";
            UpdateFrameParameters();
            
            ChnlsTrigHelpTextBox.Text = TriggerHelp;
            
            USBDevDetected = USB_device.Open();

            if (USBDevDetected)
            {

            }
        }
        
        private void GraphMeasuring_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ToolPolyline != null)
            {
                while (!((GraphsCanvas.Children[GraphsCanvas.Children.Count - 1] is Line)
                      || (GraphsCanvas.Children[GraphsCanvas.Children.Count - 1] is Label)))
                    GraphsCanvas.Children.RemoveAt(GraphsCanvas.Children.Count - 1);

                ToolPolyline = null;

                Mouse.Capture(null);
            }
        }

        private void GraphMeasuring_MouseMove(object sender, MouseEventArgs e)
        {
            if (((e.LeftButton == MouseButtonState.Pressed) && (e.MiddleButton == MouseButtonState.Released)))
            {
                if (ToolPolyline != null)
                {
                    ToolPolyline.Points[1] = new Point(e.GetPosition(sender as Canvas).X, ToolPolyline.Points[0].Y);

                    if (ToolPolyline.Points[1].X >= (ToolPolyline.Points[0].X + ScaleX[ScaleXIndex]))
                    {
                        if ((GraphsCanvas.Children[GraphsCanvas.Children.Count - 2] is Polyline) && ((GraphsCanvas.Children[GraphsCanvas.Children.Count - 2] as Polyline).Name == "EndMeasMark"))
                            GraphsCanvas.Children.RemoveAt(GraphsCanvas.Children.Count - 2);
                                                
                        double SegmentStart = ToolPolyline.Points[0].X;
                        double SegmentEnd = ToolPolyline.Points[1].X;
                        int StartIndex = 0;
                        int EndIndex = 0;

                        if (SegmentStart > SegmentEnd)
                        {
                            double Value = SegmentStart;
                            SegmentStart = SegmentEnd;
                            SegmentEnd = Value;
                        }

                        byte Chnl = (byte)ToolPolyline.Tag;
                        for (int PointIndex = 0; PointIndex < Channels[Chnl].GraphPoints.Count; PointIndex++)
                        {
                            if (SegmentStart <= Channels[Chnl].GraphPoints[PointIndex].X)
                            {
                                StartIndex = PointIndex;
                                break;
                            }
                        }

                        for (int PointIndex = Channels[Chnl].GraphPoints.Count - 1; PointIndex >= 0; PointIndex--)
                        {
                            if (SegmentEnd >= Channels[Chnl].GraphPoints[PointIndex].X)
                            {
                                EndIndex = PointIndex;
                                ToolPolyline.Points[1] = new Point(Channels[Chnl].GraphPoints[PointIndex].X, ToolPolyline.Points[1].Y);
                                Polyline MeasuringMark = new Polyline();
                                MeasuringMark.StrokeThickness = 2;
                                MeasuringMark.Stroke = ToolPolyline.Stroke;
                                MeasuringMark.Name = "EndMeasMark";
                                MeasuringMark.Opacity = TOOLS_ELEMENT_OPACITY;
                                MeasuringMark.Points.Add(new Point(ToolPolyline.Points[1].X, ToolPolyline.Points[1].Y - ((START_Y + PULSE_HEIGHT) / 2 + START_Y)));
                                MeasuringMark.Points.Add(new Point(ToolPolyline.Points[1].X, ToolPolyline.Points[1].Y + ((START_Y + PULSE_HEIGHT) / 2 + START_Y)));
                                GraphsCanvas.Children.Add(MeasuringMark);
                                break;
                            }
                        }

                        if (GraphsCanvas.Children[GraphsCanvas.Children.Count - 2] is StackPanel)
                            GraphsCanvas.Children.RemoveAt(GraphsCanvas.Children.Count - 2);

                        PointCollection PointsToMeasure = new PointCollection();
                        for (int PointIndex = StartIndex; PointIndex <= EndIndex; PointIndex++)
                            PointsToMeasure.Add(Channels[Chnl].GraphPoints[PointIndex]);
                        
                        Measurement MeasureStart = new Measurement(PointsToMeasure, DescretTime[DescretTimeIndex], ScaleX[ScaleXIndex], PULSE_HEIGHT + START_Y);

                        if (Chnl > 5)
                            Chnl -= 2;
                        MeasureStart.Result.Margin = new Thickness(ToolPolyline.Points[0].X, ((CHANNEL_COUNT - Chnl - 1) * GRAPHS_GAP) - 0, 0, 0);
                        MeasureStart.Result.Visibility = Visibility.Hidden;
                        MeasureStart.Result.Loaded += MeasureResult_Loaded;
                        GraphsCanvas.Children.Add(MeasureStart.Result);
                    }

                    else
                        ToolPolyline.Points[1] = ToolPolyline.Points[0];
                }
            }            
        }

        private void MeasureResult_Loaded(object sender, RoutedEventArgs e)
        {
            StackPanel MeasureResult = sender as StackPanel;
            int Value = (int)MeasureResult.ActualWidth;
            if ((MeasureResult.Margin.Left + Value) > GraphsCanvas.ActualWidth)
                MeasureResult.Margin = new Thickness(MeasureResult.Margin.Left - Value, MeasureResult.Margin.Top, 0, 0);
            MeasureResult.Visibility = Visibility.Visible;
        }

        private void GraphMeasuring_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point NewPoint = e.GetPosition(sender as Canvas);
            Point TestPoint = new Point();
            byte Chnl;
            for (Chnl = 0; Chnl < CHANNEL_COUNT; Chnl++)
            {
                TestPoint = NewPoint;
                double TopMargin = (GraphsCanvas.Children[Chnl] as Polyline).Margin.Top;
                int LowLimit = START_Y + (int)TopMargin;
                int HighLimit = LowLimit + PULSE_HEIGHT;
                if ((TestPoint.Y >= LowLimit)
                 && (TestPoint.Y <= HighLimit))
                {
                    TestPoint.Y = LowLimit + (HighLimit - LowLimit) / 2;
                    break;
                }

                else
                    TestPoint.Y = -1;
            }

            if (TestPoint.Y == -1)
                return;

            NewPoint.Y = TestPoint.Y;
            ToolPolyline = new Polyline();
            ToolPolyline.StrokeThickness = 2;
            ToolPolyline.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TOOLS_ELEMENT_COLOR));
            ToolPolyline.Opacity = TOOLS_ELEMENT_OPACITY;
            ToolPolyline.Tag = Chnl;
            for (int PointIndex = 0; PointIndex < Channels[Chnl].GraphPoints.Count; PointIndex++)
            {
                if (NewPoint.X <= Channels[Chnl].GraphPoints[PointIndex].X)
                {
                    NewPoint = new Point(Channels[Chnl].GraphPoints[PointIndex].X, NewPoint.Y);
                    break;
                }
            }
            ToolPolyline.Points.Add(NewPoint);
            ToolPolyline.Points.Add(NewPoint);
            Polyline MeasuringMark = new Polyline();
            MeasuringMark.StrokeThickness = 2;
            MeasuringMark.Stroke = ToolPolyline.Stroke;
            MeasuringMark.Opacity = TOOLS_ELEMENT_OPACITY;
            MeasuringMark.Points.Add(new Point(ToolPolyline.Points[0].X, ToolPolyline.Points[0].Y - ((START_Y + PULSE_HEIGHT) / 2 + START_Y)));
            MeasuringMark.Points.Add(new Point(ToolPolyline.Points[0].X, ToolPolyline.Points[0].Y + ((START_Y + PULSE_HEIGHT) / 2 + START_Y)));
            GraphsCanvas.Children.Add(MeasuringMark);
            GraphsCanvas.Children.Add(ToolPolyline);
            Mouse.Capture(GraphsCanvas, CaptureMode.Element);
        }

        private void PrevFrameButton_Click(object sender, RoutedEventArgs e)
        {
            PanningLeft();
        }

        private void NextFrameButton_Click(object sender, RoutedEventArgs e)
        {
            PanningRight();
        }

        private void FrameChange()
        {
            for (byte Chnl = 0; Chnl < CHANNEL_COUNT; Chnl++)
            {                
                Channels[Chnl].RedrawGraph(FrameStartPoint, FramePoints);
                (GraphsCanvas.Children[Chnl] as Polyline).Points = Channels[Chnl].GraphPoints;
            }
                
            double XOffset = Channels[0].Get_X_Offset();

            GraphsCanvas.Children.RemoveRange(2 * CHANNEL_COUNT, GraphsCanvas.Children.Count - 2 * CHANNEL_COUNT);

            int TimeMarkCount = (End_X_Value - START_X_VALUE) / MainTimeMarkStep[ScaleXIndex];
            int TimeUnitsIndex;
            for (byte TimeMark = 0; TimeMark <= TimeMarkCount; TimeMark++)
            {
                Line TimeMarkLine = new Line();
                TimeMarkLine.X1 = MainTimeMarkStep[ScaleXIndex] * TimeMark;
                TimeMarkLine.Y1 = GRAPHS_GAP * (CHANNEL_COUNT + 1) - 20;
                TimeMarkLine.X2 = TimeMarkLine.X1;
                TimeMarkLine.Y2 = TimeMarkLine.Y1 - MAIN_TIME_MARK_HEIGHT;
                TimeMarkLine.Stroke = new SolidColorBrush(Colors.Gray);
                TimeMarkLine.StrokeThickness = 2;
                if (TimeMarkLine.X1 < End_X_Value)
                {
                    GraphsCanvas.Children.Add(TimeMarkLine);
                    Label TimeLabel = new Label();
                    double TimeValue = DescretTime[DescretTimeIndex] * ((TimeMarkLine.X1 + XOffset) / ScaleX[ScaleXIndex]);
                    TimeUnitsIndex = 0;
                    if (TimeValue >= 1000)
                    {
                        TimeValue /= 1000;
                        TimeUnitsIndex = 1;
                    }
                    TimeLabel.Content = (TimeValue).ToString("0.000") + TimeUnits[TimeUnitsIndex];
                    TimeLabel.FontSize = 12;
                    TimeLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TOOLS_ELEMENT_COLOR));
                    TimeLabel.Margin = new Thickness(TimeMarkLine.X1 - 3, TimeMarkLine.Y2 + 10, 0, 0);
                    TimeLabel.RenderTransform = new ScaleTransform(1, -1);
                    GraphsCanvas.Children.Add(TimeLabel);
                }
                                
                for (byte SmallTimeMark = 1; SmallTimeMark < 10; SmallTimeMark++)
                {
                    Line SmallTimeMarkLine = new Line();
                    SmallTimeMarkLine.X1 = TimeMarkLine.X1 + SmallTimeMarkStep[ScaleXIndex] * SmallTimeMark;
                    SmallTimeMarkLine.Y1 = TimeMarkLine.Y1;
                    SmallTimeMarkLine.X2 = SmallTimeMarkLine.X1;
                    SmallTimeMarkLine.Y2 = SmallTimeMarkLine.Y1 - SMALL_TIME_MARK_HEIGHT;
                    SmallTimeMarkLine.Stroke = new SolidColorBrush(Colors.Gray);
                    SmallTimeMarkLine.StrokeThickness = 2;
                    if (SmallTimeMarkLine.X1 > (End_X_Value - SmallTimeMarkStep[ScaleXIndex]))
                        break;
                    GraphsCanvas.Children.Add(SmallTimeMarkLine);
                }
            }

            for (int decoder = 0; decoder < IAnalyzers.Count; decoder++)
                InterfaceDataChange(decoder);
        }

        private void UpdateFrameParameters()
        {
            FramePoints = (int)((End_X_Value - START_X_VALUE) / ScaleX[ScaleXIndex]);
            if (FramePoints > SamplesToCapture)
                FramePoints = SamplesToCapture;
            UpdatePoints = (int)(((ScaleX.Length - ScaleXIndex) * MainTimeMarkStep[ScaleXIndex]) / ScaleX[ScaleXIndex]);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            Zooming(ZOOM_OUT);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            Zooming(ZOOM_IN);
        }

        private void Zooming(byte Direction)
        {
            if (((Direction == ZOOM_OUT) && (ScaleXIndex != 0))
             || ((Direction == ZOOM_IN) && (ScaleXIndex != (ScaleX.Length - 1))))
            {
                if (Direction == ZOOM_OUT)
                    ScaleXIndex--;
                else
                    ScaleXIndex++;
                
                for (byte Chnl = 0; Chnl < CHANNEL_COUNT; Chnl++)
                    Channels[Chnl].Scale(ScaleX[ScaleXIndex]);

                UpdateFrameParameters();
                if ((FrameStartPoint + FramePoints) >= SamplesToCapture)
                    FrameStartPoint = SamplesToCapture - FramePoints;
                FrameChange();
            }
        }
        
        private void MeasureButton_Click(object sender, RoutedEventArgs e)
        {
            if (MeasuringActive)
            {
                MeasureToolDeSelect();
            }                
            else
            {
                if (LineActive)
                    LineToolDeSelect();

                MeasureToolSelect();
            }
        }

        private void GraphsCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            End_X_Value = (int)GraphsCanvas.ActualWidth;
            
            UpdateFrameParameters();
            if ((FrameStartPoint + FramePoints) >= SamplesToCapture)
                FrameStartPoint = SamplesToCapture - FramePoints;
            FrameChange();
        }

        private void ChannelColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border SenderBorder = sender as Border;

            ColorSelect ColorWindow = new ColorSelect(SenderBorder.Background);
            ColorWindow.Owner = this;
            if ((ColorWindow.ShowDialog() == true) && (ColorWindow.NewColor != null))
            {
                SenderBorder.Background = ColorWindow.NewColor;
                (GraphsCanvas.Children[int.Parse(SenderBorder.Uid)] as Polyline).Stroke = SenderBorder.Background;
            }
        }

        private void GraphsCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                Zooming(ZOOM_IN);
            else
                Zooming(ZOOM_OUT);
        }

        private void GraphsCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Middle) && (e.ButtonState == MouseButtonState.Pressed))
            {
                Point StartPoint = e.GetPosition(sender as Canvas);
                PanPolyline.Points.Add(StartPoint);
            }
        }

        private void GraphsCanvas_MouseMove_Panning(object sender, MouseEventArgs e)
        {
            if ((e.MiddleButton == MouseButtonState.Pressed))
            {
                Point EndPoint = e.GetPosition(sender as Canvas);
                PanPolyline.Points.Add(EndPoint);
                if ((PanPolyline.Points[0] - PanPolyline.Points[PanPolyline.Points.Count - 1]).Length > 50)
                {
                    if (PanPolyline.Points[0].X > PanPolyline.Points[PanPolyline.Points.Count - 1].X)
                        PanningRight();
                    else
                        PanningLeft();

                    EndPoint = PanPolyline.Points[PanPolyline.Points.Count - 1];
                    PanPolyline.Points.Clear();
                    PanPolyline.Points.Add(EndPoint);
                }                                
            }
        }

        private void PanningLeft()
        {
            if (FrameStartPoint != 0)
            {
                if (FrameStartPoint > UpdatePoints)
                    FrameStartPoint -= UpdatePoints;
                else
                    FrameStartPoint = 0;

                FrameChange();
            }
        }

        private void PanningRight()
        {
            if (FrameStartPoint < SamplesToCapture)
            {
                FrameStartPoint += UpdatePoints;

                if ((FrameStartPoint + FramePoints) >= SamplesToCapture)
                    FrameStartPoint = SamplesToCapture - FramePoints;

                FrameChange();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {            
            if (USBDevDetected)
            {                
                byte[] USBPacket = new byte[USB_CMD_PACKET_SIZE];

                USBPacket[USB_REPORT_ID_POS] = USB_CMD_ID;
                USBPacket[USB_CMD_POS] = USB_CMD_START_CAPTURE;

                int Value = Frequencies.SelectedIndex;
                USBPacket[CAPTURE_SYNC_OFFSET] = CAPTURE_SYNC_INTERNAL;
                USBPacket[CAPTURE_TIM_PSC_OFFSET] = CaptureTimPsc[Value];
                USBPacket[CAPTURE_TIM_ARR_OFFSET] = CaptureTimArr[Value];
                DescretTimeIndex = Value;

                SamplesToCapture = SampleCount[Samples.SelectedIndex];
                USBPacket[CAPTURE_SAMPLE_OFFSET] = (byte)SamplesToCapture;
                USBPacket[CAPTURE_SAMPLE_OFFSET + 1] = (byte)(SamplesToCapture >> 8);
                if (TriggerActivationCheckBox.IsChecked == true)
                {
                    USBPacket[CAPTURE_TRIGGER_ENABLE_OFFSET] = CAPTURE_TRIGGER_ENABLE;
                    USBPacket[CAPTURE_TRIGGER_MODE_OFFSET] = CAPTURE_TRIGGER_MODE_CHANNELS;
                    for (int TrigByte = 0; TrigByte < CAPTURE_TRIGGER_BYTES_COUNT; TrigByte++)
                        USBPacket[CAPTURE_TRIGGER_SET_OFFSET + TrigByte] = (byte)((Array.IndexOf(TriggersString, Channels[2 * TrigByte + 1].Trigger) << 4) | (Array.IndexOf(TriggersString, Channels[2 * TrigByte].Trigger)));
                    if ((USBPacket[CAPTURE_TRIGGER_SET_OFFSET] == 0) && (USBPacket[CAPTURE_TRIGGER_SET_OFFSET + 1] == 0)
                     && (USBPacket[CAPTURE_TRIGGER_SET_OFFSET + 2] == 0) && (USBPacket[CAPTURE_TRIGGER_SET_OFFSET + 3] == 0))
                        USBPacket[CAPTURE_TRIGGER_ENABLE_OFFSET] = CAPTURE_TRIGGER_DISABLE;
                }

                else
                    USBPacket[CAPTURE_TRIGGER_ENABLE_OFFSET] = CAPTURE_TRIGGER_DISABLE;
                
                bool USBSuccess = USB_device.Write(USBPacket);

                byte[] InputData = new byte[SamplesToCapture];

                Value = SamplesToCapture / USB_CMD_PACKET_SIZE;

                for (int i = 0; i < Value; i++)
                {
                    USBSuccess = USB_device.Read(USBPacket);
                    USBPacket.CopyTo(InputData, i * USB_CMD_PACKET_SIZE);
                }

                PointCollection[] NewChannelsData = new PointCollection[CHANNEL_COUNT];
                for (int chnl = 0; chnl < CHANNEL_COUNT; chnl++)
                    NewChannelsData[chnl] = new PointCollection();

                for (int PointIndex = 0; PointIndex < SamplesToCapture; PointIndex++)
                {
                    for (int chnl = 0; chnl < CHANNEL_COUNT; chnl++)
                    {
                        Value = START_Y;
                        if ((InputData[PointIndex] & (1 << chnl)) == (1 << chnl))
                            Value += PULSE_HEIGHT;
                        NewChannelsData[chnl].Add(new Point(PointIndex, Value));
                    }
                }

                //если активированы триггеры, в начало графика добавляются 2 точки,
                //соответствующие триггеру канала;
                //если у канала нет триггера, добавленные точки повторяют начальную точку канала
                if (TriggerActivationCheckBox.IsChecked == true)
                {
                    for (int chnl = 0; chnl < CHANNEL_COUNT; chnl++)
                    {
                        for (int PointIndex = 0; PointIndex < SamplesToCapture; PointIndex++)
                            NewChannelsData[chnl][PointIndex] = new Point(NewChannelsData[chnl][PointIndex].X + EXTRA_POINTS_COUNT_FOR_TRIGGER, NewChannelsData[chnl][PointIndex].Y);
                    }
                    
                    SamplesToCapture += EXTRA_POINTS_COUNT_FOR_TRIGGER;
                    
                    for (int chnl = 0; chnl < CHANNEL_COUNT; chnl++)
                    {
                        switch(Array.IndexOf(TriggersString, Channels[chnl].Trigger))
                        {
                            case TRIGGER_NONE:
                                NewChannelsData[chnl].Insert(0, new Point(1, NewChannelsData[chnl][0].Y));
                                NewChannelsData[chnl].Insert(0, new Point(0, NewChannelsData[chnl][0].Y));
                            break;

                            case TRIGGER_LOW_LEVEL:
                                NewChannelsData[chnl].Insert(0, new Point(1, PULSE_LOW));
                                NewChannelsData[chnl].Insert(0, new Point(0, PULSE_LOW));
                            break;

                            case TRIGGER_HIGH_LEVEL:
                                NewChannelsData[chnl].Insert(0, new Point(1, PULSE_HIGH));
                                NewChannelsData[chnl].Insert(0, new Point(0, PULSE_HIGH));
                            break;

                            case TRIGGER_RISING_EDGE:
                                NewChannelsData[chnl].Insert(0, new Point(1, PULSE_HIGH));
                                NewChannelsData[chnl].Insert(0, new Point(0, PULSE_LOW));
                            break;

                            case TRIGGER_FALLING_EDGE:
                            case TRIGGER_ANY_EDGE:
                                NewChannelsData[chnl].Insert(0, new Point(1, PULSE_LOW));
                                NewChannelsData[chnl].Insert(0, new Point(0, PULSE_HIGH));
                            break;
                        }
                    }
                }

                for (int chnl = 0; chnl < CHANNEL_COUNT; chnl++)
                    Channels[chnl].CaptureData(NewChannelsData[chnl]);
                
                for (int decoder = 0; decoder < IAnalyzers.Count; decoder++)
                {       
                    GetPointsAndAnalyze(decoder);

                    if (decoder == InterfaceComboBox.SelectedIndex)
                        InterfaceItemsListView.ItemsSource = IAnalyzers[decoder].InterfaceParts;
                }

                ScaleXIndex = 1;
                Zooming(ZOOM_OUT);
            }            
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            if (LineActive)
            {
                LineToolDeSelect();
            }
            else
            {
                if (MeasuringActive)
                    MeasureToolDeSelect();

                LineToolSelect();
            }
        }

        private void GraphLine_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ToolPolyline != null)
            {
                GraphsCanvas.Children.RemoveAt(GraphsCanvas.Children.Count - 1);
                GraphsCanvas.Children.RemoveAt(GraphsCanvas.Children.Count - 1);
                ToolPolyline = null;
                Mouse.Capture(null);
            }
        }

        private void GraphLine_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToolPolyline = new Polyline();
            ToolPolyline.StrokeThickness = 2;
            ToolPolyline.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TOOLS_ELEMENT_COLOR));
            ToolPolyline.Opacity = TOOLS_ELEMENT_OPACITY;
            ToolPolyline.StrokeDashArray = new DoubleCollection(new double[] { 10, 5 });

            double NewXPos = e.GetPosition(sender as Canvas).X;

            for (int PointIndex = 0; PointIndex < Channels[0].GraphPoints.Count; PointIndex++)
            {
                if (NewXPos <= Channels[0].GraphPoints[PointIndex].X)
                {
                    NewXPos = Channels[0].GraphPoints[PointIndex].X;
                    break;
                }
            }

            ToolPolyline.Points.Add(new Point(NewXPos, 0));
            ToolPolyline.Points.Add(new Point(NewXPos, GraphsCanvas.ActualHeight));
            GraphsCanvas.Children.Add(ToolPolyline);
            Label TimeValue = new Label();
            TimeValue.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TOOLS_ELEMENT_COLOR));
            TimeValue.Margin = new Thickness(NewXPos + 5, e.GetPosition(sender as Canvas).Y, 0, 0);
            TimeValue.RenderTransform = new ScaleTransform(1, -1);
            TimeValue.Content = (DescretTime[DescretTimeIndex] * ((Channels[0].Get_X_Offset() + NewXPos) / ScaleX[ScaleXIndex])).ToString("0.000") + " мкс";
            TimeValue.Visibility = Visibility.Hidden;
            TimeValue.Loaded += TimeValueForLineTool_Loaded;
            GraphsCanvas.Children.Add(TimeValue);
            Mouse.Capture(GraphsCanvas, CaptureMode.Element);
        }

        private void TimeValueForLineTool_Loaded(object sender, RoutedEventArgs e)
        {
            Label TimeValueForLineTool = (sender as Label);
            int Value = (int)TimeValueForLineTool.ActualWidth;
            if ((TimeValueForLineTool.Margin.Left + Value) > GraphsCanvas.ActualWidth)
                TimeValueForLineTool.Margin = new Thickness(TimeValueForLineTool.Margin.Left - Value - 10, TimeValueForLineTool.Margin.Top, 0, 0);
            TimeValueForLineTool.Visibility = Visibility.Visible;
        }

        private void MeasureToolSelect()
        {
            MeasuringActive = true;
            GraphsCanvas.MouseLeftButtonDown += GraphMeasuring_MouseLeftButtonDown;
            GraphsCanvas.MouseMove += GraphMeasuring_MouseMove;
            GraphsCanvas.MouseLeftButtonUp += GraphMeasuring_MouseLeftButtonUp;
            GraphsCanvas.Cursor = Cursors.Cross;
        }

        private void MeasureToolDeSelect()
        {
            MeasuringActive = false;
            GraphsCanvas.MouseLeftButtonDown -= GraphMeasuring_MouseLeftButtonDown;
            GraphsCanvas.MouseMove -= GraphMeasuring_MouseMove;
            GraphsCanvas.MouseLeftButtonUp -= GraphMeasuring_MouseLeftButtonUp;
            GraphsCanvas.Cursor = Cursors.Arrow;
        }

        private void LineToolSelect()
        {
            LineActive = true;
            GraphsCanvas.MouseLeftButtonDown += GraphLine_MouseLeftButtonDown;
            GraphsCanvas.MouseLeftButtonUp += GraphLine_MouseLeftButtonUp;
            GraphsCanvas.Cursor = Cursors.Cross;
        }

        private void LineToolDeSelect()
        {
            LineActive = false;
            GraphsCanvas.MouseLeftButtonDown -= GraphLine_MouseLeftButtonDown;
            GraphsCanvas.MouseLeftButtonUp -= GraphLine_MouseLeftButtonUp;
            GraphsCanvas.Cursor = Cursors.Arrow;
        }

        private void TriggerButton_Click(object sender, RoutedEventArgs e)
        {
            if (TriggerBorderMain.Visibility == Visibility.Collapsed)
                TriggerBorderMain.Visibility = Visibility.Visible;
            else if (TriggerBorderMain.Visibility == Visibility.Visible)
                TriggerBorderMain.Visibility = Visibility.Collapsed;
        }

        private void Trigger_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock ChannelTrigger = sender as TextBlock;

            int NewTriggerIndex = Array.IndexOf(TriggersString, ChannelTrigger.Text);
            NewTriggerIndex++;
            if (NewTriggerIndex == TriggersString.Length)
            {
                NewTriggerIndex = 0;
            }
                
            ChannelTrigger.Text = TriggersString[NewTriggerIndex];
        }

        private void Name_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox ChannelName = sender as TextBox;
            if (ChannelName.Text == "")
                ChannelName.Text = "канал " + ChannelName.Uid;
        }

        private void InterfaceDataChange(int decoder)
        {
            if (IAnalyzers[decoder].InterfaceParts.Count != 0)
            {
                foreach (int dataline in IAnalyzers[decoder].DataLinesNumbers)
                    (GraphsCanvas.Children[CHANNEL_COUNT + dataline] as Grid).Children.Clear();

                int StartItem = 0;
                int EndItem = 0;

                for (int i = 0; i < IAnalyzers[decoder].InterfaceParts.Count; i++)
                {
                    if (IAnalyzers[decoder].InterfaceParts[i].StartPoint >= FrameStartPoint)
                    {
                        StartItem = i;
                        break;
                    }
                }

                for (int i = IAnalyzers[decoder].InterfaceParts.Count - 1; i > StartItem; i--)
                {
                    if (IAnalyzers[decoder].InterfaceParts[i].StartPoint <= (FrameStartPoint + FramePoints))
                    {
                        EndItem = i;
                        break;
                    }
                }

                for (int i = StartItem; i <= EndItem; i++)
                {
                    TextBlock FirstTExtItem = new TextBlock();
                    FirstTExtItem.Text = IAnalyzers[decoder].InterfaceParts[i].Item;
                    FirstTExtItem.FontSize = 12;
                    FirstTExtItem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(INTERFACE_TEXT_COLOR));
                    FirstTExtItem.HorizontalAlignment = HorizontalAlignment.Center;

                    Border NewBorder = new Border();
                    NewBorder.CornerRadius = new CornerRadius(7);
                    NewBorder.Child = FirstTExtItem;
                    NewBorder.Width = IAnalyzers[decoder].InterfaceParts[i].Width * ScaleX[ScaleXIndex];
                    NewBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(IAnalyzers[decoder].InterfaceParts[i].Background));
                    NewBorder.Margin = new Thickness((IAnalyzers[decoder].InterfaceParts[i].StartPoint - FrameStartPoint) * ScaleX[ScaleXIndex], 15, 0, 0);
                    NewBorder.HorizontalAlignment = HorizontalAlignment.Left;
                    NewBorder.RenderTransform = new ScaleTransform(1, -1);
                    foreach (int dataline in IAnalyzers[decoder].DataLinesNumbers)
                        (GraphsCanvas.Children[CHANNEL_COUNT + dataline] as Grid).Children.Add(NewBorder);
                }
            }
        }

        private void InterfaceItemsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                GoToInterfaceItem((InterfaceItemsListView.SelectedItem as InterfaceItem).StartPoint);
        }

        private void AddInterfaceButton_Click(object sender, RoutedEventArgs e)
        {
            AddInterListView.Visibility = Visibility.Visible;
        }

        private void AddI2CAnalyzer()
        {
            I2CAnalyzerSettings AnalyzerWin = new I2CAnalyzerSettings();
            AnalyzerWin.Owner = this;

            if (AnalyzerWin.ShowDialog() == true)
            {
                IHardwareInterface NewI2C = new I2C();
                NewI2C.Name = AnalyzerWin.Name + "(" + NewI2C.Type + ")";
                NewI2C.Initialize(AnalyzerWin.InterfaceLines);
                NewI2C.InterfaceVisibility = AnalyzerWin.I2CInterVisibility;
                IAnalyzers.Add(NewI2C);
                GetPointsAndAnalyze(IAnalyzers.Count - 1);
                InterfaceDataChange(IAnalyzers.Count - 1);
            }
        }

        private void GetPointsAndAnalyze(int AnalyzerNumber)
        {
            List<PointCollection> SignalPoints = new List<PointCollection>();
            foreach (int lineNum in IAnalyzers[AnalyzerNumber].AllLinesNumbers)
                SignalPoints.Add(Channels[lineNum].GetCapturedData());
            IAnalyzers[AnalyzerNumber].Analyze(SignalPoints, DescretTime[DescretTimeIndex]);
        }

        private void InterfaceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            if (InterfaceComboBox.SelectedIndex != -1)
                InterfaceItemsListView.ItemsSource = IAnalyzers[InterfaceComboBox.SelectedIndex].InterfaceParts;            
        }

        private void AddInterListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                int InterfaceIndex = AddInterListView.SelectedIndex;
                AddInterListView.Visibility = Visibility.Collapsed;

                if(InterfaceIndex != (AddInterListView.Items.Count - 1))
                {
                    switch (InterfaceIndex)
                    {
                        case 0: //I2C
                            AddI2CAnalyzer();
                            break;
                    }
                }                
            }            
        }

        private void RemoveInterfaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (InterfaceComboBox.SelectedIndex != -1)
            {
                foreach (int dataline in IAnalyzers[InterfaceComboBox.SelectedIndex].DataLinesNumbers)
                    (GraphsCanvas.Children[CHANNEL_COUNT + dataline] as Grid).Children.Clear();
                InterfaceItemsListView.ItemsSource = null;
                IAnalyzers.RemoveAt(InterfaceComboBox.SelectedIndex);
                MatchingCountTextBlock.Text = "";
                SearchTextBox.Clear();
            }                
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (InterfaceComboBox.SelectedIndex != -1)
            {
                IAnalyzers[InterfaceComboBox.SelectedIndex].SearchItem(SearchTextBox.Text);
                MatchingCountTextBlock.Text = IAnalyzers[InterfaceComboBox.SelectedIndex].MatchedItemsCount.ToString();
                ActiveInterfaceItemChange();
            }
        }

        private void NextResButton_Click(object sender, RoutedEventArgs e)
        {
            IAnalyzers[InterfaceComboBox.SelectedIndex].NextMatchItem();
            ActiveInterfaceItemChange();
        }

        private void PrevResButton_Click(object sender, RoutedEventArgs e)
        {
            IAnalyzers[InterfaceComboBox.SelectedIndex].PreviousMatchItem();
            ActiveInterfaceItemChange();
        }

        private void GoToInterfaceItem(int PointToGoTo)
        {
            FrameStartPoint = PointToGoTo;

            if ((FrameStartPoint + FramePoints) >= SamplesToCapture)
                FrameStartPoint = SamplesToCapture - FramePoints;

            FrameChange();
        }

        private void ActiveInterfaceItemChange()
        {
            InterfaceItemsListView.SelectedIndex = IAnalyzers[InterfaceComboBox.SelectedIndex].CurrentMatchedItemIndex;
            InterfaceItemsListView.ScrollIntoView(InterfaceItemsListView.SelectedItem);
            GoToInterfaceItem((InterfaceItemsListView.SelectedItem as InterfaceItem).StartPoint);
        }

        private void AnalyzerButton_Click(object sender, RoutedEventArgs e)
        {
            if (AnalyzerBorderMain.Visibility == Visibility.Collapsed)
                AnalyzerBorderMain.Visibility = Visibility.Visible;
            else if (AnalyzerBorderMain.Visibility == Visibility.Visible)
                AnalyzerBorderMain.Visibility = Visibility.Collapsed;
        }
    }
}

