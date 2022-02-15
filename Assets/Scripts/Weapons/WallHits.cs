using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

public class WallHits : MonoBehaviour
{
    public GameObject particlesPrefab;

    public void OnHit(RaycastHit hit)
    {
        GameObject particles = Instantiate(particlesPrefab, hit.point,
            Quaternion.LookRotation(hit.normal), transform.root.parent);
    }
}
