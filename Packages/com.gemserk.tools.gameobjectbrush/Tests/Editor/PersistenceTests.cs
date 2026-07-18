using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Verifies the EditorPrefs contract of SavePaintTarget / RestorePaintTarget without
    // requiring a real saved scene. The GlobalObjectId round-trip inside those methods is
    // Unity's own well-tested API — we validate the wrapper contract:
    // - writes when target is set,
    // - deletes when target is null,
    // - reads back into paintTarget.
    public class PersistenceTests
    {
        const string PaintTargetPrefKey = "Gemserk.ObjectPalette.PaintTargetId";
        string previousPref;

        [SetUp]
        public void SetUp()
        {
            previousPref = EditorPrefs.GetString(PaintTargetPrefKey, "");
            EditorPrefs.DeleteKey(PaintTargetPrefKey);
            PaletteCommon.paintTarget = null;
        }

        [TearDown]
        public void TearDown()
        {
            EditorPrefs.SetString(PaintTargetPrefKey, previousPref);
            PaletteCommon.paintTarget = null;
        }

        [Test]
        public void Saving_Null_Clears_The_Pref()
        {
            EditorPrefs.SetString(PaintTargetPrefKey, "old-value");
            PaletteCommon.paintTarget = null;
            SaveViaReflection();
            Assert.IsFalse(EditorPrefs.HasKey(PaintTargetPrefKey),
                "saving with target=null should delete the pref, not keep stale data");
        }

        [Test]
        public void Saving_A_Target_Writes_A_NonEmpty_Pref()
        {
            var go = new GameObject("__PersistTarget__");
            try
            {
                PaletteCommon.paintTarget = go.transform;
                SaveViaReflection();
                var stored = EditorPrefs.GetString(PaintTargetPrefKey, "");
                Assert.IsFalse(string.IsNullOrEmpty(stored),
                    "SavePaintTarget must write a GlobalObjectId string");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Restore_When_Nothing_Saved_Leaves_Target_Null()
        {
            EditorPrefs.DeleteKey(PaintTargetPrefKey);
            PaletteCommon.paintTarget = null;
            RestoreViaReflection();
            Assert.IsNull(PaletteCommon.paintTarget);
        }

        [Test]
        public void Restore_With_Malformed_Pref_Leaves_Target_Null_Without_Throwing()
        {
            EditorPrefs.SetString(PaintTargetPrefKey, "not-a-valid-global-object-id");
            PaletteCommon.paintTarget = null;
            Assert.DoesNotThrow(() => RestoreViaReflection(),
                "malformed pref must not crash the window");
            Assert.IsNull(PaletteCommon.paintTarget);
        }

        static void SaveViaReflection()
        {
            var mi = typeof(GameObjectPaletteWindow).GetMethod("SavePaintTarget",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var window = ScriptableObject.CreateInstance<GameObjectPaletteWindow>();
            try { mi.Invoke(window, null); }
            finally { Object.DestroyImmediate(window); }
        }

        static void RestoreViaReflection()
        {
            var mi = typeof(GameObjectPaletteWindow).GetMethod("RestorePaintTarget",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var window = ScriptableObject.CreateInstance<GameObjectPaletteWindow>();
            try { mi.Invoke(window, null); }
            finally { Object.DestroyImmediate(window); }
        }
    }
}
