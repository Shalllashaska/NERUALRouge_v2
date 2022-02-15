using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AimTestScript : MonoBehaviour
{
    public bool playerFound = false;
    public WeaponManagerEnemy manager;
    public LayerMask viewMask;
    public Transform playerPos;
    private RotationOrient _orient;
    public Transform _orientation;
    public float timeBetweenShots = 2f;

    private float _currentTimeBetweenShots = 0f;
    private PlayerStats _playerStats;

    public bool _isDead = false;

    private void Awake()
    {
        playerPos = GameObject.Find("Player").transform;
    }

    void Start()
    {
        playerPos = GameObject.Find("Player").transform;
        _orient = gameObject.GetComponent<RotationOrient>();
        _playerStats = GameObject.Find("Player").GetComponent<PlayerStats>();
        timeBetweenShots -= (_playerStats.strength - 3) / 10;
        _orient.speedRot += (_playerStats.agility - 3) / 10;
    }

    // Update is called once per frame
    void Update()
    {
        if (_isDead) return;
        if (playerFound)
        {
            _orient.Rotate();
            if (!Physics.Linecast(_orientation.position, playerPos.position, viewMask))
            {
                if (manager)
                {
                    if (_currentTimeBetweenShots <= 0)
                    {
                        manager.Shoot();
                        _currentTimeBetweenShots = timeBetweenShots;
                    }
                }
            }
        }

        if (_currentTimeBetweenShots > 0)
        {
            _currentTimeBetweenShots -= Time.deltaTime;
        }
    }
}
