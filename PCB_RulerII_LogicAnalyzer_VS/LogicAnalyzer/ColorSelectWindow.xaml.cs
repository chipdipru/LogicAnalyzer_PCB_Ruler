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
using System.Reflection;
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
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class ColorSelect : Window
    {
        public Brush NewColor { get; set; }
                

        public ColorSelect(Brush Color)
        {
            InitializeComponent();

            Collection<PropertyInfo> ColorCollection = new Collection<PropertyInfo>(typeof(Colors).GetProperties());
            Collection<Border> BorderCollection = new Collection<Border>();
            
            for (int i = 0; i < ColorCollection.Count; i++)
            {
                Border NewBorder = new Border();
                NewBorder.Width = 20;
                NewBorder.Height = 20;
                NewBorder.BorderThickness = new Thickness(2);
                NewBorder.BorderBrush = Brushes.Black;
                NewBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorCollection[i].Name));
                NewBorder.MouseDown += ColorBorder_MouseDown;
                BorderCollection.Add(NewBorder);
            }
            
            ColorsItems.ItemsSource = BorderCollection;            
        }

        private void ColorBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            for (int i = 0; i < ColorsItems.Items.Count; i++)
                (ColorsItems.Items[i] as Border).BorderBrush = Brushes.Black;

            Border SelectedBorder = (sender as Border);
            SelectedBorder.BorderBrush = Brushes.Cyan;
            NewColor = SelectedBorder.Background;
        }

        private void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

