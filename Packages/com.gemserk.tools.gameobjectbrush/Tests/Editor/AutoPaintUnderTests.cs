using NUnit.Framework;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Auto-set Paint Under when the user mouse-clicks a scene object in Hierarchy.
    // Programmatic Selection changes (our own SelectBrushObject → preview) are guarded
    // out by mouseOverWindow being the palette or something else — modeled here as the
    // sourceWindowTypeName parameter passed to ShouldSetPaintUnderFromSelection.
    public class AutoPaintUnderTests : PaletteTestFixture
    {
        [Test]
        public void Accepts_HierarchyWindow_As_Trigger_Source()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            PaletteCommon.paintTarget = null;
            GameObjectPaletteWindow.autoSetPaintUnderFromHierarchyClick = true;
            try
            {
                Assert.IsTrue(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, "SceneHierarchyWindow"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Accepts_SceneView_As_Trigger_Source()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            try
            {
                Assert.IsTrue(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, "SceneView"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Rejects_Programmatic_Selection_When_MouseOver_Is_Palette_Window()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            try
            {
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, "GameObjectPaletteWindow"),
                    "clicks in palette window must not repurpose paint-under");
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, "InspectorWindow"));
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, ""));
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, null));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Rejects_When_Feature_Toggled_Off()
        {
            var go = new GameObject("__T__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            GameObjectPaletteWindow.autoSetPaintUnderFromHierarchyClick = false;
            try
            {
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, "SceneHierarchyWindow"));
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
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, "SceneHierarchyWindow"));
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
            Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(null, "SceneHierarchyWindow"));
        }

        [Test]
        public void Rejects_BrushPreview_Descendants()
        {
            // Simulate an object inside the ~BrushPreview holder — those must never
            // become the paint target because they're transient tool state.
            var holder = new GameObject("~BrushPreview");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, testScene);
            holder.AddComponent<BrushPreview>();
            var child = new GameObject("preview_child");
            child.transform.SetParent(holder.transform);
            try
            {
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    child.transform, "SceneHierarchyWindow"),
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
                Assert.IsFalse(GameObjectPaletteWindow.ShouldSetPaintUnderFromSelection(
                    go.transform, "SceneHierarchyWindow"),
                    "no-op when already the paint target — avoid pointless save/repaint");
            }
            finally { Object.DestroyImmediate(go); PaletteCommon.paintTarget = null; }
        }
    }
}
