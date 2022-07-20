using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FollowPath : MonoBehaviour
{
    public PathCreator path;
    private NavMeshAgent _navAgent;
    // Start is called before the first frame update
    void Start()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 targetPosition = path.obj.position;
        Vector3 move = targetPosition - transform.position;
        transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));
        if(move.magnitude > 5f)
            _navAgent.Move(move.normalized * path.speed * Time.deltaTime);
    }
    public void GotoPoint(Vector3 point)
    {
        transform.LookAt(new Vector3(point.x, transform.position.y, point.z));
        _navAgent.Move(Vector3.forward * path.speed * Time.deltaTime);
    }
}
