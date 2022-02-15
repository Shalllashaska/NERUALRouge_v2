using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour {
    [SerializeField]
    float rotateSpeed;

    public Transform rooms;

    new Transform transform;
    

    void Start() {
        transform = GetComponent<Transform>();
    }

    void Update() {
        transform.RotateAround(rooms.position, Vector3.up ,rotateSpeed );
    }
}
