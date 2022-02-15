using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AutoPlayerWalk : MonoBehaviour
{
    public float lenght;
    public LayerMask ground;
    public Transform check;
    private Controls plc;
    // Start is called before the first frame update
    void Start()
    {
        plc = gameObject.GetComponent<Controls>();
        plc._horizontalMovement = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(check.position, check.right, lenght, ground))
        {
            plc._horizontalMovement = -1;
        }
        else if (Physics.Raycast(check.position, -check.right, lenght, ground))
        {
            plc._horizontalMovement = 1;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(check.position, check.right * lenght);
        Gizmos.DrawRay(check.position, -check.right * lenght);
    }
}

