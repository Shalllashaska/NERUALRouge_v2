using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class WeaponManager : MonoBehaviour
{
    #region Variables
    [Header("---All weapons---")]
    public Gun[] loadout;
    public LayerMask canTake;
    
    [Header("---Weapon holder Settings---")]
    public Transform weaponHolder;
    public PlayerStats player;
    public Recoil currentRecoil;

    [Header("---Camera settings---")]
    public Camera cam;
    public Transform cameraHolder;
    public Transform spawn;
    public GameObject textTakeGun;
    public Text ammoText;
    public CanTake cnt;

    [Header("---Enemy and walls---")]
    public LayerMask canBeShot;
    

    private Transform _anchor, _stateHip, _stateAds;
    private int _currentInd = 0;
    private GameObject _currentWeapon;
    private ResourcesHolder _currentResources;
    private GameObject _currentMuzzleFlash;
    private GameObject _currentBulletPref;
    private Gun _currentGunData;
    private Transform _currentAttackPoint;
    private float _cooldown;
    private float _currentCooldown;
    private float _currentReloadCooldown = 0f; 
    private float _currentDamage;
    private float _currentDamageMult;
    private Rigidbody _currentRigidbodyGun;
    private bool _canAiming;
    private bool _cantShoot;
    private ResourcesHolder[] _weaponResourcesHolders = new ResourcesHolder[4];




    public bool aiming = false;
    
    #endregion

    #region System Methods

    private void Awake()
    {
        player = GameObject.Find("Player").GetComponent<PlayerStats>();
        ammoText = GameObject.Find("Canvas/Ammo").GetComponent<Text>();
        textTakeGun = GameObject.Find("Canvas/TakeGun");
        textTakeGun.SetActive(false);
        for (int i = 0; i < loadout.Length; i++)
        {
            ResourcesHolder newRes = ScriptableObject.CreateInstance<ResourcesHolder>();
            newRes.Initialize(loadout[i].ammo, loadout[i].clipsize);
            _weaponResourcesHolders[i] = newRes;
        }
        Equip(0);
    }

    private void Update()
    {
        MyInput();
    }

    #endregion

    #region My Private Methods

    private void MyInput()
    {
        if (_currentWeapon != null)
        {
            if (_canAiming)
            {
                aiming = Input.GetMouseButton(1);
                currentRecoil.aiming = aiming;
                Aim(aiming);
            }

            if(Input.GetKeyDown(KeyCode.G))
            {
                DropGun();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (_currentReloadCooldown <= 0 && (_currentResources.GetClip() < _currentGunData.clipsize))
                {
                    Reload();
                }
            }
            
            if (_currentGunData.allowButtonHold && _currentGunData != null)
            {
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    Shoot();
                }
            }
            else if(!_currentGunData.allowButtonHold && _currentGunData != null)
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    Shoot();
                }
            }
        }
        
        textTakeGun.SetActive(CanTakeGun());
        if (CanTakeGun())
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                TakeGun();
            }
        }

        //Switch the guns
        if (Input.GetKeyDown(KeyCode.Alpha1) && loadout.Length > 0)
        {
            if (loadout[0] != null  && _currentInd != 0)
            {
                Equip(0);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && loadout.Length > 1)
        {
            if (loadout[1] != null && _currentInd != 1)
            {
                Equip(1);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && loadout.Length > 2)
        {
            if (loadout[2] != null  && _currentInd != 2)
            {
                Equip(2);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) && loadout.Length > 3)
        {
            if (loadout[3] != null && _currentInd != 3)
            {
                Equip(3);
            }
        }
        if (_currentCooldown > 0) _currentCooldown -= Time.deltaTime;
        if (_currentReloadCooldown > 0)
        {
            _currentReloadCooldown -= Time.deltaTime;
        }
        else
        {
            UpdateAmmo();
        }
        
        
    }
    
    public void Equip(int ind)
    {
        if (_currentWeapon != null)
        {
            Destroy(_currentWeapon);
        }
        
        GameObject newWeapon =
            Instantiate(loadout[ind].prefabGun, weaponHolder.position, weaponHolder.rotation, weaponHolder);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;
        _currentInd = ind;
        _currentGunData = loadout[_currentInd];
        _currentResources = _weaponResourcesHolders[_currentInd];
        _currentWeapon = newWeapon;
        _currentWeapon.layer = 6;
        currentRecoil.snappiness = _currentGunData.snappiness;
        currentRecoil.returnSpeed = _currentGunData.returnSpeed;
        currentRecoil.SetRecoil(_currentGunData.recoilX,_currentGunData.recoilY,_currentGunData.recoilZ);
        Transform model = _currentWeapon.transform.Find("Anchor/Model");
        for (int i = 0; i < model.childCount; i++)
        {
            model.GetChild(i).gameObject.layer = 6;
        }

        _currentReloadCooldown = 0;
        _currentMuzzleFlash = _currentGunData.prefabMuzzleFlash;
        _currentBulletPref = _currentGunData.prefabBullet;
        _currentAttackPoint = _currentWeapon.transform.Find("Anchor/AttackPoint");
        _currentRigidbodyGun = _currentWeapon.transform.GetComponent<Rigidbody>();
        _currentRigidbodyGun.isKinematic = true;
        _currentRigidbodyGun.detectCollisions = false;
        _anchor = _currentWeapon.transform.Find("Anchor");
        _anchor.gameObject.layer = 6;
        _stateAds = _currentWeapon.transform.Find("States/ADS");
        _stateHip = _currentWeapon.transform.Find("States/HIP");
        _cooldown = _currentGunData.fireRate;
        _canAiming = _currentGunData.aiming;
        _currentDamage = _currentGunData.damage;
        if (player != null)
        {
            UpdateStats();
        }

        if (_currentResources != null)
        {
            UpdateAmmo();
        }
        _currentCooldown = 0;
    }

    public void UpdateStats()
    {
        _currentDamageMult = (player.stealth - 3) * player.goodMultDamage - (player.agility - 3) * player.badMultDamage;
        _currentDamage += _currentDamageMult;
    }

    public void Shoot()
    {
        /*sfx.Stop();
        sfx.clip = _currentGunData.gunShotSound;
        sfx.pitch = 1 - _currentGunData.pitchRand +
                    Random.Range(-_currentGunData.pitchRand, _currentGunData.pitchRand);
        sfx.volume = _currentGunData.volumeShot;
        sfx.Play();*/
        if (_currentCooldown <= 0)
        {
            if (_currentGunData.typeOfGun == 1)
            {
                if (CanShoot())
                {
                    if (_currentReloadCooldown <= 0)
                    {
                        if (_currentResources.FireBullet())
                        {
                            UpdateAmmo();
                            ShootManyRaycast();
                        }
                        else
                        {
                            Reload();
                        }
                    }
                }
            }
            else if (_currentGunData.typeOfGun == 0)
            {
                if (_currentReloadCooldown <= 0)
                {
                    if (_currentResources.FireBullet())
                    {
                        UpdateAmmo();
                        ShootRaycast();
                    }
                    else
                    {
                        Reload();
                    }
                }
            }
        }
    }
    
    private void Aim(bool isAiming)
    {
        if(isAiming)
        {
            _anchor.position = Vector3.Lerp(_anchor.position, _stateAds.position,
                Time.deltaTime * _currentGunData.aimSpeed);
        }
        else
        {
            _anchor.position = Vector3.Lerp(_anchor.position, _stateHip.position,
                Time.deltaTime * _currentGunData.aimSpeed);
        }
    }
    
    private void UpdateAmmo()
    {
        ammoText.text = _currentResources.GetClip().ToString("00") + "/" + _currentResources.GetStash().ToString("00");
    }
    
    private void Reload()
    {
        _currentResources.Reload();
        _currentReloadCooldown = _currentGunData.reloadTime;
    }

    private void ShootRaycast()
    {
        SpawnMuzzleFlash();
        for (int i = 0; i < Mathf.Max(1, _currentGunData.pallets); i++)
        {
            Vector3 bloom = spawn.position + spawn.forward * 1000f;
            if (aiming)
            {
                bloom += Random.Range(-_currentGunData.bloomWhenIsAiming, _currentGunData.bloomWhenIsAiming) * spawn.up;
                bloom += Random.Range(-_currentGunData.bloomWhenIsAiming, _currentGunData.bloomWhenIsAiming) *
                         spawn.right;
                bloom -= spawn.position;
                bloom.Normalize();
            }
            else
            {
                bloom += Random.Range(-_currentGunData.bloom, _currentGunData.bloom) * spawn.up;
                bloom += Random.Range(-_currentGunData.bloom, _currentGunData.bloom) * spawn.right;
                bloom -= spawn.position;
                bloom.Normalize();
            }
            RaycastHit hit = new RaycastHit();
            GameObject currentBullet = Instantiate(_currentGunData.prefabBullet, _currentAttackPoint.position,
                Quaternion.identity);
            currentBullet.transform.forward = bloom;
            if (Physics.Raycast(spawn.position, bloom, out hit, 100f, canBeShot))
            {

                EnemyHits enemy = hit.transform.GetComponent<EnemyHits>();
                WallHits wall = hit.transform.GetComponent<WallHits>();
                EquipmentHits equip = hit.transform.GetComponent<EquipmentHits>();
                if (wall != null)
                {
                    wall.OnHit(hit);
                }
                else if (enemy != null)
                {
                    enemy.OnHit(hit, _currentDamage, _currentAttackPoint.position);
                }
                else if (equip != null)
                {
                    equip.OnHit(hit);
                }
            }
        }
        currentRecoil.RecoilFire();
        _currentCooldown = _cooldown;
    }


    private bool CanShoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f,
            0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 75f, canBeShot))
        {
            if (hit.distance <= 1.9f)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            return true;
        }
    }
    private void ShootManyRaycast()
    {
        if (!_cantShoot)
        {
            SpawnMuzzleFlash();
        }
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f,
            0));
        Vector3 targetPoint = ray.GetPoint(100);
        Vector3 directionWithoutSpread = targetPoint - _currentAttackPoint.position;
        float x;
        float y;
        if (aiming)
        {
            x = Random.Range(-_currentGunData.bloomWhenIsAiming / 2, _currentGunData.bloomWhenIsAiming / 2);
            y = Random.Range(-_currentGunData.bloomWhenIsAiming / 2, _currentGunData.bloomWhenIsAiming / 2);
        }
        else
        {
            x = Random.Range(-_currentGunData.bloom / 2, _currentGunData.bloom / 2);
            y = Random.Range(-_currentGunData.bloom / 2, _currentGunData.bloom / 2);
        }
        //Calculate new direction with spread
        Vector3 directionWithSpread =
            directionWithoutSpread + y * _currentAttackPoint.up +
            x * _currentAttackPoint.right; //Just add spread to last direction

        //Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(_currentBulletPref, _currentAttackPoint.position,
            Quaternion.identity); //store instantiated bullet in currentBullet
        //Rotate bullet to shoot direction

        currentBullet.transform.forward = directionWithSpread.normalized;
        ManyRaycastBullets bulletScript = currentBullet.GetComponent<ManyRaycastBullets>();
        if (bulletScript)
        {
            Vector3 save = _currentAttackPoint.forward;
            _currentAttackPoint.forward = directionWithSpread.normalized;
            bulletScript.Initialize(_currentAttackPoint, _currentDamage, _currentAttackPoint.position);
            _currentAttackPoint.forward = save;
        }
        currentRecoil.RecoilFire();
        Destroy(currentBullet, bulletScript.LifeTime);
        _currentCooldown = _cooldown;
    }

    private void SpawnMuzzleFlash()
    {
        //muzzleFlash
        GameObject muzzleFlash = Instantiate(_currentMuzzleFlash, _currentAttackPoint.position,  _currentAttackPoint.rotation * Quaternion.Euler(new Vector3(0, -90, 0)), _currentAttackPoint.transform);
    }

    private void DropGun()
    {
        _currentRigidbodyGun.isKinematic = false;
        _currentRigidbodyGun.detectCollisions = true;

        GameObject newGun = Instantiate(_currentWeapon, transform.position + transform.forward * .5f, transform.rotation);
        ResourcesHolderMono newGunRes = newGun.transform.Find("Anchor/Resources").GetComponent<ResourcesHolderMono>();
        if (newGunRes == null)
        { 
            newGunRes = newGun.transform.Find("Anchor/Resources").gameObject.AddComponent<ResourcesHolderMono>();
        }
        newGunRes.clip = _currentResources.GetClip();
        newGunRes.stash = _currentResources.GetStash();
        
        Rigidbody rb = newGun.GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * 0.5f, ForceMode.Impulse);
        Transform model = newGun.transform.Find("Anchor/Model");
        Transform anchor = newGun.transform.Find("Anchor");
        anchor.gameObject.layer = 13;
        for (int i = 0; i < model.childCount; i++)
        {
            model.GetChild(i).gameObject.layer = 13;
        }
        newGun.layer = 13;
        _currentRigidbodyGun.isKinematic = true;
        _currentRigidbodyGun.detectCollisions = false;
        Destroy(_currentWeapon);
        _currentWeapon = null;
        loadout[_currentInd] = null;
        
    }

    private bool CanTakeGun()
    {
        return cnt.CanTakeFunc();
    }
    
    private void TakeGun()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 3.3f, canTake))
        {
          
            ResourcesGun newRGun = hit.collider.GetComponent<ResourcesGun>();
            ResourcesHolderMono newRHolderGun = hit.collider.transform.Find("Anchor/Resources").GetComponent<ResourcesHolderMono>();
            Gun newGun = newRGun.thisGun;
            if (newGun)
            {
                if (loadout[_currentInd] == null)
                {
                    loadout[_currentInd] = newGun;
                    if (newRHolderGun == null)
                    {
                        ResourcesHolder newHolder = ScriptableObject.CreateInstance<ResourcesHolder>();
                        newHolder.Initialize(loadout[_currentInd].ammo,loadout[_currentInd].clipsize);
                        _weaponResourcesHolders[_currentInd] = newHolder;
                    }
                    else
                    {
                        ResourcesHolder newHolder = ScriptableObject.CreateInstance<ResourcesHolder>();
                        newHolder.SetClip(newRHolderGun.clip);
                        newHolder.SetStash(newRHolderGun.stash);
                        newHolder.SetClipsize(loadout[_currentInd].clipsize);
                        _weaponResourcesHolders[_currentInd] = newHolder;
                    }
                    Equip(_currentInd);
                    Destroy(hit.collider.gameObject);
                }
                else
                {
                    for (int i = 0; i < loadout.Length; i++)
                    {
                        if (loadout[i] == null)
                        {
                            loadout[i] = newGun;
                            if (newRHolderGun == null)
                            {
                                ResourcesHolder newHolder = ScriptableObject.CreateInstance<ResourcesHolder>();
                                newHolder.Initialize(loadout[i].ammo,loadout[i].clipsize);
                                _weaponResourcesHolders[i] = newHolder;
                            }
                            else
                            {
                                ResourcesHolder newHolder = ScriptableObject.CreateInstance<ResourcesHolder>();
                                newHolder.SetClip(newRHolderGun.clip);
                                newHolder.SetStash(newRHolderGun.stash);
                                newHolder.SetClipsize(loadout[i].clipsize);
                                _weaponResourcesHolders[i] = newHolder;
                            }
                            Equip(i);
                            Destroy(hit.collider.gameObject);
                            break;
                        }
                    }
                }
            }
        }
    }
    #endregion
}
