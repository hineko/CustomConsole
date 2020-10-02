using System;
using System.Reflection;
using UnityEngine;

namespace CustomConsole.Editor
{
	public class SplitterGUILayout
	{
		private static readonly int splitterHash = "Splitter".GetHashCode();

		private static Type guiLayoutUtility;
		private static Type splitterGUILayout;

		private static Type GetGuiLayoutUtility
		{
			get
			{
				return guiLayoutUtility ?? (guiLayoutUtility = Assembly.Load("UnityEngine/UnityEngine.IMGUIModule.dll").GetType("UnityEngine.GUILayoutUtility"));
			}
		}
		private static Type GetSplitterGUILayout
		{
			get
			{
				return splitterGUILayout ?? (splitterGUILayout = Assembly.Load("UnityEditor.dll").GetType("UnityEditor.SplitterGUILayout"));
			}
		}

		public static void CustomBeginSplit(SplitterState state, GUIStyle style, bool vertical, params GUILayoutOption[] options)
		{
			object[] properties = new object[] { state.GetOriginalState(), style, vertical, options };
			GetSplitterGUILayout.GetMethod("BeginSplit", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, properties);
		}

		public static void BeginHorizontalSplit(SplitterState state, params GUILayoutOption[] options)
		{
			CustomBeginSplit(state, GUIStyle.none, false, options);
		}

		public static void BeginVerticalSplit(SplitterState state, params GUILayoutOption[] options)
		{
			CustomBeginSplit(state, GUIStyle.none, true, options);
		}

		public static void BeginHorizontalSplit(SplitterState state, GUIStyle style, params GUILayoutOption[] options)
		{
			CustomBeginSplit(state, style, false, options);
		}

		public static void BeginVerticalSplit(SplitterState state, GUIStyle style, params GUILayoutOption[] options)
		{
			CustomBeginSplit(state, style, true, options);
		}

		public static void EndVerticalSplit()
		{
			GetSplitterGUILayout.GetMethod("EndVerticalSplit", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new object[0]);
#if false
			GetGuiLayoutUtility.GetMethod("EndLayoutGroup", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { });
#endif
			//GUILayoutUtility.EndLayoutGroup();
		}

		public static void EndHorizontalSplit()
		{
			GetSplitterGUILayout.GetMethod("EndHorizontalSplit", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new object[0]);

#if false
			GetGuiLayoutUtility.GetMethod("EndLayoutGroup", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { });
#endif
			//GUILayoutUtility.EndLayoutGroup();
		}
	}
}