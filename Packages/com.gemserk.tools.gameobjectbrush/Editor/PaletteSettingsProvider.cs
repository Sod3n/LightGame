using UnityEditor;
using UnityEngine;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    // Adds an "Object Palette" section under Preferences (Edit > Preferences). Exposes the
    // knobs the palette window/tool care about, with per-user persistence via EditorPrefs.
    // Keeping settings here — not inside the window — makes them survive window close/reopen
    // and gives users one canonical place to tweak the tool.
    public static class PaletteSettingsProvider
    {
        internal const string DragSpacingKey = "Gemserk.ObjectPalette.DragSpacing";
        internal const string DefaultPaintUnderPathKey = "Gemserk.ObjectPalette.PaintTargetId";
        internal const string FavoritesKey = "Gemserk.ObjectPalette.Favorites";

        [InitializeOnLoadMethod]
        static void LoadOnStartup()
        {
            PaletteCommon.dragSpacing = EditorPrefs.GetFloat(DragSpacingKey, 1f);
        }

        [SettingsProvider]
        public static SettingsProvider CreatePaletteSettings()
        {
            return new SettingsProvider("Preferences/Object Palette", SettingsScope.User)
            {
                label = "Object Palette",
                keywords = new System.Collections.Generic.HashSet<string>(new[]
                {
                    "palette", "brush", "paint", "gemserk", "figma", "nudge", "snap", "drag"
                }),
                guiHandler = _ => Draw(),
            };
        }

        static void Draw()
        {
            EditorGUILayout.LabelField("Object Palette", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newSpacing = EditorGUILayout.Slider(
                new GUIContent("Drag spacing (world units)",
                    "Minimum cursor travel between paints during a paint-drag. 0 = every drag paints."),
                PaletteCommon.dragSpacing, 0f, 10f);
            if (EditorGUI.EndChangeCheck())
            {
                PaletteCommon.dragSpacing = newSpacing;
                EditorPrefs.SetFloat(DragSpacingKey, newSpacing);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Favorites: {PaletteCommon.favoriteGuids.Count}", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Favorites", GUILayout.Width(140)))
                {
                    PaletteCommon.favoriteGuids.Clear();
                    EditorPrefs.SetString(FavoritesKey, "");
                }
                if (GUILayout.Button("Clear Paint Target", GUILayout.Width(160)))
                {
                    PaletteCommon.paintTarget = null;
                    EditorPrefs.DeleteKey(DefaultPaintUnderPathKey);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Hotkeys are rebindable under Edit > Shortcuts > 'Object Palette/*'.",
                MessageType.Info);
        }
    }
}
