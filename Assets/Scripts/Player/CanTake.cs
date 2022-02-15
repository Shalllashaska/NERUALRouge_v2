using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanTake : MonoBehaviour
{
    public LayerMask canTake;

    private void Start()
    {
        
    }

    public bool CanTakeFunc()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 3.3f, canTake))
        {
            return true;
        }
        return false;
    }
}
