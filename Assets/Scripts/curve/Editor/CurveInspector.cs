using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public enum EManipulationModes
{
    Free,
    SelectAndTransform
}


[CustomEditor(typeof(curve))]
public class CurveInspector : Editor
{
    private curve t;
    private ReorderableList pointReorderableList;

    private float time;
    private EManipulationModes TranslateMode;
    private EManipulationModes RotationMode;
    private EManipulationModes handlePositionMode;
    private ECurveType allCurveType = ECurveType.Custom;
    private AnimationCurve allAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private GUIContent addPointContent = new GUIContent("Add Anchor", "Adds an Anchor at the gameobjects position");
    private GUIContent deletePointContent = new GUIContent("X", "Deletes this waypoint");
    private GUIContent testButtonContent = new GUIContent("play", "Only available in play mode");
    private GUIContent pauseButtonContent = new GUIContent("Pause", "Paused Camera at current Position");
    private GUIContent continueButtonContent = new GUIContent("Continue", "Continues Path at current position");
    private GUIContent stopButtonContent = new GUIContent("Stop", "Stops the playback");
    private GUIContent gotoPointContent = new GUIContent("Goto", "Teleports the scene camera to the specified waypoint");
    private GUIContent alwaysShowContent = new GUIContent("Always show", "When true, shows the curve even when the GameObject is not selected - \"Inactive cath color\" will be used as path color instead");
    private GUIContent chainedContent = new GUIContent("o───o", "Toggles if the handles of the specified waypoint should be chained (mirrored) or not");
    private GUIContent unchainedContent = new GUIContent("o─x─o", "Toggles if the handles of the specified waypoint should be chained (mirrored) or not");
    private GUIContent replaceAllPositionContent = new GUIContent("Replace all position lerps", "Replaces curve types (and curves when set to \"Custom\") of all the waypoint position lerp types with the specified values");
    private GUIContent replaceAllRotationContent = new GUIContent("Replace all rotation lerps", "Replaces curve types (and curves when set to \"Custom\") of all the waypoint rotation lerp types with the specified values");

    private SerializedObject serializedObjectTarget;
    private SerializedProperty targetObjectProperty;
    private SerializedProperty visualPathProperty;
    private SerializedProperty visualInactivePathProperty;
    private SerializedProperty visualFrustumProperty;
    private SerializedProperty visualHandleProperty;
    private SerializedProperty visualNewAnchorProperty;
    private SerializedProperty loopedProperty;
    private SerializedProperty alwaysShowProperty;
    private SerializedProperty afterLoopProperty;
    private SerializedProperty playOnAwakeProperty;
    private SerializedProperty playOnAwakeTimeProperty;
    private SerializedProperty sampleAmountProperty;
    private SerializedProperty useSpeedProperty;
    private SerializedProperty SpeedProperty;

    private float currentTime;
    private float previousTime;
    private bool hasScrollBar = false;
    private int selectedIndex;
    private bool visualFoldout;
    private bool manipulationFoldout;
    private bool showRawValues;
    private bool doloopConnect;

    void OnEnable()
    {
        EditorApplication.update += Update;

        t = (curve)target;
        if (t == null) return;

        SetupEditorVariables();
        GetVariableProperties();
        SetupReorderableList();
    }

    void OnDisable()
    {
        EditorApplication.update -= Update;
    }

    // Update is called once per frame
    void Update()
    {
        if (t == null) return;

        currentTime = t.CurrentAchor + t.CurrentTime;
        if (Math.Abs(currentTime - previousTime) > 0.0001f)
        {
            Repaint();
            previousTime = currentTime;
        }
    }
    public override void OnInspectorGUI()
    {
        serializedObjectTarget.Update();
        DrawPlaybackWindow();
        Rect scale = GUILayoutUtility.GetLastRect();
        hasScrollBar = (Screen.width - scale.width <= 12);
        GUILayout.Space(5);
        GUILayout.Box("", GUILayout.Width(Screen.width - 20), GUILayout.Height(3));
        GUILayout.Space(5);
        DrawBasicSettings();
        GUILayout.Space(5);
        DrawVisualDropdown();
        GUILayout.Box("", GUILayout.Width(Screen.width - 20), GUILayout.Height(3));
        DrawManipulationDropdown();
        GUILayout.Box("", GUILayout.Width(Screen.width - 20), GUILayout.Height(3));
        GUILayout.Space(10);
        DrawWaypointList();
        GUILayout.Space(10);
        DrawRawValues();
        serializedObjectTarget.ApplyModifiedProperties();
    }
    private void OnSceneGUI()
    {
        if (t.anchors.Count >= 2)
        {
            for (int i = 0; i < t.anchors.Count; i++)
            {
                DrawHandles(i);
                Handles.color = Color.white;
            }
        }

    }

