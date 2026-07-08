#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Callbacks;

namespace Light_and_controller.Scripts.Editor
{
    /// <summary>
    /// Editor window for creating new levels with automated setup
    /// </summary>
    public class LevelCreatorWindow : EditorWindow
    {
        #region Fields
        private string levelName = "";
        private SceneName selectedSceneToRemove = SceneName.Initialization;
        private SceneName selectedSceneToRename = SceneName.Initialization;
        private string newLevelName = "";
        private bool showRemoveSection = false;
        private bool showRenameSection = false;

        private const string LEVELS_FOLDER = "Assets/Light and controller/Scenes/Levels";
        private const string TEMPLATE_PATH = "Assets/Light and controller/Scenes/Levels/LevelTemplate.unity";
        private const string SCENE_NAME_PATH = "Assets/Light and controller/Scripts/SceneName.cs";
        private const string LEVEL_ORDER_ASSET_PATH = "Assets/Light and controller/GameData/LevelOrder.asset";
        private const string PENDING_LEVEL_KEY = "LevelCreator_PendingLevel";
        private const string RENAME_OLD_KEY = "LevelCreator_RenameOld";
        private const string RENAME_NEW_KEY = "LevelCreator_RenameNew";

        private static readonly SceneName[] CoreScenes = { SceneName.Initialization, SceneName.Shared, SceneName.MainMenu };
        #endregion

        #region Menu & Window
        [MenuItem("Tools/Level Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelCreatorWindow>("Level Creator");
            window.minSize = new Vector2(400, 350);
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            GUILayout.Space(10);
            DrawCreateSection();
            GUILayout.Space(20);
            DrawRenameSection();
            GUILayout.Space(20);
            DrawRemoveSection();
        }

        private void DrawCreateSection()
        {
            EditorGUILayout.LabelField("Create New Level", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will:\n1. Copy LevelTemplate.unity to a new scene\n2. Add the level to SceneName enum\n" +
                "3. Add the level to LevelOrder asset\n4. Add the level to Build Settings",
                MessageType.Info);

            GUILayout.Space(10);
            DrawLabeledTextField("Level Name:", ref levelName);
            GUILayout.Space(10);

            GUI.enabled = !string.IsNullOrWhiteSpace(levelName);
            if (GUILayout.Button("Create Level", GUILayout.Height(30)))
                CreateLevel();
            GUI.enabled = true;

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Level name should be in PascalCase (e.g., 'LevelMyNewLevel' or 'MyNewLevel')", MessageType.None);
        }

        private void DrawRenameSection()
        {
            showRenameSection = EditorGUILayout.Foldout(showRenameSection, "Rename Level", true, EditorStyles.foldoutHeader);
            if (!showRenameSection) return;

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This will:\n1. Rename the scene file\n2. Update SceneName enum\n" +
                "3. Update LevelOrder asset\n4. Update Build Settings",
                MessageType.Info);

            GUILayout.Space(10);
            DrawLabeledEnumPopup("Select Level:", ref selectedSceneToRename);
            GUILayout.Space(5);
            DrawLabeledTextField("New Name:", ref newLevelName);
            GUILayout.Space(10);

            bool isCoreScene = IsCoreScene(selectedSceneToRename);
            GUI.enabled = !isCoreScene && !string.IsNullOrWhiteSpace(newLevelName);
            if (GUILayout.Button("Rename Level", GUILayout.Height(30)))
                RenameLevel(selectedSceneToRename, newLevelName);
            GUI.enabled = true;

            if (isCoreScene)
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Core scenes (Initialization, Shared, MainMenu) cannot be renamed.", MessageType.Info);
            }
        }

        private void DrawRemoveSection()
        {
            showRemoveSection = EditorGUILayout.Foldout(showRemoveSection, "Remove Level", true, EditorStyles.foldoutHeader);
            if (!showRemoveSection) return;

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This will:\n1. Delete the scene file\n2. Remove from SceneName enum\n" +
                "3. Remove from LevelOrder asset\n4. Remove from Build Settings",
                MessageType.Warning);

            GUILayout.Space(10);
            DrawLabeledEnumPopup("Select Level:", ref selectedSceneToRemove);
            GUILayout.Space(10);

            bool isCoreScene = IsCoreScene(selectedSceneToRemove);
            GUI.enabled = !isCoreScene;
            if (GUILayout.Button("Remove Level", GUILayout.Height(30)))
                RemoveLevel(selectedSceneToRemove);
            GUI.enabled = true;

