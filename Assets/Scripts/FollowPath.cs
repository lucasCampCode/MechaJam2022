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
        Vector3 Targetposition = path.obj.position;
        Vector3 move = Targetposition - transform.position;
        _navAgent.Move(move);
    }
}
