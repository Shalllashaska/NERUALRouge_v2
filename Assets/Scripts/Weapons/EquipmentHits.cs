using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentHits : MonoBehaviour
{
    public Rigidbody rb;
    public GameObject particlesPrefab;

    private float _force = 4f;
    
    public void OnHit(RaycastHit hit)
    {
        GameObject particles = Instantiate(particlesPrefab, hit.point,
            Quaternion.LookRotation(hit.normal), hit.collider.transform);
        
        rb.AddForce(-hit.normal * _force, ForceMode.Impulse);
    }
}
