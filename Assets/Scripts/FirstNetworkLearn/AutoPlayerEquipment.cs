using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AutoPlayerEquipment : MonoBehaviour
{
    private int[,] learnEquipMass =
    {
        {0, 1, 2, 3,  0,  1,  2,  3, 2, 2, 2, 1, 1, 1,},
        {3, 3, 3, 3, 10, 10, 10, 10, 7, 4, 4, 9, 2, 2,},
        {3, 3, 3, 3, 10, 10, 10, 10, 4, 7, 4, 2, 2, 9,},
        {3, 3, 3, 3, 10, 10, 10, 10, 4, 4, 7, 2, 9, 2,},
    };

    public Gun[] allWeapons;

    private void Start()
    {
        int i = Random.Range(0, learnEquipMass.GetLength(1));
        PlayerStats ps = gameObject.GetComponent<PlayerStats>();
        ps.strength = learnEquipMass[1, i];
        ps.stealth = learnEquipMass[2, i];
        ps.agility = learnEquipMass[3, i];
        Controls pc = gameObject.GetComponent<Controls>();
        WeaponManager wp = GameObject.Find("CameraHolder/PlayerCamera/WeaponHolder").GetComponent<WeaponManager>();
        wp.loadout[0] = allWeapons[learnEquipMass[0, i]];
        wp.Equip(0);
        pc.UpdateStats();
        wp.UpdateStats();
    }
}
