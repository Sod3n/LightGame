#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

// Scene-view "Edit Path" mode for Platform waypoints (moving platforms & moving lights).
// While active:
//   - Left-click empty space  -> place a new waypoint at the end of the path
//   - Drag a waypoint dot      -> move that waypoint
//   - Click the red X on a dot -> delete that waypoint
//   - Backspace / Delete       -> delete the last waypoint
// Activated from the Scene view Tools overlay (shown while a Platform is selected)
// or via the "Edit Path (Scene Tool)" button on the Platform inspector.
[EditorTool("Edit Path", typeof(Platform))]
public class PlatformPathTool : EditorTool
{
    private const float DotScreenSize = 0.08f;   // handle size as a fraction of GetHandleSize
    private const float DeleteScreenSize = 0.06f;

    private GUIContent m_IconContent;

    public override GUIContent toolbarIcon => m_IconContent;

    private void OnEnable()
    {
        m_IconContent = new GUIContent
        {
            image = EditorGUIUtility.IconContent("d_Grid.MoveTool").image,
            text = "Edit Path",
            tooltip = "Edit Path\nClick empty space to add a point, drag to move, red X to delete."
        };
    }

    public override void OnToolGUI(EditorWindow window)
    {
        var platform = target as Platform;
        if (platform == null)
            return;

        var evt = Event.current;

        // A fallback control that "wins" whenever the cursor isn't over a waypoint
        // dot or delete button, so plain clicks on empty space fall through to us.
        int addControl = GUIUtility.GetControlID(FocusType.Passive);

        var so = new SerializedObject(platform);
        var wpProp = so.FindProperty("waypoints");

        DrawPathLines(wpProp);
        DrawWaypointHandles(platform, so, wpProp, evt);

        if (evt.type == EventType.Layout)
            HandleUtility.AddDefaultControl(addControl);

        // Empty-space click -> append a waypoint.
        if (evt.type == EventType.MouseDown && evt.button == 0 &&
            HandleUtility.nearestControl == addControl)
        {
            AddWaypointAt(platform, so, wpProp, MouseToWorld(evt.mousePosition));
            evt.Use();
        }

        // Keyboard delete removes the last point (handy for quick undo of a misclick).
        if (evt.type == EventType.KeyDown &&
            (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace) &&
            wpProp.arraySize > 0)
        {
            DeleteWaypoint(platform, so, wpProp, wpProp.arraySize - 1);
            evt.Use();
        }

        DrawHud();
    }

    private static void DrawPathLines(SerializedProperty wpProp)
    {
        Handles.color = Color.cyan;
        for (int i = 0; i < wpProp.arraySize - 1; i++)
        {
            var a = wpProp.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            var b = wpProp.GetArrayElementAtIndex(i + 1).objectReferenceValue as Transform;
            if (a != null && b != null)
                Handles.DrawLine(a.position, b.position, 2f);
        }
    }

    private void DrawWaypointHandles(Platform platform, SerializedObject so, SerializedProperty wpProp, Event evt)
    {
        for (int i = 0; i < wpProp.arraySize; i++)
        {
            var t = wpProp.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if (t == null)
                continue;

            float handleSize = HandleUtility.GetHandleSize(t.position);

            // Move handle.
            Handles.color = Color.cyan;
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(
                t.position, handleSize * DotScreenSize, Vector3.zero, Handles.CircleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Move Waypoint");
                moved.z = t.position.z;
                t.position = moved;
            }

            Handles.Label(t.position + Vector3.up * handleSize * 0.2f, $"WP {i}");

            // Delete button (red X to the upper-right of the dot).
            Vector3 deletePos = t.position + (Vector3.right + Vector3.up) * handleSize * 0.22f;
            Handles.color = new Color(0.9f, 0.25f, 0.25f);
            if (Handles.Button(deletePos, Quaternion.identity,
                    handleSize * DeleteScreenSize, handleSize * DeleteScreenSize, Handles.DotHandleCap))
            {
                DeleteWaypoint(platform, so, wpProp, i);
                evt.Use();
                return; // list changed; bail this frame to avoid stale indices
            }
        }
    }

    private static Vector3 MouseToWorld(Vector2 guiPoint)
    {
        Vector3 world = HandleUtility.GUIPointToWorldRay(guiPoint).origin;
        world.z = 0f;
        return world;
    }

    private static void AddWaypointAt(Platform platform, SerializedObject so, SerializedProperty wpProp, Vector3 world)
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Add Waypoint");

        var wp = new GameObject($"{platform.name}_Waypoint_{wpProp.arraySize}");
        Undo.RegisterCreatedObjectUndo(wp, "Add Waypoint");
        wp.transform.SetParent(platform.transform, worldPositionStays: true);
        wp.transform.position = world;

        wpProp.arraySize++;
        wpProp.GetArrayElementAtIndex(wpProp.arraySize - 1).objectReferenceValue = wp.transform;
        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(group);
    }

    private static void DeleteWaypoint(Platform platform, SerializedObject so, SerializedProperty wpProp, int index)
    {
        if (index < 0 || index >= wpProp.arraySize)
            return;

        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Delete Waypoint");

        var t = wpProp.GetArrayElementAtIndex(index).objectReferenceValue as Transform;

        // Object-reference arrays need two deletes: the first nulls the slot, the second removes it.
        if (wpProp.GetArrayElementAtIndex(index).objectReferenceValue != null)
            wpProp.DeleteArrayElementAtIndex(index);
        wpProp.DeleteArrayElementAtIndex(index);
        so.ApplyModifiedProperties();

        if (t != null)
            Undo.DestroyObjectImmediate(t.gameObject);

        Undo.CollapseUndoOperations(group);
    }

    private static void DrawHud()
    {
        Handles.BeginGUI();
        var rect = new Rect(8, 8, 260, 62);
        GUILayout.BeginArea(rect, EditorStyles.helpBox);
        GUILayout.Label("Edit Path", EditorStyles.boldLabel);
        GUILayout.Label("Click empty space: add point");
        GUILayout.Label("Drag dot: move   •   Red X: delete point");
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}
#endif
