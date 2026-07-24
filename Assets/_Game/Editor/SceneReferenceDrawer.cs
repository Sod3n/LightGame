#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using LightGame.Globals;

namespace LightGame.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var sceneAssetProp = property.FindPropertyRelative("sceneAsset");
            var sceneNameProp = property.FindPropertyRelative("sceneName");

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            var newAsset = EditorGUI.ObjectField(position, label, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetProp.objectReferenceValue = newAsset;
                sceneNameProp.stringValue = newAsset != null ? newAsset.name : null;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
