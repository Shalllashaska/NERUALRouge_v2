using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Look : MonoBehaviour
{
    #region Variables
    
    [Header("---Camera Settings---")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform orientation;
    
    [Header("---Mouse Settings---")]
    public float sensX = 100f;
    public float sensY = 100f;
    

    private float mouseY;
    private float mouseX;
    
    private float mult = 0.01f;
    
    private float xRotation;
    private float yRotation;

    #endregion

    #region SystemMethods

    private void Start()
    {
        yRotation = transform.rotation.y;
        playerCamera = GameObject.Find("CameraHolder").transform;
        ToggleCursorMode();
    }

    private void Update()
    {
        
        MyInput();
        
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation,  yRotation, 0);
        orientation.transform.rotation = Quaternion.Euler(0, yRotation,0);
    }

    #endregion

    #region MyPrivateMethods

    private void MyInput()
    {
        mouseX = Input.GetAxisRaw("Mouse X");
        mouseY = Input.GetAxisRaw("Mouse Y");
       
        yRotation += mouseX * sensX * mult;
        xRotation -= mouseY * sensY * mult;

        xRotation = Mathf.Clamp(xRotation, -80, 85);
    }
    
    private void ToggleCursorMode()
    {
        

        if (Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
        Cursor.visible = !Cursor.visible;
    }
    
    

    #endregion
    
}
