using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Verifies that a paint stroke collapses into a single undo entry: one Ctrl+Z reverts
    // every object painted during the stroke. Uses the same Undo API the tool uses.
    public class UndoStrokeGroupingTests : PaletteTestFixture
    {
        [Test]
        public void Multiple_Painted_Objects_In_One_Group_Collapse_Into_Single_Undo()
        {
            SelectFirst();

            Undo.IncrementCurrentGroup();
            var groupStart = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Paint Stroke");

            // Simulate a paint sweep: 5 paints all in the same undo group.
            for (int i = 0; i < 5; i++)
            {
                brush.UpdatePosition(new Vector2(i * 2f, 0f));
                brush.Paint();
            }
            Undo.CollapseUndoOperations(groupStart);

            Assert.AreEqual(5, PaintedCount, "5 paints landed");

            // A single undo should revert the whole stroke.
            Undo.PerformUndo();
            Assert.AreEqual(0, PaintedCount, "one Ctrl+Z must revert the whole stroke");
        }

        [Test]
        public void Two_Separate_Strokes_Are_Two_Undo_Steps()
        {
            SelectFirst();

            // stroke 1: 3 paints
            Undo.IncrementCurrentGroup();
            var g1 = Undo.GetCurrentGroup();
            for (int i = 0; i < 3; i++) { brush.UpdatePosition(new Vector2(i, 0)); brush.Paint(); }
            Undo.CollapseUndoOperations(g1);

            // stroke 2: 2 paints
            Undo.IncrementCurrentGroup();
            var g2 = Undo.GetCurrentGroup();
            for (int i = 0; i < 2; i++) { brush.UpdatePosition(new Vector2(10 + i, 0)); brush.Paint(); }
            Undo.CollapseUndoOperations(g2);

            Assert.AreEqual(5, PaintedCount);

            Undo.PerformUndo();
            Assert.AreEqual(3, PaintedCount, "one undo should remove the second stroke (2 objects)");

            Undo.PerformUndo();
            Assert.AreEqual(0, PaintedCount, "second undo removes the first stroke (3 objects)");
        }
    }
}
