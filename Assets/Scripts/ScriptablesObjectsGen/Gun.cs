using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
public class Gun : ScriptableObject
{
    public int typeOfGun = 0; // 0 - Raycast, 1 - many raycast, 2 - Shotguns raycast, 3 - melee
    public int level;
    public string nameOfGun = "New Gun";
    public float damage = 20;
    public float reloadTime = 2f;
    public int ammo = 100;
    public int clipsize = 20;
    public float fireRate = 0.7f;
    public float recoilX = 0.5f;
    public float recoilY = 05f;
    public float recoilZ = 05f;
    public float snappiness = 0.4f;
    public float returnSpeed = 0.3f;
    public float bloom = 10f;
    public float bloomWhenIsAiming = 5f;
    public float aimSpeed = 10f;
    public int pallets = 0;
    public GameObject prefabGun;
    public GameObject prefabBullet;
    public GameObject prefabMuzzleFlash;
    public GameObject prefabCrosshair;
    public bool aiming = true;
    public bool allowButtonHold = false;
    
    
    public AudioClip gunShotSound;
    public AudioClip reloadSound;
    public float pitchRand = 3f;
    public float volumeShot = 1f;
    public float volumeReload = 1f;


    
}