            if (isCoreScene)
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Core scenes (Initialization, Shared, MainMenu) cannot be removed.", MessageType.Info);
            }
        }

        private void DrawLabeledTextField(string label, ref string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            value = EditorGUILayout.TextField(value);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLabeledEnumPopup(string label, ref SceneName value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            value = (SceneName)EditorGUILayout.EnumPopup(value);
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Utility Methods
        private static bool IsCoreScene(SceneName scene) => CoreScenes.Contains(scene);

        private static bool ValidateLevelName(string name, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Level name cannot be empty!";
                return false;
            }

            if (!Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9]*$"))
            {
                error = "Level name must start with a letter and contain only letters and numbers (no spaces or special characters)!";
                return false;
            }

            return true;
        }

        private static string GetScenePath(string sceneName) => $"{LEVELS_FOLDER}/{sceneName}.unity";

        private static void ClearInputField(ref string field)
        {
            field = "";
            GUI.FocusControl(null);
        }
        #endregion

        #region Create Level
        private void CreateLevel()
        {
            if (!ValidateLevelName(levelName, out string error))
            {
                EditorUtility.DisplayDialog("Error", error, "OK");
                return;
            }

            if (!File.Exists(TEMPLATE_PATH))
            {
                EditorUtility.DisplayDialog("Error", $"Template scene not found at: {TEMPLATE_PATH}", "OK");
                return;
            }

            string newScenePath = GetScenePath(levelName);
            if (File.Exists(newScenePath))
            {
                EditorUtility.DisplayDialog("Error", $"A level with the name '{levelName}' already exists!", "OK");
                return;
            }

            try
            {
                AssetDatabase.CopyAsset(TEMPLATE_PATH, newScenePath);
                SceneNameEnumEditor.AddEntry(levelName);
                EditorPrefs.SetString(PENDING_LEVEL_KEY, levelName);
                BuildSettingsEditor.AddScene(newScenePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success",
                    $"Level '{levelName}' created successfully!\n\nScene: {newScenePath}\n" +
                    "Added to SceneName enum\nAdded to LevelOrder asset\nAdded to Build Settings", "OK");

                ClearInputField(ref levelName);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create level: {e.Message}", "OK");
                Debug.LogError($"Level creation failed: {e}");
            }
        }
        #endregion

        #region Rename Level
        private void RenameLevel(SceneName sceneToRename, string newName)
        {
            string oldName = sceneToRename.ToString();

            if (!ValidateLevelName(newName, out string error))
            {
                EditorUtility.DisplayDialog("Error", error, "OK");
                return;
            }

            if (System.Enum.TryParse<SceneName>(newName, out _))
            {
                EditorUtility.DisplayDialog("Error", $"A level with the name '{newName}' already exists!", "OK");
                return;
            }

            string oldScenePath = GetScenePath(oldName);
            string newScenePath = GetScenePath(newName);

            if (!File.Exists(oldScenePath))
            {
                EditorUtility.DisplayDialog("Error", $"Scene file not found: {oldScenePath}", "OK");
                return;
            }

            if (File.Exists(newScenePath))
            {
                EditorUtility.DisplayDialog("Error", $"A scene file with the name '{newName}.unity' already exists!", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm Rename",
                $"Rename '{oldName}' to '{newName}'?\n\nThis will:\n• Rename the scene file\n• Update SceneName enum\n" +
                "• Update LevelOrder asset\n• Update Build Settings", "Rename", "Cancel"))
                return;

            try
            {
                string result = AssetDatabase.RenameAsset(oldScenePath, newName);
                if (!string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to rename scene file: {result}", "OK");
                    return;
                }

                BuildSettingsEditor.UpdateScenePath(oldScenePath, newScenePath);
                SceneNameEnumEditor.RenameEntry(oldName, newName);
                EditorPrefs.SetString(RENAME_OLD_KEY, oldName);
                EditorPrefs.SetString(RENAME_NEW_KEY, newName);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success",
                    $"Level renamed successfully!\n\nOld name: {oldName}\nNew name: {newName}\n\n" +
                    "After script compilation, the LevelOrder asset will be updated automatically.", "OK");

                ClearInputField(ref newLevelName);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to rename level: {e.Message}", "OK");
                Debug.LogError($"Level rename failed: {e}");
            }
        }
        #endregion

        #region Remove Level
        private void RemoveLevel(SceneName sceneToRemove)
        {
            string sceneName = sceneToRemove.ToString();

            if (!EditorUtility.DisplayDialog("Confirm Removal",
                $"Are you sure you want to remove '{sceneName}'?\n\nThis will:\n• Delete the scene file\n" +
                "• Remove from SceneName enum\n• Remove from LevelOrder asset\n• Remove from Build Settings\n\n" +
                "This action cannot be undone!", "Remove", "Cancel"))
                return;

            try
            {
                string scenePath = GetScenePath(sceneName);

                BuildSettingsEditor.RemoveScene(scenePath);
                LevelOrderEditor.RemoveScene(sceneToRemove);

                if (File.Exists(scenePath))
                    AssetDatabase.DeleteAsset(scenePath);
                else
                    Debug.LogWarning($"Scene file not found: {scenePath}");

                SceneNameEnumEditor.RemoveEntry(sceneName);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success",
                    $"Level '{sceneName}' removed successfully!\n\nDeleted scene file\n" +
                    "Removed from SceneName enum\nRemoved from LevelOrder asset\nRemoved from Build Settings", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to remove level: {e.Message}", "OK");
                Debug.LogError($"Level removal failed: {e}");
            }
        }
        #endregion

        #region Post-Compilation Callbacks
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (EditorPrefs.HasKey(PENDING_LEVEL_KEY))
            {
                string pendingLevel = EditorPrefs.GetString(PENDING_LEVEL_KEY);
                EditorPrefs.DeleteKey(PENDING_LEVEL_KEY);
                LevelOrderEditor.AddScene(pendingLevel);
            }

            if (EditorPrefs.HasKey(RENAME_OLD_KEY) && EditorPrefs.HasKey(RENAME_NEW_KEY))
            {
                string oldName = EditorPrefs.GetString(RENAME_OLD_KEY);
                string newName = EditorPrefs.GetString(RENAME_NEW_KEY);
                EditorPrefs.DeleteKey(RENAME_OLD_KEY);
                EditorPrefs.DeleteKey(RENAME_NEW_KEY);
                LevelOrderEditor.RenameScene(oldName, newName);
            }
        }
        #endregion

        #region Helper Classes
        private static class SceneNameEnumEditor
        {
            public static void AddEntry(string sceneName)
            {
                if (!File.Exists(SCENE_NAME_PATH))
                {
                    Debug.LogError($"SceneName.cs not found at: {SCENE_NAME_PATH}");
                    return;
                }

                string content = File.ReadAllText(SCENE_NAME_PATH);
                if (Regex.IsMatch(content, $@"\b{sceneName}\b"))
                {
                    Debug.LogWarning($"'{sceneName}' already exists in SceneName enum");
                    return;
                }

                string pattern = @"(\s+)(\w+)(\s*}\s*})";
                Match match = Regex.Match(content, pattern);

                if (match.Success)
                {
                    string indent = match.Groups[1].Value;
                    string lastEntry = match.Groups[2].Value;
                    string closing = match.Groups[3].Value;
                    string replacement = $"{indent}{lastEntry},{indent}{sceneName}{closing}";
                    content = Regex.Replace(content, pattern, replacement);
                    File.WriteAllText(SCENE_NAME_PATH, content);
                    Debug.Log($"Added '{sceneName}' to SceneName enum");
                }
                else
                {
                    Debug.LogError("Could not find proper location to insert new scene name in enum");
                }
            }

            public static void RemoveEntry(string sceneName)
            {
                if (!File.Exists(SCENE_NAME_PATH))
                {
                    Debug.LogError($"SceneName.cs not found at: {SCENE_NAME_PATH}");
                    return;
                }

                string content = File.ReadAllText(SCENE_NAME_PATH);
                string pattern1 = $@",\s*{Regex.Escape(sceneName)}\s*(?=\r?\n)";
                string pattern2 = $@"\s*{Regex.Escape(sceneName)}\s*,?\s*(?=\r?\n)";

                if (Regex.IsMatch(content, pattern1))
                    content = Regex.Replace(content, pattern1, "");
                else if (Regex.IsMatch(content, pattern2))
                    content = Regex.Replace(content, pattern2, "");
                else
                {
                    Debug.LogWarning($"Could not find '{sceneName}' in SceneName enum");
                    return;
                }

                File.WriteAllText(SCENE_NAME_PATH, content);
                Debug.Log($"Removed '{sceneName}' from SceneName enum");
            }

            public static void RenameEntry(string oldName, string newName)
            {
                if (!File.Exists(SCENE_NAME_PATH))
                {
                    Debug.LogError($"SceneName.cs not found at: {SCENE_NAME_PATH}");
                    return;
                }

                string content = File.ReadAllText(SCENE_NAME_PATH);
                string pattern = $@"\b{Regex.Escape(oldName)}\b";

                if (Regex.IsMatch(content, pattern))
                {
                    content = Regex.Replace(content, pattern, newName);
                    File.WriteAllText(SCENE_NAME_PATH, content);
                    Debug.Log($"Renamed '{oldName}' to '{newName}' in SceneName enum");
                }
                else
                {
                    Debug.LogWarning($"Could not find '{oldName}' in SceneName enum");
                }
            }
        }

        private static class LevelOrderEditor
        {
            public static void AddScene(string sceneName)
            {
                LevelOrder levelOrder = AssetDatabase.LoadAssetAtPath<LevelOrder>(LEVEL_ORDER_ASSET_PATH);
                if (levelOrder == null)
                {
                    Debug.LogError($"LevelOrder asset not found at: {LEVEL_ORDER_ASSET_PATH}");
                    return;
                }

                try
                {
                    if (System.Enum.TryParse<SceneName>(sceneName, out SceneName sceneNameEnum))
                    {
                        if (!levelOrder.Value.Contains(sceneNameEnum))
                        {
                            levelOrder.Value.Add(sceneNameEnum);
                            EditorUtility.SetDirty(levelOrder);
                            AssetDatabase.SaveAssets();
                            Debug.Log($"Added '{sceneName}' to LevelOrder asset");
                        }
                        else
                        {
                            Debug.LogWarning($"'{sceneName}' is already in LevelOrder asset");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Could not parse '{sceneName}' as SceneName enum value");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to add '{sceneName}' to LevelOrder asset: {e.Message}");
                }
            }

            public static void RemoveScene(SceneName sceneToRemove)
            {
                LevelOrder levelOrder = AssetDatabase.LoadAssetAtPath<LevelOrder>(LEVEL_ORDER_ASSET_PATH);
                if (levelOrder == null)
                {
                    Debug.LogError($"LevelOrder asset not found at: {LEVEL_ORDER_ASSET_PATH}");
                    return;
                }

                if (levelOrder.Value.Contains(sceneToRemove))
                {
                    levelOrder.Value.Remove(sceneToRemove);
                    EditorUtility.SetDirty(levelOrder);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Removed '{sceneToRemove}' from LevelOrder asset");
                }
                else
                {
                    Debug.LogWarning($"'{sceneToRemove}' not found in LevelOrder asset");
                }
            }

            public static void RenameScene(string oldName, string newName)
            {
                LevelOrder levelOrder = AssetDatabase.LoadAssetAtPath<LevelOrder>(LEVEL_ORDER_ASSET_PATH);
                if (levelOrder == null)
                {
                    Debug.LogError($"LevelOrder asset not found at: {LEVEL_ORDER_ASSET_PATH}");
                    return;
                }

                try
                {
                    if (System.Enum.TryParse<SceneName>(oldName, out SceneName oldSceneNameEnum))
                    {
                        int index = levelOrder.Value.IndexOf(oldSceneNameEnum);
                        if (index >= 0 && System.Enum.TryParse<SceneName>(newName, out SceneName newSceneNameEnum))
                        {
                            levelOrder.Value[index] = newSceneNameEnum;
                            EditorUtility.SetDirty(levelOrder);
                            AssetDatabase.SaveAssets();
                            Debug.Log($"Updated LevelOrder asset: renamed '{oldName}' to '{newName}'");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to update LevelOrder asset after rename: {e.Message}");
                }
            }
        }

        private static class BuildSettingsEditor
        {
            public static void AddScene(string scenePath)
            {
                var scenes = EditorBuildSettings.scenes.ToList();
                if (scenes.Any(s => s.path == scenePath))
                {
                    Debug.LogWarning($"Scene '{scenePath}' is already in Build Settings");
                    return;
                }

                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"Added '{scenePath}' to Build Settings");
            }

            public static void RemoveScene(string scenePath)
            {
                var scenes = EditorBuildSettings.scenes.ToList();
                var sceneToRemove = scenes.FirstOrDefault(s => s.path == scenePath);

                if (sceneToRemove != null)
                {
                    scenes.Remove(sceneToRemove);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    Debug.Log($"Removed '{scenePath}' from Build Settings");
                }
                else
                {
                    Debug.LogWarning($"Scene '{scenePath}' not found in Build Settings");
                }
            }

            public static void UpdateScenePath(string oldPath, string newPath)
            {
                var scenes = EditorBuildSettings.scenes.ToList();
                var sceneIndex = scenes.FindIndex(s => s.path == oldPath);

                if (sceneIndex >= 0)
                {
                    scenes[sceneIndex] = new EditorBuildSettingsScene(newPath, scenes[sceneIndex].enabled);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    Debug.Log($"Updated Build Settings path from '{oldPath}' to '{newPath}'");
                }
                else
                {
                    Debug.LogWarning($"Scene '{oldPath}' not found in Build Settings");
                }
            }
        }
        #endregion
    }
}
#endif
