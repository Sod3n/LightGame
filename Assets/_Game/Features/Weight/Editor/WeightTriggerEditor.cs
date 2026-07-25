#if UNITY_EDITOR
using LightGame.Core;
using UnityEditor;
using UnityEngine;

namespace LightGame.Features
{
    [CustomEditor(typeof(WeightTrigger))]
    public class WeightTriggerEditor : UnityEditor.Editor
    {
        private bool _isPicking;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Picker", EditorStyles.boldLabel);

            var previousColor = GUI.backgroundColor;
            if (_isPicking) GUI.backgroundColor = Color.yellow;

            var label = _isPicking ? "Click an object in the Scene... (Esc to cancel)" : "Pick Target in Scene";
            if (GUILayout.Button(label))
            {
                _isPicking = !_isPicking;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = previousColor;
        }

        private void OnSceneGUI()
        {
            if (!_isPicking) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _isPicking = false;
                e.Use();
                SceneView.RepaintAll();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var picked = HandleUtility.PickGameObject(e.mousePosition, false);
                if (picked != null)
                {
                    AddTogglable(picked);
                }

                _isPicking = false;
                e.Use();
                SceneView.RepaintAll();
            }
        }

        private void AddTogglable(GameObject picked)
        {
            var togglable = picked.GetComponentInParent<Togglable>();
            if (togglable == null)
            {
                Debug.LogWarning($"'{picked.name}' has no Togglable component (or a parent with one) — nothing was added.", picked);
                return;
            }

            var togglablesProp = serializedObject.FindProperty("togglables");
            for (var i = 0; i < togglablesProp.arraySize; i++)
            {
                if (togglablesProp.GetArrayElementAtIndex(i).objectReferenceValue == togglable)
                    return;
            }

            togglablesProp.arraySize++;
            togglablesProp.GetArrayElementAtIndex(togglablesProp.arraySize - 1).objectReferenceValue = togglable;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
