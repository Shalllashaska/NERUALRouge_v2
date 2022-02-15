using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;


public class Recoil : MonoBehaviour
{

    public float recoilX;
    public float recoilY;
    public float recoilZ;

    public float snappiness;
    public float returnSpeed;

    private Vector3 _currentRotation;
    private Vector3 _targetRotation;

    public bool aiming;
    
    void Start()
    {
        
    }
    
    void Update()
    {
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(_currentRotation);
    }

    private void FixedUpdate()
    {
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, snappiness * Time.fixedDeltaTime);
    }

    public void RecoilFire()
    {
        if (aiming)
        {
            _targetRotation += new Vector3(-recoilX / 2, Random.Range(-recoilY, recoilY)/ 2, Random.Range(-recoilZ, recoilY)/ 2);
        }
        else
        {
            _targetRotation += new Vector3(-recoilX , Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilY));
        }
    }
    
    public void SetRecoil(float x, float y, float z)
    {
        recoilX = x;
        recoilY = y;
        recoilZ = z;
    }
}
