using NUnit.Framework;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Auto-set Paint Under when the user clicks a scene object anywhere (Hierarchy or
    // Scene View). Programmatic Selection changes coming from our own SelectBrushObject
    // are guarded out via a suppression counter (see BeginSuppressSelectionHook).
    public class AutoPaintUnderTests : PaletteTestFixture
    {
        [Test]
        public void Accepts_User_Selection_Of_Scene_Object()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            PaletteCommon.paintTarget = null;
            GameObjectPaletteWindow.autoSetPaintUnderFromHierarchyClick = true;
            try
            {
                Assert.IsTrue(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(go.transform));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Rejects_During_Programmatic_Suppression_Window()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            try
            {
                GameObjectPaletteWindow.BeginSuppressSelectionHook();
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(go.transform),
                    "must not retarget while our own code is programmatically changing Selection");
            }
            finally
            {
                Object.DestroyImmediate(go);
                // Force-reset counter so other tests aren't affected
                GameObjectPaletteWindow.suppressSelectionHookCount = 0;
            }
        }

        [Test]
        public void Rejects_When_Feature_Toggled_Off()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            GameObjectPaletteWindow.autoSetPaintUnderFromHierarchyClick = false;
            try
            {
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(go.transform));
            }
            finally
            {
                Object.DestroyImmediate(go);
                GameObjectPaletteWindow.autoSetPaintUnderFromHierarchyClick = true;
            }
        }

        [Test]
        public void Rejects_When_Palette_Window_Not_Visible()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            var prev = GameObjectPaletteWindow.windowVisible;
            GameObjectPaletteWindow.windowVisible = false;
            try
            {
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(go.transform));
            }
            finally
            {
                Object.DestroyImmediate(go);
                GameObjectPaletteWindow.windowVisible = prev;
            }
        }

        [Test]
        public void Rejects_Null_Selection()
        {
            Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(null));
        }

        [Test]
        public void Rejects_BrushPreview_Descendants()
        {
            var holder = new GameObject("~BrushPreview");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, testScene);
            holder.AddComponent<BrushPreview>();
            var child = new GameObject("preview_child");
            child.transform.SetParent(holder.transform);
            try
            {
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(child.transform),
                    "preview holder descendants must not become paint target");
            }
            finally { Object.DestroyImmediate(holder); }
        }

        [Test]
        public void Rejects_When_Selection_Equals_Current_PaintTarget()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            PaletteCommon.paintTarget = go.transform;
            try
            {
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(go.transform),
                    "no-op when already the paint target — avoid pointless save/repaint");
            }
            finally { Object.DestroyImmediate(go); PaletteCommon.paintTarget = null; }
        }
    }
}
