using System;
using UnityEditor;
using UnityEngine;

namespace CustomConsole.Editor
{
	public enum ListViewOptions
	{
        None = 0,
		WantsReordering = 1,
		WantsExternalFiles,
		WantsToStartCustomDrag = 4,
		WantsToAcceptCustomDrag = 8
	}

	/// <summary>
	/// リスト表示
	/// </summary>
	public class ListViewGUI
	{
		private static readonly int listViewHash = "ListView".GetHashCode();
		private static readonly int[] dummyWidths = new int[1];

		public static InternalListViewState ilvState = new InternalListViewState();

		public static ListViewShared.ListViewElementsEnumerator ListView(Rect pos, ListViewState state)
		{
			return DoListView(pos, state, null, string.Empty);
		}

		public static ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, GUIStyle style, params GUILayoutOption[] options)
		{
			return ListView(state, (ListViewOptions)0, null, string.Empty, style, options);
		}

		public static ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, int[] colWidths, GUIStyle style, params GUILayoutOption[] options)
		{
			return ListView(state, (ListViewOptions)0, colWidths, string.Empty, style, options);
		}

		public static ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, ListViewOptions lvOptions, GUIStyle style, params GUILayoutOption[] options)
		{
			return ListView(state, lvOptions, null, string.Empty, style, options);
		}

		public static ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, ListViewOptions lvOptions, string dragTitle, GUIStyle style, params GUILayoutOption[] options)
		{
			return ListView(state, lvOptions, null, dragTitle, style, options);
		}

		public static ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, ListViewOptions lvOptions, int[] colWidths, string dragTitle, GUIStyle style, params GUILayoutOption[] options)
		{
			GUILayout.BeginHorizontal(style, new GUILayoutOption[0]);
			state.scrollPos = EditorGUILayout.BeginScrollView(state.scrollPos, options);

			ilvState.beganHorizontal = true;
			state.draggedFrom = -1;
			state.draggedTo = -1;
			state.fileNames = null;
			if ((lvOptions & ListViewOptions.WantsReordering) != (ListViewOptions)0)
			{
				ilvState.wantsReordering = true;
			}
			if ((lvOptions & ListViewOptions.WantsExternalFiles) != (ListViewOptions)0)
			{
				ilvState.wantsExternalFiles = true;
			}
			if ((lvOptions & ListViewOptions.WantsToStartCustomDrag) != (ListViewOptions)0)
			{
				ilvState.wantsToStartCustomDrag = true;
			}
			if ((lvOptions & ListViewOptions.WantsToAcceptCustomDrag) != (ListViewOptions)0)
			{
				ilvState.wantsToAcceptCustomDrag = true;
			}
			return DoListView(GUILayoutUtility.GetRect(1f, (float)(state.totalRows * state.rowHeight + 3)), state, colWidths, string.Empty);
		}

		public static ListViewShared.ListViewElementsEnumerator DoListView(Rect pos, ListViewState state, int[] colWidths, string dragTitle)
		{
			int controlID = GUIUtility.GetControlID(listViewHash, FocusType.Passive);
			state.ID = controlID;
			state.selectionChanged = false;
			Rect rect;
			if (GUIClipHelper.VisibleRect.x < 0f || GUIClipHelper.VisibleRect.y < 0f)
			{
				rect = pos;
			}
			else
			{
				rect = ((pos.y >= 0f) ? new Rect(0f, state.scrollPos.y, GUIClipHelper.VisibleRect.width, GUIClipHelper.VisibleRect.height)
					: new Rect(0f, 0f, GUIClipHelper.VisibleRect.width, GUIClipHelper.VisibleRect.height));
			}
			if (rect.width <= 0f)
			{
				rect.width = 1f;
			}
			if (rect.height <= 0f)
			{
				rect.height = 1f;
			}
			ilvState.rect = rect;
			int num = (int)((-pos.y + rect.yMin) / (float)state.rowHeight);
			int num2 = num + (int)Math.Ceiling((double)(((rect.yMin - pos.y) % (float)state.rowHeight + rect.height) / (float)state.rowHeight)) - 1;
			if (colWidths == null)
			{
				dummyWidths[0] = (int)rect.width;
				colWidths = dummyWidths;
			}
			ilvState.invisibleRows = num;
			ilvState.endRow = num2;
			ilvState.rectHeight = (int)rect.height;
			ilvState.state = state;
			if (num < 0)
			{
				num = 0;
			}
			if (num2 >= state.totalRows)
			{
				num2 = state.totalRows - 1;
			}
			return new ListViewShared.ListViewElementsEnumerator(ilvState, colWidths, num, num2, dragTitle, new Rect(0f, (float)(num * state.rowHeight), pos.width, (float)state.rowHeight));
		}

		//public static bool MultiSelection(int prevSelected, int currSelected, ref int initialSelected, ref bool[] selectedItems)
		//{
		//	return ListViewShared.MultiSelection(ilvState, prevSelected, currSelected, ref initialSelected, ref selectedItems);
		//}

		//public static bool HasMouseUp(Rect r)
		//{
		//	return ListViewShared.HasMouseUp(ilvState, r, 0);
		//}

		public static bool HasMouseDown(Rect r)
		{
			return ListViewShared.HasMouseDown(ilvState, r, 0);
		}

		public static bool HasMouseDown(Rect r, int button)
		{
			return ListViewShared.HasMouseDown(ilvState, r, button);
		}
	}
}
