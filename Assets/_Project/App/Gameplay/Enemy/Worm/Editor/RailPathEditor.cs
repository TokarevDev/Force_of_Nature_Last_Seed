using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RailPath))]
public sealed class RailPathEditor : Editor
{
    private const float AddPointMaxScreenDistance = 20f;
    private const float NewEndPointDistance = 1f;
    private const float PointHandleSize = 0.08f;
    private const float SelectedPointHandleSize = 0.11f;

    private static readonly Color PathColor = new(0.1f, 1f, 0.25f, 0.95f);
    private static readonly Color SelectedPointColor = new(1f, 0.9f, 0.1f, 1f);
    private static readonly Color PointColor = new(0.2f, 0.95f, 1f, 1f);

    private SerializedProperty _localPointsProperty;
    private SerializedProperty _sampleStepProperty;
    private SerializedProperty _interpolationModeProperty;
    private SerializedProperty _cornerRadiusProperty;
    private SerializedProperty _cornerSamplesProperty;
    private SerializedProperty _legacyWaypointsProperty;

    private int _selectedPointIndex = -1;

    private RailPath Path => (RailPath)target;

    private void OnEnable()
    {
        _localPointsProperty = serializedObject.FindProperty("_localPoints");
        _sampleStepProperty = serializedObject.FindProperty("_sampleStep");
        _interpolationModeProperty = serializedObject.FindProperty("_interpolationMode");
        _cornerRadiusProperty = serializedObject.FindProperty("_cornerRadius");
        _cornerSamplesProperty = serializedObject.FindProperty("_cornerSamples");
        _legacyWaypointsProperty = serializedObject.FindProperty("_waypoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_sampleStepProperty);
        EditorGUILayout.PropertyField(_interpolationModeProperty);

        if ((RailPathInterpolationMode)_interpolationModeProperty.enumValueIndex == RailPathInterpolationMode.Smooth)
        {
            EditorGUILayout.PropertyField(_cornerRadiusProperty);
            EditorGUILayout.PropertyField(_cornerSamplesProperty);
        }

        EditorGUILayout.PropertyField(_localPointsProperty, includeChildren: true);

        serializedObject.ApplyModifiedProperties();

        DrawPathControls();
        DrawImportControls();
        DrawUsageHelp();
    }

    private void OnSceneGUI()
    {
        RailPath path = Path;
        if (path == null)
            return;

        HandleKeyboardDelete(path);
        HandleShiftClickInsert(path);
        DrawPath(path);
        DrawPointHandles(path);
    }

    private void DrawPathControls()
    {
        RailPath path = Path;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Path Tools", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Point"))
                AddPointAtEnd(path);

