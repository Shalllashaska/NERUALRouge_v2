using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class BulletRaycastMoves : MonoBehaviour
{
    public float speed;
    public LayerMask canBeShoot;
    private RaycastHit hit; 

    private void Start()
    {
        Destroy(gameObject, 1f);
        hit = new RaycastHit();
    }

    private void Update()
    {
        gameObject.transform.position += ( gameObject.transform.forward * Time.fixedDeltaTime * speed);
        if (Physics.Raycast(gameObject.transform.position, gameObject.transform.forward, out hit, 1.5f, canBeShoot))
        {
            Destroy(gameObject);
        }
        if (Physics.Raycast(gameObject.transform.position, -gameObject.transform.forward, out hit, 1.5f, canBeShoot))
        {
            Destroy(gameObject);
        }
        
    }

    
}
