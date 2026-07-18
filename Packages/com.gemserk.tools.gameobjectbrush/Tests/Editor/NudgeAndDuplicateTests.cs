using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Batch A: Arrow-key nudge + Alt+click duplicate-last + V select-mode.
    public class NudgeAndDuplicateTests : PaletteTestFixture
    {
        [Test]
        public void Nudge_Moves_Selected_Scene_Transforms_By_Delta()
        {
            var go = new GameObject("__Nudge__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            try
            {
                var start = go.transform.position;
                Selection.activeGameObject = go;
                var moved = PaletteShortcuts.Nudge(new Vector3(1, 0, 0));
                Assert.AreEqual(1, moved);
                Assert.AreEqual(start + new Vector3(1, 0, 0), go.transform.position);
            }
            finally { Object.DestroyImmediate(go); Selection.activeGameObject = null; }
        }

        [Test]
        public void Nudge_Ignores_Project_Assets()
        {
            Selection.activeObject = tilePrefab;   // project asset, not a scene object
            var moved = PaletteShortcuts.Nudge(new Vector3(1, 0, 0));
            Assert.AreEqual(0, moved, "asset transforms must not be nudged");
            Selection.activeObject = null;
        }

        [Test]
        public void Nudge_Requires_Palette_Window_Visible()
        {
            var go = new GameObject("__Nudge__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            try
            {
                Selection.activeGameObject = go;
                GameObjectPaletteWindow.windowVisible = false;
                var moved = PaletteShortcuts.Nudge(new Vector3(1, 0, 0));
                Assert.AreEqual(0, moved, "nudge is guarded by palette window visibility");
                GameObjectPaletteWindow.windowVisible = true; // restore for other tests / teardown
            }
            finally { Object.DestroyImmediate(go); Selection.activeGameObject = null; }
        }

        [Test]
        public void Nudge_With_No_Selection_Is_A_No_Op()
        {
            Selection.activeObject = null;
            var moved = PaletteShortcuts.Nudge(new Vector3(1, 0, 0));
            Assert.AreEqual(0, moved);
        }

        [Test]
        public void Paint_Remembers_LastPaintedEntry()
        {
            SelectFirst();
            brush.UpdatePosition(new Vector2(0, 0));
            brush.Paint();
            Assert.AreEqual(firstEntry, PaletteCommon.lastPaintedEntry);
        }

        [Test]
        public void DuplicateLastPaintedAt_Paints_Under_PaintTarget_At_Cursor()
        {
            // Setup: paint once so lastPaintedEntry is populated.
            SelectFirst();
            brush.UpdatePosition(new Vector2(1, 1));
            brush.Paint();
            var beforeCount = PaintedCount;
            PaletteCommon.selection.Clear();
            brush.DestroyPreview();

            GameObjectPaletteWindow.DuplicateLastPaintedAt(new Vector3(42f, -17f, 0f));

            Assert.AreEqual(beforeCount + 1, PaintedCount, "one new painted object expected");
            var painted = NewestPainted();
            Assert.AreEqual(42f, painted.position.x, 0.001f);
            Assert.AreEqual(-17f, painted.position.y, 0.001f);
            Assert.AreEqual(paintTarget.transform, painted.parent);
            Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(painted.gameObject));
        }

        [Test]
        public void DuplicateLastPaintedAt_Preserves_Existing_Selection()
        {
            // Populate lastPainted with firstEntry
            SelectFirst();
            brush.UpdatePosition(new Vector2(0, 0));
            brush.Paint();
            brush.DestroyPreview();

            // Now select secondEntry; DuplicateLastPaintedAt should paint firstEntry
            // AND restore secondEntry as the active selection afterwards.
            PaletteCommon.selection.Clear();
            PaletteCommon.selection.Add(secondEntry);
            brush.CreatePreview(PaletteCommon.selection.selection);

            GameObjectPaletteWindow.DuplicateLastPaintedAt(new Vector3(5, 5, 0));

            Assert.AreEqual(1, PaletteCommon.selection.selection.Count);
            Assert.AreEqual(secondEntry, PaletteCommon.selection.selection[0], "original selection restored");
            Assert.IsNotNull(brush.previewParent, "preview restored");
        }

        [Test]
        public void DuplicateLastPaintedAt_With_Null_LastPainted_Is_A_No_Op()
        {
            PaletteCommon.lastPaintedEntry = null;
            var before = PaintedCount;
            GameObjectPaletteWindow.DuplicateLastPaintedAt(Vector3.zero);
            Assert.AreEqual(before, PaintedCount);
        }
    }
}
