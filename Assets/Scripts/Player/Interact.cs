using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour
{
    public bool activePresentation = true;
    public float distanceToInteract = 3.3f;

    private GameObject interact;
    private RaycastHit _hit;
    private Ray _ray;

    private void Start()
    {
        interact = GameObject.Find("Canvas/Interact");
        interact.SetActive(false);
    }

    void Update()
    {
        Ray();
        DoorInteract();
    }

    private void Ray()
    {
        _ray = new Ray(transform.position, transform.forward);
        Physics.Raycast(_ray, out _hit, distanceToInteract);
    }


    private void DoorInteract()
    {

        if (_hit.transform != null && _hit.transform.GetComponent<DoorScript>())
        {
            interact.SetActive(true);
            if (Input.GetKey(KeyCode.E))
            {
                _hit.transform.GetComponent<DoorScript>().Open();
            }
        }
        else
        {
            interact.SetActive(false);
        }
    }
}
