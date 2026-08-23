using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Batch D: Alt+drag existing to clone, drag-out to rotate.
    public class DragInteractionTests : PaletteTestFixture
    {
        // -------- Alt+drag existing to clone --------

        [Test]
        public void CloneExistingAt_Prefab_Instance_Preserves_Prefab_Link()
        {
            var source = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, testScene);
            source.transform.SetParent(paintTarget.transform);
            source.transform.position = new Vector3(1, 1, 0);
            var sourceSr = source.GetComponent<SpriteRenderer>();
            sourceSr.color = new Color(0.2f, 0.4f, 0.8f, 0.9f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sourceSr);

            var clone = GameObjectPaletteWindow.CloneExistingAt(source, new Vector3(5, 6, 0));
            Assert.IsNotNull(clone);
            Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(clone),
                "clone must remain a prefab instance");
            var cloneSr = clone.GetComponent<SpriteRenderer>();
            Assert.AreEqual(sourceSr.color, cloneSr.color, "clone must carry source overrides");
            Assert.AreEqual(new Vector3(5, 6, 0), clone.transform.position);
            Assert.AreEqual(source.transform.parent, clone.transform.parent);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(clone);
        }

        [Test]
        public void CloneExistingAt_Plain_GameObject_Copies_Without_Prefab_Link()
        {
            var source = new GameObject("__PlainSource__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(source, testScene);
            source.transform.SetParent(paintTarget.transform);
            source.transform.position = new Vector3(1, 1, 0);
            source.AddComponent<SpriteRenderer>();

            var clone = GameObjectPaletteWindow.CloneExistingAt(source, new Vector3(2, 3, 0));
            Assert.IsNotNull(clone);
            Assert.IsFalse(PrefabUtility.IsAnyPrefabInstanceRoot(clone));
            Assert.AreEqual(source.name, clone.name);
            Assert.AreEqual(new Vector3(2, 3, 0), clone.transform.position);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(clone);
        }

        [Test]
        public void CloneExistingAt_Null_Source_Is_A_No_Op()
        {
            Assert.IsNull(GameObjectPaletteWindow.CloneExistingAt(null, Vector3.zero));
        }

        // -------- Drag-out RMB to rotate --------

        [Test]
        public void RotateTargetTo_Straight_Right_Preserves_Initial_Rotation()
        {
            var go = new GameObject("__RotTarget__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            go.transform.position = Vector3.zero;
            try
            {
                // Drag straight-right of the object → 0° swept angle
                GameObjectPaletteWindow.RotateTargetTo(go.transform,
                    dragStartWorld: new Vector2(1, 0), currentWorld: new Vector2(2, 0),
                    initialRotationZ: 20f);
                Assert.AreEqual(20f, go.transform.eulerAngles.z, 0.01f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RotateTargetTo_Sweeps_90_Degrees_When_Cursor_Moves_From_Right_To_Up()
        {
            var go = new GameObject("__RotTarget__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            go.transform.position = Vector3.zero;
            try
            {
                GameObjectPaletteWindow.RotateTargetTo(go.transform,
                    dragStartWorld: new Vector2(1, 0), currentWorld: new Vector2(0, 1),
                    initialRotationZ: 0f);
                // 90° CCW rotation. eulerAngles.z reports it as 90.
                Assert.AreEqual(90f, go.transform.eulerAngles.z, 0.01f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RotateTargetTo_Sweeps_Negative_When_Cursor_Moves_CW()
        {
            var go = new GameObject("__RotTarget__");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            go.transform.position = Vector3.zero;
            try
            {
                GameObjectPaletteWindow.RotateTargetTo(go.transform,
                    dragStartWorld: new Vector2(1, 0), currentWorld: new Vector2(0, -1),
                    initialRotationZ: 0f);
                // -90° reports as 270 in eulerAngles.
                Assert.AreEqual(270f, go.transform.eulerAngles.z, 0.01f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RotateTargetTo_Null_Target_Is_A_No_Op()
        {
            Assert.DoesNotThrow(() => GameObjectPaletteWindow.RotateTargetTo(null, Vector2.zero, Vector2.one, 0f));
        }

        [Test]
        public void Paint_Records_LastPaintedGameObject_For_Rotate_Target()
        {
            SelectFirst();
            brush.UpdatePosition(new Vector2(3, 3));
            brush.Paint();
            Assert.IsNotNull(PaletteCommon.lastPaintedGameObject);
            Assert.AreEqual(NewestPainted().gameObject, PaletteCommon.lastPaintedGameObject);
        }
    }
}
