using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows;
using System.Drawing;

namespace FlightPlanWin
{
	public class DocPaginator : DocumentPaginator
	{
		#region Fields

		private int _rows;
		private int _columns;
		private int _rowsPerPage;
		private int _columnsPerPage;
		private int _firstColumnWidth;
		private int _horizontalPageCount;
		private int _verticalPageCount;
		private System.Windows.Size _pageSize;
		private DataGrid _dataGrid;
		private List<string> _columnsList;
		private bool _isFirstColumnLarger;
		private List<Dictionary<int, Pair<int, int>>> _columnSlotsList;
		private string _strTitle;
		private string _strComment;

		#endregion Fields

		#region Constructor

		public DocPaginator(DataGrid dataGrid, System.Windows.Size pageSize, List<string> columnsList,
								bool isFirstColumnLarger = false, string strTitle = null, string strComment = null)
		{
			_rows = dataGrid.Items.Count;
			_columns = dataGrid.Columns.Count;
			_dataGrid = dataGrid;
			_columnsList = columnsList;
			_isFirstColumnLarger = isFirstColumnLarger;
			_strTitle = strTitle;
			_strComment = strComment;

			CalculateFirstColumnWidth();

			PageSize = pageSize;

			_horizontalPageCount = HorizontalPageCount;
			_verticalPageCount = VerticalPageCount;

			GenerateColumnSlots();
		}

		#endregion Constructor

		#region Public Methods

		public override DocumentPage GetPage(int pageNumber)
		{
			double pgNumber = Math.IEEERemainder(pageNumber, _verticalPageCount) >= 0 ?
								Math.IEEERemainder(pageNumber, _verticalPageCount) :
									Math.IEEERemainder(pageNumber, _verticalPageCount) + _verticalPageCount;

			int currentRow = Convert.ToInt32(_rowsPerPage * pgNumber);

			var page = new PageElement(currentRow, Math.Min(_rowsPerPage, _rows - currentRow), _dataGrid,
											_columnsList, _firstColumnWidth, _isFirstColumnLarger,
												GetColumnSlot(pageNumber), _strTitle, _strComment, GetPageNumber(pageNumber)) {
													Width = PageSize.Width,
													Height = PageSize.Height,
												};

			page.Measure(PageSize);
			page.Arrange(new Rect(new System.Windows.Point(0, 0), PageSize));

			return new DocumentPage(page);
		}



		#endregion Public Methods

		#region Public Properties

		public override bool IsPageCountValid
		{ get { return true; } }

		public override int PageCount
		{ get { return _horizontalPageCount * _verticalPageCount; } }

		public override System.Windows.Size PageSize
		{
			get { return _pageSize; }
			set
			{
				_pageSize = value;
				_rowsPerPage = PageElement.RowsPerPage(PageSize.Height);
				_columnsPerPage = PageElement.ColumnsPerPage(PageSize.Width, _firstColumnWidth);

				//Can't print anything if you can't fit a row on a page
				Debug.Assert(_rowsPerPage > 0);
			}
		}

		public override IDocumentPaginatorSource Source
		{ get { return null; } }

		public int HorizontalPageCount
		{
			get { return (int)Math.Ceiling((_columns - 1) / (double)_columnsPerPage); }
		}

		public int VerticalPageCount
		{
			get { return (int)Math.Ceiling(_rows / (double)_rowsPerPage); }
		}

		#endregion Public Properties

		#region Private Methods

		private void CalculateFirstColumnWidth()
		{
			int maxDataLen = 0;

			for (int i = 0; i < _dataGrid.Items.Count; i++) {
				List<Object> icol = (List<Object>)_dataGrid.Items[i];
				var largestDataItem = (from d in icol
									   select d != null ? d.ToString().Length : 0).Max();
				maxDataLen = maxDataLen < largestDataItem ? largestDataItem : maxDataLen;
			}

			string strDataLen = string.Join("a", new string[maxDataLen + 1]);

			_firstColumnWidth = PageElement.CalculateBitLength(strDataLen,
												new Font("Tahoma", 8, System.Drawing.FontStyle.Regular, GraphicsUnit.Point));
		}

		private void GenerateColumnSlots()
		{
			_columnSlotsList = new List<Dictionary<int, Pair<int, int>>>();

			for (int i = 0; i < _horizontalPageCount; i++) {
				Dictionary<int, Pair<int, int>> columnSlot = new Dictionary<int, Pair<int, int>>();
				columnSlot.Add(1, new Pair<int, int>((_columnsPerPage * i) + 1,
														Math.Min(_columnsPerPage * (i + 1), _columns - 1)));

				_columnSlotsList.Add(columnSlot);
			}
		}

		private Dictionary<int, Pair<int, int>> GetColumnSlot(int pageNumber)
		{
			for (int i = 0; i <= _columnSlotsList.Count; i++) {
				if (i == Math.Ceiling(Convert.ToDouble(pageNumber / _verticalPageCount)))
					return _columnSlotsList[i];
			}
			return new Dictionary<int, Pair<int, int>>();
		}

		private string GetPageNumber(int intPageNumber)
		{
			string strPageNumber = String.Empty;

			if (_horizontalPageCount == 1)
				strPageNumber = (intPageNumber + 1).ToString();
			else { }

			return strPageNumber;
		}

		#endregion Private Methods
	}

	#region Pair Class

	public class Pair<TStart, TEnd>
	{
		public Pair(TStart start, TEnd end)
		{
			Start = start;
			End = end;
		}

		public TStart Start { get; set; }
		public TEnd End { get; set; }
	}

	#endregion
}
