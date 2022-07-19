using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Visual
{
    public Color pathColor = Color.green;
    public Color inactivePathColor = Color.gray;
    public Color frustrumColor = Color.white;
    public Color handleColor = Color.yellow;
    public Color AddAnchorColor = Color.cyan;
}
public enum ECurveType
{
    EaseInAndOut,
    Linear,
    Custom
}

public enum EAfterLoop
{
    Continue,
    Stop
}

[System.Serializable]
public class Anchor
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 nextTangent;
    public Vector3 previousTangent;
    public ECurveType curveTypePosition;
    public AnimationCurve positionCurve;
    public ECurveType curveTypeRotation;
    public AnimationCurve rotationCurve;
    public float[] LUT;
    public bool chained;
    public Anchor(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
        nextTangent = rot * Vector3.right;
        previousTangent = rot * Vector3.left;
        curveTypeRotation = ECurveType.EaseInAndOut;
        rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        curveTypePosition = ECurveType.Linear;
        positionCurve = AnimationCurve.Linear(0, 0, 1, 1);
        LUT = new float[0];
        chained = true;
    }
} 
public class curve : MonoBehaviour
{
    public Transform obj;
    public float pathCompleteTime = 30;
    public bool looped;
    public Visual visual;
    public List<Anchor> anchors = new List<Anchor>();
    public EAfterLoop afterLoop = EAfterLoop.Continue;
    public bool alwaysShow = true;
    public bool playOnAwake = false;
    public int sampleAmount = 1;
    public bool useSpeed = false;
    public float speed = 5;
    public float currentDistance;

    private int currentAnchorIndex;
    public int CurrentAchor { get => currentAnchorIndex; set => currentAnchorIndex = value;  }
    private float currentTimeInAnchor;
    public float CurrentTime { get => currentTimeInAnchor; set => currentTimeInAnchor = value; }
    private bool paused;
    public bool IsPaused { get => paused;  }
    private bool playing;
    public bool IsPlaying { get => playing; }
    private float timePerSegment;


    // Start is called before the first frame update
    void Start()
    {
        foreach (var index in anchors)
        {
            if (index.curveTypeRotation == ECurveType.EaseInAndOut) index.rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            if (index.curveTypeRotation == ECurveType.Linear) index.rotationCurve = AnimationCurve.Linear(0, 0, 1, 1);
            if (index.curveTypePosition == ECurveType.EaseInAndOut) index.positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            if (index.curveTypePosition == ECurveType.Linear) index.positionCurve = AnimationCurve.Linear(0, 0, 1, 1);
        }
        if(playOnAwake)
            PlayPath(pathCompleteTime);
    }
    public void PlayPath(float time)
    {
        if (time <= 0) time = 0.001f;
        playing = true;
        paused = false;
        StopAllCoroutines();
        StartCoroutine(FollowPath(useSpeed ? speed : time));
    }
    public void PausePath()
    {
        paused = true;
        playing = false;
    }
    public void ResumePath()
    {
        if (paused)
            playing = true;
        paused = false;
    }
    public void RefreshTransform()
    {
        obj.position = GetBezierPosition(currentAnchorIndex, currentTimeInAnchor);
        obj.rotation = GetLerpRotation(currentAnchorIndex, currentTimeInAnchor);
    }
    public void UpdateTimeInSeconds(float seconds)
    {
        timePerSegment = seconds / ((looped) ? anchors.Count : anchors.Count - 1);
    }
    IEnumerator FollowPath(float time)
    {
        UpdateTimeInSeconds(time);
        currentAnchorIndex = 0;
        while (currentAnchorIndex < anchors.Count)
        {
            if(useSpeed)
                GetSamplePoints(currentAnchorIndex, sampleAmount);
            currentTimeInAnchor = 0f;
            currentDistance = 0f;
            while (currentTimeInAnchor < 1)
            {
                if (!paused)
                {
                    currentDistance += Time.deltaTime * time;
                    currentTimeInAnchor = useSpeed ? GetTime(anchors[currentAnchorIndex].LUT,currentDistance): GetTime(timePerSegment);
                    obj.position = GetBezierPosition(currentAnchorIndex, currentTimeInAnchor);
                    obj.rotation = GetLerpRotation(currentAnchorIndex, currentTimeInAnchor);
                }
                yield return 0;
            }
            ++currentAnchorIndex;
            if (currentAnchorIndex == anchors.Count - 1 && !looped) break;
            if (currentAnchorIndex == anchors.Count && afterLoop == EAfterLoop.Continue) currentAnchorIndex = 0;
        }
    }
    float GetTime(float time)
    {
        return currentTimeInAnchor + Time.deltaTime / time;
    }
    float GetTime(float[] LUT,float distance)
    {
        float arcLength = LUT[LUT.Length - 1];
        int n = LUT.Length;
        if (distance > 0 && distance < arcLength)
        {
            for (int i = 0; i < n - 1; i++)
            {
                if (distance > LUT[i] && distance < LUT[i + 1])
                {
                    float lerpValue = Mathf.InverseLerp(LUT[i], LUT[i + 1], distance);
                    return Mathf.Lerp(i / (n - 1f), (i + 1) / (n - 1f), lerpValue);
                }
            }
        }
        return distance / arcLength;
    }
    public Vector3 GetBezierPosition(int pointIndex, float time)
    {
        float t = anchors[pointIndex].positionCurve.Evaluate(time);
        int nextIndex = pointIndex == anchors.Count - 1 ? 0 : pointIndex + 1;
        return
            Vector3.Lerp(
                Vector3.Lerp(
                    Vector3.Lerp(anchors[pointIndex].position,
                        anchors[pointIndex].position + anchors[pointIndex].nextTangent, t),

                    Vector3.Lerp(anchors[pointIndex].position + anchors[pointIndex].nextTangent,
                        anchors[nextIndex].position + anchors[nextIndex].previousTangent, t)
                    , t),
                Vector3.Lerp(
                    Vector3.Lerp(anchors[pointIndex].position + anchors[pointIndex].nextTangent,
                        anchors[nextIndex].position + anchors[nextIndex].previousTangent, t),

                    Vector3.Lerp(anchors[nextIndex].position + anchors[nextIndex].previousTangent,
                        anchors[nextIndex].position, t),
                    t),
                t);
    }
    int GetNextIndex(int index)
    {
        return index == anchors.Count - 1 ? 0 : index + 1;
    }
    public Quaternion GetLerpRotation(int pointIndex, float time)
    {
        return Quaternion.LerpUnclamped(anchors[pointIndex].rotation, anchors[GetNextIndex(pointIndex)].rotation, anchors[pointIndex].rotationCurve.Evaluate(time));
    }

