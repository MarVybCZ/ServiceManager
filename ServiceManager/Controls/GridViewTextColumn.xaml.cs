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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace ServiceManager.Controls
{
    /// <summary>
    /// Interaction logic for GridViewTextColumn.xaml
    /// </summary>
    public partial class GridViewTextColumn : UserControl
    {
        public static readonly DependencyProperty ColumnDataProperty = DependencyProperty.Register("ColumnData", typeof(string), typeof(GridViewTextColumn), new PropertyMetadata(""));

        public string ColumnData
        {
            get { return (string)GetValue(ColumnDataProperty); }
            set { SetValue(ColumnDataProperty, value); }
        }

        public GridViewTextColumn()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;            
        }
    }
}
