using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Batch C: Drag-to-paint spacing (ShouldPaintAt predicate) + Undo stroke grouping.
    public class DragSpacingTests
    {
        [Test]
        public void MouseDown_Always_Paints_Regardless_Of_Spacing()
        {
            Assert.IsTrue(PalettePaintTool.ShouldPaintAt(EventType.MouseDown, Vector2.zero, null, 1f));
            Assert.IsTrue(PalettePaintTool.ShouldPaintAt(EventType.MouseDown, Vector2.zero, Vector2.zero, 1f));
        }

        [Test]
        public void MouseDrag_First_Time_Paints_Even_With_Spacing()
        {
            Assert.IsTrue(PalettePaintTool.ShouldPaintAt(EventType.MouseDrag, Vector2.zero, null, 1f),
                "no lastPaintedWorld → treat as first drag, paint");
        }

        [Test]
        public void MouseDrag_Below_Spacing_Threshold_Does_Not_Paint()
        {
            var last = new Vector2(0f, 0f);
            var current = new Vector2(0.5f, 0f);
            Assert.IsFalse(PalettePaintTool.ShouldPaintAt(EventType.MouseDrag, current, last, 1f));
        }

        [Test]
        public void MouseDrag_At_Or_Beyond_Spacing_Paints()
        {
            var last = new Vector2(0f, 0f);
            Assert.IsTrue(PalettePaintTool.ShouldPaintAt(EventType.MouseDrag, new Vector2(1f, 0f), last, 1f));
            Assert.IsTrue(PalettePaintTool.ShouldPaintAt(EventType.MouseDrag, new Vector2(2f, 0f), last, 1f));
            Assert.IsTrue(PalettePaintTool.ShouldPaintAt(EventType.MouseDrag, new Vector2(0f, 1.5f), last, 1f));
        }

        [Test]
        public void Spacing_Zero_Means_Every_Drag_Paints()
        {
            Assert.IsTrue(PalettePaintTool.ShouldPaintAt(EventType.MouseDrag, new Vector2(0.001f, 0f), Vector2.zero, 0f),
                "spacing=0 disables gating");
        }

        [Test]
        public void NonMouse_Events_Do_Not_Paint_Regardless()
        {
            Assert.IsFalse(PalettePaintTool.ShouldPaintAt(EventType.KeyDown, Vector2.zero, null, 1f));
            Assert.IsFalse(PalettePaintTool.ShouldPaintAt(EventType.MouseMove, Vector2.zero, null, 1f));
            Assert.IsFalse(PalettePaintTool.ShouldPaintAt(EventType.Layout, Vector2.zero, null, 1f));
            Assert.IsFalse(PalettePaintTool.ShouldPaintAt(EventType.ScrollWheel, Vector2.zero, null, 1f));
        }
    }
}
