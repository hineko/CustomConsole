using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Networking.PlayerConnection;
using Assembly = System.Reflection.Assembly;

namespace CustomConsole.Editor
{
	public class CustomConsoleWindow : EditorWindow, IHasCustomMenu
	{
		#region Class

		private class StackTraceData
		{
			public int LineNumber;
			public string MethodName;
			public string DataPath;
		}

		#endregion

		#region Enum

		[Flags]
		private enum Mode
		{
			Error = 1 << 0,
			Assert = 1 << 1,
			Log = 1 << 2,
			Fatal = 1 << 4,
			DontPreprocessCondition = 1 << 5,
			AssetImportError = 1 << 6,
			AssetImportWarning = 1 << 7,
			ScriptingError = 1 << 8,
			ScriptingWarning = 1 << 9,
			ScriptingLog = 1 << 10,
			ScriptCompileError = 1 << 11,
			ScriptCompileWarning = 1 << 12,
			StickyError = 1 << 13,
			MayIgnoreLineNumber = 1 << 14,
			ReportBug = 1 << 15,
			DisplayPreviousErrorInStatusBar = 1 << 16,
			ScriptingException = 1 << 17,
			DontExtractStacktrace = 1 << 18,
			ShouldClearOnPlay = 1 << 19,
			GraphCompileError = 1 << 20,
			ScriptingAssertion = 1 << 21,
			VisualScriptingError = 1 << 22
		}

		private enum ConsoleFlags
		{
			Collapse = 1 << 0,
			ClearOnPlay = 1 << 1,
			ErrorPause = 1 << 2,
			Verbose = 1 << 3,
			StopForAssert = 1 << 4,
			StopForError = 1 << 5,
			AutoScroll = 1 << 6,
			LogLevelLog = 1 << 7,
			LogLevelWarning = 1 << 8,
			LogLevelError = 1 << 9,
			ShowTimestamp = 1 << 10,
			ClearOnBuild = 1 << 11,
		};
		#endregion

		public const string ASSEMBLY_NAME = "UnityEditor.dll";

		private const string SPLITTER_RELATIVE_X_PREFS_KEY = "SplitterXkey";
		private const string SPLITTER_RELATIVE_Y_PREFS_KEY = "SplitterYkey";

		private const string LOG_LINT_COUNT_KEY = "CustomConsoleWindowLogLineCount";
		private const string WINDOW_SPLIT_TYPE_KEY = "CustomConsoleWindowSplitType";
		private const string CLEAR_ON_COMPILATION_KEY = "CustomConsole_ClearOnCompilation";

		private static readonly Type ListViewGUIType = Assembly.Load(ASSEMBLY_NAME).GetType("UnityEditor.ListViewGUI");

		private static readonly Type SplitterStateType = Assembly.Load(ASSEMBLY_NAME).GetType("UnityEditor.SplitterState");
		private static readonly Type EditorGUIType = Assembly.Load(ASSEMBLY_NAME).GetType("UnityEditor.EditorGUI");

		private static readonly Type ListViewStateType = Assembly.Load(ASSEMBLY_NAME).GetType("UnityEditor.ListViewState");

#if UNITY_2017
		private static readonly Type ConsoleAttachProfilerUIType = Assembly.Load(ASSEMBLY_NAME).GetType("UnityEditor.ConsoleWindow").GetNestedType("ConsoleAttachProfilerUI", BindingFlags.NonPublic);
#endif
		//private static readonly Type GUIStyleType = Assembly.Load("UnityEngine.dll").GetType("UnityEngine.GUIStyle");

		private static CustomConsoleWindow customConsoleWindow = null;

		private static int prevCount = 0;

		// 上下でGUIをスプリットする設定の値
		private object splitGUI;
		private object consoleAttachProfilerUI;

		// スタックトレース
		private readonly List<StackTraceData> stackTraceList = new List<StackTraceData>();

		private readonly SplitterState splitterState = new SplitterState(new float[2] { 50f, 50f }, new int[2] { 32, 32 }, null);
		private readonly SplitterState splitterState2 = new SplitterState(new float[2] { 70f, 30f }, new int[2] { 32, 32 }, null);

		private bool hasUpdateGUIStyles;

		private bool isLoaded = false;
		private bool isDragging = false;
		private bool isHorizontal;

		private static bool isClearOnCompilation;

		private int lineHeight;
		private int borderHeight;

		private int lvHeight = 0;
		private int activeInstanceID = 0;

		private int LogStyleLineCount { get; set; }

		private string activeText = "";
		private string searchText;

		private GUIContent clearOnCompilationContent;

		private GenericMenu.MenuFunction openPlayerLogFunction;
		private GenericMenu.MenuFunction openEditorLogFunction;

		// ログリスト描画用
		private readonly ListViewState listViewState;
		private readonly ListViewState stackTraceViewState;

		private Vector2 textScrollPos = Vector2.zero;
		private Vector2 stackTraceScrollPos = Vector2.zero;

		private GUIStyle toolbar;
		private GUIStyle toolbarButton;
		private GUIStyle box;

		private GUIStyle evenBackground;
		private GUIStyle oddBackground;

		private GUIStyle CountBadge;

		private GUIStyle LogStyle;
		private GUIStyle WarningStyle;
		private GUIStyle ErrorStyle;

		private GUIStyle watchStyle;

		private GUIStyle IconErrorStyle;
		private GUIStyle IconWarningStyle;
		private GUIStyle IconLogStyle;

		private GUIStyle IconErrorSmallStyle;
		private GUIStyle IconWarningSmallStyle;
		private GUIStyle IconLogSmallStyle;

		private GUIStyle ErrorSmallStyle;
		private GUIStyle WarningSmallStyle;
		private GUIStyle LogSmallStyle;

		private GUIStyle MessageStyle;

		private Texture2D iconInfo;
		private Texture2D iconInfoSmall;
		private Texture2D iconInfoMono;

		private Texture2D iconWarn;
		private Texture2D iconWarnMono;
		private Texture2D iconWarnSmall;

		private Texture2D iconError;
		private Texture2D iconErrorMono;
		private Texture2D iconErrorSmall;

#if UNITY_2019_1_OR_NEWER
		private IConnectionState consoleAttachToPlayerState;
#else
#endif

#if UNITY_2019_1_OR_NEWER
		// L10n は Localization の 略
		public static readonly string ClearLabel = L10n.Tr("Clear");
		public static readonly string ClearOnPlayLabel = L10n.Tr("Clear on Play");
		public static readonly string ErrorPauseLabel = L10n.Tr("Error Pause");
		public static readonly string CollapseLabel = L10n.Tr("Collapse");
		public static readonly string StopForAssertLabel = L10n.Tr("Stop for Assert");
		public static readonly string StopForErrorLabel = L10n.Tr("Stop for Error");
		public static readonly string ClearOnBuildLabel = L10n.Tr("Clear on Build");
		public static readonly string ClearOnCompilation = L10n.Tr("Clear on Compilation");
#else
		public static readonly string ClearLable = "Clear";
		public static readonly string ClearOnPlayLabel = "Clear on Play";
		public static readonly string ErrorPauseLabel = "Error Pause";
		public static readonly string CollapseLabel = "Collapse";
		public static readonly string StopForAssertLabel = "Stop for Assert";
		public static readonly string StopForErrorLabel = "Stop for Error";
		public static readonly string ClearOnBuildLabel = "Clear on Build";
#endif


