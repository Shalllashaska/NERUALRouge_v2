using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
   [SerializeField] private Transform cameraPosition;

   private void Start()
   {
      cameraPosition = GameObject.Find("Player/Camera Position").transform;
   }

   private void Update()
   {
      transform.position = cameraPosition.position;
   }
}
