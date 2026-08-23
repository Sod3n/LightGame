using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Simulates the "click palette, edit in Inspector, then paint" flow. The Inspector edit
    // is faked with a direct property set + PrefabUtility.RecordPrefabInstancePropertyModifications,
    // which is exactly what Unity does when the user Inspector-edits a prefab instance.
    public class OverrideCarryTests : PaletteTestFixture
    {
        [Test]
        public void Color_Override_On_Preview_Carries_To_Painted_Instance()
        {
            SelectFirst();
            var previewInst = brush.previewParent.GetChild(0).gameObject;
            var previewSr = previewInst.GetComponent<SpriteRenderer>();
            Assume.That(previewSr, Is.Not.Null, "test tile prefab must have a SpriteRenderer");

            var expected = new Color(0.9f, 0.1f, 0.2f, 0.6f);
            previewSr.color = expected;
            PrefabUtility.RecordPrefabInstancePropertyModifications(previewSr);

            brush.UpdatePosition(new Vector2(0f, 0f));
            brush.Paint();

            var painted = NewestPainted();
            var paintedSr = painted.GetComponent<SpriteRenderer>();
            Assert.AreEqual(expected, paintedSr.color, "painted color must match preview override");
        }

        [Test]
        public void Sprite_Override_On_Preview_Carries_To_Painted_Instance()
        {
            SelectFirst();
            var previewInst = brush.previewParent.GetChild(0).gameObject;
            var previewSr = previewInst.GetComponent<SpriteRenderer>();
            var otherSprite = secondPrefab.GetComponent<SpriteRenderer>().sprite;
            Assume.That(otherSprite, Is.Not.Null);
            Assume.That(previewSr.sprite, Is.Not.EqualTo(otherSprite));

            previewSr.sprite = otherSprite;
            PrefabUtility.RecordPrefabInstancePropertyModifications(previewSr);

            brush.UpdatePosition(new Vector2(0f, 0f));
            brush.Paint();

            var painted = NewestPainted();
            var paintedSr = painted.GetComponent<SpriteRenderer>();
            Assert.AreEqual(otherSprite, paintedSr.sprite, "painted sprite must match preview override");
        }

        [Test]
        public void Painted_Instance_After_Override_Is_Still_A_Prefab_Instance()
        {
            SelectFirst();
            var previewSr = brush.previewParent.GetChild(0).GetComponent<SpriteRenderer>();
            previewSr.color = Color.magenta;
            PrefabUtility.RecordPrefabInstancePropertyModifications(previewSr);
            brush.UpdatePosition(new Vector2(0f, 0f));
            brush.Paint();
            var painted = NewestPainted();
            Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(painted.gameObject),
                "painted should keep prefab link even after override was applied");
        }

        [Test]
        public void Repeated_Paints_Consistently_Apply_The_Same_Override()
        {
            SelectFirst();
            var previewSr = brush.previewParent.GetChild(0).GetComponent<SpriteRenderer>();
            var expected = new Color(0f, 0.5f, 1f, 1f);
            previewSr.color = expected;
            PrefabUtility.RecordPrefabInstancePropertyModifications(previewSr);
            for (int i = 0; i < 3; i++)
            {
                brush.UpdatePosition(new Vector2(i, 0f));
                brush.Paint();
            }
            Assert.AreEqual(3, PaintedCount);
            for (int i = 0; i < 3; i++)
            {
                var sr = paintTarget.transform.GetChild(i).GetComponent<SpriteRenderer>();
                Assert.AreEqual(expected, sr.color, $"paint #{i} must carry override");
            }
        }
    }
}
