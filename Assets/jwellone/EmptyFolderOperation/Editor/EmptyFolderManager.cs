using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace jwellone.Editor
{
	[InitializeOnLoad]
	public static class EmptyFolderManager
	{
		private const string EUS_KEY = "EUS_KEY_EMPTY_FOLDER_MANAGER";
		private static readonly string LOCK_FILE_PATH = "Temp/EmptyFolderManagerLockFile";

		[Serializable]
		private class SettingInfo
		{
			public bool isAutoDelete = false;
			public bool isOutputLog = true;
			public List<string> paths;
		}

		private static SettingInfo s_info = null;

		public static bool IsOutputLog
		{
			get
			{
				return Info.isOutputLog;
			}
			set
			{
				if (value != Info.isOutputLog)
				{
					Info.isOutputLog = value;
					SaveInfo();
				}
			}
		}

		public static bool IsAutoDelete
		{
			get
			{
				return Info.isAutoDelete;
			}
			set
			{
				if (value != Info.isAutoDelete)
				{
					Info.isAutoDelete = value;
					SaveInfo();
				}
			}
		}

		public static IReadOnlyList<string> EntryPaths
		{
			get
			{
				return Info.paths;
			}
		}

		private static SettingInfo Info
		{
			get
			{
				if (s_info == null)
				{
					var json = EditorUserSettings.GetConfigValue(EUS_KEY);
					s_info = JsonUtility.FromJson<SettingInfo>(json);
				}

				return s_info;
			}
		}

		static EmptyFolderManager()
		{
			if (File.Exists(LOCK_FILE_PATH))
			{
				return;
			}

			File.Create(LOCK_FILE_PATH);

			if(string.IsNullOrEmpty(EditorUserSettings.GetConfigValue(EUS_KEY)))
			{
				s_info = new SettingInfo();
				s_info.paths = new List<string>(new string[] { "Assets" });
				SaveInfo();
			}

			if (!IsAutoDelete)
			{
				return;
			}

			var emptyFolders = new List<string>();
			for (var i = 0; i < EntryPaths.Count; ++i)
			{
				var path = RelativePathToAbsolutePath(EntryPaths[i]);
				EmptyFolderManager.GetEmptyFolder(path, ref emptyFolders);
			}

			foreach (var targetFolder in emptyFolders)
			{
				Delete(targetFolder);
			}

			for (var i = 0; i < EntryPaths.Count; ++i)
			{
				var path = RelativePathToAbsolutePath(EntryPaths[i]);
				if (!Directory.Exists(path))
				{
					EditorApplication.delayCall += DelayAndOpenWindow;
					Log("<color=red>" + path + "は存在しません. 設定ファイルを確認してください.</color>");
					break;
				}
			}
		}

		private static void DelayAndOpenWindow()
		{
			EditorApplication.delayCall -= DelayAndOpenWindow;
			EmptyFolderWindow.Open();
		}

		public static void SetEntryPath(IReadOnlyList<string> list)
		{
			Info.paths = new List<string>(list);
			SaveInfo();
		}

		private static void SaveInfo()
		{
			var json = JsonUtility.ToJson(Info);
			EditorUserSettings.SetConfigValue(EUS_KEY, json);
		}

		public static bool GetEmptyFolder(string path, ref List<string> list)
		{
			if (!Directory.Exists(path))
			{
				return true;
			}

			var isEmpty = true;
			foreach (var targetPath in Directory.GetDirectories(path, "*.*"))
			{
				isEmpty &= GetEmptyFolder(targetPath, ref list);
			}

			if (!isEmpty || !IsEmpty(path))
			{
				return false;
			}

			list.Add(path);

			return true;
		}

		public static bool IsEmpty(string path)
		{
			var count = 0;
			foreach (var filePath in Directory.GetFiles(path))
			{
				if (Path.GetExtension(filePath) != ".meta")
				{
					++count;
				}
			}

			return count == 0;
		}

		public static bool Delete(string fullPath)
		{
			if (!Directory.Exists(fullPath) || fullPath.EndsWith("/") || fullPath.Equals(Application.dataPath))
			{
				return false;
			}

			Directory.Delete(fullPath, true);

			var metaFile = fullPath + ".meta";
			if (File.Exists(metaFile))
			{
				File.Delete(metaFile);
			}

			Log("[削除]" + fullPath);

			return true;
		}

		public static string RelativePathToAbsolutePath(string relativePath)
		{
			var path = relativePath.Replace("Assets/", string.Empty);
			path = path.Replace("Assets", string.Empty);
			path = Path.Combine(Application.dataPath, path);
			return path;
		}

		public static void Log(string message)
		{
			if (IsOutputLog)
			{
				Debug.Log(message);
			}
		}
	}
}
