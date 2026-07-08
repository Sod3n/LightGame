#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Provides a global editor shortcut to toggle visibility of editor-only sprites.
/// Default shortcut: Ctrl+Shift+E (Cmd+Shift+E on Mac)
/// </summary>
public static class EditorSpriteToggle
{
    private const string PREF_KEY = "EditorSpriteToggle_Enabled";
    private const string MENU_PATH = "Tools/Toggle Editor Sprites";

    private static bool isEnabled
    {
        get => EditorPrefs.GetBool(PREF_KEY, true);
        set => EditorPrefs.SetBool(PREF_KEY, value);
    }

    [MenuItem(MENU_PATH + " %#e")] // Ctrl+Shift+E (Cmd+Shift+E on Mac)
    private static void ToggleEditorSprites()
    {
        isEnabled = !isEnabled;

        // Force all SimpleEditModeSprite components to update
        var simpleSprites = Object.FindObjectsOfType<SimpleEditModeSprite>();
        foreach (var sprite in simpleSprites)
        {
            // Trigger OnValidate by disabling and re-enabling
            sprite.enabled = false;
            sprite.enabled = true;
        }

        // Force all EditModeSpriteRenderer components to update
        var editModeSprites = Object.FindObjectsOfType<EditModeSpriteRenderer>();
        foreach (var sprite in editModeSprites)
        {
            // Trigger OnValidate by disabling and re-enabling
            sprite.enabled = false;
            sprite.enabled = true;
        }

        // Refresh the scene view
        SceneView.RepaintAll();

        Debug.Log($"Editor Sprites: {(isEnabled ? "ENABLED" : "DISABLED")}");
    }

    [MenuItem(MENU_PATH, true)]
    private static bool ToggleEditorSpritesValidate()
    {
        Menu.SetChecked(MENU_PATH, isEnabled);
        return true;
    }

    /// <summary>
    /// Check if editor sprites should be rendered
    /// </summary>
    public static bool ShouldRenderEditorSprites()
    {
        return isEnabled;
    }
}
#endif