using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CustomConsole.Editor
{
	/// <summary>
	/// UnityEditor.LogEntriesを操作するクラス
	/// </summary>
	public static class LogEntries
	{
#if UNITY_2017_1_OR_NEWER
		private const string AssemblyLogEntries = "UnityEditor.LogEntries";
#else
		private const string AssemblyLogEntries = "UnityEditorInternal.LogEntries";
#endif

		private static readonly Type LogEntriesType;
		private static readonly BindingFlags sFlags = BindingFlags.Static | BindingFlags.Public;

		static LogEntries()
		{
			if (LogEntriesType == null)
			{
				LogEntriesType = Assembly.Load(CustomConsoleWindow.ASSEMBLY_NAME).GetType(AssemblyLogEntries);
			}
		}

		/// <summary>
		/// 指定したログを開く
		/// </summary>
		/// <param name="index"></param>
		public static void RowGotDoubleClicked(int index)
		{
			LogEntriesType.GetMethod("RowGotDoubleClicked", sFlags)?.Invoke(null, new object[1] { index });
		}

		/// <summary>
		/// 指定したファイルを開く？
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="line"></param>
		/// <param name="column"></param>
		public static void OpenFileOnSpecificLineAndColumn(string filePath, int line, int column)
		{
			LogEntriesType.GetMethod("OpenFileOnSpecificLineAndColumn", sFlags)?.Invoke(null, new object[] { filePath, line, column });
		}

		public static string GetStatusText()
		{
			return (string)LogEntriesType.GetMethod("GetStatusText", sFlags)?.Invoke(null, new object[0]);
		}

		public static int GetStatusMask()
		{
			return (int)(LogEntriesType.GetMethod("GetStatusMask", sFlags)?.Invoke(null, new object[0]) ?? 0);
		}

		/// <summary>
		/// GetEntryInternal・GetFirstTwoLinesEntryTextAndModeInternalの使用区間開始を宣言する
		/// </summary>
		/// <returns></returns>
		public static int StartGettingEntries()
		{
			return (int)(LogEntriesType.GetMethod("StartGettingEntries", sFlags)?.Invoke(null, new object[0]) ?? 0);
		}

		/// <summary>
		/// ログコンソールの情報フラグを取得する
		/// </summary>
		public static int consoleFlags
		{
			get
			{
				return (int)(LogEntriesType.GetProperty("consoleFlags", sFlags)?.GetValue(null, new object[0]) ?? 0);
			}
			set
			{
				LogEntriesType.GetProperty("consoleFlags", sFlags)?.SetValue(null, value, new object[0]);
			}
		}

		/// <summary>
		/// ログコンソール設定情報を設定する
		/// </summary>
		/// <param name="bit"></param>
		/// <param name="value"></param>
		public static void SetConsoleFlag(int bit, bool value)
		{
			LogEntriesType.GetMethod("SetConsoleFlag", sFlags)?.Invoke(null, new object[] { bit, value });
		}

		/// <summary>
		/// ログ検索
		/// </summary>
		/// <param name="filteringText"></param>
		public static void SetFilteringText(string filteringText)
		{
			LogEntriesType.GetMethod("SetFilteringText", sFlags)?.Invoke(null, new object[] { filteringText });
		}

		public static string GetFilteringText()
		{
			return (string)LogEntriesType.GetMethod("GetFilteringText", sFlags)?.Invoke(null, new object[0]);
		}

		/// <summary>
		/// 指定したログと同一のログの数を得る
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		public static int GetEntryCount(int row)
		{
			return (int)(LogEntriesType.GetMethod("GetEntryCount", sFlags)?.Invoke(null, new object[] {row}) ?? 0);
		}

		/// <summary>
		/// 現在のログの総数を得る
		/// </summary>
		/// <returns></returns>
		public static int GetCount()
		{
			return (int)(LogEntriesType.GetMethod("GetCount", sFlags)?.Invoke(null, new object[0]) ?? 0);
		}

		/// <summary>
		/// ログをクリアする
		/// </summary>
		public static void Clear()
		{
			LogEntriesType.GetMethod("Clear", sFlags)?.Invoke(null, new object[0]);
		}

		/// <summary>
		/// 現在の各ログのタイプごとの数を返す
		/// </summary>
		/// <param name="errorCount"></param>
		/// <param name="warningCount"></param>
		/// <param name="logCount"></param>
		public static void GetCountsByType(ref int errorCount, ref int warningCount, ref int logCount)
		{
			object[] properties = new object[3] { null, null, null };
			LogEntriesType.GetMethod("GetCountsByType", sFlags)?.Invoke(LogEntriesType, properties);

			errorCount = (int)properties[0];
			warningCount = (int)properties[1];
			logCount = (int)properties[2];
		}

		/// <summary>
		/// 指定した行のログを取得する
		/// 必ずStartGettingEntries・EndGettingEntriesで囲むこと
		/// </summary>
		/// <param name="row"></param>
		/// <param name="logEntry"></param>
		/// <returns></returns>
		public static bool GetEntryInternal(int row, ref LogEntry logEntry)
		{
			ConstructorInfo ctor = LogEntry.GetOriginalType.GetConstructor(Type.EmptyTypes);
			object logEntryObject = ctor?.Invoke(null);
			bool result = (bool) (LogEntriesType.GetMethod("GetEntryInternal", sFlags)?.Invoke(null, new [] {row, logEntryObject}) ?? false);

			// オリジナルログの中身を取り出し利用できるように割り当てる
			logEntry.SetValueFromOriginalObject(logEntryObject);
			return result;
		}

		/// <summary>
		/// 指定したログの指定した行目までの文字情報とログの種類を返す
		/// </summary>
		/// <param name="row">位置</param>
		/// <param name="numberOfLines">必要な行数</param>
		/// <param name="mode">現在のモード</param>
		/// <param name="outString">ログ内容</param>
		public static void GetFirstTwoLinesEntryTextAndModeInternal(int row, int numberOfLines, ref int mode, ref string outString)
		{
#if UNITY_2017_1_OR_NEWER
			object[] properties = new object[4] { row, numberOfLines, null, null };
			LogEntriesType.GetMethod("GetLinesAndModeFromEntryInternal", sFlags)?.Invoke(LogEntriesType, properties);
			mode = (int)properties[2];
			outString = (string)properties[3];
#else
			object[] properties = new object[3] { row, null, null };
			LogEntriesType.GetMethod("GetFirstTwoLinesEntryTextAndModeInternal", sFlags).Invoke(LogEntriesType, properties);
			mode = (int)properties[1];
			outString = (string)properties[2];
#endif
		}

#if UNITY_2017_1_OR_NEWER
		public static void GetLineAndModeFromEntryInternal(int row, int numberOfLines, ref int mask, [In, Out] ref string outString)
		{
			object[] properties = new object[] { row, numberOfLines, null, null };
			LogEntriesType.GetMethod("GetLinesAndModeFromEntryInternal", sFlags)?.Invoke(LogEntriesType, properties);
			mask = (int)properties[2];
			outString = (string)properties[3];
		}
#endif

		/// <summary>
		/// GetEntryInternal・GetFirstTwoLinesEntryTextAndModeInternalを使用区間の終了を宣言する
		/// </summary>
		public static void EndGettingEntries()
		{
			LogEntriesType.GetMethod("EndGettingEntries", sFlags)?.Invoke(null, new object[0]);
		}
	}
}