    private void DrawHandles(int i)
    {
        Handles.color = t.visual.handleColor;
        DrawHandleLines(i);
        DrawNextHandle(i);
        DrawPrevHandle(i);
        DrawAnchorHandles(i);
        DrawAddAnchorHandles(i);
        DrawSelectionHandles(i);
    }
    void SelectIndex(int index)
    {
        selectedIndex = index;
        pointReorderableList.index = index;
        Repaint();
    }
    private void DrawHandleLines(int i)
    {
        if (i < t.anchors.Count - 1 || t.looped == true)
            Handles.DrawLine(t.anchors[i].position, t.anchors[i].position + t.anchors[i].nextTangent);
        if (i > 0 || t.looped == true)
            Handles.DrawLine(t.anchors[i].position, t.anchors[i].position + t.anchors[i].previousTangent);
    }
    private void DrawNextHandle(int i)
    {
        if (i < t.anchors.Count - 1 || loopedProperty.boolValue)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 posNext = Vector3.zero;
            float size = HandleUtility.GetHandleSize(t.anchors[i].position + t.anchors[i].nextTangent) * 0.1f;
            if (handlePositionMode == EManipulationModes.Free)
            {
                posNext = Handles.FreeMoveHandle(t.anchors[i].position + t.anchors[i].nextTangent, Quaternion.identity, size, Vector3.zero, Handles.SphereHandleCap);
            }
            else
            {
                if (selectedIndex == i)
                {
                    Handles.SphereHandleCap(0, t.anchors[i].position + t.anchors[i].nextTangent, Quaternion.identity, size, EventType.Repaint);

                    posNext = Handles.PositionHandle(t.anchors[i].position + t.anchors[i].nextTangent, Quaternion.identity);
                }
                else if (Event.current.button != 1)
                {
                    if (Handles.Button(t.anchors[i].position + t.anchors[i].nextTangent, Quaternion.identity, size, size, Handles.CubeHandleCap))
                    {
                        SelectIndex(i);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed Handle Position");
                t.anchors[i].nextTangent = posNext - t.anchors[i].position;
                t.GetSamplePoints(i, sampleAmountProperty.intValue);
                if (t.anchors[i].chained)
                {
                    t.anchors[i].previousTangent = t.anchors[i].nextTangent * -1;
                    int n = i == 0 ? t.anchors.Count - 1 : i - 1;
                    t.GetSamplePoints(n, sampleAmountProperty.intValue);
                }

            }
        }
    }

    private void DrawPrevHandle(int i)
    {
        if (i > 0 || loopedProperty.boolValue)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 posNext = Vector3.zero;
            float size = HandleUtility.GetHandleSize(t.anchors[i].position + t.anchors[i].previousTangent) * 0.1f;
            if (handlePositionMode == EManipulationModes.Free)
            {
                posNext = Handles.FreeMoveHandle(t.anchors[i].position + t.anchors[i].previousTangent, Quaternion.identity, size, Vector3.zero, Handles.SphereHandleCap);
            }
            else
            {
                if (selectedIndex == i)
                {
                    Handles.SphereHandleCap(0, t.anchors[i].position + t.anchors[i].previousTangent, Quaternion.identity, size, EventType.Repaint);

                    posNext = Handles.PositionHandle(t.anchors[i].position + t.anchors[i].previousTangent, Quaternion.identity);
                }
                else if (Event.current.button != 1)
                {
                    if (Handles.Button(t.anchors[i].position + t.anchors[i].previousTangent, Quaternion.identity, size, size, Handles.CubeHandleCap))
                    {
                        SelectIndex(i);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed Handle Position");
                t.anchors[i].previousTangent = posNext - t.anchors[i].position;
                int n = i == 0 ? t.anchors.Count - 1 : i - 1;
                t.GetSamplePoints(n, sampleAmountProperty.intValue);
                if (t.anchors[i].chained)
                {
                    t.GetSamplePoints(i, sampleAmountProperty.intValue);
                    t.anchors[i].nextTangent = t.anchors[i].previousTangent * -1;
                }
            }
        }
    }
    private void DrawAnchorHandles(int i)
    {
        if (Tools.current == Tool.Move)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 pos = Vector3.zero;
            if (TranslateMode == EManipulationModes.SelectAndTransform)
            {
                if (i == selectedIndex) pos = Handles.PositionHandle(t.anchors[i].position, (Tools.pivotRotation == PivotRotation.Local) ? t.anchors[i].rotation : Quaternion.identity);
            }
            else
            {
                pos = Handles.FreeMoveHandle(t.anchors[i].position, (Tools.pivotRotation == PivotRotation.Local) ? t.anchors[i].rotation : Quaternion.identity, HandleUtility.GetHandleSize(t.anchors[i].position) * 0.2f, Vector3.zero, Handles.RectangleHandleCap);

            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Moved Waypoint");
                t.anchors[i].position = pos;
                if(i != t.anchors.Count - 1 || loopedProperty.boolValue) 
                    t.GetSamplePoints(i, sampleAmountProperty.intValue);
                if (i != 0 || loopedProperty.boolValue)
                {
                    int n = i == 0 ? t.anchors.Count - 1 : i - 1;
                    t.GetSamplePoints(n, sampleAmountProperty.intValue);
                }
            }
        }
        else if (Tools.current == Tool.Rotate)
        {

            EditorGUI.BeginChangeCheck();
            Quaternion rot = Quaternion.identity;
            if (RotationMode == EManipulationModes.SelectAndTransform)
            {
                if (i == selectedIndex) rot = Handles.RotationHandle(t.anchors[i].rotation, t.anchors[i].position);
            }
            else
            {
                rot = Handles.FreeRotateHandle(t.anchors[i].rotation, t.anchors[i].position, HandleUtility.GetHandleSize(t.anchors[i].position) * 0.2f);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Rotated Waypoint");
                t.anchors[i].rotation = rot;
            }
        }
    }

    private void DrawSelectionHandles(int i)
    {
        if (Event.current.button != 1 && selectedIndex != i)
        {
            if (TranslateMode == EManipulationModes.SelectAndTransform && Tools.current == Tool.Move
                || RotationMode == EManipulationModes.SelectAndTransform && Tools.current == Tool.Rotate)
            {
                float size = HandleUtility.GetHandleSize(t.anchors[i].position) * 0.2f;
                if (Handles.Button(t.anchors[i].position, Quaternion.identity, size, size, Handles.CubeHandleCap))
                {
                    SelectIndex(i);
                }
            }
        }
    }
    private void DrawAddAnchorHandles(int i)
    {
        if (selectedIndex == i)
        {
            doloopConnect = false;
            Undo.RecordObject(target, "Added Anchor Point");
            Handles.color = t.visual.AddAnchorColor;
            float size = HandleUtility.GetHandleSize(t.GetBezierPosition(i, 0.5f)) * 0.2f;
            if (i != t.anchors.Count - 1 || loopedProperty.boolValue)
            {
                if (Handles.Button(t.GetBezierPosition(i, 0.5f), Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    t.anchors.Insert(i + 1, new Anchor(t.GetBezierPosition(i, 0.5f), t.GetLerpRotation(i, 0.5f)));
                }
            }
            else
            {
                int nextpos = i - 1;
                if (nextpos == -1)
                    nextpos = t.anchors.Count - 1;
                if (((t.anchors[i].position + (t.anchors[i].position - t.GetBezierPosition(nextpos, 0.5f)).normalized * 2.5f) - t.anchors[0].position).magnitude < 2)
                {
                    Handles.color = t.visual.AddAnchorColor + Color.black;
                    doloopConnect = true;
                }
                if (Handles.Button(t.anchors[i].position + (t.anchors[i].position - t.GetBezierPosition(nextpos, 0.5f)).normalized * 2.5f, t.anchors[i].rotation, size, size, Handles.SphereHandleCap))
                {
                    loopedProperty.boolValue = doloopConnect;
                    t.anchors.Add(new Anchor(t.anchors[i].position + (t.anchors[i].position - t.GetBezierPosition(nextpos, 0.5f)).normalized * 2.5f, t.anchors[i].rotation));
                    serializedObjectTarget.ApplyModifiedProperties();
                }
                Handles.color = t.visual.AddAnchorColor;
            }
            doloopConnect = false;
            //previous anchor
            if (i > 0 || loopedProperty.boolValue)
            {
                int nextpos = i - 1;
                if (nextpos == -1)
                    nextpos = t.anchors.Count - 1;
                if (Handles.Button(t.GetBezierPosition(nextpos, 0.5f), Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    t.anchors.Insert(i, new Anchor(t.GetBezierPosition(nextpos, 0.5f), t.GetLerpRotation(nextpos, 0.5f)));
                }
            }
            else
            {
                if (((t.anchors[i].position + (t.anchors[i].position - t.GetBezierPosition(i, 0.5f)).normalized * 2.5f) - t.anchors[t.anchors.Count - 1].position).magnitude < 2)
                {
                    Handles.color = t.visual.AddAnchorColor + Color.black;
                    doloopConnect = true;
                }
                if (Handles.Button(t.anchors[i].position + (t.anchors[i].position - t.GetBezierPosition(i, 0.5f)).normalized * 2.5f, t.anchors[i].rotation, size, size, Handles.SphereHandleCap))
                {
                    loopedProperty.boolValue = doloopConnect;
                    t.anchors.Insert(i, new Anchor(t.anchors[i].position + (t.anchors[i].position - t.GetBezierPosition(i, 0.5f)).normalized * 2.5f, t.anchors[i].rotation));
                    serializedObjectTarget.ApplyModifiedProperties();

                }
                Handles.color = t.visual.AddAnchorColor;
            }
        }
    }

    private void SetupReorderableList()
    {
        pointReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("anchors"), true, true, false, false);

        pointReorderableList.elementHeight *= 2;

        pointReorderableList.drawElementCallback = (rect, index, active, focused) =>
        {
            float startRectY = rect.y;
            if (index > t.anchors.Count - 1) return;
            rect.height -= 2;
            float fullWidth = rect.width - 16 * (hasScrollBar ? 1 : 0);
            rect.width = 40;
            fullWidth -= 40;
            rect.height /= 2;
            GUI.Label(rect, "#" + (index + 1));
            rect.y += rect.height - 3;
            rect.x -= 14;
            rect.width += 12;
            if (GUI.Button(rect, t.anchors[index].chained ? chainedContent : unchainedContent))
            {
                Undo.RecordObject(t, "Changed chain type");
                t.anchors[index].chained = !t.anchors[index].chained;
            }
            rect.y += rect.height - 3;
            rect.x += rect.width + 2;

            rect.y = startRectY;

            //Position
            rect.width = (fullWidth - 22) / 3 - 1;
            EditorGUI.BeginChangeCheck();
            ECurveType tempP = (ECurveType)EditorGUI.EnumPopup(rect, t.anchors[index].curveTypePosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed enum value");
                t.anchors[index].curveTypePosition = tempP;
            }
            rect.y += pointReorderableList.elementHeight / 2 - 4;
            //rect.x += rect.width + 2;
            EditorGUI.BeginChangeCheck();
            GUI.enabled = t.anchors[index].curveTypePosition == ECurveType.Custom;
            AnimationCurve tempACP = EditorGUI.CurveField(rect, t.anchors[index].positionCurve);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed curve");
                t.anchors[index].positionCurve = tempACP;
            }
            GUI.enabled = true;
            rect.x += rect.width + 2;
            rect.y = startRectY;

            //Rotation

            rect.width = (fullWidth - 22) / 3 - 1;
            EditorGUI.BeginChangeCheck();
            ECurveType temp = (ECurveType)EditorGUI.EnumPopup(rect, t.anchors[index].curveTypeRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed enum value");
                t.anchors[index].curveTypeRotation = temp;
            }
            rect.y += pointReorderableList.elementHeight / 2 - 4;
            //rect.height /= 2;
            //rect.x += rect.width + 2;
            EditorGUI.BeginChangeCheck();
            GUI.enabled = t.anchors[index].curveTypeRotation == ECurveType.Custom;
            AnimationCurve tempAC = EditorGUI.CurveField(rect, t.anchors[index].rotationCurve);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed curve");
                t.anchors[index].rotationCurve = tempAC;
            }
            GUI.enabled = true;

            rect.y = startRectY;
            rect.height *= 2;
            rect.x += rect.width + 2;
            rect.width = (fullWidth - 22) / 3;
            rect.height = rect.height / 2 - 1;
            if (GUI.Button(rect, gotoPointContent))
            {
                pointReorderableList.index = index;
                selectedIndex = index;
                SceneView.lastActiveSceneView.pivot = t.anchors[pointReorderableList.index].position;
                SceneView.lastActiveSceneView.size = 3;
                SceneView.lastActiveSceneView.Repaint();
            }
            rect.y += rect.height + 2;
            GUI.enabled = index < t.anchors.Count - 1 || loopedProperty.boolValue;
            if (GUI.Button(rect, new GUIContent("Update Distance")))
            {
                t.GetSamplePoints(index, sampleAmountProperty.intValue);
            }
            GUI.enabled = true;
            rect.height = (rect.height + 1) * 2;
            rect.y = startRectY;
            rect.x += rect.width + 2;
            rect.width = 20;

            if (GUI.Button(rect, deletePointContent))
            {
                Undo.RecordObject(t, "Deleted a waypoint");
                t.anchors.Remove(t.anchors[index]);
                SceneView.RepaintAll();
            }
        };

        pointReorderableList.drawHeaderCallback = rect =>
        {
            float fullWidth = rect.width;
            rect.width = 56;
            GUI.Label(rect, "Sum: " + t.anchors.Count);
            rect.x += rect.width;
            rect.width = (fullWidth - 78) / 3;
            GUI.Label(rect, "Position Lerp");
            rect.x += rect.width;
            GUI.Label(rect, "Rotation Lerp");
            //rect.x += rect.width*2;
            //GUI.Label(rect, "Del.");
        };

        pointReorderableList.onSelectCallback = l =>
        {
            selectedIndex = l.index;
            SceneView.RepaintAll();
        };
    }

