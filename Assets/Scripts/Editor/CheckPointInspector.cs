using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(FollowPath))]
public class CheckPointInspector : Editor
{
    private FollowPath t;
    private ReorderableList list;
    private SerializedObject serializedObjectTarget;
    private SerializedProperty targetObjectProperty;

    private void OnEnable()
    {
        t = (FollowPath)target;
        if (t == null) return;

        GetVariableProperties();
        SetupReorderableList();
    }

    private void GetVariableProperties()
    {
        serializedObjectTarget = new SerializedObject(t);
        targetObjectProperty = serializedObjectTarget.FindProperty("path");
    }

    public override void OnInspectorGUI()
    {
        serializedObjectTarget.Update();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Path To follow");
        targetObjectProperty.objectReferenceValue = (PathCreator)EditorGUILayout.ObjectField(targetObjectProperty.objectReferenceValue, typeof(PathCreator), true);
        GUILayout.EndHorizontal();

        serializedObject.Update();
        list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();

        serializedObjectTarget.ApplyModifiedProperties();
    }
    private void OnSceneGUI()
    {
        for(int i = 0; i < t.checkPoints.Count; i++)
        {
            if (t.checkPoints[i].pauseAtTime < t.path.anchors.Count)
            {
                Vector3 pos = t.path.GetBezierPosition(Mathf.FloorToInt(t.checkPoints[i].pauseAtTime), t.checkPoints[i].pauseAtTime % 1);
                float size = HandleUtility.GetHandleSize(pos) * 0.2f;
                Handles.SphereHandleCap(-1, pos, Quaternion.identity, size, EventType.Repaint);
            }
        }
    }
    private void SetupReorderableList()
    {
        list = new ReorderableList(serializedObject, serializedObject.FindProperty("checkPoints"), true, false, true, true);

        list.elementHeight *= 2;

        list.drawElementCallback = (rect, index, active, focused) =>
        {
            float startRectY = rect.y;
            if (index > t.checkPoints.Count - 1) return;
            rect.height /= 2;
            EditorGUI.BeginChangeCheck();
            float tempPauseTime = EditorGUI.Slider(rect, "Pause At Time", t.checkPoints[index].pauseAtTime, index == 0 ? 0 : t.checkPoints[index -1].pauseAtTime, index < t.checkPoints.Count - 1 ? t.checkPoints[index + 1].pauseAtTime: t.path.anchors.Count - 1);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "change Pause time");
                t.checkPoints[index].pauseAtTime = tempPauseTime;
            }
            rect.y += rect.height;
            EditorGUI.BeginChangeCheck();
            float tempWaitTime = EditorGUI.Slider(rect, "Wait Time", t.checkPoints[index].WaitTime, 0, 30);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "change wait time");
                t.checkPoints[index].WaitTime = tempWaitTime;
            }

        };
    }
}