    public void StopPath()
    {
        StopAllCoroutines();
        paused = false;
        playing = false;
    }
    public void GetSamplePoints(int i, int numOfSamples)
    {
        float samplefraction = (1f / numOfSamples);
        float[] samplePoints = new float[numOfSamples];
        float previousPoint = 0f;
        for (int f = 0; f < samplePoints.Length; f++)
        {
            float nextPoint = previousPoint + samplefraction;
            float distance = (GetBezierPosition(i, previousPoint) - GetBezierPosition(i, nextPoint)).magnitude;
            previousPoint = nextPoint;
            if (f == 0)
                samplePoints[f] = distance;
            else
                samplePoints[f] = samplePoints[f - 1] + distance;
        }
        anchors[i].LUT = samplePoints;

    }
#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (UnityEditor.Selection.activeGameObject == gameObject || alwaysShow)
        {
            if (anchors.Count >= 2)
            {
                for (int i = 0; i < anchors.Count; i++)
                {
                    if (i < anchors.Count - 1)
                    {
                        var index = anchors[i];
                        var indexNext = anchors[i + 1];
                        UnityEditor.Handles.DrawBezier(index.position, indexNext.position, index.position + index.nextTangent,
                            indexNext.position + indexNext.previousTangent, ((UnityEditor.Selection.activeGameObject == gameObject) ? visual.pathColor : visual.inactivePathColor), null, 5);
                    }
                    else if (looped)
                    {
                        var index = anchors[i];
                        var indexNext = anchors[0];
                        UnityEditor.Handles.DrawBezier(index.position, indexNext.position, index.position + index.nextTangent,
                            indexNext.position + indexNext.previousTangent, ((UnityEditor.Selection.activeGameObject == gameObject) ? visual.pathColor : visual.inactivePathColor), null, 5);
                    }
                }
            }

            for (int i = 0; i < anchors.Count; i++)
            {
                var index = anchors[i];
                Gizmos.matrix = Matrix4x4.TRS(index.position, index.rotation, Vector3.one);
                Gizmos.color = visual.frustrumColor;
                Gizmos.DrawRay(Vector3.zero, Vector3.forward);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
#endif

}

