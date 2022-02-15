using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlayer : MonoBehaviour
{
    void Start()
    {
        GameObject.Find("Player").transform.position = transform.position;
    }
    
}
