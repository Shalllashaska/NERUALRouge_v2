using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float health = 100f;
    public ArmorMenager armorOfGuard;

    private bool _scriptInit = false;

    private float _currentHealth;

    private bool _isDead = false;
    
    private Image _hitmarkerImage;
    private float _hitmarkerWaitMax = 0.3f;
    private float _hitmarkerWait;

    private void Start()
    {
        _hitmarkerImage = GameObject.Find("Canvas/Hitmarker/Image").GetComponent<Image>();
        _hitmarkerImage.color = new Color(1, 1, 1, 0);
    }

    void Update()
    {
        if (!_scriptInit)
        {
            if (armorOfGuard.IsInit())
            {
                if (armorOfGuard.GetHealthMult() > 0)
                {
                    _currentHealth = health + armorOfGuard.GetHealthMult();
                }

                _scriptInit = true;
            }
            else
            {
                return;
            }
        }
        
        if (_hitmarkerImage.color == Color.white && _hitmarkerWait <=0)
        {
            _hitmarkerWait = _hitmarkerWaitMax;
        }
        
        if (_hitmarkerWait > 0)
        {
            _hitmarkerWait -= Time.deltaTime;
        }
        
        _hitmarkerImage.color = new Color(1, 1, 1, _hitmarkerWait / _hitmarkerWaitMax);
    }

    public void Damage(float damage)
    {
        if (_isDead) return;
        Debug.Log("Damage: " + damage);
        _hitmarkerImage.color = Color.white;
        _currentHealth -= damage;
        UpdateHealth();
        Debug.Log("Health: " + _currentHealth);
        _hitmarkerWait = _hitmarkerWaitMax;
    }

    private void UpdateHealth()
    {
        if (_currentHealth <= 0)
        {
            _isDead = true;
            SendDeadMessage();
        }
        if (_currentHealth <= 40)
        {
            gameObject.GetComponent<Guard>()._isToLowHealth = true;
        }
    }

    private void SendDeadMessage()
    {
        gameObject.GetComponent<Guard>()._isDead = true;
        gameObject.GetComponent<AimTestScript>()._isDead = true;
        gameObject.GetComponent<RotationOrient>()._isDead = true;
    }
}
