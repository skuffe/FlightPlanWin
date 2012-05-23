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
using System.ComponentModel;
using System.Data;

namespace FlightPlanWin
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private FlightPlanContext _context = new FlightPlanContext();
        private readonly BackgroundWorker worker = new BackgroundWorker();
		private CollectionViewSource airfieldViewSource;
		private List<ColourState> colourStates = new List<ColourState>();
		

		public MainWindow()
		{
			InitializeComponent();
            InitializeBackgroundWorker();
			InitializeColourStates();
		}

		private void InitializeColourStates()
		{
			this.colourStates.Add(new ColourState("RED", 0, 0));
			this.colourStates.Add(new ColourState("AMB", 800, 200));
			this.colourStates.Add(new ColourState("YLO", 1600, 300));
			this.colourStates.Add(new ColourState("GRN", 3700, 700));
			this.colourStates.Add(new ColourState("WHT", 5000, 1500));
			this.colourStates.Add(new ColourState("BLU", 8000, 2500));
		}

        private void InitializeBackgroundWorker()
        {
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{

			this.airfieldViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("airfieldViewSource")));
            this.comboBox1.ItemsSource = (from c in _context.Airfields
										  orderby c.Country
										  select c.Country).Distinct().ToList();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);
			this._context.Dispose();
		}

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChangedHandler();
        }

        private void SelectionChangedHandler()
        {
            if (!worker.IsBusy) {
                statusLabel.Content = "Fetching data, please wait...";
                worker.RunWorkerAsync(comboBox1.SelectedItem.ToString());
            } else {
                statusLabel.Content = "Please wait while the fetcher finishes...";
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            string selectedValue = (string)e.Argument;
            
            var airfields = (from a in _context.Airfields
                             where a.Country == selectedValue
                             orderby a.Name
                             select a).ToList();
            int airfieldsCount = airfields.Count;
            int counter = 1;

			foreach (Airfield af in airfields) {
                if (!worker.CancellationPending) {
                    double percentage = Math.Floor((double)(((double)counter / (double)airfieldsCount) * 100));
                    Console.WriteLine("{0}/{1}*100=~{2}", counter, airfieldsCount, (int)percentage);
                    worker.ReportProgress((int)percentage);
                    Observation ob = new Observation(af.ICAO, this.colourStates);
                    af.Observation = ob.Metar;
                    af.ColourState = ob.ColourState.Abbreviation.ToString();
                    if (!ob.Cloudbase.ToString().Equals("")) {
                        af.Cloudbase = ob.Cloudbase.ToString() + " ft";
                    } else {
                        af.Cloudbase = "N/A";
                    }
                    if (ob.Visibility.ToString().Equals("9999")) {
                        af.Visibility = "> 10km";
                    } else if (!ob.Visibility.ToString().Equals("")) {
                        af.Visibility = ob.Visibility.ToString() + " m";
                    } else {
                        af.Visibility = "N/A";
                    }
                    af.ObservationAge = ob.ObservationAge;
                    af.isInvalid = ob.isInvalid;
                    counter++;
                } else {
                    e.Cancel = true;
                    break;
                }
			}
            e.Result = airfields;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null) {
                statusLabel.Content = e.Error.Message;
                progressBar1.Value = 0;
            } else if (e.Cancelled) {
                statusLabel.Content = "Cancelled";
                this.airfieldViewSource.Source = null;
                progressBar1.Value = 0;
            } else {
                this.airfieldViewSource.Source = (List<Airfield>)e.Result;
                statusLabel.Content = "";
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            SelectionChangedHandler();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
        }
	}
}
