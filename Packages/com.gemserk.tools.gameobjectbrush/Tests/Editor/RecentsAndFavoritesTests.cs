using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Batch B: Recent list, favorite pinning, status-bar cursor tracking.
    public class RecentsAndFavoritesTests : PaletteTestFixture
    {
        [Test]
        public void RememberRecent_Inserts_At_Front_And_Dedupes()
        {
            PaletteCommon.recentEntries.Clear();
            PaletteCommon.RememberRecent(firstEntry);
            PaletteCommon.RememberRecent(secondEntry);
            PaletteCommon.RememberRecent(firstEntry);   // dedupe: moves to front
            Assert.AreEqual(2, PaletteCommon.recentEntries.Count);
            Assert.AreEqual(firstEntry.sourceObject, PaletteCommon.recentEntries[0].sourceObject);
            Assert.AreEqual(secondEntry.sourceObject, PaletteCommon.recentEntries[1].sourceObject);
        }

        [Test]
        public void RememberRecent_Caps_At_RecentCap()
        {
            PaletteCommon.recentEntries.Clear();
            for (int i = 0; i < PaletteCommon.RecentCap + 5; i++)
            {
                PaletteCommon.RememberRecent(new PaletteObject { name = "e" + i, sourceObject = firstEntry.sourceObject });
                // hack: give each a unique sourceObject to prevent dedupe
            }
            Assert.LessOrEqual(PaletteCommon.recentEntries.Count, PaletteCommon.RecentCap);
        }

        [Test]
        public void Paint_Pushes_Entry_Into_Recent_List()
        {
            PaletteCommon.recentEntries.Clear();
            SelectFirst();
            brush.UpdatePosition(new Vector2(0, 0));
            brush.Paint();
            Assert.IsTrue(PaletteCommon.recentEntries.Count >= 1);
            Assert.AreEqual(firstEntry.sourceObject, PaletteCommon.recentEntries[0].sourceObject);
        }

        [Test]
        public void ToggleFavorite_Adds_Then_Removes_The_Guid_And_Persists()
        {
            var prevPref = EditorPrefs.GetString("Gemserk.ObjectPalette.Favorites", "");
            try
            {
                PaletteCommon.favoriteGuids.Clear();
                Assert.IsFalse(GameObjectPaletteWindow.IsFavorite(firstEntry));

                GameObjectPaletteWindow.ToggleFavorite(firstEntry);
                Assert.IsTrue(GameObjectPaletteWindow.IsFavorite(firstEntry));

                var stored = EditorPrefs.GetString("Gemserk.ObjectPalette.Favorites", "");
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(firstEntry.sourceObject));
                Assert.IsTrue(stored.Contains(guid), "guid should be in pref after toggle");

                GameObjectPaletteWindow.ToggleFavorite(firstEntry);
                Assert.IsFalse(GameObjectPaletteWindow.IsFavorite(firstEntry));
            }
            finally
            {
                EditorPrefs.SetString("Gemserk.ObjectPalette.Favorites", prevPref);
                PaletteCommon.favoriteGuids.Clear();
            }
        }

        [Test]
        public void UpdatePosition_Records_Cursor_World_For_StatusBar()
        {
            SelectFirst();
            var pos = new Vector2(123.4f, -56.7f);
            brush.UpdatePosition(pos);
            Assert.AreEqual(pos.x, PaletteCommon.lastCursorWorld.x, 0.001f);
            Assert.AreEqual(pos.y, PaletteCommon.lastCursorWorld.y, 0.001f);
        }
    }
}