		private int RowHeight
		{
			get
			{
				if (LogStyleLineCount > 1)
					return Mathf.Max(32, LogStyleLineCount * lineHeight) + borderHeight;
				else
					return lineHeight + borderHeight;
			}
		}

		[MenuItem("Window/CustomConsole")]
		public static void GetWindow()
		{
			if (customConsoleWindow == null)
			{
				customConsoleWindow = CreateInstance<CustomConsoleWindow>();
				var pos = customConsoleWindow.position;
				customConsoleWindow.position = new Rect(pos.x, pos.y, 1100, 400);
				customConsoleWindow.Show();
				customConsoleWindow.Focus();
			}
			else
			{
				customConsoleWindow.Show();
				customConsoleWindow.Focus();
			}
		}

		public CustomConsoleWindow()
		{
			listViewState = new ListViewState(0, 38);
			stackTraceViewState = new ListViewState(0, 19);
			searchText = string.Empty;

			EditorApplication.update += CheckLogChanged;
		}

		// ログが即時に変更されていないか確認する
		public static void CheckLogChanged()
		{
			int count = LogEntries.GetCount();
			if (prevCount != count)
			{
				if (customConsoleWindow != null)
				{
					customConsoleWindow.DoLogChanged();
					prevCount = count;
				}
			}
		}

		// コンパイル完了コールバック
		private static void OnCompilationFinished(object obj)
		{
			if (isClearOnCompilation)
			{
				LogEntries.Clear();
				GUIUtility.keyboardControl = 0;
			}
		}

		// 再描画する
		public void DoLogChanged()
		{
			customConsoleWindow.Repaint();
		}

		private void OnEnable()
		{
			// ウィンドウタブのアイコンと名前を設定する
			Texture2D titleIcon = EditorGUIUtility.Load("icons/d_UnityEditor.ConsoleWindow.png") as Texture2D;
			titleContent = new GUIContent("CustomConsole", titleIcon);
			customConsoleWindow = this;

#if UNITY_2019_1_OR_NEWER
			// Editorプルダウンメニューのインスタンス取得
			if (consoleAttachToPlayerState == null)
			{
				var type = Assembly.Load(ASSEMBLY_NAME).GetType("UnityEditor.ConsoleWindow");
				type = type.GetNestedType("ConsoleAttachToPlayerState", BindingFlags.NonPublic);
				var con = type.GetConstructor(new[] { typeof(EditorWindow), typeof(Action<string>) });
				consoleAttachToPlayerState = (IConnectionState)con?.Invoke(new object[] { (EditorWindow)this, null });
			}

			// コンパイルコールバック設定 Unity2018.4～
			CompilationPipeline.compilationFinished += OnCompilationFinished;
			isClearOnCompilation = EditorPrefs.GetBool(CLEAR_ON_COMPILATION_KEY, false);
			clearOnCompilationContent = new GUIContent(L10n.Tr("Clear on Compilation"), L10n.Tr("Clear log at compile time"));
#else
			if (consoleAttachProfilerUI == null)
			{
				consoleAttachProfilerUI = ConsoleAttachProfilerUIType.GetConstructor(new Type[0]).Invoke(new object[0]);
			}
#endif
			// 画面分割の割合を引き出す
			float splitterX = EditorPrefs.GetFloat(SPLITTER_RELATIVE_X_PREFS_KEY, 0.3f);
			float splitterY = EditorPrefs.GetFloat(SPLITTER_RELATIVE_Y_PREFS_KEY, 0.7f);

			ConstructorInfo ctor = SplitterStateType.GetConstructor(new Type[] { typeof(float[]), typeof(int[]), typeof(int[]) });
			splitGUI = ctor.Invoke(new object[3] { new float[] { splitterX, splitterY }, new int[] { 32, 32 }, null });

			LogStyleLineCount = EditorPrefs.GetInt(LOG_LINT_COUNT_KEY, 2);
			isHorizontal = EditorPrefs.GetBool(WINDOW_SPLIT_TYPE_KEY, false);
			
		}

		private void OnDisable()
		{
#if UNITY_2019_1_OR_NEWER
			consoleAttachToPlayerState?.Dispose();
			consoleAttachToPlayerState = (IConnectionState)null;

			CompilationPipeline.compilationFinished -= OnCompilationFinished;
#endif
			customConsoleWindow = null;

			if (SplitterStateType != null)
			{
				float[] array = (float[])SplitterStateType.GetField("relativeSizes", BindingFlags.Public | BindingFlags.Instance)?.GetValue(splitGUI) ?? new[] { 0.3f, 0.7f };
				EditorPrefs.SetFloat(SPLITTER_RELATIVE_X_PREFS_KEY, array[0]);
				EditorPrefs.SetFloat(SPLITTER_RELATIVE_Y_PREFS_KEY, array[1]);
			}
		}

		private void UpdateListView()
		{
			hasUpdateGUIStyles = true;
			int rowHeight = RowHeight;
			listViewState.rowHeight = rowHeight;
			listViewState.currentRow = -1;
			stackTraceViewState.currentRow = -1;
			listViewState.scrollPos.y = (float)(LogEntries.GetCount() * rowHeight);
		}

