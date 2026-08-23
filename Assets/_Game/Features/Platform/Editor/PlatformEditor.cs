#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Platform))]
public class PlatformEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        Platform platform = (Platform)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Waypoint Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add Waypoint"))
        {
            AddWaypoint(platform);
        }
        
        if (GUILayout.Button("Clear All Waypoints"))
        {
            if (EditorUtility.DisplayDialog("Clear Waypoints", 
                "Are you sure you want to clear all waypoints?", "Yes", "No"))
            {
                ClearWaypoints(platform);
            }
        }
    }
    
    private void AddWaypoint(Platform platform)
    {
        // Create a new GameObject for the waypoint
        GameObject waypointObj = new GameObject($"{platform.name}_Waypoint_{GetWaypointCount(platform)}");
        
        // Position it relative to the platform
        SerializedObject serializedPlatform = new SerializedObject(platform);
        SerializedProperty waypointsProperty = serializedPlatform.FindProperty("waypoints");
        
        if (waypointsProperty.arraySize == 0)
        {
            // First waypoint at platform position
            waypointObj.transform.position = platform.transform.position;
        }
        else
        {
            // Position new waypoint offset from the last one
            SerializedProperty lastWaypoint = waypointsProperty.GetArrayElementAtIndex(waypointsProperty.arraySize - 1);
            Transform lastTransform = lastWaypoint.objectReferenceValue as Transform;
            if (lastTransform != null)
            {
                waypointObj.transform.position = lastTransform.position + Vector3.right * 2f;
            }
            else
            {
                waypointObj.transform.position = platform.transform.position;
            }
        }
        
        // Parent waypoint to the platform
        waypointObj.transform.SetParent(platform.transform);
        
        // Add it to the waypoints list
        waypointsProperty.arraySize++;
        SerializedProperty newWaypoint = waypointsProperty.GetArrayElementAtIndex(waypointsProperty.arraySize - 1);
        newWaypoint.objectReferenceValue = waypointObj.transform;
        
        serializedPlatform.ApplyModifiedProperties();
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(waypointObj, "Add Waypoint");
        
        // Select the new waypoint
        Selection.activeGameObject = waypointObj;
    }
    
    private void ClearWaypoints(Platform platform)
    {
        SerializedObject serializedPlatform = new SerializedObject(platform);
        SerializedProperty waypointsProperty = serializedPlatform.FindProperty("waypoints");
        
        // Destroy all waypoint GameObjects
        for (int i = waypointsProperty.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty waypoint = waypointsProperty.GetArrayElementAtIndex(i);
            Transform waypointTransform = waypoint.objectReferenceValue as Transform;
            
            if (waypointTransform != null)
            {
                Undo.DestroyObjectImmediate(waypointTransform.gameObject);
            }
        }
        
        // Clear the list
        waypointsProperty.ClearArray();
        serializedPlatform.ApplyModifiedProperties();
    }
    
    private int GetWaypointCount(Platform platform)
    {
        SerializedObject serializedPlatform = new SerializedObject(platform);
        SerializedProperty waypointsProperty = serializedPlatform.FindProperty("waypoints");
        return waypointsProperty.arraySize;
    }
    
    // Draw waypoint gizmos in the scene view
    private void OnSceneGUI()
    {
        Platform platform = (Platform)target;
        SerializedObject serializedPlatform = new SerializedObject(platform);
        SerializedProperty waypointsProperty = serializedPlatform.FindProperty("waypoints");
        
        if (waypointsProperty.arraySize < 2)
            return;
        
        // Draw lines between waypoints
        Handles.color = Color.cyan;
        for (int i = 0; i < waypointsProperty.arraySize - 1; i++)
        {
            SerializedProperty waypoint1 = waypointsProperty.GetArrayElementAtIndex(i);
            SerializedProperty waypoint2 = waypointsProperty.GetArrayElementAtIndex(i + 1);
            
            Transform t1 = waypoint1.objectReferenceValue as Transform;
            Transform t2 = waypoint2.objectReferenceValue as Transform;
            
            if (t1 != null && t2 != null)
            {
                Handles.DrawLine(t1.position, t2.position);
                Handles.Label(t1.position, $"WP {i}");
            }
        }
        
        // Label the last waypoint
        SerializedProperty lastWP = waypointsProperty.GetArrayElementAtIndex(waypointsProperty.arraySize - 1);
        Transform lastT = lastWP.objectReferenceValue as Transform;
        if (lastT != null)
        {
            Handles.Label(lastT.position, $"WP {waypointsProperty.arraySize - 1}");
        }
    }
}
#endif