            using (new EditorGUI.DisabledScope(path.PointCount <= 0))
            {
                if (GUILayout.Button("Reverse"))
                    ApplyPathChange(path, "Reverse Rail Path", path.ReverseEditorPoints);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!CanRemoveSelectedPoint(path)))
            {
                if (GUILayout.Button("Remove Selected"))
                    RemoveSelectedPoint(path);
            }

            using (new EditorGUI.DisabledScope(path.PointCount <= 0))
            {
                if (GUILayout.Button("Flatten Z"))
                    ApplyPathChange(path, "Flatten Rail Path Z", path.FlattenEditorLocalZ);
            }
        }

        using (new EditorGUI.DisabledScope(path.PointCount <= 0))
        {
            if (GUILayout.Button("Clear Points") &&
                EditorUtility.DisplayDialog(
                    "Clear Rail Path",
                    "Remove all local path points?",
                    "Clear",
                    "Cancel"))
            {
                _selectedPointIndex = -1;
                ApplyPathChange(path, "Clear Rail Path", path.ClearEditorPoints);
            }
        }
    }

    private void DrawImportControls()
    {
        RailPath path = Path;

        if (path.LegacyWaypointCount <= 0 && path.ChildTransformCount < 2)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);

        if (path.LegacyWaypointCount >= 2)
        {
            EditorGUILayout.HelpBox(
                $"Found {path.LegacyWaypointCount} legacy waypoint references.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import Legacy Waypoints"))
                    ImportLegacyWaypoints(path);

                if (GUILayout.Button("Clear Legacy References"))
                    ApplyPathChange(path, "Clear Legacy Rail Path References", path.ClearLegacyWaypoints);
            }

            EditorGUILayout.PropertyField(_legacyWaypointsProperty, includeChildren: true);
        }

        if (path.ChildTransformCount >= 2)
        {
            if (GUILayout.Button("Import Child Transforms"))
                ImportChildTransforms(path);
        }
    }

    private static void DrawUsageHelp()
    {
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Scene View: click a point to select it, move the selected point with the Position handle, Shift+Left Click a segment to insert a point, Delete/Backspace removes the selected point. Smooth mode rounds corners without changing control point handles.",
            MessageType.None);
    }

    private void DrawPath(RailPath path)
    {
        if (path.PointCount < 2)
            return;

        Vector3[] points = path.GetEditorPreviewWorldPoints();
        if (points == null || points.Length < 2)
            return;

        Handles.color = PathColor;
        Handles.DrawAAPolyLine(4f, points);
    }

    private void DrawPointHandles(RailPath path)
    {
        for (int i = 0; i < path.PointCount; i++)
        {
            Vector3 point = path.GetEditorWorldPoint(i);
            float size = HandleUtility.GetHandleSize(point) *
                         (i == _selectedPointIndex ? SelectedPointHandleSize : PointHandleSize);

            Handles.color = i == _selectedPointIndex ? SelectedPointColor : PointColor;

            if (Handles.Button(point, Quaternion.identity, size, size, Handles.SphereHandleCap))
            {
                _selectedPointIndex = i;
                SceneView.RepaintAll();
            }

            Handles.Label(point + Vector3.up * size * 1.5f, GetPointLabel(i, path.PointCount));
        }

        if (_selectedPointIndex < 0 || _selectedPointIndex >= path.PointCount)
            return;

        Vector3 selectedPoint = path.GetEditorWorldPoint(_selectedPointIndex);

        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.PositionHandle(selectedPoint, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck())
            return;

        Undo.RecordObject(path, "Move Rail Path Point");
        path.SetEditorWorldPoint(_selectedPointIndex, newPosition);
        EditorUtility.SetDirty(path);
    }

    private void HandleShiftClickInsert(RailPath path)
    {
        Event current = Event.current;

        if (current.shift && current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (!current.shift || current.type != EventType.MouseDown || current.button != 0)
            return;

        if (path.PointCount < 2)
            return;

        if (!TryGetMouseWorldPosition(path, out Vector3 mouseWorldPosition))
            return;

        if (!TryFindClosestSegmentPoint(
                path,
                mouseWorldPosition,
                out int insertIndex,
                out Vector3 insertPosition))
        {
            return;
        }

        Undo.RecordObject(path, "Insert Rail Path Point");
        path.InsertEditorWorldPoint(insertIndex, insertPosition);
        _selectedPointIndex = insertIndex;
        EditorUtility.SetDirty(path);
        current.Use();
    }

    private void HandleKeyboardDelete(RailPath path)
    {
        Event current = Event.current;
        if (current.type != EventType.KeyDown)
            return;

        if (current.keyCode != KeyCode.Delete && current.keyCode != KeyCode.Backspace)
            return;

        if (!CanRemoveSelectedPoint(path))
            return;

        RemoveSelectedPoint(path);
        current.Use();
    }

    private bool TryFindClosestSegmentPoint(
        RailPath path,
        Vector3 mouseWorldPosition,
        out int insertIndex,
        out Vector3 insertPosition)
    {
        insertIndex = -1;
        insertPosition = default;

        float closestScreenDistance = float.MaxValue;

        for (int i = 0; i < path.PointCount - 1; i++)
        {
            Vector3 start = path.GetEditorWorldPoint(i);
            Vector3 end = path.GetEditorWorldPoint(i + 1);

            float screenDistance = HandleUtility.DistanceToLine(start, end);
            if (screenDistance >= closestScreenDistance)
                continue;

            closestScreenDistance = screenDistance;
            insertIndex = i + 1;
            insertPosition = ProjectPointOnSegment(mouseWorldPosition, start, end);
        }

        return insertIndex >= 0 && closestScreenDistance <= AddPointMaxScreenDistance;
    }

    private static Vector3 ProjectPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float sqrMagnitude = segment.sqrMagnitude;

        if (sqrMagnitude <= 0.0001f)
            return start;

        float t = Vector3.Dot(point - start, segment) / sqrMagnitude;
        return Vector3.Lerp(start, end, Mathf.Clamp01(t));
    }

    private static bool TryGetMouseWorldPosition(RailPath path, out Vector3 position)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, path.transform.position.z));

        if (plane.Raycast(ray, out float distance))
        {
            position = ray.GetPoint(distance);
            return true;
        }

        position = default;
        return false;
    }

    private void AddPointAtEnd(RailPath path)
    {
        Vector3 newPoint = GetNewEndPoint(path);

        Undo.RecordObject(path, "Add Rail Path Point");
        path.AddEditorWorldPoint(newPoint);
        _selectedPointIndex = path.PointCount - 1;
        EditorUtility.SetDirty(path);
    }

    private Vector3 GetNewEndPoint(RailPath path)
    {
        if (path.PointCount <= 0)
            return path.transform.position;

        Vector3 last = path.GetEditorWorldPoint(path.PointCount - 1);

        if (path.PointCount == 1)
            return last + Vector3.up;

        Vector3 previous = path.GetEditorWorldPoint(path.PointCount - 2);
        Vector3 offset = last - previous;

        return offset.sqrMagnitude > 0.0001f
            ? last + offset.normalized * NewEndPointDistance
            : last + Vector3.up * NewEndPointDistance;
    }

    private void RemoveSelectedPoint(RailPath path)
    {
        Undo.RecordObject(path, "Remove Rail Path Point");
        path.RemoveEditorPointAt(_selectedPointIndex);
        _selectedPointIndex = Mathf.Clamp(_selectedPointIndex, -1, path.PointCount - 1);
        EditorUtility.SetDirty(path);
    }

    private bool CanRemoveSelectedPoint(RailPath path)
    {
        return _selectedPointIndex >= 0 &&
               _selectedPointIndex < path.PointCount &&
               path.PointCount > 2;
    }

    private void ImportLegacyWaypoints(RailPath path)
    {
        Undo.RecordObject(path, "Import Legacy Rail Path Waypoints");
        int importedCount = path.ImportLegacyWaypointsToLocalPoints();
        _selectedPointIndex = importedCount > 0 ? 0 : -1;
        EditorUtility.SetDirty(path);
    }

    private void ImportChildTransforms(RailPath path)
    {
        Undo.RecordObject(path, "Import Rail Path Child Transforms");
        int importedCount = path.ImportChildTransformsToLocalPoints();
        _selectedPointIndex = importedCount > 0 ? 0 : -1;
        EditorUtility.SetDirty(path);
    }

    private void ApplyPathChange(RailPath path, string undoName, System.Action action)
    {
        Undo.RecordObject(path, undoName);
        action?.Invoke();
        _selectedPointIndex = Mathf.Clamp(_selectedPointIndex, -1, path.PointCount - 1);
        EditorUtility.SetDirty(path);
    }

    private static string GetPointLabel(int index, int count)
    {
        if (index == 0)
            return "Start";

        if (index == count - 1)
            return "End";

        return index.ToString();
    }
}
