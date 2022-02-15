using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float health = 100f;
    public Text textHealth;
    

    private PlayerStats _playerStats;
    [SerializeField]
    private float _currentHealth;
    private float _currentHealthMult;

    private bool _enabledRecoveryHealth = false;
    private bool _enabledShield = false;

    private float _timeToRecovery = 5f;
    private float _currentTime;
    
    private float _timeBetweenRecovery = 1.5f;
    private float _currentTimeBetweenRecovery = 0;

    private float _prevHealth;

    private Text _textShield;
    private Color _textShieldColor;
    private Image _shieldBar;
    private Transform _shieldBarTr;
    private Color _shieldColor;
    
    private Text _textShieldRecovery;
    private Color _textShieldColorRecovery;
    private Image _shieldBarRecovery;
    private Transform _shieldBarTrRecovery;
    private Color _shieldColorRecovery;

    private bool _recovering;

    private bool _shieldIsActive = false;
    private float _currentShiedTime;
    private float _currentShield;
    private float _shieldMax;
    private float _currentShieldRecoveryTime;
    private float _shieldRecoveryTime;
    private bool _shieldIsReloading = false;
    private Color _textEndRecoverColor = new Color(0.6f, 0.4f, 0.145f);


    private void Start()
    {
        textHealth = GameObject.Find("Canvas/HealthTest").gameObject.GetComponent<Text>();
        
        _textShield = GameObject.Find("Canvas/TextShield").gameObject.GetComponent<Text>();
        _shieldBar = GameObject.Find("Canvas/Shield").gameObject.GetComponent<Image>();
        _shieldBarTr = _shieldBar.gameObject.transform;
        _shieldColor = _shieldBar.color;
        _shieldBar.color = new Color(0, 0, 0, 0);
        _textShieldColor = _textShield.color;
        _textShield.color = new Color(0, 0, 0, 0);
        
        _textShieldRecovery = GameObject.Find("Canvas/TextShieldRecovery").gameObject.GetComponent<Text>();
        _shieldBarRecovery = GameObject.Find("Canvas/ShieldRecovery").gameObject.GetComponent<Image>();
        _shieldBarTrRecovery =  _shieldBarRecovery.gameObject.transform;
        _shieldColorRecovery =  _shieldBarRecovery.color;
        _shieldBarRecovery.color = new Color(0, 0, 0, 0);
        _textShieldColorRecovery = _textShieldRecovery.color;
        _textShieldRecovery.color = new Color(0, 0, 0, 0);

        _playerStats = gameObject.GetComponent<PlayerStats>();
        UpdateStats();
        _currentHealth = health;
        _prevHealth = _currentHealth;
        UpdateHealth();
    }

    public void Damage(float damage)
    {
        if (_shieldIsActive) return;
        _currentHealth -= damage;
        _recovering = false;
        UpdateHealth();
    }

    private void UpdateHealth()
    {
        textHealth.text = _currentHealth.ToString("0.000");
        if (_currentHealth <= 0)
        {
            //Debug.Log("You dead");
        }
    }

    private void Update()
    {
        if (_enabledRecoveryHealth)
        {
            if ((_currentHealth < health / 3) && (_prevHealth == _currentHealth))
            {
                _currentTime -= Time.deltaTime;
            }
            else
            {
                _currentTime = _timeToRecovery;
                _prevHealth = _currentHealth;
            }
            if (_currentTime <= 0)
            {
                _recovering = true;
            }

            if (_currentTimeBetweenRecovery > 0)
            {
                _currentTimeBetweenRecovery -= Time.deltaTime;
            }
            
            if (_recovering && _currentTimeBetweenRecovery <=0)
            {
                RecoveryHealth();
                _currentTimeBetweenRecovery = _timeBetweenRecovery;
            }
        }

        if (Input.GetKeyDown(KeyCode.B) && _enabledShield && !_shieldIsActive && !_shieldIsReloading)
        {
            Shield();
        }

        if (_enabledShield)
        {
            if (_currentShiedTime > 0)
            {
                _currentShiedTime -= Time.deltaTime;
                _currentShield = Mathf.Max(0, _currentShiedTime - Time.deltaTime);
                _shieldBarTr.localScale = new Vector3(_currentShield / _shieldMax, 1,
                    1);
                _textShield.color = new Color(_textShieldColor.r, _textShieldColor.g, _textShieldColor.b,
                    _currentShield / _shieldMax);
            }
            else
            {
                if (!_shieldIsReloading && _shieldIsActive)
                {
                    _currentShieldRecoveryTime = _shieldRecoveryTime;
                    _shieldIsReloading = true;
                }
                _shieldIsActive = false;
            }

            if (_currentShieldRecoveryTime > 0)
            {
                _currentShieldRecoveryTime -= Time.deltaTime;
                float t = (_shieldRecoveryTime - _currentShieldRecoveryTime)/_shieldRecoveryTime ;
                _shieldBarTrRecovery.localScale = new Vector3( t, 1,
                    1);
                _shieldBarRecovery.color = new Color(_shieldColorRecovery.r, _shieldColorRecovery.g,
                    _shieldColorRecovery.b, t);
                _textShieldRecovery.color = new Color(_textShieldColorRecovery.r, _textShieldColorRecovery.g,
                    _textShieldColorRecovery.b, t);
                
            }
            else
            {
                _shieldIsReloading = false;
                if (!_shieldIsActive)
                {
                    _textShieldRecovery.color = _textEndRecoverColor;
                }
                else
                {
                    _textShieldRecovery.color = new Color(0, 0, 0, 0);
                }
            }
        }
    }


    private void RecoveryHealth()
    {
        if ((_currentHealth < health / 4))
        {
            if ((_currentHealth + (_playerStats.strength / 3)) >= health / 4)
            {
                _currentHealth = health / 4;
                _recovering = false;
                UpdateHealth();
            }
            else
            {
                _currentHealth += _playerStats.strength / 3;
                UpdateHealth();
            }
        }
        
    }

    private void Shield()
    {
        _shieldIsActive = true;
        _shieldMax = 7 + (_playerStats.strength - 7) * 2;
        _currentShiedTime =  _shieldMax;
        _shieldBar.color = _shieldColor;
        _textShield.color = _textShieldColor;
        _shieldBarRecovery.color = new Color(0, 0, 0, 0);
        _textShieldRecovery.color = new Color(0, 0, 0, 0);
        _shieldRecoveryTime = 180 - (_playerStats.strength - 7) * 20;
    }

    private void UpdateStats()
    {
        _currentHealthMult = (_playerStats.strength - 3) * _playerStats.goodMultHealth -
                             (_playerStats.stealth - 3) * _playerStats.badMultHealth;
        health += _currentHealthMult;

        if (_playerStats.strength > 4)
        {
            _enabledRecoveryHealth = true;
        }
        else
        {
            _enabledRecoveryHealth = false;
        }

        if (_playerStats.strength > 6)
        {
            _enabledShield = true;
            _shieldBarRecovery.color = _shieldColorRecovery;
            _textShieldRecovery.color = _textEndRecoverColor;
        }
        else
        {
            _enabledShield = false;
        }
    }
}
