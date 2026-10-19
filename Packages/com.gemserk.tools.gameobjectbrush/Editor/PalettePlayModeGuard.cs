using UnityEditor;

namespace Gemserk.Tools.ObjectPalette.Editor
{
    // The brush preview holds live clones of the selected prefab. Left in place when Play Mode
    // starts, they wake up as real level objects, so a palette selection showed up in the game
    // without being painted. Drop the preview before entering Play Mode and bring it back after.
    [InitializeOnLoad]
    static class PalettePlayModeGuard
    {
        static PalettePlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                PaletteCommon.brush?.DestroyPreview();
            }
            else if (change == PlayModeStateChange.EnteredEditMode
                     && PaletteCommon.brush != null
                     && PaletteCommon.mode == PaletteToolMode.Paint
                     && !PaletteCommon.selection.IsEmpty)
            {
                PaletteCommon.brush.CreatePreview(PaletteCommon.selection.selection);
            }
        }
    }
}
