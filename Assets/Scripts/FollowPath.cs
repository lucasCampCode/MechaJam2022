using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


[System.Serializable]
public class CheckPoint
{
    public float pauseAtTime;
    public float WaitTime;

    public CheckPoint()
    {
        pauseAtTime = 0f;
        WaitTime = 0f;
    }
}
[RequireComponent(typeof(NavMeshAgent))]
public class FollowPath : MonoBehaviour
{
    public PathCreator path;
    public List<CheckPoint> checkPoints = new List<CheckPoint>();
    private NavMeshAgent _navAgent;
    private float TimeWaited;
    private bool ShouldWait = false;
    public int checkPointReached { get; set; }

    void Start()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        ShouldWait = false;
    }

    void FixedUpdate()
    {
        if (checkPointReached != 0)
        {
            if (TimeWaited < checkPoints[checkPointReached - 1].WaitTime && ShouldWait)
                TimeWaited += Time.deltaTime;
            else
            {
                ShouldWait = false;
                path.ResumePath();
            }
        }
        Vector3 targetPosition = path.obj.position;
        Vector3 move = targetPosition - transform.position;
        transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));
        if (checkPoints.Count > checkPointReached)
        {
            if (path.CurrentAchor + path.CurrentTime > checkPoints[checkPointReached].pauseAtTime)
            {
                path.PausePath();
                checkPointReached++;
                TimeWaited = 0f;
                ShouldWait = true;
            }
        }
        if(move.magnitude > 5f)
            _navAgent.Move(move.normalized * path.speed * Time.deltaTime);
    }
    public void GotoPoint(Vector3 point)
    {
        transform.LookAt(new Vector3(point.x, transform.position.y, point.z));
        _navAgent.Move(Vector3.forward * path.speed * Time.deltaTime);
    }
}
