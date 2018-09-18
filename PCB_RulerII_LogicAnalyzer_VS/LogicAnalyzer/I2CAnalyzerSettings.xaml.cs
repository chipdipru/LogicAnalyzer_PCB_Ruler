using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace LogicAnalyzer
{
    /// <summary>
    /// Interaction logic for I2CAnalyzerSettings.xaml
    /// </summary>
    public partial class I2CAnalyzerSettings : Window
    {
        private List<InterfaceLine> I2CLines = new List<InterfaceLine>();
        private List<bool> I2CVisibility = new List<bool>();
        private string name;

        public List<InterfaceLine> InterfaceLines { get { return I2CLines; } }
        public new string Name { get { return name; } }
        public List<bool> I2CInterVisibility { get { return I2CVisibility; } }

        public I2CAnalyzerSettings()
        {
            InitializeComponent();
            
            List<String> Channels = new List<String>();
            for (int i = 0; i < 8; i++)
                Channels.Add("канал " + i.ToString());
            SCLcomboBox.ItemsSource = Channels;
            SDAcomboBox.ItemsSource = Channels;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            I2CLines.Add(new InterfaceLine(SCLcomboBox.SelectedIndex));
            I2CLines[0].Type = InterfaceLine.CLKLineType;
            I2CLines.Add(new InterfaceLine(SDAcomboBox.SelectedIndex));
            I2CLines[1].Type = InterfaceLine.DataLineType;
            
            name = NameTextBox.Text;

            DialogResult = true;
        }
    }
}
