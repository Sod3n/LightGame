using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    public class PaintFlowTests : PaletteTestFixture
    {
        [Test]
        public void Selecting_A_Palette_Entry_Creates_Preview_Under_BrushPreview_Holder()
        {
            SelectFirst();
            Assert.IsNotNull(brush.previewParent, "preview holder should exist after selection");
            Assert.IsTrue(brush.previewParent.gameObject.name.Contains("BrushPreview"));
            Assert.GreaterOrEqual(brush.previewParent.childCount, 1, "at least one preview child");
        }

        [Test]
        public void Single_Paint_Produces_One_Instance_By_Default()
        {
            SelectFirst();
            brush.UpdatePosition(new Vector2(1f, 1f));
            var before = PaintedCount;
            brush.Paint();
            Assert.AreEqual(before + brush.previewParent.childCount, PaintedCount);
            // With the DefaultMultiply set to max=1, the modifier is a no-op — 1 preview → 1 paint.
            Assert.AreEqual(before + 1, PaintedCount, "expected exactly one paint per click");
        }

        [Test]
        public void Painted_Object_Is_Under_PaintTarget_Not_Under_Selection()
        {
            SelectFirst();
            // Deliberately point Unity Selection at the preview instance (mirrors what
            // GameObjectPaletteWindow.SelectBrushObject does so users can inspect overrides).
            Selection.activeGameObject = brush.previewParent.GetChild(0).gameObject;
            brush.UpdatePosition(new Vector2(5f, 5f));
            brush.Paint();
            var painted = NewestPainted();
            Assert.IsNotNull(painted);
            Assert.AreEqual(paintTarget.transform, painted.parent,
                "painted object must be under PaintTarget, never under Selection (which is the preview)");
        }

        [Test]
        public void Painted_Object_Keeps_Prefab_Instance_Connection()
        {
            SelectFirst();
            brush.UpdatePosition(new Vector2(10f, 10f));
            brush.Paint();
            var painted = NewestPainted();
            Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(painted.gameObject),
                "painted object must remain a prefab instance");
            Assert.AreEqual(
                AssetDatabase.GetAssetPath(tilePrefab),
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(painted.gameObject));
        }

        [Test]
        public void Paint_World_Position_Matches_Brush_Cursor()
        {
            SelectFirst();
            var cursor = new Vector2(42.5f, -17.25f);
            brush.UpdatePosition(cursor);
            brush.Paint();
            var painted = NewestPainted();
            Assert.AreEqual(cursor.x, painted.position.x, 0.001f);
            Assert.AreEqual(cursor.y, painted.position.y, 0.001f);
        }

        [Test]
        public void Paint_Applies_Session_Rotation_Offset()
        {
            SelectFirst();
            PaletteCommon.paintRotationDegrees = 45f;
            (brush as ScriptableBrushBaseAsset).ApplyPreviewTransform();
            brush.UpdatePosition(new Vector2(0f, 0f));
            brush.Paint();
            var painted = NewestPainted();
            Assert.AreEqual(45f, painted.rotation.eulerAngles.z, 0.01f);
        }

        [Test]
        public void Paint_Multiplies_Session_Scale_Over_Prefab_Local_Scale()
        {
            SelectFirst();
            var prefabLocalScale = tilePrefab.transform.localScale;
            PaletteCommon.paintScaleMultiplier = 2f;
            (brush as ScriptableBrushBaseAsset).ApplyPreviewTransform();
            brush.UpdatePosition(new Vector2(0f, 0f));
            brush.Paint();
            var painted = NewestPainted();
            Assert.AreEqual(prefabLocalScale.x * 2f, painted.localScale.x, 0.001f);
            Assert.AreEqual(prefabLocalScale.y * 2f, painted.localScale.y, 0.001f);
        }
    }
}
