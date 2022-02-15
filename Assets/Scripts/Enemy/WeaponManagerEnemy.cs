using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WeaponManagerEnemy : MonoBehaviour
{
    #region Variables
    
    public int currentLevel = 1;

    [Header("---All weapons---")] 
    public Gun[] loadout;

    [Header("---Weapon holder Settings---")]
    public Transform weaponHolder;
    public ArmorMenager armor;
    public Recoil currentRecoil;

    [Header("---View settings---")] 
    public Transform View;
    public Transform spawn;

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
    private float _currentReloadCooldown;
    private float _currentDamage;
    private float _currentDamageMult;
    private bool _cantShoot;
    private ResourcesHolder[] _weaponResourcesHolders = new ResourcesHolder[4];

    [HideInInspector]
    public bool _isDead = false;
    
    private float[,] _weightsWeapon =
    { 
        //pistol revolver shotgun rifle
        {0.70f,  0.5f,    0.10f,   0.20f,}, //lvl 1
        {0.15f,  0.20f,   0.15f,  0.50f,}, //lvl 2
        {0.10f,   0.20f,   0.55f,  0.20f,}, //lvl 3
    };

    private int _numWeapon;
    private int _lenghtWeapWeights;
    #endregion

    #region System Methods

    private void Start()
    {
        _lenghtWeapWeights = _weightsWeapon.GetLength(1);
        CalculateChances(0);
        
        ResourcesHolder newRes = ScriptableObject.CreateInstance<ResourcesHolder>();
        newRes.Initialize(loadout[_numWeapon].ammo, loadout[_numWeapon].clipsize);
        _weaponResourcesHolders[_numWeapon] = newRes;
        Equip(_numWeapon);
    }

    private void Update()
    {
        MyInput();
    }

    #endregion

    #region My Private Methods

    private void MyInput()
    {
        if (_currentCooldown > 0) _currentCooldown -= Time.deltaTime;
        if (_currentReloadCooldown > 0) _currentReloadCooldown -= Time.deltaTime;
    }

    private void CalculateChances(int j)
    {
        if (j == _lenghtWeapWeights - 1)
        {
            _numWeapon = j;
            return;
        }
        float chance = _weightsWeapon[currentLevel - 1, j];
        float i = Random.Range(0f, 1f);

        if (i <= chance)
        {
            _numWeapon = j;
        }
        else
        {
            j += 1;
            CalculateChances(j);
        }

    }
    private void Equip(int ind)
    {
        GameObject newWeapon =
            Instantiate(loadout[ind].prefabGun, weaponHolder.position, weaponHolder.rotation, weaponHolder);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;
        _currentInd = ind;
        _currentGunData = loadout[_currentInd];
        _currentResources = _weaponResourcesHolders[_currentInd];
        _currentWeapon = newWeapon;
        currentRecoil.snappiness = _currentGunData.snappiness;
        currentRecoil.returnSpeed = _currentGunData.returnSpeed;
        currentRecoil.SetRecoil(_currentGunData.recoilX, _currentGunData.recoilY, _currentGunData.recoilZ);

        _currentMuzzleFlash = _currentGunData.prefabMuzzleFlash;
        _currentBulletPref = _currentGunData.prefabBullet;
        _currentAttackPoint = _currentWeapon.transform.Find("Anchor/AttackPoint");
        _anchor = _currentWeapon.transform.Find("Anchor");
        _anchor.gameObject.layer = 6;
        _stateHip = _currentWeapon.transform.Find("States/HIP");
        _cooldown = _currentGunData.fireRate;
        _currentDamageMult = armor.GetDamageMult();
        _currentDamage = _currentGunData.damage + _currentDamageMult;
        _currentCooldown = 0;
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
                if (_currentReloadCooldown <= 0)
                {
                    if (CanShoot())
                    {
                        if (_currentResources.FireBullet())
                        {
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

    private void Reload()
    {
        _currentResources.Reload();
        _currentReloadCooldown = _currentGunData.reloadTime;
    }

    private void ShootRaycast()
    {
        SpawnMuzzleFlash();
        //bloom
        for (int i = 0; i < Mathf.Max(1, _currentGunData.pallets); i++)
        {
            Vector3 bloom = spawn.position + spawn.forward * 1000f;

            bloom += Random.Range(-_currentGunData.bloom, _currentGunData.bloom) * spawn.up;
            bloom += Random.Range(-_currentGunData.bloom, _currentGunData.bloom) *
                     spawn.right;
            bloom -= spawn.position;
            bloom.Normalize();

            RaycastHit hit = new RaycastHit();
            GameObject currentBullet = Instantiate(_currentGunData.prefabBullet, _currentAttackPoint.position,
                Quaternion.identity);
            currentBullet.transform.forward = bloom;
            if (Physics.Raycast(spawn.position, bloom, out hit, 100f, canBeShot))
            {
                PlayerHits  player = hit.collider.transform.GetComponent<PlayerHits>();
                WallHits wall = hit.transform.GetComponent<WallHits>();
                if (wall != null)
                {
                    wall.OnHit(hit);
                }
                else if (player != null)
                {
                    player.OnHit(_currentDamage);
                }
            }
        }

        currentRecoil.RecoilFire();
        _currentCooldown = _cooldown;
    }


    private bool CanShoot()
    {
        Ray ray = new Ray(spawn.position, spawn.forward);
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

        Ray ray = new Ray(spawn.position, spawn.forward);
        Vector3 targetPoint = ray.GetPoint(100);
        Vector3 directionWithoutSpread = targetPoint - _currentAttackPoint.position;
        float x;
        float y;

        x = Random.Range(-_currentGunData.bloom/ 2, _currentGunData.bloom / 2);
        y = Random.Range(-_currentGunData.bloom / 2, _currentGunData.bloom/ 2);
        //Calculate new direction with spread
        Vector3 directionWithSpread =
            directionWithoutSpread + y * _currentAttackPoint.up +
            x * _currentAttackPoint.right; //Just add spread to last direction

        //Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(_currentBulletPref, _currentAttackPoint.position,
            Quaternion.identity); //store instantiated bullet in currentBullet
        //Rotate bullet to shoot direction

        currentBullet.transform.forward = directionWithSpread.normalized;
        ManyRaycastBulletsEnemy bulletScript = currentBullet.GetComponent<ManyRaycastBulletsEnemy>();
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
        GameObject muzzleFlash = Instantiate(_currentMuzzleFlash, _currentAttackPoint.position,
            _currentAttackPoint.rotation * Quaternion.Euler(new Vector3(0, -90, 0)), _currentAttackPoint.transform);
    }


    #endregion
}
