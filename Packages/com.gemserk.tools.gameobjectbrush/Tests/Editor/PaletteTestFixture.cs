using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Shared setup: fresh empty scene, a paint target GameObject, a synthetic in-memory palette
    // with a couple of prefabs cloned from real project prefabs, and the default brush wired up.
    // Every test starts from this known state and tears down anything it created.
    public abstract class PaletteTestFixture
    {
        protected Scene testScene;
        protected GameObject paintTarget;
        protected ObjectPaletteAsset palette;
        protected ScriptableBrushBaseAsset brush;
        protected GameObject tilePrefab;
        protected GameObject secondPrefab;
        protected PaletteObject firstEntry;
        protected PaletteObject secondEntry;

        private readonly List<Object> tempAssets = new List<Object>();
        private readonly List<GameObject> tempScenegameObjects = new List<GameObject>();

        [SetUp]
        public void BaseSetUp()
        {
            // NewPreviewScene is in-memory, doesn't participate in the editor hierarchy,
            // and doesn't demand that already-open scenes be saved first — perfect for tests.
            testScene = EditorSceneManager.NewPreviewScene();

            paintTarget = new GameObject("__PaintTarget__");
            SceneManager.MoveGameObjectToScene(paintTarget, testScene);
            tempScenegameObjects.Add(paintTarget);

            tilePrefab = LoadFirstProjectPrefab("t:Prefab", "Assets/_Game/Features/Environment/Prefabs/Tiles");
            secondPrefab = LoadFirstProjectPrefab("t:Prefab", "Assets/_Game/Features/Environment/Prefabs/Tiles", tilePrefab);
            Assert.IsNotNull(tilePrefab, "test needs at least one prefab under Tiles/");
            Assert.IsNotNull(secondPrefab, "test needs a second prefab under Tiles/");

            palette = ScriptableObject.CreateInstance<ObjectPaletteAsset>();
            palette.prefabs = new List<GameObject> { tilePrefab, secondPrefab };
            tempAssets.Add(palette);

            var brushInstance = ScriptableObject.CreateInstance<ScriptableDefaultBrushAsset>();
            brush = brushInstance;
            tempAssets.Add(brush);

            var entries = palette.CreatePaletteObjects();
            firstEntry = entries[0];
            secondEntry = entries[1];

            PaletteCommon.brush = brush;
            PaletteCommon.paintTarget = paintTarget.transform;
            PaletteCommon.paintRotationDegrees = 0f;
            PaletteCommon.paintScaleMultiplier = 1f;
            PaletteCommon.selection.Clear();
            PaletteCommon.mode = PaletteToolMode.Paint;

            // Shortcuts guard on `GameObjectPaletteWindow.windowVisible`. In real usage the
            // window sets it via OnBecameVisible; in tests we drive it directly.
            previousWindowVisible = GameObjectPaletteWindow.windowVisible;
            GameObjectPaletteWindow.windowVisible = true;
        }

        private bool previousWindowVisible;

        [TearDown]
        public void BaseTearDown()
        {
            brush?.DestroyPreview();
            foreach (var go in tempScenegameObjects)
                if (go != null) Object.DestroyImmediate(go);
            tempScenegameObjects.Clear();
            foreach (var a in tempAssets)
                if (a != null) Object.DestroyImmediate(a);
            tempAssets.Clear();

            PaletteCommon.brush = null;
            PaletteCommon.paintTarget = null;
            PaletteCommon.selection.Clear();
            PaletteCommon.paintRotationDegrees = 0f;
            PaletteCommon.paintScaleMultiplier = 1f;
            PaletteCommon.mode = PaletteToolMode.Paint;

            if (testScene.IsValid())
                EditorSceneManager.ClosePreviewScene(testScene);

            GameObjectPaletteWindow.windowVisible = previousWindowVisible;
        }

        protected void SelectFirst()
        {
            PaletteCommon.selection.Clear();
            PaletteCommon.selection.Add(firstEntry);
            brush.CreatePreview(PaletteCommon.selection.selection);
        }

        protected int PaintedCount => paintTarget.transform.childCount;

        protected Transform NewestPainted()
        {
            return paintTarget.transform.childCount == 0
                ? null
                : paintTarget.transform.GetChild(paintTarget.transform.childCount - 1);
        }

        static GameObject LoadFirstProjectPrefab(string filter, string folder, GameObject exclude = null)
        {
            var guids = AssetDatabase.FindAssets(filter, new[] { folder });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go != exclude) return go;
            }
            return null;
        }
    }
}
