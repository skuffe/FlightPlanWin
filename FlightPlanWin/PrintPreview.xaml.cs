using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlightPlanWin
{
    /// <summary>
    /// Interaction logic for PrintPreview.xaml
    /// </summary>
    public partial class PrintPreview : Window
    {
		/// <summary>
		/// Interaction logic for PrintPreview.xaml
		/// </summary>
        public IDocumentPaginatorSource Document
        {
            get { return viewer.Document; }
            set { viewer.Document = value; }
        }

        public PrintPreview()
        {
            InitializeComponent();
        }
    }
}
