using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class ResourcesArmor : MonoBehaviour
{
    public int typeOfArmor;
    public string nameOfArmor;
    public Armor prefabOfArmor;
    [Range(0, 1)] public float maxHealthArmor;
    private float _currentHealthArmor;
    public bool init = false;

    private void Start()
    {
        
    }

    public void Initialize()
    {
        _currentHealthArmor = maxHealthArmor;
        init = true;
    }
    
    public float GetCurHealthArmor()
    {
        return _currentHealthArmor;
    }

    public void HitDamageArmor(float damage)
    {
        _currentHealthArmor -= damage;
    }

    public void NewArmor(ResourcesArmor newArm)
    {
        typeOfArmor = newArm.typeOfArmor;
        nameOfArmor = newArm.nameOfArmor;
        prefabOfArmor = newArm.prefabOfArmor;
        maxHealthArmor = newArm.maxHealthArmor;
        _currentHealthArmor = newArm.GetCurHealthArmor();
        init = newArm.init;
    }
}