		/// <summary>
		/// 描画
		/// </summary>
		private void OnGUI()
		{
			try
			{
				Event current = Event.current;

				// スタイルアイコン系ロード
				LoadStyleAndIcon();

				if (!hasUpdateGUIStyles)
				{
					lineHeight = Mathf.RoundToInt(ErrorStyle.lineHeight);
					borderHeight = ErrorStyle.border.top + ErrorStyle.border.bottom;
					UpdateListView();
				}

				// ツールバー描画
				DrawToolbar(current);

				if (current.type == EventType.MouseDrag && !isDragging)
				{
					isDragging = true;
				}

				if (current.type == EventType.MouseUp && isDragging)
				{
					isDragging = false;
					float[] array = (float[])SplitterStateType.GetField("relativeSizes", BindingFlags.Public | BindingFlags.Instance)?.GetValue(splitGUI) ?? new[] { 0.3f, 0.7f };

					EditorPrefs.SetFloat(SPLITTER_RELATIVE_X_PREFS_KEY, array[0]);
					EditorPrefs.SetFloat(SPLITTER_RELATIVE_Y_PREFS_KEY, array[1]);
				}

				// ログリスト表示とログ詳細・スタックトレース表示の分割
				SplitterGUILayout.BeginVerticalSplit(splitterState2);
				{
					// ログリスト描画
					DrawLogListView(current);

					// ログ詳細とスタックトレース表示の分割
					using (new EditorGUILayout.VerticalScope())
					{
						// GUIを分割する領域の指定
						if (isHorizontal)
							SplitterGUILayout.BeginHorizontalSplit(splitterState);
						else
							SplitterGUILayout.BeginVerticalSplit(splitterState);

						// 選択されたログの詳細表示
						DrawLogDetailView();
						// 拡張部描画
						DrawStackTraceView(current);

						if (isHorizontal)
							SplitterGUILayout.EndVerticalSplit();
						else
							SplitterGUILayout.EndHorizontalSplit();
					}
				}
				SplitterGUILayout.EndVerticalSplit();

				// コピーコマンドが実行されたか？(ctrl+C など)
				if ((current.type == EventType.ValidateCommand || current.type == EventType.ExecuteCommand)
					&& current.commandName == "Copy" && activeText != string.Empty)
				{
					if (current.type == EventType.ExecuteCommand)
					{
						// クリップボードにコピーする
						EditorGUIUtility.systemCopyBuffer = activeText;
					}
					current.Use();
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		/// <summary>
		/// 内臓GUIStyleとIconを取得する
		/// </summary>
		private void LoadStyleAndIcon()
		{
			if (!isLoaded)
			{
				isLoaded = true;
				toolbar = new GUIStyle("Toolbar");
				toolbarButton = new GUIStyle("ToolbarButton");
				box = new GUIStyle("CN Box");
				CountBadge = new GUIStyle("CN CountBadge");

				LogStyle = new GUIStyle("CN EntryInfo");
				WarningStyle = new GUIStyle("CN EntryWarn");
				ErrorStyle = new GUIStyle("CN EntryError");
				MessageStyle = new GUIStyle("CN Message");

				watchStyle = new GUIStyle("CN EntryInfo");

				IconErrorStyle = new GUIStyle("CN EntryErrorIcon");
				IconWarningStyle = new GUIStyle("CN EntryWarnIcon");
				IconLogStyle = new GUIStyle("CN EntryInfoIcon");

				IconErrorSmallStyle = new GUIStyle("CN EntryErrorIconSmall");
				IconWarningSmallStyle = new GUIStyle("CN EntryWarnIconSmall");
				IconLogSmallStyle = new GUIStyle("CN EntryInfoIconSmall");

				ErrorSmallStyle = new GUIStyle("CN EntryErrorSmall");
				WarningSmallStyle = new GUIStyle("CN EntryWarnSmall");
				LogSmallStyle = new GUIStyle("CN EntryInfoSmall");

				evenBackground = new GUIStyle("CN EntryBackEven");
				oddBackground = new GUIStyle("CN EntryBackodd");

				iconInfo = EditorGUIUtility.Load("icons/d_console.infoicon.png") as Texture2D;
				iconInfoMono = EditorGUIUtility.Load("icons/d_console.infoicon.inactive.sml.png") as Texture2D;
				iconInfoSmall = EditorGUIUtility.Load("icons/d_console.infoicon.sml.png") as Texture2D;

				iconWarn = EditorGUIUtility.Load("icons/d_console.warnicon.png") as Texture2D;
				iconWarnMono = EditorGUIUtility.Load("icons/d_console.warnicon.inactive.sml.png") as Texture2D;
				iconWarnSmall = EditorGUIUtility.Load("icons/d_console.warnicon.sml.png") as Texture2D;

				iconError = EditorGUIUtility.Load("icons/d_console.erroricon.png") as Texture2D;
				iconErrorMono = EditorGUIUtility.Load("icons/d_console.erroricon.inactive.sml.png") as Texture2D;
				iconErrorSmall = EditorGUIUtility.Load("d_console.erroricon.sml") as Texture2D;

				LogStyleLineCount = EditorPrefs.GetInt(LOG_LINT_COUNT_KEY, 2);

				UpdateLogStyleFixedHeights();
			}
		}


		/// <summary>
		/// コンソール上部ツールバー描画
		/// </summary>
		private void DrawToolbar(Event e)
		{
			using (new GUILayout.HorizontalScope(toolbar))
			{
				// ログクリアボタン
				if (GUILayout.Button(ClearLabel, toolbarButton))
				{
					LogEntries.Clear();
					GUIUtility.keyboardControl = 0;
				}

				int count = LogEntries.GetCount();
				if (listViewState.totalRows != count && listViewState.totalRows > 0)
				{
					if (listViewState.scrollPos.y >= (double)(listViewState.rowHeight * listViewState.totalRows - lvHeight))
					{
						listViewState.scrollPos.y = (float)(count * RowHeight - lvHeight);
					}
				}
				EditorGUILayout.Space();

				bool isCollapse = HasFlag(ConsoleFlags.Collapse);
				SetFlag(ConsoleFlags.Collapse, GUILayout.Toggle(isCollapse, CollapseLabel, toolbarButton));
				bool isChangeCollapse = isCollapse != HasFlag(ConsoleFlags.Collapse);
				if (isChangeCollapse)
				{
					listViewState.currentRow = -1;
					stackTraceViewState.currentRow = -1;
					listViewState.scrollPos.y = (LogEntries.GetCount() * 32);
				}

				SetFlag(ConsoleFlags.ClearOnPlay, GUILayout.Toggle(HasFlag(ConsoleFlags.ClearOnPlay), ClearOnPlayLabel, toolbarButton));
				SetFlag(ConsoleFlags.ClearOnBuild, GUILayout.Toggle(HasFlag(ConsoleFlags.ClearOnBuild), ClearOnBuildLabel, toolbarButton));
#if UNITY_2019_1_OR_NEWER
				// コンパイル後ログクリア設定
				bool isCoc = GUILayout.Toggle(isClearOnCompilation, clearOnCompilationContent, toolbarButton);
				if (isClearOnCompilation != isCoc)
				{
					isClearOnCompilation = isCoc;
					EditorPrefs.SetBool(CLEAR_ON_COMPILATION_KEY, isClearOnCompilation);
				}
#endif
				SetFlag(ConsoleFlags.ErrorPause, GUILayout.Toggle(HasFlag(ConsoleFlags.ErrorPause), ErrorPauseLabel, toolbarButton));

#if UNITY_2019_1_OR_NEWER
				if (consoleAttachToPlayerState != null)
					UnityEditor.Experimental.Networking.PlayerConnection.EditorGUILayout.AttachToPlayerDropdown(consoleAttachToPlayerState, EditorStyles.toolbarDropDown);
#else
				if (consoleAttachProfilerUI != null)
				{
					var method = ConsoleAttachProfilerUIType.GetMethod("OnGUILayout");
					method.Invoke(consoleAttachProfilerUI, new object[] { this });
				}
				//this.consoleAttachProfilerUI.OnGUILayout((EditorWindow)this); 
#endif

				EditorGUILayout.Space();

#if false
				float a = (float)((EditorGUIUtility.labelWidth + (double)EditorGUIUtility.fieldWidth + 5.0) * 1.5);
				Rect rect = GUILayoutUtility.GetRect(0.0f, a, 16f, 16f, toolbarSeach, GUILayout.MinWidth(65f), GUILayout.MaxWidth(300f));
				int controlId = GUIUtility.GetControlID("CustomConsoleSearchField".GetHashCode(), FocusType.Passive, rect);
				string str = (string)EditorGUIType.GetMethod("ToolbarSearchField", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(Rect), typeof(string), typeof(bool) }, null).Invoke(null, new object[] { controlId, rect, searchFieldText, false });
				if(str != searchFieldText)
				{
					searchFieldText = str;
				}
#endif

				GUILayout.FlexibleSpace();

				SetFlag(ConsoleFlags.StopForAssert, GUILayout.Toggle(HasFlag(ConsoleFlags.StopForAssert), StopForAssertLabel, toolbarButton));
				SetFlag(ConsoleFlags.StopForError, GUILayout.Toggle(HasFlag(ConsoleFlags.StopForError), StopForErrorLabel, toolbarButton));

				bool isSplitHorizontal = GUILayout.Toggle(isHorizontal, "SplitHorizontal", toolbarButton);

				if (isHorizontal != isSplitHorizontal)
				{
					isHorizontal = isSplitHorizontal;
					EditorPrefs.SetBool(WINDOW_SPLIT_TYPE_KEY, isHorizontal);
				}

				GUILayout.FlexibleSpace();

				// 検索窓
				GUILayout.Space(4f);
				SearchField(e);
				GUILayout.Space(4f);

				// 各種ログ数表示
				using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
				{
					int logCount = 0, warningCount = 0, errorCount = 0;
					LogEntries.GetCountsByType(ref errorCount, ref warningCount, ref logCount);

					// ログ種類表示設定
					GUIContent debugLogContent = new GUIContent((logCount > 9999) ? "9999+" : logCount.ToString(), logCount > 0 ? iconInfoSmall : iconInfoMono);
					GUIContent warningContent = new GUIContent((warningCount > 9999) ? "9999+" : warningCount.ToString(), (warningCount > 0) ? iconWarnSmall : iconWarnMono);
					GUIContent errorContent = new GUIContent((errorCount > 9999) ? "9999+" : errorCount.ToString(), (errorCount > 0) ? iconErrorSmall : iconErrorMono);

					bool isShowDebugLog = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelLog), debugLogContent, toolbarButton);
					bool isShowWarningLog = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelWarning), warningContent, toolbarButton);
					bool isShowErrorLog = GUILayout.Toggle(HasFlag(ConsoleFlags.LogLevelError), errorContent, toolbarButton);
					if (changeScope.changed)
					{
						SetActiveEntry(null);
					}

					SetFlag(ConsoleFlags.LogLevelLog, isShowDebugLog);
					SetFlag(ConsoleFlags.LogLevelWarning, isShowWarningLog);
					SetFlag(ConsoleFlags.LogLevelError, isShowErrorLog);
				}
			}
		}

		/// <summary>
		/// ログリスト描画
		/// </summary>
		/// <param name="current"></param>
		private void DrawLogListView(Event current)
		{
			GUIContent gUIContent = new GUIContent();

			// ログ情報を取得するための関数を使用する区間を宣言する
			int totalCount = LogEntries.StartGettingEntries();

			EditorGUIUtility.SetIconSize(new Vector2(32f, 32f));
			listViewState.totalRows = totalCount;
			int controlID = GUIUtility.GetControlID(FocusType.Passive);
			try
			{
				bool isClicked = false;
				bool isCollapse = HasFlag(ConsoleFlags.Collapse);

				// totalRowsに基づいて繰り返す
				IEnumerator enumerator = ListViewGUI.ListView(listViewState, box, new GUILayoutOption[0]).GetEnumerator();
				try
				{
					// ログ描画
					while (enumerator.MoveNext())
					{
						if (enumerator.Current != null)
						{
							ListViewElement element = (ListViewElement)enumerator.Current;

							// クリックされた
							if (current.type == EventType.MouseDown && current.button == 0 && element.position.Contains(current.mousePosition))
							{
								// ダブルクリックか？
								if (current.clickCount == 2)
								{
									// 対応したStacktraceからファイルを開く
									DoubleClicked(listViewState.currentRow);
								}
								isClicked = true;
							}

							// ログを描画する
							if (current.type == EventType.Repaint)
							{
								int mode = 0;
								string logText = null;

								// ログに指定数行分とログの種類を取得
								LogEntries.GetFirstTwoLinesEntryTextAndModeInternal(element.row, LogStyleLineCount, ref mode, ref logText);

								// 2019.3対応 2020.2.27
								int num2 = LogStyleLineCount == 1 ? 4 : 8;

								// ログの背景のスタイルを決定する
								GUIStyle gUIStyle = (element.row % 2 != 0) ? evenBackground : oddBackground;

								// ログ背景描画
								gUIStyle.Draw(element.position, false, false, listViewState.currentRow == element.row, false);
								gUIContent.text = logText;

								// 2019.3対応 2020.2.27
								Rect position1 = element.position;
								position1.x += (float)num2;
								position1.y += 2f;

								// ログアイコンの描画
								GUIStyle iconStyle = GetStyleForErrorMode(mode, true, LogStyleLineCount == 1);
								iconStyle.Draw(position1, false, false, listViewState.currentRow == element.row, false);

								// IconとStyleを取得する
								GUIStyle styleForErrorMode = GetStyleForErrorMode(mode, false, LogStyleLineCount == 1);

								// 2019.3対応 2020.2.27
								Rect position2 = element.position;
								position2.x += (float)num2;

								// ログテキスト描画
								if (string.IsNullOrEmpty(searchText))
								{
									// 検索処理なし　通常表示
									//styleForErrorMode.Draw(element.position, false, false, listViewState.currentRow == element.row, false);
									styleForErrorMode.Draw(position2, gUIContent, controlID, listViewState.currentRow == element.row);
								}
								else
								{
									// ログ検索結果描画
									int startIndex = logText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
									if (startIndex == -1)
									{
										// 見つからなかった
										styleForErrorMode.Draw(position2, gUIContent, controlID, listViewState.currentRow == element.row);
									}
									else
									{
										int endIndex = startIndex + searchText.Length;
										Color selectionColor = GUI.skin.settings.selectionColor;
										var method = styleForErrorMode.GetType().GetMethod("DrawWithTextSelection", BindingFlags.Instance | BindingFlags.NonPublic, null,
											new Type[] { typeof(Rect), typeof(GUIContent), typeof(bool), typeof(bool), typeof(int), typeof(int), typeof(bool), typeof(Color) }, null);

										if (method != null)
											method.Invoke(styleForErrorMode, new object[] { position2, gUIContent, true, true, startIndex, endIndex, false, selectionColor });
										else
											//styleForErrorMode.DrawWithTextSelection(element.position, gUIContent, true, true, startIndex, endIndex, false, selectionColor);
											styleForErrorMode.DrawWithTextSelection(position2, gUIContent, controlID, startIndex, endIndex);
									}
								}

								//　同一のログをまとめる
								if (isCollapse)
								{
									Rect pos = element.position;
									gUIContent.text = LogEntries.GetEntryCount(element.row).ToString(CultureInfo.InvariantCulture);
									Vector2 vector = CountBadge.CalcSize(gUIContent);
									pos.xMin = pos.xMax - vector.x;
									pos.yMin += (pos.yMax - pos.yMin - vector.y) * 0.5f;
									pos.x -= 5f;
									GUI.Label(pos, gUIContent, CountBadge);
								}
							}
						}
					}
				}
				finally
				{
					IDisposable disposable;
					if ((disposable = (enumerator as IDisposable)) != null)
					{
						disposable.Dispose();
					}
				}

				if (isClicked)
				{
					// 選択したログが画面端にいた場合は補正して全部見えるようにする
					if (listViewState.scrollPos.y >= (float)(listViewState.rowHeight * listViewState.totalRows - lvHeight))
					{
						listViewState.scrollPos.y = (float)(listViewState.rowHeight * listViewState.totalRows - lvHeight - 1);
					}
				}

				// ログがない または 現在の指定ログの位置が、ログの最大数以上の位置にいる または 選択されていない
				if (listViewState.totalRows == 0 || listViewState.currentRow >= listViewState.totalRows || listViewState.currentRow < 0)
				{
					// ログ詳細テキストに何かあるなら、消す
					if (activeText.Length != 0)
					{
						SetActiveEntry(null);
					}
				}
				else
				{
					// 選択されたログがある
					LogEntry logEntry = new LogEntry();
					// ログの情報をすべて取得する
					LogEntries.GetEntryInternal(listViewState.currentRow, ref logEntry);
					// 対象のログをアクティブにする
					SetActiveEntry(logEntry);
					LogEntries.GetEntryInternal(listViewState.currentRow, ref logEntry);
#if UNITY_2019_3_OR_NEWER
					if (listViewState.selectionChanged || !activeText.Equals(logEntry.message))
#else
					if (listViewState.selectionChanged || !activeText.Equals(logEntry.condition))
#endif
					{
						SetActiveEntry(logEntry);
					}
				}

				// キーボード入力でEnterキーを押された
				if (GUIUtility.keyboardControl == listViewState.ID && current.type == EventType.KeyDown && current.keyCode == KeyCode.Return && listViewState.currentRow != 0)
				{
					DoubleClicked(listViewState.currentRow);
					Event.current.Use();
				}

				if (current.type != EventType.Layout && ListViewGUI.ilvState.rectHeight != 1)
				{
					lvHeight = ListViewGUI.ilvState.rectHeight;
				}
			}
			finally
			{
				// 例外が発生しても正しく終わらせるように
				LogEntries.EndGettingEntries();
				EditorGUIUtility.SetIconSize(Vector2.zero);
			}
		}

		/// <summary>
		/// 選択されたログの詳細を描画する
		/// </summary>
		private void DrawLogDetailView()
		{
			using (GUILayout.ScrollViewScope scrollViewScope = new GUILayout.ScrollViewScope(textScrollPos, box))
			{
				string stackWithHyperlinks = StacktraceWithHyperlinks(activeText);

				float minHeight = MessageStyle.CalcHeight(new GUIContent(stackWithHyperlinks, ""), base.position.width);
				textScrollPos = scrollViewScope.scrollPosition;

				EditorGUILayout.SelectableLabel(stackWithHyperlinks, MessageStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(minHeight));
			}
		}

		/// <summary>
		/// スタックトレース表示描画
		/// </summary>
		/// <param name="current"></param>
		private void DrawStackTraceView(Event current)
		{
			// ログスタックトレース　描画
			using (EditorGUILayout.ScrollViewScope svs2 = new EditorGUILayout.ScrollViewScope(stackTraceScrollPos, box))
			{
				stackTraceScrollPos = svs2.scrollPosition;

				// スタックトレース表示
				IEnumerator enumerator = ListViewGUI.ListView(stackTraceViewState, box).GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current != null)
						{
							ListViewElement element = (ListViewElement)enumerator.Current;

							// 選択した位置でのスクリプトファイルを開く
							if (current.type == EventType.MouseDown && current.button == 0 && element.position.Contains(current.mousePosition))
							{
								if (current.clickCount == 2)
								{
									UnityEngine.Object scriptFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(stackTraceList[stackTraceViewState.currentRow].DataPath);
									AssetDatabase.OpenAsset(scriptFile, stackTraceList[stackTraceViewState.currentRow].LineNumber);
								}
							}

							// パースしたスタックトレースを描画する
							if (current.type == EventType.Repaint)
							{
								GUIStyle gUIStyle = (element.row % 2 != 0) ? evenBackground : oddBackground;
								Rect r = element.position;

								// 背景
								gUIStyle.Draw(new Rect(r.x + 1, r.y + 1, r.width, r.height), false, false, stackTraceViewState.currentRow == element.row, false);

								if (stackTraceList.Count > element.row)
								{
									// 行番号：メソッド名:ファイルの場所
									LogStyle.Draw(new Rect(r.x - 10, r.y, 70, r.height), stackTraceList[element.row].LineNumber.ToString("0000"), false, false, stackTraceViewState.currentRow == element.row, false);
									LogStyle.Draw(new Rect(r.x + 30, r.y, 400, r.height), stackTraceList[element.row].MethodName, false, false, stackTraceViewState.currentRow == element.row, false);
									LogStyle.Draw(new Rect(r.x + 60 + 400, r.y, 500, r.height), stackTraceList[element.row].DataPath, false, false, stackTraceViewState.currentRow == element.row, false);
								}
							}
						}
					}
				}
				finally
				{
					IDisposable disposable;
					if ((disposable = (enumerator as IDisposable)) != null)
					{
						disposable.Dispose();
					}
				}

				// キー入力　エンターキーで押された時の処理
				if (GUIUtility.keyboardControl == stackTraceViewState.ID && current.type == EventType.KeyDown && current.keyCode == KeyCode.Return && stackTraceViewState.currentRow != 0)
				{
					UnityEngine.Object scriptFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(stackTraceList[stackTraceViewState.currentRow].DataPath);
					AssetDatabase.OpenAsset(scriptFile, stackTraceList[stackTraceViewState.currentRow].LineNumber);
				}
			}
		}

		/// <summary>
		/// ログをダブルクリックしたときの動作
		/// </summary>
		/// <param name="currentRow"></param>
		private static void DoubleClicked(int currentRow)
		{
			LogEntry logEntry = new LogEntry();
			LogEntries.GetEntryInternal(currentRow, ref logEntry);

#if UNITY_2019_3_OR_NEWER
			string condition = logEntry.message;
#else
			string condition = logEntry.condition;
#endif

			//if (condition.Contains("CustomDebug:Log"))
			//{
			//	string[] splitLog = condition.Split(new string[] { "UnityEngine.Debug:Log" }, StringSplitOptions.RemoveEmptyEntries);
			//	string[] splitStackTrace = splitLog[1].Split('\n');

			//	string scriptFileData = string.Empty;

			//	// 対象のスクリプトを抽出する
			//	for (int i = 1; i < splitStackTrace.Length; i++)
			//	{
			//		if (!string.IsNullOrEmpty(splitStackTrace[i]) && !splitStackTrace[i].Contains("CustomDebug") && splitStackTrace[i].Contains("Assets"))
			//		{
			//			scriptFileData = splitStackTrace[i];
			//			break;
			//		}
			//	}

			//	// 解析 (at を区切りに分割
			//	var split = scriptFileData.Split(new string[] { "(at " }, StringSplitOptions.RemoveEmptyEntries);

			//	scriptFileData = split[1].Replace(")", "");

			//	string[] targetScriptData = scriptFileData.Split(new string[] { "Assets" }, StringSplitOptions.None)[1].Split(':');

			//	string dataPath = "Assets" + targetScriptData[0];
			//	int lineNumber = int.Parse(targetScriptData[targetScriptData.Length - 1]);

			//	UnityEngine.Object scriptFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dataPath);

			//	// 指定したファイルを指定した行で開く
			//	AssetDatabase.OpenAsset(scriptFile, lineNumber);
			//}
			//else
			if (condition.Contains("Exception:"))
			{
				// 分割したときに空白になる場合は配列に入れない設定
				string[] splitLog = condition.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

				if (splitLog.Length > 1)
				{
					for (int i = 1; i < splitLog.Length; i++)
					{
						if (splitLog[i].Contains("Assets/"))
						{
							string scriptFileData = splitLog[i].Split(new string[] { "(at " }, StringSplitOptions.RemoveEmptyEntries)[1].Replace(")", "");
							string[] targetScriptData = scriptFileData.Split(':');
							string dataPath = targetScriptData[0];
							int lineNumber = int.Parse(targetScriptData[1]);

							UnityEngine.Object scriptFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dataPath);

							if (scriptFile != null)
							{
								// 指定したファイルを指定した行で開く
								AssetDatabase.OpenAsset(scriptFile, lineNumber);
								break;
							}
						}
					}
				}
				else
				{
					// 通常のConsoleの開く処理をさせる
					LogEntries.RowGotDoubleClicked(currentRow);
				}
			}
			else
			{
				// 通常のConsoleの開く処理をさせる
				LogEntries.RowGotDoubleClicked(currentRow);
			}
		}

		/// <summary>
		/// スタックトレースの解析
		/// </summary>
		/// <param name="text"></param>
		private void SetCurrentStackTrace(string text)
		{
#if false
			if (!string.IsNullOrEmpty(text) && text.Contains("UnityEngine.Debug:Log") && !text.Contains("Exception:"))
			{
				stackTraceList.Clear();

				string[] splitLog = text.Split(new string[] { "UnityEngine.Debug:Log" }, StringSplitOptions.RemoveEmptyEntries);
				string[] traces = splitLog[1].Split('\n');

				for (int i = 1; i < traces.Length; i++)
				{
					if (!string.IsNullOrEmpty(traces[i]))
					{
						string[] split = traces[i].Split(new string[] { " (at " }, StringSplitOptions.RemoveEmptyEntries);
						string[] stSplit = split[1].Replace(")", "").Split(':');

						stackTraceList.Add(new StackTraceData() { DataPath = stSplit[0], MethodName = split[0], LineNumber = int.Parse(stSplit[1]) });
					}
				}

				stackTraceViewState.totalRows = stackTraceList.Count;
			}
#else
			if (!string.IsNullOrEmpty(text))
			{
				stackTraceList.Clear();

				// 改行文字で分割
				string[] splitLog = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
				if (splitLog.Length > 1)
				{
					for (int i = 1; i < splitLog.Length; i++)
					{
						// パスが含まれた行を探す
						if (splitLog[i].Contains("Assets/"))
						{
							// パスを分離させる
							string[] split = splitLog[i].Split(new string[] { " (at " }, StringSplitOptions.RemoveEmptyEntries);

							if (1 < split.Length)
							{
								// パスと行番号を分離
								string[] stSplit = split[1].Replace(")", "").Split(':');

								stackTraceList.Add(new StackTraceData()
								{
									DataPath = stSplit[0],
									MethodName = split[0],
									LineNumber = int.Parse(stSplit[1])
								});
							}
						}
					}
					stackTraceViewState.totalRows = stackTraceList.Count;
				}
			}
#endif
			else
			{
				stackTraceViewState.currentRow = -1;
				stackTraceList.Clear();
				stackTraceViewState.totalRows = 0;
			}
		}

		/// <summary>
		/// 検索フィールド
		/// </summary>
		/// <param name="e"></param>
		private void SearchField(Event e)
		{
			string searchBarName = "SearchFilter";
			//if (e.commandName == EventCommandNames.Find)
			if (e.commandName == "Find")
			{
				if (e.type == EventType.ExecuteCommand)
				{
					// キーボードフォーカスを向ける
					EditorGUI.FocusTextInControl(searchBarName);
				}

				if (e.type != EventType.Layout)
					e.Use();
			}

			string text = searchText;
			if (e.type == EventType.KeyDown)
			{
				// エスケープキー押下
				if (e.keyCode == KeyCode.Escape)
				{
					// 検索中止 元に戻す
					text = string.Empty;
					GUIUtility.keyboardControl = listViewState.ID;
					Repaint();
				}
				else if ((e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow) && GUI.GetNameOfFocusedControl() == searchBarName)
				{
					// 検索窓で上下キー入力を行ったら、ログリストにコントロールを移動させる
					GUIUtility.keyboardControl = listViewState.ID;
				}
			}

			GUI.SetNextControlName(searchBarName);

#if UNITY_2019_1_OR_NEWER
			GUIStyle toolbarSearchField = EditorStyles.toolbarSearchField;
#else
			GUIStyle toolbarSearchField = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("ToolbarSeachTextField");
#endif

			Rect rect = GUILayoutUtility.GetRect(0f, (EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + 5.0f) * 1.5f, 16f, 16f, toolbarSearchField, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));

