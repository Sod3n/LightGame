using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // The Multiply modifier previously created extras via a fresh PrefabUtility.InstantiatePrefab
    // that lost any overrides on the source preview. Guarding against that regression.
    public class MultiplyModifierTests : PaletteTestFixture
    {
        [Test]
        public void Multiply_With_Max_1_Is_A_No_Op()
        {
            var mult = ScriptableObject.CreateInstance<MultiplyModifier>();
            mult.min = 1;
            mult.max = 1;
            SelectFirst();
            var before = brush.previewParent.childCount;
            mult.ApplyModifier(brush as ScriptableBrushBaseAsset);
            Assert.AreEqual(before, brush.previewParent.childCount,
                "max<=1 must skip duplication so single click = single paint");
            Object.DestroyImmediate(mult);
        }

        [Test]
        public void Multiply_Extras_Inherit_Preview_Color_Override()
        {
            SelectFirst();
            var previewSr = brush.previewParent.GetChild(0).GetComponent<SpriteRenderer>();
            var expected = new Color(0f, 1f, 0f, 0.5f);
            previewSr.color = expected;
            PrefabUtility.RecordPrefabInstancePropertyModifications(previewSr);

            var mult = ScriptableObject.CreateInstance<MultiplyModifier>();
            mult.min = 2;
            mult.max = 3;   // Random.Range(int,int) is exclusive of max → guaranteed 2
            mult.ApplyModifier(brush as ScriptableBrushBaseAsset);
            Object.DestroyImmediate(mult);

            // Original preview + 2 extras = 3 children
            Assert.AreEqual(3, brush.previewParent.childCount);
            for (int i = 0; i < brush.previewParent.childCount; i++)
            {
                var sr = brush.previewParent.GetChild(i).GetComponent<SpriteRenderer>();
                Assert.AreEqual(expected, sr.color,
                    $"preview[{i}] must share the source preview's color override");
            }
        }
    }
}
