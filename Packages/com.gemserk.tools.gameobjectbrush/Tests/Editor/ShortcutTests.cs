using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Invokes the [Shortcut] handlers the same way Unity would: as static methods with
    // no arguments. Verifies both the action AND the Guard() gate.
    public class ShortcutTests : PaletteTestFixture
    {
        static MethodInfo GetShortcut(string methodName)
        {
            var type = typeof(PaletteShortcuts);
            return type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }

        static void Invoke(string methodName) => GetShortcut(methodName).Invoke(null, null);

        [Test]
        public void Every_Documented_Shortcut_Is_Registered_With_Manager()
        {
            var ids = ShortcutManager.instance.GetAvailableShortcutIds()
                .Where(id => id.StartsWith("Object Palette/")).ToList();
            Assert.Contains("Object Palette/Rotate CCW 15°", ids);
            Assert.Contains("Object Palette/Rotate CW 15°", ids);
            Assert.Contains("Object Palette/Scale Down 10%", ids);
            Assert.Contains("Object Palette/Scale Up 10%", ids);
            Assert.Contains("Object Palette/Reset Rotation & Scale", ids);
            Assert.Contains("Object Palette/Toggle Erase Mode", ids);
        }

        [Test]
        public void Shortcut_Ignored_When_Guard_Fails_No_Selection()
        {
            // no SelectFirst → selection.IsEmpty, Guard returns false
            PaletteCommon.paintRotationDegrees = 0f;
            Invoke("RotateCW");
            Assert.AreEqual(0f, PaletteCommon.paintRotationDegrees,
                "rotation must not change when no palette entry is selected");
        }

        [Test]
        public void RotateCW_Adds_15_Degrees_When_Guarded_Active()
        {
            SelectFirst();
            Invoke("RotateCW");
            Assert.AreEqual(15f, PaletteCommon.paintRotationDegrees);
            Invoke("RotateCW");
            Assert.AreEqual(30f, PaletteCommon.paintRotationDegrees);
        }

        [Test]
        public void RotateCCW_Subtracts_15_Degrees()
        {
            SelectFirst();
            Invoke("RotateCCW");
            Assert.AreEqual(-15f, PaletteCommon.paintRotationDegrees);
        }

        [Test]
        public void Reset_Zeros_Rotation_And_Restores_Scale()
        {
            SelectFirst();
            PaletteCommon.paintRotationDegrees = 90f;
            PaletteCommon.paintScaleMultiplier = 3f;
            Invoke("Reset");
            Assert.AreEqual(0f, PaletteCommon.paintRotationDegrees);
            Assert.AreEqual(1f, PaletteCommon.paintScaleMultiplier);
        }

        [Test]
        public void ScaleUp_And_ScaleDown_Clamp_To_Bounds()
        {
            SelectFirst();
            for (int i = 0; i < 200; i++) Invoke("ScaleUp");
            Assert.LessOrEqual(PaletteCommon.paintScaleMultiplier, 10f);
            for (int i = 0; i < 400; i++) Invoke("ScaleDown");
            Assert.GreaterOrEqual(PaletteCommon.paintScaleMultiplier, 0.1f);
        }

        [Test]
        public void ToggleErase_Switches_Between_Modes()
        {
            SelectFirst();
            Assert.AreEqual(PaletteToolMode.Paint, PaletteCommon.mode);
            Invoke("ToggleErase");
            Assert.AreEqual(PaletteToolMode.Erase, PaletteCommon.mode);
            Invoke("ToggleErase");
            Assert.AreEqual(PaletteToolMode.Paint, PaletteCommon.mode);
        }

        [Test]
        public void Rotation_Shortcut_Updates_Preview_World_Rotation_Immediately()
        {
            SelectFirst();
            Invoke("RotateCW");
            var previewChild = brush.previewParent.GetChild(0);
            Assert.AreEqual(15f, previewChild.rotation.eulerAngles.z, 0.01f,
                "preview world rotation should reflect the rotation offset");
        }
    }
}
