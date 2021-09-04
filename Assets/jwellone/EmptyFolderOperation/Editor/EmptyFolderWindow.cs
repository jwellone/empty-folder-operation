using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace jwellone.Editor
{
	public class EmptyFolderWindow : EditorWindow
	{
		private enum eIconType
		{
			Refresh,
			FolderAdded,
			FolderRemoved,
			OpenFolder,
			Trash,
			Error,
			Num,
		}

		[MenuItem("jewllone/Window/Empty Folder Window")]
		public static void Open()
		{
			var window = EditorWindow.GetWindow<EmptyFolderWindow>();
			window.titleContent = new GUIContent("Empty Folder Window");
			window.Show();

		}

		private readonly GUILayoutOption ICON_WIDTH = GUILayout.Width(28);
		private readonly GUILayoutOption ICON_HEIGHT = GUILayout.Height(20);

		private List<string> m_entryFolders;
		private Vector2 m_entryFolderScrollPos = Vector2.zero;
		private Vector2 m_emptyFoldersScrollPos = Vector2.zero;
		private List<string> m_serachFolders = new List<string>();
		private List<string> m_emptyfolders = new List<string>();
		private Texture2D[] m_icons = new Texture2D[(int)eIconType.Num];

		private void OnEnable()
		{
			m_entryFolders = new List<string>(EmptyFolderManager.EntryPaths);

			LoadAndAddIcon(eIconType.Refresh, "d_Refresh");
			LoadAndAddIcon(eIconType.FolderAdded, "d_Toolbar Plus");
			LoadAndAddIcon(eIconType.FolderRemoved, "d_Toolbar Minus");
			LoadAndAddIcon(eIconType.OpenFolder, "d_FolderOpened Icon");
			LoadAndAddIcon(eIconType.Trash, "TreeEditor.Trash");
			LoadAndAddIcon(eIconType.Error, "CollabError");

			OnSearch();
		}

		private void OnDisable()
		{
			for (var i = 0; i < m_icons.Length; ++i)
			{
				m_icons[i] = null;
			}
		}

		private void OnGUI()
		{
			EmptyFolderManager.IsOutputLog = GUILayout.Toggle(EmptyFolderManager.IsOutputLog, "ログ出力");
			OnDrawEntryFolders();
			OnDrawEmptyFolders();
		}

		private void OnDrawEntryFolders()
		{
			GUILayout.Box(string.Empty, GUILayout.Width(position.width), GUILayout.Height(1.5f));
			GUILayout.Space(10);

			EmptyFolderManager.IsAutoDelete = GUILayout.Toggle(EmptyFolderManager.IsAutoDelete, "エディタ起動時に自動削除");

			var removeIcon = GetIcon(eIconType.FolderRemoved);
			var openIcon = GetIcon(eIconType.OpenFolder);
			var warningIcon = GetIcon(eIconType.Error);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("検索フォルダ(" + EmptyFolderManager.EntryPaths.Count + ")", ICON_HEIGHT);

			if (GUILayout.Button(removeIcon, ICON_WIDTH, ICON_HEIGHT))
			{
				if (EditorUtility.DisplayDialog("検索フォルダ", "登録フォルダをクリアしますか？", "Ok", "Cancel"))
				{
					m_entryFolders.Clear();
					EmptyFolderManager.SetEntryPath(m_entryFolders);
					OnSearch(true);
				}
			}

			if (GUILayout.Button(GetIcon(eIconType.FolderAdded), ICON_WIDTH, ICON_HEIGHT))
			{
				var path = OnClickOpenFolder();
				if (!string.IsNullOrEmpty(path))
				{
					m_entryFolders.Add(path);
					EmptyFolderManager.SetEntryPath(m_entryFolders);
					OnSearch(true);
				}
			}

			GUILayout.Box(string.Empty, GUILayout.Width(0.01f));

			EditorGUILayout.EndHorizontal();

			using (var scroll = new EditorGUILayout.ScrollViewScope(m_entryFolderScrollPos, GUI.skin.box, GUILayout.Height(114)))
			{
				m_entryFolderScrollPos = scroll.scrollPosition;

				var styles = new GUIStyle(EditorStyles.miniButton);
				styles.alignment = TextAnchor.MiddleLeft;
				var deleteIndex = -1;
				for (var i = 0; i < m_entryFolders.Count; ++i)
				{
					var path = m_entryFolders[i];

					EditorGUILayout.BeginHorizontal();

					var isValid = AssetDatabase.IsValidFolder(path);
					if (GUILayout.Button(path, styles))
					{
						if (isValid)
						{
							Selection.activeObject = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
						}
						else
						{
							EditorUtility.DisplayDialog("Error", path + "\nフォルダは存在しません", "Close");
						}
					}

					if (GUILayout.Button(removeIcon, ICON_WIDTH, ICON_HEIGHT))
					{
						deleteIndex = i;
					}

					if (GUILayout.Button(isValid ? openIcon : warningIcon, ICON_WIDTH, ICON_HEIGHT))
					{
						if (isValid)
						{
							EditorUtility.RevealInFinder(path);
						}
						else
						{
							EditorUtility.DisplayDialog("Error", path + "\nフォルダは存在しません", "Close");
						}
					}
					EditorGUILayout.EndHorizontal();
				}

				if (deleteIndex != -1)
				{
					if (EditorUtility.DisplayDialog("検索フォルダ", m_entryFolders[deleteIndex] + "フォルダをリストから外しますか？", "Ok", "Cancel"))
					{
						m_entryFolders.RemoveAt(deleteIndex);
						EmptyFolderManager.SetEntryPath(m_entryFolders);
						OnSearch(true);
					}
				}
			}
		}

		private void OnDrawEmptyFolders()
		{
			GUILayout.Box(string.Empty, GUILayout.Width(position.width), GUILayout.Height(1.5f));
			GUILayout.Space(10);

			EditorGUILayout.BeginHorizontal();

			GUILayout.Label("空フォルダ(" + m_emptyfolders.Count + ")", ICON_HEIGHT);

			if (GUILayout.Button(GetIcon(eIconType.Trash), ICON_WIDTH, ICON_HEIGHT))
			{
				if (EditorUtility.DisplayDialog("空フォルダ", "全ての空フォルダを削除しますか？", "Ok", "Cancel"))
				{
					foreach (var folder in m_emptyfolders)
					{
						EmptyFolderManager.Delete(folder);
					}

					OnRefresh(true);
				}
			}

			if (GUILayout.Button(GetIcon(eIconType.Refresh), ICON_WIDTH, ICON_HEIGHT))
			{
				OnSearch();
			}

			GUILayout.Box(string.Empty, GUILayout.Width(0.01f));

			EditorGUILayout.EndHorizontal();

			var trashIcon = GetIcon(eIconType.Trash);
			var selectIcon = GetIcon(eIconType.OpenFolder);

			using (var scroll = new EditorGUILayout.ScrollViewScope(m_emptyFoldersScrollPos, GUI.skin.box))
			{
				var deleteIndex = -1;
				m_emptyFoldersScrollPos = scroll.scrollPosition;

				var styles = new GUIStyle(EditorStyles.miniButton);
				styles.alignment = TextAnchor.MiddleLeft;
				for (var i = 0; i < m_emptyfolders.Count; ++i)
				{
					EditorGUILayout.BeginHorizontal();
					var targetPath = m_emptyfolders[i];

					if (GUILayout.Button(targetPath, styles))
					{
						Selection.activeObject = AssetDatabase.LoadAssetAtPath(targetPath, typeof(UnityEngine.Object));
					}

					if (GUILayout.Button(trashIcon, ICON_WIDTH, ICON_HEIGHT))
					{
						deleteIndex = i;
					}

					if (GUILayout.Button(selectIcon, ICON_WIDTH, ICON_HEIGHT))
					{
						EditorUtility.RevealInFinder(targetPath);
					}

					EditorGUILayout.EndHorizontal();
				}

				if (deleteIndex != -1)
				{
					if (EditorUtility.DisplayDialog("空フォルダ", m_emptyfolders[deleteIndex] + "フォルダを削除しますか？", "Ok", "Cancel"))
					{
						var path = Application.dataPath + m_emptyfolders[deleteIndex].Remove(0, "Assets".Length);
						if (EmptyFolderManager.Delete(path))
						{
							m_emptyfolders.RemoveAt(deleteIndex);
							OnRefresh(true);
						}
					}
				}
			}
		}

		private void OnSearch(bool isAssetDatabaseRefresh = false)
		{
			m_serachFolders.Clear();
			for (var i = 0; i < m_entryFolders.Count; ++i)
			{
				var path = EmptyFolderManager.RelativePathToAbsolutePath(m_entryFolders[i]);
				m_serachFolders.Add(path);
			}

			OnRefresh(isAssetDatabaseRefresh);
		}

		private void OnRefresh(bool isAssetDatabaseRefresh = false)
		{
			var list = new List<string>();
			foreach (var folder in m_serachFolders)
			{
				EmptyFolderManager.GetEmptyFolder(folder, ref list);
			}

			m_emptyfolders.Clear();
			var dataPathLen = Application.dataPath.Length - ("Assets").Length;
			for (var i = 0; i < list.Count; ++i)
			{
				var path = list[i].Substring(dataPathLen, list[i].Length - dataPathLen);
				if (!m_emptyfolders.Exists(x => x == path))
				{
					m_emptyfolders.Add(path);
				}
			}

			if (isAssetDatabaseRefresh)
			{
				AssetDatabase.Refresh();
			}
		}

		private string OnClickOpenFolder()
		{
			var selectFolder = EditorUtility.OpenFolderPanel("フォルダ選択", Application.dataPath, string.Empty);
			if (string.IsNullOrEmpty(selectFolder))
			{
				return string.Empty;
			}

			if (!selectFolder.Contains(Application.dataPath))
			{
				EditorUtility.DisplayDialog("フォルダ選択", "プロジェクト内のフォルダを選択してください", "Close");
				return string.Empty;
			}

			selectFolder = selectFolder.Remove(0, Application.dataPath.Length - 6);

			foreach (var entryPath in m_entryFolders)
			{
				if (entryPath.Equals(selectFolder))
				{
					EditorUtility.DisplayDialog("フォルダ選択", "既に登録済みです\n" + selectFolder, "Close");
					return string.Empty;
				}
			}

			return selectFolder;
		}

		private void LoadAndAddIcon(eIconType type, string name)
		{
			m_icons[(int)type] = EditorGUIUtility.Load(name) as Texture2D;
		}

		private Texture2D GetIcon(eIconType type)
		{
			return m_icons[(int)type];
		}
	}
}
