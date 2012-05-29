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
using System.Printing;
using System.Windows.Xps.Packaging;
using System.IO;
using System.Windows.Xps;

namespace FlightPlanWin
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		// Global variables
		private FlightPlanContext _context = new FlightPlanContext();
        private readonly BackgroundWorker _worker = new BackgroundWorker();
		private CollectionViewSource _airfieldViewSource;
		private List<ColourState> _colourStates = new List<ColourState>();
		private AboutBox _aboutBox = new AboutBox();
		private DateTime? _dataUpdated = null;

		private const int TABLE_COLUMNS = 8;
		private const int PRINT_FONT_SIZE = 10;
		private const string PRINT_FONT_FAMILY = "Calibri Verdana sans-serif";

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
			this._colourStates.Add(new ColourState("RED", 0, 0));
			this._colourStates.Add(new ColourState("AMB", 800, 200));
			this._colourStates.Add(new ColourState("YLO", 1600, 300));
			this._colourStates.Add(new ColourState("GRN", 3700, 700));
			this._colourStates.Add(new ColourState("WHT", 5000, 1500));
			this._colourStates.Add(new ColourState("BLU", 8000, 2500));
		}

		///<summary>
		///Initialization of background worker
		///</summary>
        private void InitializeBackgroundWorker()
        {
			// Allow worker to report progress.
            _worker.WorkerReportsProgress = true;
			// Allow cancellation of a running worker.
            _worker.WorkerSupportsCancellation = true;
			// Add event handler for when RunWorkerAsync() is called - i.e. start of thread.
            _worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			// Add event handler for when the work has been completed.
            _worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
			// Add event handler for when a progress change occurs.
            _worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
        }
		
		///<summary>
		///Event handler for when window is loaded
		///</summary>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{

			// Initialize variable with viewsource defined in XAML for data binding.
			this._airfieldViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("airfieldViewSource")));
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
            if (!_worker.IsBusy) {
                statusLabel.Content = "Fetching data, please wait...";
                _worker.RunWorkerAsync(comboBox1.SelectedItem.ToString());
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
				Observation ob = new Observation(af.ICAO, this._colourStates);
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
				// Calculate distance
				af.Distance = String.Format("{0:0.00} NM", Math.Round(GeoCalc.RhumbDistance(new LatLonPoint(Properties.Settings.Default.HomeLatitude, Properties.Settings.Default.HomeLongitude), new LatLonPoint((double)af.Latitude, (double)af.Longitude)), 2));
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
                this._airfieldViewSource.Source = null; // Empty the contents of the grid.
                progressBar1.Value = 0; // reset progress
            } else {
				// Populate the datagrid with the processed result.
                this._airfieldViewSource.Source = (List<Airfield>)e.Result;
                statusLabel.Content = "";
				this._dataUpdated = DateTime.Now;
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
            _worker.CancelAsync();
        }

        private FlowDocument CreateDocument(PrintDialog printDlg)
        {
            FlowDocument fd = new FlowDocument();
            fd.PageWidth = printDlg.PrintableAreaWidth;
            fd.PageHeight = printDlg.PrintableAreaHeight;
            fd.IsColumnWidthFlexible = false;
            fd.ColumnWidth = printDlg.PrintableAreaWidth;
            fd.FontSize = PRINT_FONT_SIZE;
			fd.FontFamily = new FontFamily(PRINT_FONT_FAMILY);
            Size pageSize = new Size(printDlg.PrintableAreaWidth, printDlg.PrintableAreaHeight);

            fd.Blocks.Add(new Paragraph(new Run("Observations for " + this.comboBox1.SelectedItem.ToString() + " - " + this._dataUpdated.ToString() + " (local time)")));
            Table table = new Table();

            table.RowGroups.Add(new TableRowGroup());

            for (int i = 0; i < TABLE_COLUMNS; i++)
                table.Columns.Add(new TableColumn());

            table.Columns[0].Width = new GridLength(300);
            for (int i = 1; i < TABLE_COLUMNS - 1; i++)
                table.Columns[i].Width = new GridLength(55);


            TableRow headerRow = new TableRow();
            headerRow.Background = Brushes.SkyBlue;
            headerRow.FontWeight = FontWeights.Bold;
            table.RowGroups[0].Rows.Add(headerRow);
            // Name
            table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(this.airfieldDataGrid.Columns[1].Header.ToString()))));
            // ICAO
            table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(this.airfieldDataGrid.Columns[2].Header.ToString()))));
            // Distance
            table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(this.airfieldDataGrid.Columns[5].Header.ToString()))));
            // Visibility
            table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(this.airfieldDataGrid.Columns[6].Header.ToString()))));
            // Cloudbase
            table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(this.airfieldDataGrid.Columns[7].Header.ToString()))));
            // Colour State
            table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(this.airfieldDataGrid.Columns[8].Header.ToString()))));
            // Observation Age
            table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(this.airfieldDataGrid.Columns[9].Header.ToString()))));
            // Observation
            table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(this.airfieldDataGrid.Columns[10].Header.ToString()))));


            for (int h = 0; h < this.airfieldDataGrid.Items.Count; h++) {
                TableRow tr = new TableRow();
                if (h % 2 != 0)
                    tr.Background = Brushes.Beige;
                table.RowGroups[0].Rows.Add(tr);
                table.RowGroups[0].Rows[h + 1].Cells.Add(new TableCell(new Paragraph(new Run((string)((Airfield)this.airfieldDataGrid.Items[h]).Name))));
                table.RowGroups[0].Rows[h + 1].Cells.Add(new TableCell(new Paragraph(new Run((string)((Airfield)this.airfieldDataGrid.Items[h]).ICAO))));
                table.RowGroups[0].Rows[h + 1].Cells.Add(new TableCell(new Paragraph(new Run((string)((Airfield)this.airfieldDataGrid.Items[h]).Distance.ToString()))));
                table.RowGroups[0].Rows[h + 1].Cells.Add(new TableCell(new Paragraph(new Run((string)((Airfield)this.airfieldDataGrid.Items[h]).Visibility.ToString()))));
                table.RowGroups[0].Rows[h + 1].Cells.Add(new TableCell(new Paragraph(new Run((string)((Airfield)this.airfieldDataGrid.Items[h]).Cloudbase.ToString()))));
                TableCell tc_ColourState = new TableCell(new Paragraph(new Run((string)((Airfield)this.airfieldDataGrid.Items[h]).ColourState)));
                switch ((string)((Airfield)this.airfieldDataGrid.Items[h]).ColourState) {
                    case "BLU":
                        tc_ColourState.Background = Brushes.Blue;
                        tc_ColourState.Foreground = Brushes.White;
                        break;
                    case "WHT":
                        tc_ColourState.Background = Brushes.White;
                        break;
                    case "GRN":
                        tc_ColourState.Background = Brushes.Green;
                        tc_ColourState.Foreground = Brushes.White;
                        break;
                    case "YLO":
                        tc_ColourState.Background = Brushes.Yellow;
                        break;
                    case "AMB":
                        tc_ColourState.Background = Brushes.Orange;
                        break;
                    case "RED":
                        tc_ColourState.Background = Brushes.Red;
                        tc_ColourState.Foreground = Brushes.White;
                        break;
                }
                table.RowGroups[0].Rows[h + 1].Cells.Add(tc_ColourState);
                TableCell tc_ObservationAge = new TableCell(new Paragraph(new Run((string)((Airfield)this.airfieldDataGrid.Items[h]).ObservationAge)));
                if (((Airfield)this.airfieldDataGrid.Items[h]).isInvalid) {
                    tc_ObservationAge.Background = Brushes.Red;
                    tc_ObservationAge.Foreground = Brushes.White;
                }
                table.RowGroups[0].Rows[h + 1].Cells.Add(tc_ObservationAge);
                table.RowGroups[0].Rows[h + 1].Cells.Add(new TableCell(new Paragraph(new Run((string)((Airfield)this.airfieldDataGrid.Items[h]).Observation))));
            }
            fd.Blocks.Add(table);
            return fd;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (this.comboBox1.SelectedItem == null || _worker.IsBusy == true || airfieldDataGrid.ItemsSource == null) {
                statusLabel.Content = "Not ready to print, please fetch a list of airfields first";
                return;
            }

            FlowDocument fd = null;
            PrintDialog printDlg = new PrintDialog();
            printDlg.PrintTicket.PageOrientation = PageOrientation.Landscape;
            if ((bool)printDlg.ShowDialog().GetValueOrDefault()) {
                fd = CreateDocument(printDlg);
            }

            try {
                DocumentPaginator paginator = ((IDocumentPaginatorSource)fd).DocumentPaginator;
                printDlg.PrintDocument(paginator, "METAR - " + this.comboBox1.SelectedItem.ToString());
            } catch (Exception ex) {
                this.statusLabel.Content = ex.Message;
            }
        }

        private void PrintPreview_Click(object sender, RoutedEventArgs e)
        {
            if (this.comboBox1.SelectedItem == null || _worker.IsBusy == true || airfieldDataGrid.ItemsSource == null) {
                statusLabel.Content = "Not ready to print, please fetch a list of airfields first";
                return;
            }

            FlowDocument fd = null;
            PrintDialog printDlg = new PrintDialog();
            printDlg.PrintTicket.PageOrientation = PageOrientation.Landscape;
            fd = CreateDocument(printDlg);
			fd.FontFamily = new FontFamily(PRINT_FONT_FAMILY);
			fd.FontSize = PRINT_FONT_SIZE;

            try {
                DocumentPaginator paginator = ((IDocumentPaginatorSource)fd).DocumentPaginator;

                string tempFileName = System.IO.Path.GetTempFileName();

                File.Delete(tempFileName);
                using (XpsDocument xpsDocument = new XpsDocument(tempFileName, FileAccess.ReadWrite)) {
                    XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                    writer.Write(paginator);

                    PrintPreview previewWindow = new PrintPreview {
                        Owner = this,
                        Document = xpsDocument.GetFixedDocumentSequence()
                    };
                    previewWindow.ShowDialog();
                }
            } catch (Exception ex) {
                this.statusLabel.Content = ex.Message;
            }
        }

		private void About_Click(object sender, RoutedEventArgs e)
		{
			this._aboutBox.Show();			
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void SetAsHome_Click(object sender, RoutedEventArgs e)
		{
			Airfield selectedAirfield = (Airfield)this.airfieldDataGrid.SelectedItem;
			Properties.Settings.Default.HomeLatitude = (double)selectedAirfield.Latitude;
			Properties.Settings.Default.HomeLongitude = (double)selectedAirfield.Longitude;
			Properties.Settings.Default.Save();
		}
	}
}
