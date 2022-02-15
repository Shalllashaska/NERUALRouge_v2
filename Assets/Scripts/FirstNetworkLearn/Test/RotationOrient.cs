using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class RotationOrient : MonoBehaviour
{
    public Transform orientation;
    public Transform playerPos;
    public float speedRot = 4f;

    [HideInInspector]
    public bool _isDead = false;

    private void Start()
    {
        playerPos = GameObject.Find("Player").transform;
    }

    void Update()
    {
        
    }


    public void Rotate()
    {
        Vector3 targetPosition = playerPos.position - transform.position;
        Vector3 orientTargetRot = playerPos.position - orientation.position;
        Quaternion rotation = Quaternion.LookRotation(targetPosition);
        rotation.x = 0;
        rotation.z = 0;
        Quaternion rotOr = Quaternion.LookRotation(orientTargetRot);
        orientation.rotation = Quaternion.Lerp(orientation.rotation, rotOr, speedRot * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, speedRot * Time.deltaTime);
    }
}
