using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gemserk.Tools.ObjectPalette.Editor;

namespace Gemserk.Tools.ObjectPalette.Tests
{
    // Copy component values from the active selection to every other selected transform,
    // mimicking Figma's "Copy Properties".
    public class CopyPropertiesTests : PaletteTestFixture
    {
        [Test]
        public void Apply_Copies_Non_Transform_Values_From_Active_To_Others()
        {
            var src = MakeSpriteGO("__src__", new Color(0.9f, 0.1f, 0.1f, 1f));
            var a = MakeSpriteGO("__a__", Color.white);
            var b = MakeSpriteGO("__b__", Color.white);
            try
            {
                Selection.objects = new Object[] { src, a, b };
                Selection.activeGameObject = src;

                var updated = PaletteShortcuts.ApplyPropertiesToSelection();

                Assert.AreEqual(2, updated);
                Assert.AreEqual(src.GetComponent<SpriteRenderer>().color, a.GetComponent<SpriteRenderer>().color);
                Assert.AreEqual(src.GetComponent<SpriteRenderer>().color, b.GetComponent<SpriteRenderer>().color);
            }
            finally
            {
                Object.DestroyImmediate(src);
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Selection.objects = null;
            }
        }

        [Test]
        public void Apply_With_Single_Selection_Does_Nothing()
        {
            var src = MakeSpriteGO("__src__", Color.red);
            try
            {
                Selection.objects = new Object[] { src };
                Selection.activeGameObject = src;
                Assert.AreEqual(0, PaletteShortcuts.ApplyPropertiesToSelection());
            }
            finally { Object.DestroyImmediate(src); Selection.objects = null; }
        }

        [Test]
        public void Apply_With_No_Active_Selection_Is_A_No_Op()
        {
            Selection.objects = null;
            Selection.activeGameObject = null;
            Assert.AreEqual(0, PaletteShortcuts.ApplyPropertiesToSelection());
        }

        [Test]
        public void Apply_Does_Not_Modify_World_Position_Of_Targets()
        {
            var src = MakeSpriteGO("__src__", Color.red);
            var a = MakeSpriteGO("__a__", Color.white);
            a.transform.position = new Vector3(5, 5, 0);
            try
            {
                Selection.objects = new Object[] { src, a };
                Selection.activeGameObject = src;
                PaletteShortcuts.ApplyPropertiesToSelection();
                Assert.AreEqual(new Vector3(5, 5, 0), a.transform.position,
                    "world transform should be untouched — only components copy");
            }
            finally { Object.DestroyImmediate(src); Object.DestroyImmediate(a); Selection.objects = null; }
        }

        GameObject MakeSpriteGO(string name, Color color)
        {
            var go = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, testScene);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = color;
            return go;
        }
    }
}
