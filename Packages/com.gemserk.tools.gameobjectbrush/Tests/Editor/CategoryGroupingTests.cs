using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Verifies the palette-window categorization: entries group by their leaf-folder name,
    // preserving insertion order within each group and overall category-first-seen order.
    public class CategoryGroupingTests
    {
        static PaletteObject MakeEntry(string name, GameObject sourceInProject)
        {
            return new PaletteObject { name = name, sourceObject = sourceInProject };
        }

        [Test]
        public void Null_Or_Empty_Input_Returns_Empty_List()
        {
            Assert.AreEqual(0, GameObjectPaletteWindow.GroupByCategory(null).Count);
            Assert.AreEqual(0, GameObjectPaletteWindow.GroupByCategory(new PaletteObject[0]).Count);
        }

        [Test]
        public void Entries_Without_Source_Fall_Under_General()
        {
            var e = new PaletteObject { name = "x", sourceObject = null };
            var groups = GameObjectPaletteWindow.GroupByCategory(new[] { e });
            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual("General", groups[0].Key);
        }

        [Test]
        public void Real_Project_Prefabs_Group_By_Leaf_Folder_Name()
        {
            // Grab two prefabs from different subfolders under Assets/_Game/Features
            var tile = FindPrefabIn("Assets/_Game/Features/Environment/Prefabs/Tiles");
            var platform = FindPrefabIn("Assets/_Game/Features/Environment/Prefabs/Platforms");
            if (tile == null || platform == null) Assert.Ignore("test needs Tiles + Platforms folders populated");

            var entries = new List<PaletteObject>
            {
                MakeEntry("t1", tile),
                MakeEntry("p1", platform),
                MakeEntry("t2", tile),
            };
            var groups = GameObjectPaletteWindow.GroupByCategory(entries);
            var byName = groups.ToDictionary(g => g.Key, g => g.Value);
            Assert.IsTrue(byName.ContainsKey("Tiles"));
            Assert.IsTrue(byName.ContainsKey("Platforms"));
            Assert.AreEqual(2, byName["Tiles"].Count);
            Assert.AreEqual(1, byName["Platforms"].Count);
        }

        [Test]
        public void First_Seen_Category_Comes_First_In_Result()
        {
            var tile = FindPrefabIn("Assets/_Game/Features/Environment/Prefabs/Tiles");
            var platform = FindPrefabIn("Assets/_Game/Features/Environment/Prefabs/Platforms");
            if (tile == null || platform == null) Assert.Ignore("test needs Tiles + Platforms populated");
            var entries = new List<PaletteObject> { MakeEntry("p1", platform), MakeEntry("t1", tile) };
            var groups = GameObjectPaletteWindow.GroupByCategory(entries);
            Assert.AreEqual("Platforms", groups[0].Key);
            Assert.AreEqual("Tiles", groups[1].Key);
        }

        static GameObject FindPrefabIn(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) return go;
            }
            return null;
        }
    }
}
