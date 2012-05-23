using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using d = System.Drawing;

namespace FlightPlanWin
{
	public class PageElement : UserControl
	{
		#region Constants

		private const int PAGE_MARGIN = 40;
		private const int HEADER_HEIGHT = 50;
		private const int LINE_HEIGHT = 20;
		private const int COLUMN_WIDTH = 60;
		private const int HEADER_CHR_WIDTH = 9;
		private const int HEADER_LINE_HEIGHT = 12;
		private const string EXCAPE_CHAR = "\r\n";
		private const string NOT_APPLICAPLE = "N/A";

		#endregion Constants

		#region Fields

		private int _currentRow;
		private int _rows;
		private DataGrid _dataGrid;
		private List<string> _columns;
		private int _firstColumnWidth;
		private bool _isFirstColumnLarger;
		private static int _columnsPerPage;
		private Dictionary<int, Pair<int, int>> _columnSlot;
		private string _strTitle;
		private string _strComment;
		private string _strPageNumber;

		#endregion Fields

		#region Constructor

		public PageElement(int currentRow, int rows, DataGrid dataGrid, List<string> columns,
							int firstColumnWidth, bool isFirstColumnLarger, Dictionary<int,
								Pair<int, int>> columnSlot, string strTitle, string strComment,
								string strPageNumber)
		{
			Margin = new Thickness(PAGE_MARGIN);
			_currentRow = currentRow;
			_rows = rows;
			_dataGrid = dataGrid;
			_columns = columns;
			_firstColumnWidth = firstColumnWidth;
			_isFirstColumnLarger = isFirstColumnLarger;
			_columnSlot = columnSlot;
			_strTitle = strTitle;
			_strComment = strComment;
			_strPageNumber = strPageNumber;
		}

		#endregion Constructor

		#region Public Static Functions

		public static int RowsPerPage(double height)
		{
			//5 times Line Height deducted for: 1 for Title and Comments each; 2 for Page Number and 1 for Date
			return (int)Math.Floor((height - (2 * PAGE_MARGIN) - HEADER_HEIGHT - (5 * LINE_HEIGHT)) / LINE_HEIGHT);
		}

		public static int ColumnsPerPage(double width, int firstColumnWidth)
		{
			_columnsPerPage = (int)Math.Floor((width - (2 * PAGE_MARGIN) - firstColumnWidth) / COLUMN_WIDTH);
			return _columnsPerPage;
		}

		public static int CalculateBitLength(string strData, d.Font font)
		{
			using (d.Graphics graphics = d.Graphics.FromImage(new d.Bitmap(1, 1))) {
				d.SizeF dtsize = graphics.MeasureString(strData, font);

				return Convert.ToInt32(dtsize.Width);
			}
		}

		#endregion Public Static Functions

		#region Private Functions

		private static FormattedText MakeText(string text, int fontSize)
		{
			return new FormattedText(text, CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight, new Typeface("Tahoma"), fontSize, Brushes.Black);
		}

		#endregion Private Functions

		#region Protected Functions

		protected override void OnRender(DrawingContext dc)
		{
			Point curPoint = new Point(0, 0);
			int beginCounter = 0;
			double intYAxisTracker = 0;
			double TitleHeight = 0;

			//Print Title.
			if (_strTitle != null) {
				int intTitleLength = CalculateBitLength(_strTitle, new d.Font("Tahoma", 9, d.FontStyle.Regular));
				curPoint.X = ((Width - (2 * PAGE_MARGIN)) / 2) - (intTitleLength / 2);
				dc.DrawText(MakeText(_strTitle, 9), curPoint);
				curPoint.Y += LINE_HEIGHT;
				curPoint.X = 0;
			}

			//Print Comment.
			if (_strTitle != null) {
				int intCommentLength = CalculateBitLength(_strComment, new d.Font("Tahoma", 9, d.FontStyle.Regular));
				curPoint.X = ((Width - (2 * PAGE_MARGIN)) / 2) - (intCommentLength / 2);
				dc.DrawText(MakeText(_strComment, 9), curPoint);
				curPoint.Y += LINE_HEIGHT;
				curPoint.X = 0;
			}

			//Print current Date.
			int intDatLength = CalculateBitLength(String.Format("{0:MMMM dd, yyyy}", DateTime.Now), new d.Font("Tahoma", 9, d.FontStyle.Regular));
			curPoint.X = ((Width - (2 * PAGE_MARGIN)) / 2) - (intDatLength / 2);
			dc.DrawText(MakeText(String.Format("{0:MMMM dd, yyyy}", DateTime.Now), 9), curPoint);
			curPoint.Y += LINE_HEIGHT;
			curPoint.X = 0;

			TitleHeight = curPoint.Y;

			//Print First column of header row.
			dc.DrawText(MakeText(_columns[0], 9), curPoint);
			curPoint.X += _firstColumnWidth;
			beginCounter = _columnSlot[1].Start;

			//Print other columns of header row
			for (int i = beginCounter; i <= _columnSlot[1].End; i++) {
				//Remove unwanted characters
				_columns[i] = _columns[i].Replace(EXCAPE_CHAR, " ");

				if (_columns[i].Length > HEADER_CHR_WIDTH) {
					//Loop through to wrap the header text
					for (int k = 0; k < _columns[i].Length; k += HEADER_CHR_WIDTH) {
						int subsLength = k > _columns[i].Length - HEADER_CHR_WIDTH ? _columns[i].Length - k : HEADER_CHR_WIDTH;
						dc.DrawText(MakeText(_columns[i].Substring(k, subsLength), 9), curPoint);
						curPoint.Y += HEADER_LINE_HEIGHT;
					}
				} else
					dc.DrawText(MakeText(_columns[i], 9), curPoint);

				//YAxisTracker keeps track of maximum lines used to print the headers.
				intYAxisTracker = intYAxisTracker < curPoint.Y ? curPoint.Y : intYAxisTracker;
				curPoint.X += COLUMN_WIDTH;
				curPoint.Y = TitleHeight;
			}

			//Reset X and Y pointers
			curPoint.X = 0;
			curPoint.Y += intYAxisTracker - TitleHeight;

			//Draw a solid line
			dc.DrawRectangle(Brushes.Black, null, new Rect(curPoint, new Size(Width, 2)));
			curPoint.Y += HEADER_HEIGHT - (2 * LINE_HEIGHT);

			//Loop through each collection in dataGrid to print the data
			for (int i = _currentRow; i < _currentRow + _rows; i++) {
				List<Object> icol = (List<Object>)_dataGrid.Items[i];

				//Print first column data
				dc.DrawText(MakeText(icol[0].ToString(), 10), curPoint);
				curPoint.X += _firstColumnWidth;
				beginCounter = _columnSlot[1].Start;

				//Loop through items in the collection; Loop only the items for currect column slot.
				for (int j = beginCounter; j <= _columnSlot[1].End; j++) {
					dc.DrawText(MakeText(icol[j] == null ? NOT_APPLICAPLE : icol[j].ToString(), 10), curPoint);
					curPoint.X += COLUMN_WIDTH;
				}
				curPoint.Y += LINE_HEIGHT;
				curPoint.X = 0;
			}

			//Print Page numbers
			curPoint.Y = Height - (2 * PAGE_MARGIN) - LINE_HEIGHT;
			curPoint.X = Width - (2 * PAGE_MARGIN) - COLUMN_WIDTH;

			dc.DrawText(MakeText(_strPageNumber, 9), curPoint);

		}

		#endregion Protected Functions
	}
}