    private void GetVariableProperties()
    {
        serializedObjectTarget = new SerializedObject(t);
        targetObjectProperty = serializedObjectTarget.FindProperty("obj");
        visualPathProperty = serializedObjectTarget.FindProperty("visual.pathColor");
        visualInactivePathProperty = serializedObjectTarget.FindProperty("visual.inactivePathColor");
        visualFrustumProperty = serializedObjectTarget.FindProperty("visual.frustrumColor");
        visualHandleProperty = serializedObjectTarget.FindProperty("visual.handleColor");
        visualNewAnchorProperty = serializedObjectTarget.FindProperty("visual.AddAnchorColor");
        loopedProperty = serializedObjectTarget.FindProperty("looped");
        alwaysShowProperty = serializedObjectTarget.FindProperty("alwaysShow");
        afterLoopProperty = serializedObjectTarget.FindProperty("afterLoop");
        playOnAwakeProperty = serializedObjectTarget.FindProperty("playOnAwake");
        playOnAwakeTimeProperty = serializedObjectTarget.FindProperty("pathCompleteTime");
        sampleAmountProperty = serializedObjectTarget.FindProperty("sampleAmount");
        useSpeedProperty = serializedObjectTarget.FindProperty("useSpeed");
        SpeedProperty = serializedObjectTarget.FindProperty("speed");
    }

