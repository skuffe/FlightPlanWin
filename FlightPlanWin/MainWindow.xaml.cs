using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FlightPlanModel;

namespace FlightPlanWin
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private FlightPlanContext _context = new FlightPlanContext();
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{

			System.Windows.Data.CollectionViewSource airfieldViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("airfieldViewSource")));
			// Load data by setting the CollectionViewSource.Source property:
			// airfieldViewSource.Source = [generic data source]
			_context.Airfields.Load();

			airfieldViewSource.Source = _context.Airfields.Local;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);
			this._context.Dispose();
		}
	}
}
