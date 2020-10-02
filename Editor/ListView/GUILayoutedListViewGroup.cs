using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CustomConsole.Editor
{
	public enum GUILayoutOptionType
	{
		fixedWidth,
		fixedHeight,
		minWidth,
		maxWidth,
		minHeight,
		maxHeight,
		stretchWidth,
		stretchHeight,
		alignStart,
		alignMiddle,
		alignEnd,
		alignJustify,
		equalSize,
		spacing
	}

	public class GUILayoutedListViewGroup : GUILayoutGroup
	{
		internal ListViewState State;

		public override void CalcWidth()
		{
			base.CalcWidth();
			minWidth = 0f;
			maxWidth = 0f;
			stretchWidth = 10000;
		}

		public override void CalcHeight()
		{
			minHeight = 0f;
			maxHeight = 0f;
			base.CalcHeight();
			margin.top = 0;
			margin.bottom = 0;
			if (minHeight == 0f)
			{
				minHeight = 1f;
				maxHeight = 1f;
				State.rowHeight = 1;
			}
			else
			{
				State.rowHeight = (int)minHeight;
				minHeight *= (float)State.totalRows;
				maxHeight *= (float)State.totalRows;
			}
		}

		private void AddYRecursive(GUILayoutEntry e, float y)
		{
			e.rect.y = e.rect.y + y;
			GUILayoutGroup gUILayoutGroup = e as GUILayoutGroup;
			if (gUILayoutGroup != null)
			{
				for (int i = 0; i < gUILayoutGroup.entries.Count; i++)
				{
					AddYRecursive(gUILayoutGroup.entries[i], y);
				}
			}
		}

		public void AddY()
		{
			if (entries.Count > 0)
			{
				AddYRecursive(entries[0], entries[0].minHeight);
			}
		}

		public void AddY(float val)
		{
			if (entries.Count > 0)
			{
				AddYRecursive(entries[0], val);
			}
		}
	}

	public class GUILayoutOptionHelper
	{
		public static GUILayoutOptionType GetType(object gUILayoutOption)
		{
			return (GUILayoutOptionType)typeof(GUILayoutOption).GetField("type", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gUILayoutOption);
		}

		public static float GetValue(object gUILayoutOption)
		{
			return (float)typeof(GUILayoutOption).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gUILayoutOption);
		}
	}

	#region Origin

	public class GUILayoutEntry
	{
		public float minWidth;
		public float maxWidth;
		public float minHeight;
		public float maxHeight;

		public Rect rect = new Rect(0f, 0f, 0f, 0f);

		public int stretchWidth;
		public int stretchHeight;

		private GUIStyle m_Style = GUIStyle.none;

		internal static Rect kDummyRect = new Rect(0f, 0f, 1f, 1f);

		protected static int indent = 0;

		public GUIStyle style
		{
			get
			{
				return m_Style;
			}
			set
			{
				m_Style = value;
				ApplyStyleSettings(value);
			}
		}

		public virtual RectOffset margin
		{
			get
			{
				return style.margin;
			}
		}

		public GUILayoutEntry(float _minWidth, float _maxWidth, float _minHeight, float _maxHeight, GUIStyle _style)
		{
			minWidth = _minWidth;
			maxWidth = _maxWidth;
			minHeight = _minHeight;
			maxHeight = _maxHeight;
			if (_style == null)
			{
				_style = GUIStyle.none;
			}
			style = _style;
		}

		public GUILayoutEntry(float _minWidth, float _maxWidth, float _minHeight, float _maxHeight, GUIStyle _style, GUILayoutOption[] options)
		{
			minWidth = _minWidth;
			maxWidth = _maxWidth;
			minHeight = _minHeight;
			maxHeight = _maxHeight;
			style = _style;
			ApplyOptions(options);
		}

		public virtual void CalcWidth()
		{
		}

		public virtual void CalcHeight()
		{
		}

		public virtual void SetHorizontal(float x, float width)
		{
			rect.x = x;
			rect.width = width;
		}

		public virtual void SetVertical(float y, float height)
		{
			rect.y = y;
			rect.height = height;
		}

		protected virtual void ApplyStyleSettings(GUIStyle style)
		{
			stretchWidth = ((style.fixedWidth != 0f || !style.stretchWidth) ? 0 : 1);
			stretchHeight = ((style.fixedHeight != 0f || !style.stretchHeight) ? 0 : 1);
			m_Style = style;
		}

		public virtual void ApplyOptions(GUILayoutOption[] options)
		{
			if (options != null)
			{
				for (int i = 0; i < options.Length; i++)
				{
					GUILayoutOption gUILayoutOption = options[i];

					GUILayoutOptionType type = GUILayoutOptionHelper.GetType(gUILayoutOption);

					switch (type)
					{
						case GUILayoutOptionType.fixedWidth:
							minWidth = (maxWidth = GUILayoutOptionHelper.GetValue(gUILayoutOption));
							stretchWidth = 0;
							break;
						case GUILayoutOptionType.fixedHeight:
							minHeight = (maxHeight = GUILayoutOptionHelper.GetValue(gUILayoutOption));
							stretchHeight = 0;
							break;
						case GUILayoutOptionType.minWidth:
							minWidth = GUILayoutOptionHelper.GetValue(gUILayoutOption);
							if (maxWidth < minWidth)
							{
								maxWidth = minWidth;
							}
							break;
						case GUILayoutOptionType.maxWidth:
							maxWidth = GUILayoutOptionHelper.GetValue(gUILayoutOption);
							if (minWidth > maxWidth)
							{
								minWidth = maxWidth;
							}
							stretchWidth = 0;
							break;
						case GUILayoutOptionType.minHeight:
							minHeight = GUILayoutOptionHelper.GetValue(gUILayoutOption);
							if (maxHeight < minHeight)
							{
								maxHeight = minHeight;
							}
							break;
						case GUILayoutOptionType.maxHeight:
							maxHeight = GUILayoutOptionHelper.GetValue(gUILayoutOption);
							if (minHeight > maxHeight)
							{
								minHeight = maxHeight;
							}
							stretchHeight = 0;
							break;
						case GUILayoutOptionType.stretchWidth:
							stretchWidth = (int)GUILayoutOptionHelper.GetValue(gUILayoutOption);
							break;
						case GUILayoutOptionType.stretchHeight:
							stretchHeight = (int)GUILayoutOptionHelper.GetValue(gUILayoutOption);
							break;
					}
				}
				if (maxWidth != 0f && maxWidth < minWidth)
				{
					maxWidth = minWidth;
				}
				if (maxHeight != 0f && maxHeight < minHeight)
				{
					maxHeight = minHeight;
				}
			}
		}

		public override string ToString()
		{
			string text = "";
			for (int i = 0; i < GUILayoutEntry.indent; i++)
			{
				text += " ";
			}
			return string.Concat(new object[]
			{
				text,
				string.Format("{1}-{0} (x:{2}-{3}, y:{4}-{5})", new object[]
				{
					(style == null) ? "NULL" : style.name,
					base.GetType(),
					rect.x,
					rect.xMax,
					rect.y,
					rect.yMax
				}),
				"   -   W: ",
				minWidth,
				"-",
				maxWidth,
				(stretchWidth == 0) ? "" : "+",
				", H: ",
				minHeight,
				"-",
				maxHeight,
				(stretchHeight == 0) ? "" : "+"
			});
		}
	}

	public class GUILayoutGroup : GUILayoutEntry
	{
		public List<GUILayoutEntry> entries = new List<GUILayoutEntry>();

		public bool isVertical = true;
		public bool resetCoords = false;
		public float spacing = 0f;
		public bool sameSize = true;
		public bool isWindow = false;
		public int windowID = -1;

		private int m_Cursor = 0;

		protected int m_StretchableCountX = 100;
		protected int m_StretchableCountY = 100;

		protected bool m_UserSpecifiedWidth = false;
		protected bool m_UserSpecifiedHeight = false;

		protected float m_ChildMinWidth = 100f;
		protected float m_ChildMaxWidth = 100f;
		protected float m_ChildMinHeight = 100f;
		protected float m_ChildMaxHeight = 100f;

		private readonly RectOffset m_Margin = new RectOffset();

		public override RectOffset margin
		{
			get
			{
				return m_Margin;
			}
		}

		public GUILayoutGroup() : base(0f, 0f, 0f, 0f, GUIStyle.none)
		{
			spaceStyle = new GUIStyle();
			spaceStyle.stretchWidth = true;
		}

		public GUILayoutGroup(GUIStyle _style, GUILayoutOption[] options) : base(0f, 0f, 0f, 0f, _style)
		{
			if (options != null)
			{
				ApplyOptions(options);
			}
			m_Margin.left = _style.margin.left;
			m_Margin.right = _style.margin.right;
			m_Margin.top = _style.margin.top;
			m_Margin.bottom = _style.margin.bottom;

			spaceStyle = new GUIStyle();
			spaceStyle.stretchWidth = true;
		}

		public override void ApplyOptions(GUILayoutOption[] options)
		{
			if (options != null)
			{
				base.ApplyOptions(options);
				for (int i = 0; i < options.Length; i++)
				{
					GUILayoutOption gUILayoutOption = options[i];
					switch (GUILayoutOptionHelper.GetType(gUILayoutOption))
					{
						case GUILayoutOptionType.fixedWidth:
						case GUILayoutOptionType.minWidth:
						case GUILayoutOptionType.maxWidth:
							m_UserSpecifiedHeight = true;
							break;
						case GUILayoutOptionType.fixedHeight:
						case GUILayoutOptionType.minHeight:
						case GUILayoutOptionType.maxHeight:
							m_UserSpecifiedWidth = true;
							break;
						case GUILayoutOptionType.spacing:
							spacing = (float)((int)GUILayoutOptionHelper.GetValue(gUILayoutOption));
							break;
					}
				}
			}
		}

		protected override void ApplyStyleSettings(GUIStyle style)
		{
			base.ApplyStyleSettings(style);
			RectOffset margin = style.margin;
			m_Margin.left = margin.left;
			m_Margin.right = margin.right;
			m_Margin.top = margin.top;
			m_Margin.bottom = margin.bottom;
		}

		public void ResetCursor()
		{
			m_Cursor = 0;
		}

		public Rect PeekNext()
		{
			if (m_Cursor < entries.Count)
			{
				GUILayoutEntry gUILayoutEntry = entries[m_Cursor];
				return gUILayoutEntry.rect;
			}
			throw new ArgumentException(string.Concat(new object[]
			{
				"Getting control ",
				m_Cursor,
				"'s position in a group with only ",
				entries.Count,
				" controls when doing ",
				Event.current.rawType,
				"\nAborting"
			}));
		}

		public GUILayoutEntry GetNext()
		{
			if (m_Cursor < entries.Count)
			{
				GUILayoutEntry result = entries[m_Cursor];
				m_Cursor++;
				return result;
			}
			throw new ArgumentException(string.Concat(new object[]
			{
				"Getting control ",
				m_Cursor,
				"'s position in a group with only ",
				entries.Count,
				" controls when doing ",
				Event.current.rawType,
				"\nAborting"
			}));
		}

		public Rect GetLast()
		{
			Rect result;
			if (m_Cursor == 0)
			{
				Debug.LogError("You cannot call GetLast immediately after beginning a group.");
				result = GUILayoutEntry.kDummyRect;
			}
			else if (m_Cursor <= entries.Count)
			{
				GUILayoutEntry gUILayoutEntry = entries[m_Cursor - 1];
				result = gUILayoutEntry.rect;
			}
			else
			{
				Debug.LogError(string.Concat(new object[]
				{
					"Getting control ",
					m_Cursor,
					"'s position in a group with only ",
					entries.Count,
					" controls when doing ",
					Event.current.type
				}));
				result = GUILayoutEntry.kDummyRect;
			}
			return result;
		}

		public void Add(GUILayoutEntry e)
		{
			entries.Add(e);
		}

		GUIStyle spaceStyle;

		public override void CalcWidth()
		{
			if (entries.Count == 0)
			{
				maxWidth = (minWidth = (float)base.style.padding.horizontal);
			}
			else
			{
				int num = 0;
				int num2 = 0;
				m_ChildMinWidth = 0f;
				m_ChildMaxWidth = 0f;
				m_StretchableCountX = 0;
				bool flag = true;
				if (isVertical)
				{
					foreach (GUILayoutEntry current in entries)
					{
						current.CalcWidth();
						RectOffset margin = current.margin;
						if (current.style != spaceStyle)
						{
							if (!flag)
							{
								num = Mathf.Min(margin.left, num);
								num2 = Mathf.Min(margin.right, num2);
							}
							else
							{
								num = margin.left;
								num2 = margin.right;
								flag = false;
							}
							m_ChildMinWidth = Mathf.Max(current.minWidth + (float)margin.horizontal, m_ChildMinWidth);
							m_ChildMaxWidth = Mathf.Max(current.maxWidth + (float)margin.horizontal, m_ChildMaxWidth);
						}
						m_StretchableCountX += current.stretchWidth;
					}
					m_ChildMinWidth -= (float)(num + num2);
					m_ChildMaxWidth -= (float)(num + num2);
				}
				else
				{
					int num3 = 0;
					foreach (GUILayoutEntry current2 in entries)
					{
						current2.CalcWidth();
						RectOffset margin2 = current2.margin;
						if (current2.style != spaceStyle)
						{
							int num4;
							if (!flag)
							{
								num4 = ((num3 <= margin2.left) ? margin2.left : num3);
							}
							else
							{
								num4 = 0;
								flag = false;
							}
							m_ChildMinWidth += current2.minWidth + spacing + (float)num4;
							m_ChildMaxWidth += current2.maxWidth + spacing + (float)num4;
							num3 = margin2.right;
							m_StretchableCountX += current2.stretchWidth;
						}
						else
						{
							m_ChildMinWidth += current2.minWidth;
							m_ChildMaxWidth += current2.maxWidth;
							m_StretchableCountX += current2.stretchWidth;
						}
					}
					m_ChildMinWidth -= spacing;
					m_ChildMaxWidth -= spacing;
					if (entries.Count != 0)
					{
						num = entries[0].margin.left;
						num2 = num3;
					}
					else
					{
						num2 = (num = 0);
					}
				}
				float num5;
				float num6;
				if (base.style != GUIStyle.none || m_UserSpecifiedWidth)
				{
					num5 = (float)Mathf.Max(base.style.padding.left, num);
					num6 = (float)Mathf.Max(base.style.padding.right, num2);
				}
				else
				{
					m_Margin.left = num;
					m_Margin.right = num2;
					num6 = (num5 = 0f);
				}
				minWidth = Mathf.Max(minWidth, m_ChildMinWidth + num5 + num6);
				if (maxWidth == 0f)
				{
					stretchWidth += m_StretchableCountX + ((!base.style.stretchWidth) ? 0 : 1);
					maxWidth = m_ChildMaxWidth + num5 + num6;
				}
				else
				{
					stretchWidth = 0;
				}
				maxWidth = Mathf.Max(maxWidth, minWidth);
				if (base.style.fixedWidth != 0f)
				{
					maxWidth = (minWidth = base.style.fixedWidth);
					stretchWidth = 0;
				}
			}
		}

		public override void SetHorizontal(float x, float width)
		{
			base.SetHorizontal(x, width);
			if (resetCoords)
			{
				x = 0f;
			}
			RectOffset padding = base.style.padding;
			if (isVertical)
			{
				if (base.style != GUIStyle.none)
				{
					foreach (GUILayoutEntry current in entries)
					{
						float num = (float)Mathf.Max(current.margin.left, padding.left);
						float x2 = x + num;
						float num2 = width - (float)Mathf.Max(current.margin.right, padding.right) - num;
						if (current.stretchWidth != 0)
						{
							current.SetHorizontal(x2, num2);
						}
						else
						{
							current.SetHorizontal(x2, Mathf.Clamp(num2, current.minWidth, current.maxWidth));
						}
					}
				}
				else
				{
					float num3 = x - (float)margin.left;
					float num4 = width + (float)margin.horizontal;
					foreach (GUILayoutEntry current2 in entries)
					{
						if (current2.stretchWidth != 0)
						{
							current2.SetHorizontal(num3 + (float)current2.margin.left, num4 - (float)current2.margin.horizontal);
						}
						else
						{
							current2.SetHorizontal(num3 + (float)current2.margin.left, Mathf.Clamp(num4 - (float)current2.margin.horizontal, current2.minWidth, current2.maxWidth));
						}
					}
				}
			}
			else
			{
				if (base.style != GUIStyle.none)
				{
					float num5 = (float)padding.left;
					float num6 = (float)padding.right;
					if (entries.Count != 0)
					{
						num5 = Mathf.Max(num5, (float)entries[0].margin.left);
						num6 = Mathf.Max(num6, (float)entries[entries.Count - 1].margin.right);
					}
					x += num5;
					width -= num6 + num5;
				}
				float num7 = width - spacing * (float)(entries.Count - 1);
				float t = 0f;
				if (m_ChildMinWidth != m_ChildMaxWidth)
				{
					t = Mathf.Clamp((num7 - m_ChildMinWidth) / (m_ChildMaxWidth - m_ChildMinWidth), 0f, 1f);
				}
				float num8 = 0f;
				if (num7 > m_ChildMaxWidth)
				{
					if (m_StretchableCountX > 0)
					{
						num8 = (num7 - m_ChildMaxWidth) / (float)m_StretchableCountX;
					}
				}
				int num9 = 0;
				bool flag = true;
				foreach (GUILayoutEntry current3 in entries)
				{
					float num10 = Mathf.Lerp(current3.minWidth, current3.maxWidth, t);
					num10 += num8 * (float)current3.stretchWidth;
					if (current3.style != spaceStyle)
					{
						int num11 = current3.margin.left;
						if (flag)
						{
							num11 = 0;
							flag = false;
						}
						int num12 = (num9 <= num11) ? num11 : num9;
						x += (float)num12;
						num9 = current3.margin.right;
					}
					current3.SetHorizontal(Mathf.Round(x), Mathf.Round(num10));
					x += num10 + spacing;
				}
			}
		}

		public override void CalcHeight()
		{
			if (entries.Count == 0)
			{
				maxHeight = (minHeight = (float)base.style.padding.vertical);
			}
			else
			{
				int num = 0;
				int num2 = 0;
				m_ChildMinHeight = 0f;
				m_ChildMaxHeight = 0f;
				m_StretchableCountY = 0;
				if (isVertical)
				{
					int num3 = 0;
					bool flag = true;
					foreach (GUILayoutEntry current in entries)
					{
						current.CalcHeight();
						RectOffset margin = current.margin;
						if (current.style != spaceStyle)
						{
							int num4;
							if (!flag)
							{
								num4 = Mathf.Max(num3, margin.top);
							}
							else
							{
								num4 = 0;
								flag = false;
							}
							m_ChildMinHeight += current.minHeight + spacing + (float)num4;
							m_ChildMaxHeight += current.maxHeight + spacing + (float)num4;
							num3 = margin.bottom;
							m_StretchableCountY += current.stretchHeight;
						}
						else
						{
							m_ChildMinHeight += current.minHeight;
							m_ChildMaxHeight += current.maxHeight;
							m_StretchableCountY += current.stretchHeight;
						}
					}
					m_ChildMinHeight -= spacing;
					m_ChildMaxHeight -= spacing;
					if (entries.Count != 0)
					{
						num = entries[0].margin.top;
						num2 = num3;
					}
					else
					{
						num = (num2 = 0);
					}
				}
				else
				{
					bool flag2 = true;
					foreach (GUILayoutEntry current2 in entries)
					{
						current2.CalcHeight();
						RectOffset margin2 = current2.margin;
						if (current2.style != spaceStyle)
						{
							if (!flag2)
							{
								num = Mathf.Min(margin2.top, num);
								num2 = Mathf.Min(margin2.bottom, num2);
							}
							else
							{
								num = margin2.top;
								num2 = margin2.bottom;
								flag2 = false;
							}
							m_ChildMinHeight = Mathf.Max(current2.minHeight, m_ChildMinHeight);
							m_ChildMaxHeight = Mathf.Max(current2.maxHeight, m_ChildMaxHeight);
						}
						m_StretchableCountY += current2.stretchHeight;
					}
				}
				float num5;
				float num6;
				if (base.style != GUIStyle.none || m_UserSpecifiedHeight)
				{
					num5 = (float)Mathf.Max(base.style.padding.top, num);
					num6 = (float)Mathf.Max(base.style.padding.bottom, num2);
				}
				else
				{
					m_Margin.top = num;
					m_Margin.bottom = num2;
					num6 = (num5 = 0f);
				}
				minHeight = Mathf.Max(minHeight, m_ChildMinHeight + num5 + num6);
				if (maxHeight == 0f)
				{
					stretchHeight += m_StretchableCountY + ((!base.style.stretchHeight) ? 0 : 1);
					maxHeight = m_ChildMaxHeight + num5 + num6;
				}
				else
				{
					stretchHeight = 0;
				}
				maxHeight = Mathf.Max(maxHeight, minHeight);
				if (base.style.fixedHeight != 0f)
				{
					maxHeight = (minHeight = base.style.fixedHeight);
					stretchHeight = 0;
				}
			}
		}

		public override void SetVertical(float y, float height)
		{
			base.SetVertical(y, height);
			if (entries.Count != 0)
			{
				RectOffset padding = base.style.padding;
				if (resetCoords)
				{
					y = 0f;
				}
				if (isVertical)
				{
					if (base.style != GUIStyle.none)
					{
						float num = (float)padding.top;
						float num2 = (float)padding.bottom;
						if (entries.Count != 0)
						{
							num = Mathf.Max(num, (float)entries[0].margin.top);
							num2 = Mathf.Max(num2, (float)entries[entries.Count - 1].margin.bottom);
						}
						y += num;
						height -= num2 + num;
					}
					float num3 = height - spacing * (float)(entries.Count - 1);
					float t = 0f;
					if (m_ChildMinHeight != m_ChildMaxHeight)
					{
						t = Mathf.Clamp((num3 - m_ChildMinHeight) / (m_ChildMaxHeight - m_ChildMinHeight), 0f, 1f);
					}
					float num4 = 0f;
					if (num3 > m_ChildMaxHeight)
					{
						if (m_StretchableCountY > 0)
						{
							num4 = (num3 - m_ChildMaxHeight) / (float)m_StretchableCountY;
						}
					}
					int num5 = 0;
					bool flag = true;
					foreach (GUILayoutEntry current in entries)
					{
						float num6 = Mathf.Lerp(current.minHeight, current.maxHeight, t);
						num6 += num4 * (float)current.stretchHeight;
						if (current.style != spaceStyle)
						{
							int num7 = current.margin.top;
							if (flag)
							{
								num7 = 0;
								flag = false;
							}
							int num8 = (num5 <= num7) ? num7 : num5;
							y += (float)num8;
							num5 = current.margin.bottom;
						}
						current.SetVertical(Mathf.Round(y), Mathf.Round(num6));
						y += num6 + spacing;
					}
				}
				else if (base.style != GUIStyle.none)
				{
					foreach (GUILayoutEntry current2 in entries)
					{
						float num9 = (float)Mathf.Max(current2.margin.top, padding.top);
						float y2 = y + num9;
						float num10 = height - (float)Mathf.Max(current2.margin.bottom, padding.bottom) - num9;
						if (current2.stretchHeight != 0)
						{
							current2.SetVertical(y2, num10);
						}
						else
						{
							current2.SetVertical(y2, Mathf.Clamp(num10, current2.minHeight, current2.maxHeight));
						}
					}
				}
				else
				{
					float num11 = y - (float)margin.top;
					float num12 = height + (float)margin.vertical;
					foreach (GUILayoutEntry current3 in entries)
					{
						if (current3.stretchHeight != 0)
						{
							current3.SetVertical(num11 + (float)current3.margin.top, num12 - (float)current3.margin.vertical);
						}
						else
						{
							current3.SetVertical(num11 + (float)current3.margin.top, Mathf.Clamp(num12 - (float)current3.margin.vertical, current3.minHeight, current3.maxHeight));
						}
					}
				}
			}
		}

		public override string ToString()
		{
			string text = "";
			string text2 = "";
			for (int i = 0; i < GUILayoutEntry.indent; i++)
			{
				text2 += " ";
			}
			string text3 = text;
			text = string.Concat(new object[]
			{
				text3,
				base.ToString(),
				" Margins: ",
				m_ChildMinHeight,
				" {\n"
			});
			GUILayoutEntry.indent += 4;
			foreach (GUILayoutEntry current in entries)
			{
				text = text + current.ToString() + "\n";
			}
			text = text + text2 + "}";
			GUILayoutEntry.indent -= 4;
			return text;
		}
	}
	#endregion
}