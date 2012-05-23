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
		// Global variables
		private FlightPlanContext _context			= new FlightPlanContext();
        private readonly BackgroundWorker worker	= new BackgroundWorker();
		private CollectionViewSource airfieldViewSource;
		private List<ColourState> colourStates		= new List<ColourState>();
		

		///<summary>
		///Constructor
		///</summary>
		public MainWindow()
		{
			InitializeComponent();
            InitializeBackgroundWorker();
			InitializeColourStates();
		}

		///<summary>
		///Initialization of available colour states
		///</summary>
		private void InitializeColourStates()
		{
			this.colourStates.Add(new ColourState("RED", 0, 0));
			this.colourStates.Add(new ColourState("AMB", 800, 200));
			this.colourStates.Add(new ColourState("YLO", 1600, 300));
			this.colourStates.Add(new ColourState("GRN", 3700, 700));
			this.colourStates.Add(new ColourState("WHT", 5000, 1500));
			this.colourStates.Add(new ColourState("BLU", 8000, 2500));
		}

		///<summary>
		///Initialization of background worker
		///</summary>
        private void InitializeBackgroundWorker()
        {
			// Allow worker to report progress.
            worker.WorkerReportsProgress = true;
			// Allow cancellation of a running worker.
            worker.WorkerSupportsCancellation = true;
			// Add event handler for when RunWorkerAsync() is called - i.e. start of thread.
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			// Add event handler for when the work has been completed.
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
			// Add event handler for when a progress change occurs.
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
        }
		
		///<summary>
		///Event handler for when window is loaded
		///</summary>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{

			// Initialize variable with viewsource defined in XAML for data binding.
			this.airfieldViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("airfieldViewSource")));
			// Fetch a list of countries from the database, and use the list for the combobox.
            this.comboBox1.ItemsSource = (from c in _context.Airfields
										  orderby c.Country
										  select c.Country).Distinct().ToList();
		}

		///<summary>
		///Event handler for when the window is closing
		///</summary>
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);
			this._context.Dispose();
		}

		///<summary>
		///Event handler for when selecting an item in the combobox
		///</summary>
        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChangedHandler();
        }

		///<summary>
		///Load weather informations for the country selected in the combobox
		///</summary>
        private void SelectionChangedHandler()
        {
            if (!worker.IsBusy) {
                statusLabel.Content = "Fetching data, please wait...";
                worker.RunWorkerAsync(comboBox1.SelectedItem.ToString());
            } else {
                statusLabel.Content = "Please wait while the fetcher finishes...";
            }
        }

		///<summary>
		///Actual work in backgroundworker thread
		///</summary>
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            string selectedValue = (string)e.Argument;
            
			// Fetch a list of airfields in the specified country from database.
            var airfields = (from a in _context.Airfields
                             where a.Country == selectedValue
                             orderby a.Name
                             select a).ToList();
			// Total number of airfields.
            int airfieldsCount = airfields.Count;
			// Counter for tracking progress.
            int counter = 1;

			foreach (Airfield af in airfields) {
				// If a cancellation event has ocurred, break out of the loop.
				if (worker.CancellationPending) {
					e.Cancel = true;
					break;
				}

				// Current progress in percantage.
				double percentage = Math.Floor((double)(((double)counter / (double)airfieldsCount) * 100));
				// Report the progress (to be used in the progressbar)
				worker.ReportProgress((int)percentage);

				// Create a new observation instance with the ICAO code of currently processed airfield,
				// .. and available colour states to be used.
				Observation ob = new Observation(af.ICAO, this.colourStates);
				// Get the metar string.
				af.Observation = ob.Metar;
				// Get the colour code abbreviation.
				af.ColourState = ob.ColourState.Abbreviation.ToString();

				// Get cloudstate if available.
				if (!ob.Cloudbase.ToString().Equals("")) {
					af.Cloudbase = ob.Cloudbase.ToString() + " ft";
				} else {
					af.Cloudbase = "N/A";
				}

				// Get visibility if available.
				if (ob.Visibility >= 9999) { // Display visibility as >10km if visibility is >=9999 meters.
					af.Visibility = "> 10km";
				} else if (!ob.Visibility.ToString().Equals("")) {
					af.Visibility = ob.Visibility.ToString() + " m"; // Display as meters.
				} else {
					af.Visibility = "N/A";
				}
				// Get the age of the observation.
				af.ObservationAge = ob.ObservationAge;
				// Gets valid state - will be false if the data is over an hour old.
				af.isInvalid = ob.isInvalid;
				// Update counter to reflect progress.
				counter++;
			}
			// Return the processed list of airfields.
            e.Result = airfields;
        }

		///<summary>
		///Event for when the actual work in the backgroundworker thread has completed.
		///</summary>
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
			// Error/Exception handling - e.Error will contain the actual exception thrown.
            if (e.Error != null) {
				// Update the label on top of the progressbar with the exception error message.
                statusLabel.Content = e.Error.Message;
                progressBar1.Value = 0; // Reset the progress bar
            } else if (e.Cancelled) { // Handling of a cancellation event
                statusLabel.Content = "Cancelled";
                this.airfieldViewSource.Source = null; // Empty the contents of the grid.
                progressBar1.Value = 0; // reset progress
            } else {
				// Populate the datagrid with the processed result.
                this.airfieldViewSource.Source = (List<Airfield>)e.Result;
                statusLabel.Content = "";
            }
        }

		///<summary>
		///Progress changed event
		///</summary>
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage; // Update the progressbar.
        }

		///<summary>
		///Refresh button clicked event
		///</summary>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            SelectionChangedHandler();
        }

		///<summary>
		///Stop button clicked event
		///</summary>
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
        }
	}
}