#if UNITY_2019_1_OR_NEWER
			// EditorGUI.ToolbarSearchField(rect, text, false);
			var method = EditorGUIType.GetMethod("ToolbarSearchField", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Rect), typeof(string), typeof(bool) }, null);
			string filteringText = (string)method?.Invoke(null, new object[] { rect, text, false });
#else
			var method = EditorGUIType.GetMethod("ToolbarSearchField", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Rect), typeof(string[]), typeof(int).MakeByRefType(), typeof(string) }, null);
			string filteringText = (string)method.Invoke(null, new object[] { rect, new string[] { "" }, 0, text });
#endif

			if (searchText != filteringText)
			{
				searchText = filteringText;
				LogEntries.SetFilteringText(filteringText);
				SetActiveEntry(null);
			}
		}

		/// <summary>
		/// 詳細ログ内のハイパーテキストをリンクにする
		/// </summary>
		/// <param name="stacktraceText"></param>
		/// <returns></returns>
		static string StacktraceWithHyperlinks(string stacktraceText)
		{
#if UNITY_2019_1_OR_NEWER
			StringBuilder textWithHyperlinks = new StringBuilder();
			var lines = stacktraceText.Split(new string[] { "\n" }, StringSplitOptions.None);
			for (int i = 0; i < lines.Length; ++i)
			{
				string textBeforeFilePath = ") (at ";
				int filePathIndex = lines[i].IndexOf(textBeforeFilePath, StringComparison.Ordinal);
				if (filePathIndex > 0)
				{
					filePathIndex += textBeforeFilePath.Length;
					if (lines[i][filePathIndex] != '<') // sometimes no url is given, just an id between <>, we can't do an hyperlink
					{
						string filePathPart = lines[i].Substring(filePathIndex);
						int lineIndex = filePathPart.LastIndexOf(":", StringComparison.Ordinal); // LastIndex because the url can contain ':' ex:"C:"
						if (lineIndex > 0)
						{
							int endLineIndex = filePathPart.LastIndexOf(")", StringComparison.Ordinal); // LastIndex because files or folder in the url can contain ')'
							if (endLineIndex > 0)
							{
								string lineString =
									filePathPart.Substring(lineIndex + 1, (endLineIndex) - (lineIndex + 1));
								string filePath = filePathPart.Substring(0, lineIndex);

								// aタグを埋め込む
								textWithHyperlinks.Append(lines[i].Substring(0, filePathIndex));
								textWithHyperlinks.Append("<a href=\"" + filePath + "\"" + " line=\"" + lineString + "\">");
								textWithHyperlinks.Append(filePath + ":" + lineString);
								textWithHyperlinks.Append("</a>)\n");

								continue; // continue to evade the default case
							}
						}
					}
				}
				// default case if no hyperlink : we just write the line
				textWithHyperlinks.Append(lines[i] + "\n");
			}
			// Remove the last \n
			if (textWithHyperlinks.Length > 0) // textWithHyperlinks always ends with \n if it is not empty
				textWithHyperlinks.Remove(textWithHyperlinks.Length - 1, 1);
			return textWithHyperlinks.ToString();
#else
			return stacktraceText;
#endif
		}

		#region 操作関数系

		// 選択されたログをアクティブにする。nullが渡された場合はアクティブを解除する
		private void SetActiveEntry(LogEntry entry)
		{
			if (entry != null)
			{

#if UNITY_2019_3_OR_NEWER
				activeText = entry.message;
#else
				activeText = entry.condition;
#endif
				if (activeInstanceID != entry.instanceID)
				{
					activeInstanceID = entry.instanceID;
					if (entry.instanceID != 0)
					{
						EditorGUIUtility.PingObject(entry.instanceID);
					}
				}
			}
			else
			{
				activeText = string.Empty;
				activeInstanceID = 0;
				listViewState.currentRow = -1;
			}

			SetCurrentStackTrace(activeText);
		}

		// ログの設定を変更する
		private void SetFlag(ConsoleFlags flags, bool value)
		{
			LogEntries.SetConsoleFlag((int)flags, value);
		}

		// ログの設定が有効確認する
		private bool HasFlag(ConsoleFlags flags)
		{
			return (LogEntries.consoleFlags & (int)flags) != 0;
		}

		private bool HasMode(int mode, Mode modeToCheck)
		{
			return (mode & (int)modeToCheck) != 0;
		}

		//　モードからどのIconを使用するかを取得する
		private Texture2D GetIconForErrorMode(int mode, bool large)
		{
			Texture2D result;
			if (HasMode(mode, (Mode)3148115))
			{
				result = ((!large) ? iconErrorSmall : iconError);
			}
			else if (HasMode(mode, (Mode)4736))
			{
				result = ((!large) ? iconWarnSmall : iconWarn);
			}
			else if (HasMode(mode, (Mode)1028))
			{
				result = ((!large) ? iconInfoSmall : iconInfo);
			}
			else
			{
				result = null;
			}
			return result;
		}

		/// <summary>
		/// ログの種類から対応したGUIStyleを取得する
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="isIcon"></param>
		/// <param name="isSmall"></param>
		/// <returns></returns>
		private GUIStyle GetStyleForErrorMode(int mode, bool isIcon, bool isSmall)
		{
			GUIStyle result;
			if (HasMode(mode, Mode.Error | Mode.Assert | Mode.Fatal | Mode.AssetImportError | Mode.ScriptingError | Mode.ScriptCompileError | Mode.GraphCompileError | Mode.ScriptingAssertion))
			{
				if (isIcon)
				{
					if (isSmall)
					{
						result = IconErrorSmallStyle;
					}
					else
					{
						result = IconErrorStyle;
					}
				}
				else
				{
					if (isSmall)
					{
						result = ErrorSmallStyle;
					}
					else
					{
						result = ErrorStyle;
					}
				}
			}
			else if (HasMode(mode, Mode.AssetImportWarning | Mode.ScriptingWarning | Mode.ScriptCompileWarning))
			{
				if (isIcon)
				{
					if (isSmall)
					{
						result = IconWarningSmallStyle;
					}
					else
					{
						result = IconWarningStyle;
					}
				}
				else
				{
					if (isSmall)
					{
						result = WarningSmallStyle;
					}
					else
					{
						result = WarningStyle;
					}
				}
			}
			else
			{
				if (isIcon)
				{
					if (isSmall)
					{
						result = IconLogSmallStyle;
					}
					else
					{
						result = IconLogStyle;
					}
				}
				else
				{
					if (isSmall)
					{
						result = LogSmallStyle;
					}
					else
					{
						result = LogStyle;
					}
				}
			}
			return result;
		}

		#endregion

		#region コンテキストメニュー拡張

		public struct StackTraceLogTypeData
		{
			public LogType logType;
			public StackTraceLogType stackTraceLogType;
		}
		
		/// <summary>
		/// Window右上のコンテキストメニューに追加する
		/// IHasCustomMenuの必要な関数
		/// </summary>
		/// <param name="menu"></param>
		public void AddItemsToMenu(GenericMenu menu)
		{
			// Editor Logを開く処理
			if (Application.platform == RuntimePlatform.OSXEditor)
			{
				GUIContent openPlayerLogContent = new GUIContent("Open Player Log");

				if (openPlayerLogFunction == null)
				{
					openPlayerLogFunction = new GenericMenu.MenuFunction(InternalEditorUtility.OpenPlayerConsole);
				}
				menu.AddItem(openPlayerLogContent, false, openPlayerLogFunction);
			}

			GUIContent openEditorLogContent = new GUIContent("Open Editor Log");
			bool on = false;

			if (openEditorLogFunction == null)
			{
				openEditorLogFunction = new GenericMenu.MenuFunction(InternalEditorUtility.OpenEditorConsole);
			}

			menu.AddItem(openEditorLogContent, on, openEditorLogFunction);

#if UNITY_2019_1_OR_NEWER
			menu.AddItem(EditorGUIUtility.TrTextContent("Show Timestamp"), HasFlag(ConsoleFlags.ShowTimestamp), () => { SetFlag(ConsoleFlags.ShowTimestamp, !HasFlag(ConsoleFlags.ShowTimestamp)); });
#else
			menu.AddItem(new GUIContent("Show Timestamp"), HasFlag(ConsoleFlags.ShowTimestamp), () => { SetFlag(ConsoleFlags.ShowTimestamp, !HasFlag(ConsoleFlags.ShowTimestamp)); });
#endif
			// ログ行数設定
			for (int index = 1; index <= 10; index++)
			{
				menu.AddItem(new GUIContent("Log Entry/" + index + " Lines"), index == LogStyleLineCount, new GenericMenu.MenuFunction2(SetLogLineCount), (object)index);
			}

			// StackTraceの詳細設定拡張
			AddStackTraceLoggingMenu(menu);
		}

		/// <summary>
		/// ログ表示行数設定
		/// </summary>
		/// <param name="obj"></param>
		private void SetLogLineCount(object obj)
		{
			int num = (int)obj;
			LogStyleLineCount = num;
			EditorPrefs.SetInt(LOG_LINT_COUNT_KEY, num);
			UpdateLogStyleFixedHeights();
			UpdateListView();
		}

		public void UpdateLogStyleFixedHeights()
		{
			ErrorStyle.fixedHeight = LogStyleLineCount * ErrorStyle.lineHeight + ErrorStyle.border.top;
			WarningStyle.fixedHeight = LogStyleLineCount * WarningStyle.lineHeight + WarningStyle.border.top;
			LogStyle.fixedHeight = LogStyleLineCount * LogStyle.lineHeight + LogStyle.border.top;
		}

		/// <summary>
		/// コンテキストメニュー Stack Trace Logging 設定
		/// </summary>
		/// <param name="menu"></param>
		private void AddStackTraceLoggingMenu(GenericMenu menu)
		{
			IEnumerator enumerator = Enum.GetValues(typeof(LogType)).GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					LogType logType = (LogType)enumerator.Current;
					IEnumerator enumerator2 = Enum.GetValues(typeof(StackTraceLogType)).GetEnumerator();
					try
					{
						while (enumerator2.MoveNext())
						{
							StackTraceLogType stackTraceLogType = (StackTraceLogType)enumerator2.Current;
							StackTraceLogTypeData stackTraceLogTypeData;
							stackTraceLogTypeData.logType = logType;
							stackTraceLogTypeData.stackTraceLogType = stackTraceLogType;
							menu.AddItem(new GUIContent(string.Concat(new object[]
							{
								"Stack Trace Logging/",
								logType,
								"/",
								stackTraceLogType
							})), PlayerSettings.GetStackTraceLogType(logType) == stackTraceLogType, new GenericMenu.MenuFunction2(ToggleLogStackTraces), stackTraceLogTypeData);
						}
					}
					finally
					{
						IDisposable disposable;
						if ((disposable = (enumerator2 as IDisposable)) != null)
						{
							disposable.Dispose();
						}
					}
				}
			}
			finally
			{
				IDisposable disposable2;
				if ((disposable2 = (enumerator as IDisposable)) != null)
				{
					disposable2.Dispose();
				}
			}
			int num = (int)PlayerSettings.GetStackTraceLogType(LogType.Log);
			IEnumerator enumerator3 = Enum.GetValues(typeof(LogType)).GetEnumerator();
			try
			{
				while (enumerator3.MoveNext())
				{
					LogType logType2 = (LogType)enumerator3.Current;
					if (PlayerSettings.GetStackTraceLogType(logType2) != (StackTraceLogType)num)
					{
						num = -1;
						break;
					}
				}
			}
			finally
			{
				IDisposable disposable3;
				if ((disposable3 = (enumerator3 as IDisposable)) != null)
				{
					disposable3.Dispose();
				}
			}
			IEnumerator enumerator4 = Enum.GetValues(typeof(StackTraceLogType)).GetEnumerator();
			try
			{
				while (enumerator4.MoveNext())
				{
					StackTraceLogType stackTraceLogType2 = (StackTraceLogType)enumerator4.Current;
					menu.AddItem(new GUIContent("Stack Trace Logging/All/" + stackTraceLogType2), num == (int)stackTraceLogType2, new GenericMenu.MenuFunction2(ToggleLogStackTracesForAll), stackTraceLogType2);
				}
			}
			finally
			{
				IDisposable disposable4;
				if ((disposable4 = (enumerator4 as IDisposable)) != null)
				{
					disposable4.Dispose();
				}
			}
		}

		public void ToggleLogStackTraces(object userData)
		{
			StackTraceLogTypeData stackTraceLogTypeData = (StackTraceLogTypeData)userData;
			PlayerSettings.SetStackTraceLogType(stackTraceLogTypeData.logType, stackTraceLogTypeData.stackTraceLogType);
		}

		public void ToggleLogStackTracesForAll(object userData)
		{
			IEnumerator enumerator = Enum.GetValues(typeof(LogType)).GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					LogType logType = (LogType)enumerator.Current;
					PlayerSettings.SetStackTraceLogType(logType, (StackTraceLogType)userData);
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = (enumerator as IDisposable)) != null)
				{
					disposable.Dispose();
				}
			}
		}
		#endregion
	}
}