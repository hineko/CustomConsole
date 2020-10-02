using System;
using UnityEngine;

namespace CustomConsole.Editor
{
	[Serializable]
	public class ListViewState
	{
		private const int c_rowHeight = 16;

		public int currentRow;
		public int column;
		public Vector2 scrollPos;
		public int totalRows;
		public int rowHeight;
		public int ID;
		public bool selectionChanged;
		public int draggedFrom;
		public int draggedTo;
		public bool drawDropHere = false;
		public Rect dropHereRect = new Rect(0f, 0f, 0f, 0f);
		public string[] fileNames = null;
		public int customDraggedFromID = 0;
		public InternalLayoutedListViewState ilvState = new InternalLayoutedListViewState();

		public ListViewState()
		{
			Init(0, 16);
		}

		public ListViewState(int totalRows)
		{
			Init(totalRows, 16);
		}

		public ListViewState(int totalRows, int rowHeight)
		{
			Init(totalRows, rowHeight);
		}

		private void Init(int totalRows, int rowHeight)
		{
			currentRow = -1;
			column = 0;
			scrollPos = Vector2.zero;
			this.totalRows = totalRows;
			this.rowHeight = rowHeight;
			selectionChanged = false;
		}
	}
}