    private void SetupEditorVariables()
    {
        TranslateMode = (EManipulationModes)PlayerPrefs.GetInt("TranslateMode", 1);
        RotationMode = (EManipulationModes)PlayerPrefs.GetInt("RotationMode", 1);
        handlePositionMode = (EManipulationModes)PlayerPrefs.GetInt("handlePositionMode", 0);
        time = PlayerPrefs.GetFloat("TotalCurveTime", 10);
    }

    private void DrawBasicSettings()
    {
        GUILayout.BeginHorizontal();
        GUI.enabled = true;
        targetObjectProperty.objectReferenceValue = (Transform)EditorGUILayout.ObjectField(targetObjectProperty.objectReferenceValue, typeof(Transform), true);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        loopedProperty.boolValue = GUILayout.Toggle(loopedProperty.boolValue, "Looped", GUILayout.Width(Screen.width / 3f));
        GUI.enabled = loopedProperty.boolValue;
        GUILayout.Label("After loop:", GUILayout.Width(Screen.width / 4f));
        afterLoopProperty.enumValueIndex = Convert.ToInt32(EditorGUILayout.EnumPopup((EAfterLoop)afterLoopProperty.intValue));
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        playOnAwakeProperty.boolValue = GUILayout.Toggle(playOnAwakeProperty.boolValue, "Play on awake", GUILayout.Width(Screen.width / 3f));
        GUI.enabled = playOnAwakeProperty.boolValue;
        if (useSpeedProperty.boolValue)
        {
            GUILayout.Label("Speed: ", GUILayout.Width(Screen.width / 4f));
            SpeedProperty.floatValue = EditorGUILayout.FloatField(SpeedProperty.floatValue);
        }
        else
        {
            GUILayout.Label("Time: ", GUILayout.Width(Screen.width / 4f));
            playOnAwakeTimeProperty.floatValue = EditorGUILayout.FloatField(playOnAwakeTimeProperty.floatValue);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        useSpeedProperty.boolValue = GUILayout.Toggle(useSpeedProperty.boolValue, new GUIContent("Use Speed","if true it will use distance based calculation"), GUILayout.Width(Screen.width / 3f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        sampleAmountProperty.intValue = EditorGUILayout.IntSlider(new GUIContent("Sample Amount ","how accucurate is the distance between Anchors,used during speed calculation, lower if experincing a framerate issue"),sampleAmountProperty.intValue, 2, 64);
        GUILayout.EndHorizontal();

    }

    void DrawPlaybackWindow()
    {
        GUI.enabled = Application.isPlaying;
        GUILayout.BeginVertical("Box");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(testButtonContent))
        {
            t.PlayPath(time);
        }
        if (!t.IsPaused)
        {
            if (Application.isPlaying && !t.IsPlaying) GUI.enabled = false;
            if (GUILayout.Button(pauseButtonContent))
            {
                t.PausePath();
            }
        }
        else if (GUILayout.Button(continueButtonContent))
        {
            t.ResumePath();
        }

        if (GUILayout.Button(stopButtonContent))
        {
            t.StopPath();
        }
        GUI.enabled = true;
        EditorGUI.BeginChangeCheck();
        if (!useSpeedProperty.boolValue)
            GUILayout.Label("Time (seconds): ");
        else
            GUILayout.Label("Speed: ");
        time = EditorGUILayout.FloatField("", time, GUILayout.MinWidth(5), GUILayout.MaxWidth(50));
        if (EditorGUI.EndChangeCheck())
        {
            time = Mathf.Clamp(time, 0.001f, Mathf.Infinity);
            if (!useSpeedProperty.boolValue)
                t.UpdateTimeInSeconds(time);
            else
                SpeedProperty.floatValue = time;
            PlayerPrefs.SetFloat("time", time);
        }
        GUILayout.EndHorizontal();
        GUI.enabled = Application.isPlaying;
        EditorGUI.BeginChangeCheck();
        currentTime = EditorGUILayout.Slider(currentTime, 0, t.anchors.Count - ((t.looped) ? 0.01f : 1.01f));
        if (EditorGUI.EndChangeCheck())
        {
            t.CurrentAchor = Mathf.FloorToInt(currentTime);
            t.CurrentTime = currentTime % 1;
            t.RefreshTransform();
        }
        GUI.enabled = false;
        Rect rr = GUILayoutUtility.GetRect(4, 8);
        float endWidth = rr.width - 60;
        rr.y -= 4;
        rr.width = 4;
        int c = t.anchors.Count + ((t.looped) ? 1 : 0);
        for (int i = 0; i < c; ++i)
        {
            GUI.Box(rr, "");
            rr.x += endWidth / (c - 1);
        }
        GUILayout.EndVertical();
        GUI.enabled = true;
    }
    void DrawVisualDropdown()
    {
        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        visualFoldout = EditorGUILayout.Foldout(visualFoldout, "Visual");
        alwaysShowProperty.boolValue = GUILayout.Toggle(alwaysShowProperty.boolValue, alwaysShowContent);
        GUILayout.EndHorizontal();
        if (visualFoldout)
        {
            GUILayout.BeginVertical("Box");
            visualPathProperty.colorValue = EditorGUILayout.ColorField("Path color", visualPathProperty.colorValue);
            visualInactivePathProperty.colorValue = EditorGUILayout.ColorField("Inactive path color", visualInactivePathProperty.colorValue);
            visualFrustumProperty.colorValue = EditorGUILayout.ColorField("Frustum color", visualFrustumProperty.colorValue);
            visualHandleProperty.colorValue = EditorGUILayout.ColorField("Handle color", visualHandleProperty.colorValue);
            visualNewAnchorProperty.colorValue = EditorGUILayout.ColorField("Add Anchor color", visualNewAnchorProperty.colorValue);
            if (GUILayout.Button("Default colors"))
            {
                Undo.RecordObject(t, "Reset to default color values");
                t.visual = new Visual();
            }
            GUILayout.EndVertical();
        }
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }
    void DrawManipulationDropdown()
    {
        manipulationFoldout = EditorGUILayout.Foldout(manipulationFoldout, "Transform manipulation modes");
        EditorGUI.BeginChangeCheck();
        if (manipulationFoldout)
        {
            GUILayout.BeginVertical("Box");
            TranslateMode = (EManipulationModes)EditorGUILayout.EnumPopup("Waypoint Translation", TranslateMode);
            RotationMode = (EManipulationModes)EditorGUILayout.EnumPopup("Waypoint Rotation", RotationMode);
            handlePositionMode = (EManipulationModes)EditorGUILayout.EnumPopup("Handle Translation", handlePositionMode);
            GUILayout.EndVertical();
        }
        if (EditorGUI.EndChangeCheck())
        {
            PlayerPrefs.SetInt("TranslateMode", (int)TranslateMode);
            PlayerPrefs.SetInt("RotationMode", (int)RotationMode);
            PlayerPrefs.SetInt("handlePositionMode", (int)handlePositionMode);
            SceneView.RepaintAll();
        }
    }
    void DrawWaypointList()
    {
        GUILayout.Label("Replace all lerp types");
        GUILayout.BeginVertical("Box");
        GUILayout.BeginHorizontal();
        allCurveType = (ECurveType)EditorGUILayout.EnumPopup(allCurveType, GUILayout.Width(Screen.width / 3f));
        if (GUILayout.Button(replaceAllPositionContent))
        {
            Undo.RecordObject(t, "Applied new position");
            foreach (var index in t.anchors)
            {
                index.curveTypePosition = allCurveType;
                if (allCurveType == ECurveType.Custom)
                    index.positionCurve.keys = allAnimationCurve.keys;
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUI.enabled = allCurveType == ECurveType.Custom;
        allAnimationCurve = EditorGUILayout.CurveField(allAnimationCurve, GUILayout.Width(Screen.width / 3f));
        GUI.enabled = true;
        if (GUILayout.Button(replaceAllRotationContent))
        {
            Undo.RecordObject(t, "Applied new rotation");
            foreach (var index in t.anchors)
            {
                index.curveTypeRotation = allCurveType;
                if (allCurveType == ECurveType.Custom)
                    index.rotationCurve = allAnimationCurve;
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Space(Screen.width / 2f - 20);
        GUILayout.Label("↓");
        GUILayout.EndHorizontal();
        serializedObject.Update();
        pointReorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
        Rect r = GUILayoutUtility.GetRect(Screen.width - 16, 18);
        //r.height = 18;
        r.y -= 10;
        GUILayout.Space(-30); GUILayout.BeginHorizontal();
        GUI.enabled = t.anchors.Count < 2;
        if (GUILayout.Button(addPointContent) )
        {
            Undo.RecordObject(t, "Added Anchor point");
            t.anchors.Add(new Anchor(t.gameObject.transform.position, Quaternion.identity));
            if (t.anchors.Count == 2)
            {
                t.GetSamplePoints(0,sampleAmountProperty.intValue);
            }
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }
    void DrawRawValues()
    {
        if (GUILayout.Button(showRawValues ? "Hide raw values" : "Show raw values"))
            showRawValues = !showRawValues;

        if (showRawValues)
        {
            foreach (var i in t.anchors)
            {
                EditorGUI.BeginChangeCheck();
                GUILayout.BeginVertical("Box");
                Vector3 pos = EditorGUILayout.Vector3Field("Anchor Position", i.position);
                Quaternion rot = Quaternion.Euler(EditorGUILayout.Vector3Field("Anchor Rotation", i.rotation.eulerAngles));
                Vector3 posp = EditorGUILayout.Vector3Field("Previous Tanget Offset", i.previousTangent);
                Vector3 posn = EditorGUILayout.Vector3Field("Next Tanget Offset", i.nextTangent);

                GUILayout.EndVertical();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(t, "Changed anchor transform");
                    i.position = pos;
                    i.rotation = rot;
                    i.previousTangent = posp;
                    i.nextTangent = posn;
                    SceneView.RepaintAll();
                }
                if (i.LUT.Length != 0)
                    GUILayout.Label("Distance: " + i.LUT[i.LUT.Length - 1].ToString());
                else
                    GUILayout.Label("Distance: no Data");
            }
        }
    }
}
