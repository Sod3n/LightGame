#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LightGame.Features
{
    [CustomEditor(typeof(SimpleTeleportTrigger))]
    public class SimpleTeleportTriggerEditor : UnityEditor.Editor
    {
        private enum PickMode { None, Destination, LinkedTeleport }

        private PickMode _pickMode = PickMode.None;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Picker", EditorStyles.boldLabel);

            DrawPickButton("Pick Destination in Scene", PickMode.Destination);
            DrawPickButton("Link with Teleport in Scene", PickMode.LinkedTeleport);
        }

        private void DrawPickButton(string label, PickMode mode)
        {
            var previousColor = GUI.backgroundColor;
            var isActive = _pickMode == mode;
            if (isActive) GUI.backgroundColor = Color.yellow;

            var buttonLabel = isActive ? "Click an object in the Scene... (Esc to cancel)" : label;
            if (GUILayout.Button(buttonLabel))
            {
                _pickMode = isActive ? PickMode.None : mode;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = previousColor;
        }

        private void OnSceneGUI()
        {
            if (_pickMode == PickMode.None) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _pickMode = PickMode.None;
                e.Use();
                SceneView.RepaintAll();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var picked = HandleUtility.PickGameObject(e.mousePosition, false);
                if (picked != null)
                {
                    ApplyPick(picked);
                }

                _pickMode = PickMode.None;
                e.Use();
                SceneView.RepaintAll();
            }
        }

        private void ApplyPick(GameObject picked)
        {
            switch (_pickMode)
            {
                case PickMode.Destination:
                    SetReference("destinationPoint", picked.transform);
                    break;
                case PickMode.LinkedTeleport:
                    var linked = picked.GetComponentInParent<SimpleTeleportTrigger>();
                    if (linked == null)
                    {
                        Debug.LogWarning($"'{picked.name}' has no SimpleTeleportTrigger component (or a parent with one) — nothing was set.", picked);
                        return;
                    }
                    if (linked == target)
                    {
                        Debug.LogWarning("A teleport trigger cannot link to itself.", picked);
                        return;
                    }
                    LinkPair((SimpleTeleportTrigger)target, linked);
                    break;
            }
        }

        private void SetReference(string propertyName, Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        // Wires both triggers to each other: entering A sends you to B's position and vice versa.
        private void LinkPair(SimpleTeleportTrigger a, SimpleTeleportTrigger b)
        {
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Link Teleport Pair");

            SetPair(a, b);
            SetPair(b, a);

            Undo.CollapseUndoOperations(group);
        }

        private static void SetPair(SimpleTeleportTrigger from, SimpleTeleportTrigger to)
        {
            var serialized = new SerializedObject(from);
            serialized.FindProperty("linkedTeleport").objectReferenceValue = to;
            serialized.FindProperty("destinationPoint").objectReferenceValue = to.transform;
            serialized.ApplyModifiedProperties();
        }
    }
}
#endif
