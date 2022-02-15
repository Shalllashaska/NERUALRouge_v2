using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class ArmorMenager : MonoBehaviour
{
    public Armor[] heads;
    public Armor[] bodies;

    public int currentLevel = 1;

    private float _currentSpeedMult;
    private float _currentHealthMult;
    private float _currentDamageMult;

    private Armor _currentHeadData;
    private Armor _currentBodyData;

    private bool init = false;

    private PlayerStats _plStats;

    private float[] _lvlChances = {0.55f, 0.75f, 085f}; //spawn of armor
    
    private float[,] _weightsHead =
    {
        //stnd   eng   combat
        {0.60f, 0.30f, 0.10f}, //lvl 1
        {0.15f, 0.60f, 0.25f}, //lvl 2
        {0.15f, 0.15f, 0.70f}, //lvl 3
    };
    
    private float[,] _weightsBody =
    {
        //stnd   eng   combat
        {0.60f, 0.30f, 0.10f}, //lvl 1
        {0.15f, 0.60f, 0.25f}, //lvl 2
        {0.15f, 0.15f, 0.70f}, //lvl 3
    };
    

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _plStats = GameObject.Find("Player").GetComponent<PlayerStats>();
        
        if (heads.Length > 0 && bodies.Length > 0)
        {
            CalculateChances();
        }
        else
        {
            _currentHeadData = ScriptableObject.CreateInstance<Armor>();
            _currentHeadData.damageMult = 0;
            _currentHeadData.healthMult = 0;
            _currentHeadData.speedMult = 0;
            
            _currentBodyData = ScriptableObject.CreateInstance<Armor>();
            _currentBodyData.damageMult = 0;
            _currentBodyData.healthMult = 0;
            _currentBodyData.speedMult = 0;
        }
        
        _currentDamageMult = _currentHeadData.damageMult + _currentBodyData.damageMult;
        _currentHealthMult = _currentHeadData.healthMult + _currentBodyData.healthMult;
        _currentSpeedMult = _currentHeadData.speedMult + _currentBodyData.speedMult;
        init = true;
    }

    private void CalculateChances() //Считает щансы спавна брони взависимости от уровня, сначала щас будет ли спавниться броня, затем щанс конкретной брони
    {
        float iH= Random.Range(0f, 1f);
        float jH = Random.Range(0f, 1f);
        float iB= Random.Range(0f, 1f);
        float jB = Random.Range(0f, 1f);
        float a = Math.Min(_lvlChances[currentLevel-1] + (_plStats.stealth - 3) / 10, 1);
        
        if (iH <= a)
        {
            float stndBound = _weightsHead[currentLevel-1,0];
            float engBound = stndBound + _weightsHead[currentLevel-1,1];
            float combBound = engBound + _weightsHead[currentLevel-1,2];
            if (jH <= stndBound)
            {
                _currentHeadData = heads[0];
            }
            else if (jH >= stndBound && jH < engBound)
            {
                _currentHeadData = heads[1];
            }
            else if (jH >= engBound && jH < combBound)
            {
                _currentHeadData = heads[2];
            }
            SpawnPartOfArmor(_currentHeadData);
        }
        else
        {
            _currentHeadData = ScriptableObject.CreateInstance<Armor>();
            _currentHeadData.damageMult = 0;
            _currentHeadData.healthMult = 0;
            _currentHeadData.speedMult = 0;
        }

        if (iB <= a)
        {
            float stndBound = _weightsBody[currentLevel-1,0];
            float engBound = stndBound + _weightsBody[currentLevel-1,1];
            float combBound = engBound + _weightsBody[currentLevel-1,2];
            
            if (jB <= stndBound)
            {
                _currentBodyData = bodies[0];
            }
            else if (jB >= stndBound && jB < engBound)
            {
                _currentBodyData = bodies[1];
            }
            else if (jB >= engBound && jB <= combBound)
            {
                _currentBodyData = bodies[2];
            }
            SpawnPartOfArmor(_currentBodyData);
        }
        else
        {
            _currentBodyData = ScriptableObject.CreateInstance<Armor>();
            _currentBodyData.damageMult = 0;
            _currentBodyData.healthMult = 0;
            _currentBodyData.speedMult = 0;
        }
    }
    
    
    private void SpawnPartOfArmor(Armor arm)
    {
        Instantiate(arm.armorPrefab, gameObject.transform);
    }

    public float GetSpeedMult()
    {
        return _currentSpeedMult;
    }
    public float GetHealthMult()
    {
        return _currentHealthMult;
    }
    public float GetDamageMult()
    {
        return _currentDamageMult;
    }

    public bool IsInit()
    {
        return init;
    }
}
