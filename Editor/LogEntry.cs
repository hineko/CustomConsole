using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CustomConsole.Editor
{
	/// <summary>
	/// コンソールログのデータ構造
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public sealed class LogEntry
	{
#if UNITY_2017_1_OR_NEWER
		private const string AssemblyLogEntry = "UnityEditor.LogEntry";
#else
		private const string AssemblyLogEntry = "UnityEditorInternal.LogEntry";
#endif
		public static Type GetOriginalType { get; } = Assembly.Load(CustomConsoleWindow.ASSEMBLY_NAME).GetType(AssemblyLogEntry);

#if UNITY_2019_3_OR_NEWER
		public string message;
		public int column;
#else
		public string condition;
		public int errorNum;
#endif

		public string file;
		public int line;
		public int mode;
		public int instanceID;
		public int identifier;
		public int isWorldPlaying;

		/// <summary>
		/// オリジナルのログから内容を引き出す
		/// </summary>
		/// <param name="original">internalなオリジナルログデータ</param>
		public void SetValueFromOriginalObject(object original)
		{
			// オリジナルログの各変数情報を取得する
			FieldInfo[] infos = GetOriginalType.GetFields(BindingFlags.Public | BindingFlags.Instance);

			// こちらの変数に割り当てなおす
			foreach (FieldInfo info in infos)
			{
				// 自身の型を取得し、同名の変数にデータを当てなおす
				GetType().GetField(info.Name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(this, info.GetValue(original));
			}
		}

		public string Dump()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			sb.AppendLine("///////////////////////////////////////////////////////\n");

#if UNITY_2019_3_OR_NEWER
			sb.AppendLine("message :\t" + message);
			sb.AppendLine("column :\t" + column);
#else
			sb.AppendLine("condition :\t" + condition);
			sb.AppendLine("errorNum :\t" + errorNum);
#endif
			sb.AppendLine("file :\t" + file);
			sb.AppendLine("line :\t" + line);
			sb.AppendLine("mode :\t" + mode);
			sb.AppendLine("instanceID :\t" + instanceID);
			sb.AppendLine("identifier :\t" + identifier);
			sb.AppendLine("isWorldPlaying :\t" + isWorldPlaying);

			sb.AppendLine("\n///////////////////////////////////////////////////////\n");

			return sb.ToString();
		}
	}
}