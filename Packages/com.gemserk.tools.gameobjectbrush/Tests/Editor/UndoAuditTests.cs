using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Audit that every mutating action registers a proper undo entry and Ctrl+Z restores state.
    public class UndoAuditTests : PaletteTestFixture
    {
        [Test]
        public void Paint_Then_Undo_Removes_The_Instance()
        {
            SelectFirst();
            brush.UpdatePosition(new Vector2(0, 0));
            brush.Paint();
            Assert.AreEqual(1, PaintedCount);
            Undo.PerformUndo();
            Assert.AreEqual(0, PaintedCount);
        }

        [Test]
        public void Nudge_Then_Undo_Restores_Position()
        {
            var go = new GameObject("__Nudge__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            try
            {
                var start = go.transform.position;
                Selection.activeGameObject = go;
                PaletteShortcuts.Nudge(new Vector3(3, 4, 0));
                Assert.AreEqual(start + new Vector3(3, 4, 0), go.transform.position);
                Undo.PerformUndo();
                Assert.AreEqual(start, go.transform.position);
            }
            finally { Object.DestroyImmediate(go); Selection.activeGameObject = null; }
        }

        [Test]
        public void RotateTargetTo_Then_Undo_Restores_Rotation()
        {
            var go = new GameObject("__Rot__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            go.transform.position = Vector3.zero;
            try
            {
                var startRot = go.transform.rotation;
                GameObjectPaletteWindow.RotateTargetTo(go.transform, new Vector2(1, 0), new Vector2(0, 1), 0f);
                Assert.AreNotEqual(startRot, go.transform.rotation);
                Undo.PerformUndo();
                Assert.AreEqual(startRot, go.transform.rotation);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CloneExistingAt_Then_Undo_Removes_Clone()
        {
            var source = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, testScene);
            source.transform.SetParent(paintTarget.transform);
            try
            {
                var beforeChildren = paintTarget.transform.childCount;
                var clone = GameObjectPaletteWindow.CloneExistingAt(source, new Vector3(2, 2, 0));
                Assert.IsNotNull(clone);
                Assert.AreEqual(beforeChildren + 1, paintTarget.transform.childCount);
                Undo.PerformUndo();
                Assert.AreEqual(beforeChildren, paintTarget.transform.childCount, "clone should be undone");
            }
            finally { Object.DestroyImmediate(source); }
        }

        [Test]
        public void CopyProperties_Then_Undo_Restores_Original_Values()
        {
            var src = new GameObject("__src__"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(src, testScene);
            var dst = new GameObject("__dst__"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(dst, testScene);
            var srcSr = src.AddComponent<SpriteRenderer>(); srcSr.color = Color.red;
            var dstSr = dst.AddComponent<SpriteRenderer>(); dstSr.color = Color.white;
            try
            {
                Selection.objects = new Object[] { src, dst };
                Selection.activeGameObject = src;
                PaletteShortcuts.ApplyPropertiesToSelection();
                Assert.AreEqual(Color.red, dstSr.color);
                Undo.PerformUndo();
                Assert.AreEqual(Color.white, dstSr.color);
            }
            finally { Object.DestroyImmediate(src); Object.DestroyImmediate(dst); Selection.objects = null; }
        }
    }
}
