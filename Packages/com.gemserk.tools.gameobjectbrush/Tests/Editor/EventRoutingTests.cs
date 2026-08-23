using NUnit.Framework;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Drives the input-classification and hotkey-handling logic with synthetic Events.
    // These are the same Event objects Unity's editor input pipeline would deliver, so
    // the tests exercise the real branching logic without needing a live SceneView.
    public class EventRoutingTests : PaletteTestFixture
    {
        static Event MakeMouse(EventType type, int button, Vector2 position = default)
        {
            return new Event { type = type, button = button, mousePosition = position };
        }

        [Test]
        public void LeftMouseButton_MouseDown_IsPaintTriggering()
        {
            Assert.IsTrue(PalettePaintTool.IsPaintTriggeringEvent(MakeMouse(EventType.MouseDown, 0)));
        }

        [Test]
        public void LeftMouseButton_MouseDrag_IsPaintTriggering()
        {
            Assert.IsTrue(PalettePaintTool.IsPaintTriggeringEvent(MakeMouse(EventType.MouseDrag, 0)));
        }

        [Test]
        public void MiddleMouseButton_IsNever_PaintTriggering()
        {
            Assert.IsFalse(PalettePaintTool.IsPaintTriggeringEvent(MakeMouse(EventType.MouseDown, 2)),
                "MMB must be reserved for camera pan");
            Assert.IsFalse(PalettePaintTool.IsPaintTriggeringEvent(MakeMouse(EventType.MouseDrag, 2)));
        }

        [Test]
        public void RightMouseButton_IsNever_PaintTriggering()
        {
            Assert.IsFalse(PalettePaintTool.IsPaintTriggeringEvent(MakeMouse(EventType.MouseDown, 1)));
            Assert.IsFalse(PalettePaintTool.IsPaintTriggeringEvent(MakeMouse(EventType.MouseDrag, 1)));
        }

        [Test]
        public void NonMouse_Events_AreNot_PaintTriggering()
        {
            Assert.IsFalse(PalettePaintTool.IsPaintTriggeringEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.A }));
            Assert.IsFalse(PalettePaintTool.IsPaintTriggeringEvent(new Event { type = EventType.ScrollWheel }));
            Assert.IsFalse(PalettePaintTool.IsPaintTriggeringEvent(new Event { type = EventType.Layout }));
            Assert.IsFalse(PalettePaintTool.IsPaintTriggeringEvent(new Event { type = EventType.MouseMove, button = 0 }));
        }

        [Test]
        public void HandlePaintHotkeys_LeftBracket_Rotates_CCW()
        {
            SelectFirst();
            PaletteCommon.paintRotationDegrees = 0f;
            var evt = new Event { type = EventType.KeyDown, keyCode = KeyCode.LeftBracket };
            Assert.IsTrue(PalettePaintTool.HandlePaintHotkeys(evt), "[ must be consumed");
            Assert.AreEqual(-15f, PaletteCommon.paintRotationDegrees);
        }

        [Test]
        public void HandlePaintHotkeys_RightBracket_Rotates_CW()
        {
            SelectFirst();
            PaletteCommon.paintRotationDegrees = 0f;
            PalettePaintTool.HandlePaintHotkeys(new Event { type = EventType.KeyDown, keyCode = KeyCode.RightBracket });
            Assert.AreEqual(15f, PaletteCommon.paintRotationDegrees);
        }

        [Test]
        public void HandlePaintHotkeys_Equals_Increases_Scale()
        {
            SelectFirst();
            PaletteCommon.paintScaleMultiplier = 1f;
            PalettePaintTool.HandlePaintHotkeys(new Event { type = EventType.KeyDown, keyCode = KeyCode.Equals });
            Assert.AreEqual(1.1f, PaletteCommon.paintScaleMultiplier, 0.001f);
        }

        [Test]
        public void HandlePaintHotkeys_UnrelatedKey_Returns_False()
        {
            SelectFirst();
            var evt = new Event { type = EventType.KeyDown, keyCode = KeyCode.Q };
            Assert.IsFalse(PalettePaintTool.HandlePaintHotkeys(evt),
                "Q is not a palette hotkey — must not consume the event");
        }

        [Test]
        public void HandlePaintHotkeys_UpdatesPreview_After_Rotate()
        {
            SelectFirst();
            PaletteCommon.paintRotationDegrees = 0f;
            PalettePaintTool.HandlePaintHotkeys(new Event { type = EventType.KeyDown, keyCode = KeyCode.RightBracket });
            var previewChild = brush.previewParent.GetChild(0);
            Assert.AreEqual(15f, previewChild.rotation.eulerAngles.z, 0.01f,
                "preview rotation should update immediately after hotkey");
        }
    }
}
