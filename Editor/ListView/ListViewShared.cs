using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CustomConsole.Editor
{
	public struct ListViewElement
	{
		public int row;
		public int column;
		public Rect position;
	}

	public static class GUIClipHelper
	{
#if UNITY_2018_2_OR_NEWER
		private static readonly Type GUIClip = Assembly.Load("UnityEngine/UnityEngine.IMGUIModule.dll").GetType("UnityEngine.GUIClip");
#else
		private static readonly Type GUIClip = Assembly.Load("UnityEngine.dll").GetType("UnityEngine.GUIClip");
#endif
		public static Rect VisibleRect
		{
			get
			{
#if UNITY_2018_2_OR_NEWER
				return (Rect)GUIClip.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, new object[0]);
#else
				return (Rect)GUIClip.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.Public).GetValue(null, new object[0]);
#endif
			}
		}
	}

	public class InternalLayoutedListViewState : InternalListViewState
	{
		public GUILayoutedListViewGroup group;
	}

	public class InternalListViewState
	{
		public int id = -1;

		public int invisibleRows;

		public int endRow;

		public int rectHeight;

		public ListViewState state;

		public bool beganHorizontal = false;

		public Rect rect;

		public bool wantsReordering = false;

		public bool wantsExternalFiles = false;

		public bool wantsToStartCustomDrag = false;

		public bool wantsToAcceptCustomDrag = false;

		public int dragItem;
	}

	public class DragAndDropDelay
	{
		public Vector2 mouseDownPosition;

		public bool CanStartDrag()
		{
			return Vector2.Distance(mouseDownPosition, Event.current.mousePosition) > 6f;
		}
	}
	
	public class ListViewShared
	{
		public static bool OSX = Application.platform == RuntimePlatform.OSXEditor;
		static int dragControlID = -1;
		static bool isDragging = false;

		public class ListViewElementsEnumerator : IEnumerator<ListViewElement>, IDisposable, IEnumerator
		{
			private int[] colWidths;
			private int xTo;
			private int yFrom;
			private int yTo;
			private Rect firstRect;
			private Rect rect;
			private int xPos = -1;
			private int yPos = -1;

			private ListViewElement element;
			private InternalListViewState ilvState;
			private InternalLayoutedListViewState ilvStateL;

			private bool quiting;
			private bool isLayouted;
			private string dragTitle;

			ListViewElement IEnumerator<ListViewElement>.Current
			{
				get
				{
					return element;
				}
			}

			object IEnumerator.Current { get { return element; } }

			public ListViewElementsEnumerator(InternalListViewState ilvState, int[] colWidths, int yFrom, int yTo, string dragTitle, Rect firstRect)
			{
				this.colWidths = colWidths;
				xTo = colWidths.Length - 1;
				this.yFrom = yFrom;
				this.yTo = yTo;
				this.firstRect = firstRect;
				rect = firstRect;
				quiting = (ilvState.state.totalRows == 0);
				this.ilvState = ilvState;
				ilvStateL = (ilvState as InternalLayoutedListViewState);
				isLayouted = (ilvStateL != null); ;
				this.dragTitle = dragTitle;
				ilvState.state.customDraggedFromID = 0;
				Reset();
			}

			public bool MoveNext()
			{
				try
				{
					if (xPos > -1)
					{
						if (HasMouseDown(ilvState, rect))
						{
							ilvState.state.selectionChanged = true;
							ilvState.state.currentRow = yPos;
							ilvState.state.column = xPos;
							ilvState.state.scrollPos = ListViewScrollToRow(ilvState, yPos);
							if ((ilvState.wantsReordering || ilvState.wantsToStartCustomDrag) && GUIUtility.hotControl == ilvState.state.ID)
							{
								DragAndDropDelay dragAndDropDelay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), ilvState.state.ID);
								dragAndDropDelay.mouseDownPosition = Event.current.mousePosition;
								ilvState.dragItem = yPos;
								dragControlID = ilvState.state.ID;
							}
						}

						Rect visibleRect = GUIClipHelper.VisibleRect;

						if (!isDragging && (ilvState.wantsReordering || ilvState.wantsToAcceptCustomDrag)
							&& GUIUtility.hotControl == ilvState.state.ID && Event.current.type == EventType.MouseDrag
							&& visibleRect.Contains(Event.current.mousePosition))
						{
							DragAndDropDelay dragAndDropDelay2 = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), ilvState.state.ID);
							if (dragAndDropDelay2.CanStartDrag())
							{
								DragAndDrop.PrepareStartDrag();
								DragAndDrop.objectReferences = new UnityEngine.Object[0];
								DragAndDrop.paths = null;
								if (ilvState.wantsReordering)
								{
									ilvState.state.dropHereRect = new Rect(ilvState.rect.x, 0f, ilvState.rect.width, (float)(ilvState.state.rowHeight * 2));
									DragAndDrop.StartDrag(dragTitle);
								}
								else if (ilvState.wantsToStartCustomDrag)
								{
									DragAndDrop.SetGenericData("CustomDragID", ilvState.state.ID);
									DragAndDrop.StartDrag(dragTitle);
								}
								isDragging = true;
							}
							Event.current.Use();
						}
					}

					xPos++;

					if (xPos > xTo)
					{
						xPos = 0;
						yPos++;
						rect.x = firstRect.x;
						rect.width = (float)colWidths[0];
						if (yPos > yTo)
						{
							quiting = true;
						}
						else
						{
							rect.y = rect.y + rect.height;
						}
					}
					else
					{
						if (xPos >= 1)
						{
							rect.x = rect.x + (float)colWidths[xPos - 1];
						}
						rect.width = (float)colWidths[xPos];
					}

					element.row = yPos;
					element.column = xPos;
					element.position = rect;

					if (element.row >= ilvState.state.totalRows)
					{
						quiting = true;
					}

					if (isLayouted && Event.current.type == EventType.Layout)
					{
						if (yFrom + 1 == yPos)
						{
							quiting = true;
						}
					}

					if (isLayouted && yPos != yFrom)
					{
						GUILayout.EndHorizontal();
					}

					if (quiting)
					{
						if (ilvState.state.drawDropHere && Event.current.GetTypeForControl(ilvState.state.ID) == EventType.Repaint)
						{
							GUIStyle gUIStyle = "PR Insertion";
							gUIStyle.Draw(gUIStyle.margin.Remove(ilvState.state.dropHereRect), false, false, false, false);
						}

						if (ListViewKeyboard(ilvState, colWidths.Length))
						{
							ilvState.state.selectionChanged = true;
						}

						if (Event.current.GetTypeForControl(ilvState.state.ID) == EventType.MouseUp)
						{
							GUIUtility.hotControl = 0;
						}

						if (ilvState.wantsReordering && GUIUtility.hotControl == ilvState.state.ID)
						{
							ListViewState state = ilvState.state;
							EventType type = Event.current.type;
							if (type != EventType.DragUpdated)
							{
								if (type != EventType.DragPerform)
								{
									if (type == EventType.DragExited)
									{
										ilvState.wantsReordering = false;
										ilvState.state.drawDropHere = false;
										GUIUtility.hotControl = 0;
									}
								}
								else
								{
									if (GUIClipHelper.VisibleRect.Contains(Event.current.mousePosition))
									{
										ilvState.state.draggedFrom = ilvState.dragItem;
										ilvState.state.draggedTo = Mathf.RoundToInt(Event.current.mousePosition.y / (float)state.rowHeight);
										if (ilvState.state.draggedTo > ilvState.state.totalRows)
										{
											ilvState.state.draggedTo = ilvState.state.totalRows;
										}
										if (ilvState.state.draggedTo > ilvState.state.draggedFrom)
										{
											ilvState.state.currentRow = ilvState.state.draggedTo - 1;
										}
										else
										{
											ilvState.state.currentRow = ilvState.state.draggedTo;
										}
										ilvState.state.selectionChanged = true;
										DragAndDrop.AcceptDrag();
										Event.current.Use();
										ilvState.wantsReordering = false;
										ilvState.state.drawDropHere = false;
									}
									GUIUtility.hotControl = 0;
								}
							}
							else
							{
								DragAndDrop.visualMode = ((!ilvState.rect.Contains(Event.current.mousePosition)) ? DragAndDropVisualMode.None : DragAndDropVisualMode.Move);
								Event.current.Use();
								if (DragAndDrop.visualMode != DragAndDropVisualMode.None)
								{
									state.dropHereRect.y = (float)((Mathf.RoundToInt(Event.current.mousePosition.y / (float)state.rowHeight) - 1) * state.rowHeight);
									if (state.dropHereRect.y >= (float)(state.rowHeight * state.totalRows))
									{
										state.dropHereRect.y = (float)(state.rowHeight * (state.totalRows - 1));
									}
									state.drawDropHere = true;
								}
							}
						}
						else if (ilvState.wantsExternalFiles)
						{
							EventType type2 = Event.current.type;
							if (type2 != EventType.DragUpdated)
							{
								if (type2 != EventType.DragPerform)
								{
									if (type2 == EventType.DragExited)
									{
										ilvState.wantsExternalFiles = false;
										ilvState.state.drawDropHere = false;
										GUIUtility.hotControl = 0;
									}
								}
								else
								{
									if (GUIClipHelper.VisibleRect.Contains(Event.current.mousePosition))
									{
										ilvState.state.fileNames = DragAndDrop.paths;
										DragAndDrop.AcceptDrag();
										Event.current.Use();
										ilvState.wantsExternalFiles = false;
										ilvState.state.drawDropHere = false;
										ilvState.state.draggedTo = Mathf.RoundToInt(Event.current.mousePosition.y / (float)ilvState.state.rowHeight);
										if (ilvState.state.draggedTo > ilvState.state.totalRows)
										{
											ilvState.state.draggedTo = ilvState.state.totalRows;
										}
										ilvState.state.currentRow = ilvState.state.draggedTo;
									}
									GUIUtility.hotControl = 0;
								}
							}
							else if (GUIClipHelper.VisibleRect.Contains(Event.current.mousePosition) && DragAndDrop.paths != null && DragAndDrop.paths.Length != 0)
							{
								DragAndDrop.visualMode = ((!ilvState.rect.Contains(Event.current.mousePosition)) ? DragAndDropVisualMode.None : DragAndDropVisualMode.Copy);
								Event.current.Use();
								if (DragAndDrop.visualMode != DragAndDropVisualMode.None)
								{
									ilvState.state.dropHereRect = new Rect(ilvState.rect.x, (float)((Mathf.RoundToInt(Event.current.mousePosition.y / (float)ilvState.state.rowHeight) - 1) * ilvState.state.rowHeight), ilvState.rect.width, (float)ilvState.state.rowHeight);
									if (ilvState.state.dropHereRect.y >= (float)(ilvState.state.rowHeight * ilvState.state.totalRows))
									{
										ilvState.state.dropHereRect.y = (float)(ilvState.state.rowHeight * (ilvState.state.totalRows - 1));
									}
									ilvState.state.drawDropHere = true;
								}
							}
						}
						else if (ilvState.wantsToAcceptCustomDrag && dragControlID != ilvState.state.ID)
						{
							EventType type3 = Event.current.type;
							if (type3 != EventType.DragUpdated)
							{
								if (type3 != EventType.DragPerform)
								{
									if (type3 == EventType.DragExited)
									{
										GUIUtility.hotControl = 0;
									}
								}
								else
								{
									object genericData = DragAndDrop.GetGenericData("CustomDragID");
									if (GUIClipHelper.VisibleRect.Contains(Event.current.mousePosition) && genericData != null)
									{
										ilvState.state.customDraggedFromID = (int)genericData;
										DragAndDrop.AcceptDrag();
										Event.current.Use();
									}
									GUIUtility.hotControl = 0;
								}
							}
							else
							{
								object genericData2 = DragAndDrop.GetGenericData("CustomDragID");
								if (GUIClipHelper.VisibleRect.Contains(Event.current.mousePosition) && genericData2 != null)
								{
									DragAndDrop.visualMode = ((!ilvState.rect.Contains(Event.current.mousePosition)) ? DragAndDropVisualMode.None : DragAndDropVisualMode.Move);
									Event.current.Use();
								}
							}
						}

						if (ilvState.beganHorizontal)
						{
							EditorGUILayout.EndScrollView();
							GUILayout.EndHorizontal();
							ilvState.beganHorizontal = false;
						}

						if (isLayouted)
						{
							typeof(GUILayoutUtility).GetMethod("EndLayoutGroup", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[0]);
							//GUILayoutUtility.EndLayoutGroup();
							EditorGUILayout.EndScrollView();
						}

						ilvState.wantsReordering = false;
						ilvState.wantsExternalFiles = false;

					}
					else if (isLayouted)
					{
						if (yPos != yFrom)
						{
							ilvStateL.group.ResetCursor();
							ilvStateL.group.AddY();
						}
						else
						{
							ilvStateL.group.AddY((float)(ilvState.invisibleRows * ilvState.state.rowHeight));
						}
					}

					if (isLayouted)
					{
						if (!quiting)
						{
							GUILayout.BeginHorizontal(GUIStyle.none, new GUILayoutOption[0]);
						}
						else
						{
							GUILayout.EndHorizontal();
						}
					}
					return !quiting;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					return false;
				}
			}

			internal IEnumerator GetEnumerator()
			{
				return this;
			}

			public void Dispose()
			{
			}

			public void Reset()
			{
				xPos = -1;
				yPos = yFrom;
			}
		}

		static Vector2 ListViewScrollToRow(InternalListViewState ilvState, int row)
		{
			return ListViewScrollToRow(ilvState, ilvState.state.scrollPos, row);
		}

		static int ListViewScrollToRow(InternalListViewState ilvState, int currPosY, int row)
		{
			return (int)ListViewScrollToRow(ilvState, new Vector2(0f, (float)currPosY), row).y;
		}

		static Vector2 ListViewScrollToRow(InternalListViewState ilvState, Vector2 currPos, int row)
		{
			Vector2 result;
			if (ilvState.invisibleRows < row && ilvState.endRow > row)
			{
				result = currPos;
			}
			else
			{
				if (row <= ilvState.invisibleRows)
				{
					currPos.y = (float)(ilvState.state.rowHeight * row);
				}
				else
				{
					currPos.y = (float)(ilvState.state.rowHeight * (row + 1) - ilvState.rectHeight);
				}
				if (currPos.y < 0f)
				{
					currPos.y = 0f;
				}
				else if (currPos.y > (float)(ilvState.state.totalRows * ilvState.state.rowHeight - ilvState.rectHeight))
				{
					currPos.y = (float)(ilvState.state.totalRows * ilvState.state.rowHeight - ilvState.rectHeight);
				}
				result = currPos;
			}
			return result;
		}

		static bool ListViewKeyboard(InternalListViewState ilvState, int totalCols)
		{
			int totalRows = ilvState.state.totalRows;
			return Event.current.type == EventType.KeyDown && totalRows != 0
				&& GUIUtility.keyboardControl == ilvState.state.ID && Event.current.GetTypeForControl(ilvState.state.ID) == EventType.KeyDown
				&& SendKey(ilvState, Event.current.keyCode, totalCols);
		}

		static void SendKey(ListViewState state, KeyCode keyCode)
		{
			SendKey(state.ilvState, keyCode, 1);
		}

		static bool SendKey(InternalListViewState ilvState, KeyCode keyCode, int totalCols)
		{
			ListViewState state = ilvState.state;
			bool result;
			switch (keyCode)
			{
				case KeyCode.UpArrow:
					if (state.currentRow > 0)
					{
						state.currentRow--;
					}
					goto IL_14C;
				case KeyCode.DownArrow:
					if (state.currentRow < state.totalRows - 1)
					{
						state.currentRow++;
					}
					goto IL_14C;
				case KeyCode.RightArrow:
					if (state.column < totalCols - 1)
					{
						state.column++;
					}
					goto IL_14C;
				case KeyCode.LeftArrow:
					if (state.column > 0)
					{
						state.column--;
					}
					goto IL_14C;
				case KeyCode.Home:
					state.currentRow = 0;
					goto IL_14C;
				case KeyCode.End:
					state.currentRow = state.totalRows - 1;
					goto IL_14C;
				case KeyCode.PageUp:
					if (!DoLVPageUpDown(ilvState, ref state.currentRow, ref state.scrollPos, true))
					{
						Event.current.Use();
						result = false;
						return result;
					}
					goto IL_14C;
				case KeyCode.PageDown:
					if (!DoLVPageUpDown(ilvState, ref state.currentRow, ref state.scrollPos, false))
					{
						Event.current.Use();
						result = false;
						return result;
					}
					goto IL_14C;
			}
			result = false;
			return result;
			IL_14C:
			state.scrollPos = ListViewScrollToRow(ilvState, state.scrollPos, state.currentRow);
			Event.current.Use();
			result = true;
			return result;
		}

		public static bool HasMouseDown(InternalListViewState ilvState, Rect r)
		{
			return HasMouseDown(ilvState, r, 0);
		}

		public static bool HasMouseDown(InternalListViewState ilvState, Rect r, int button)
		{
			bool result;
			if (Event.current.type == EventType.MouseDown && Event.current.button == button)
			{
				if (r.Contains(Event.current.mousePosition))
				{
					GUIUtility.hotControl = ilvState.state.ID;
					GUIUtility.keyboardControl = ilvState.state.ID;
					isDragging = false;
					Event.current.Use();
					result = true;
					return result;
				}
			}
			result = false;
			return result;
		}

		public static bool DoLVPageUpDown(InternalListViewState ilvState, ref int selectedRow, ref Vector2 scrollPos, bool up)
		{
			int num = ilvState.endRow - ilvState.invisibleRows;
			bool result;
			if (up)
			{
				if (!OSX)
				{
					selectedRow -= num;
					if (selectedRow < 0)
					{
						selectedRow = 0;
					}
					result = true;
					return result;
				}
				scrollPos.y -= (float)(ilvState.state.rowHeight * num);
				if (scrollPos.y < 0f)
				{
					scrollPos.y = 0f;
				}
			}
			else
			{
				if (!OSX)
				{
					selectedRow += num;
					if (selectedRow >= ilvState.state.totalRows)
					{
						selectedRow = ilvState.state.totalRows - 1;
					}
					result = true;
					return result;
				}
				scrollPos.y += (float)(ilvState.state.rowHeight * num);
			}
			result = false;
			return result;
		}
	}
}