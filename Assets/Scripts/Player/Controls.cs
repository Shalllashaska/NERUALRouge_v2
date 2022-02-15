using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using UnityEngine.VFX;

public class Controls : MonoBehaviour
{
#region Variables

    [Header("---Movement---")] 
    public bool lockMovement = false;
    public float moveSpeed = 10f;
    public float acceleration = 15f;
    public float moveMultiplier = 10f;
    public float airMultiplier = 0.1f;
    public float jumpForce = 5f;
    public Transform orientation;

    [Header("---Stairs---")] 
    public GameObject stairRayUpper;
    public GameObject stairRayLower;
    public float stepHieght = 0.3f;
    public float stepSmooths = 0.2f;

    [Header("---Crouch---")]
    public Transform ceilingCheck;
    public float crouchSpeed = 3f;
    
    [Header("---Sprint---")]
    public float sprintSpeed = 12f;
    public float strafeForce = 3f;

    [Header("---Walk---")]
    public float walkSpeed = 7f;

    [Header("---Control Drag---")]
    public float groundDrag = 6f;
    public float airDrag = 2f;
    
    [Header("---Ground---")]
    public LayerMask groundedMask;
    public Transform groundCheck;

    [Header("---Keybinds---")]
    [SerializeField]
    private KeyCode jumpKey = KeyCode.Space;
    [SerializeField]
    private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField]
    private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("---Player stats script---")] 
    public PlayerStats playerStats;

    private float _playerHeight = 2f;
    private float _playerCrouchHeight = 1f;
    private float _G = 9.8f;
    [HideInInspector]
    public float _horizontalMovement;
    [HideInInspector]
    public float _verticalMovement;
    private Vector3 _moveDirection;
    private Vector3 _slopeMoveDirection;
    private Rigidbody _rb;
    
    private bool _grounded;
    private bool _ceiling = false;
    private bool _enabledStrafe = false;
    private bool _enabledDoubleJump = false;
    private bool _canDoubleJump = false;
    
    private float _groundDist = 0.4f;
    private RaycastHit slopeHit;
    private float _currentMoveMult;
    private float _currentCooldownStrafe = 0f;
    private float _currentCooldownStrafeCount = 0f;
    private int _strafeCount = 0;
    private float _walkSpeed;
    private float _sprintSpeed;
    private float _crouchSpeed;
    private float _jumpForce;
    
    
#endregion

#region SystemFunctions

private void Awake()
{
    stairRayUpper.transform.position =
        new Vector3(stairRayUpper.transform.position.x, stairRayLower.transform.position.y + stepHieght, stairRayUpper.transform.position.z);
    
}

private void Start()
{
    _walkSpeed = walkSpeed;
    _sprintSpeed = sprintSpeed;
    _crouchSpeed = crouchSpeed;
    _jumpForce = jumpForce;
    _rb = GetComponent<Rigidbody>();
    _rb.freezeRotation = true;
    UpdateStats();
}

private void Update()
{
    MyInput();
    ControlSpeed();
}

private void FixedUpdate()
{
    Movement();
    ControlDrag();
    if (!OnSlope())
    {
        StepClimb();
    }
}


#endregion

#region MyPrivateMethods

    private void ControlDrag()
    {
        if (_grounded)
        {
            _rb.drag = groundDrag;
        }
        else if (!_grounded)
        {
            _rb.drag = airDrag;
        }
    }

    private void ControlSpeed()
    {
        if (Input.GetKey(crouchKey) && _grounded)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, crouchSpeed, acceleration * Time.deltaTime);
        }
        else if (Input.GetKey(sprintKey) && _grounded)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
        }
        
    }

    private void MyInput()
    {
        _grounded =
            Physics
                .CheckSphere(groundCheck.position, _groundDist, groundedMask);

        if (!lockMovement)
        {
            _horizontalMovement = Input.GetAxisRaw("Horizontal");
            _verticalMovement = Input.GetAxisRaw("Vertical");
            _moveDirection =
                orientation.forward * _verticalMovement +
                orientation.right * _horizontalMovement;
        }
        else
        {
            _moveDirection = transform.right * _horizontalMovement;
            _verticalMovement = 0;
        }


       
        
        _slopeMoveDirection =
            Vector3.ProjectOnPlane(_moveDirection, slopeHit.normal);
        Strafe();
        Crouching();
        
        if (Input.GetKeyDown(jumpKey) && _grounded)
        {
            Jump();
        }
        else if (Input.GetKeyDown(jumpKey) && !_grounded)
        {
            if (_enabledDoubleJump && _canDoubleJump)
            {
                Jump();
                _canDoubleJump = false;
            }
        }
        if (_grounded) _canDoubleJump = true;
    }

    private void Movement()
    {
        _rb.AddForce(-transform.up.normalized * _G, ForceMode.Acceleration);
        if (_grounded && !OnSlope())
        {
            _rb
                .AddForce(_moveDirection.normalized *
                moveSpeed *
                moveMultiplier,
                ForceMode.Acceleration);
        }
        else if (_grounded && OnSlope())
        {
            _rb
                .AddForce(_slopeMoveDirection.normalized *
                moveSpeed *
                moveMultiplier,
                ForceMode.Acceleration);
        }
        else if (!_grounded)
        {
            _rb
                .AddForce(_moveDirection.normalized *
                moveSpeed *
                moveMultiplier *
                airMultiplier,
                ForceMode.Acceleration);
        }
    }
    
    

    private void Crouching()
    {
        RaycastHit hit;
        Ray ray = new Ray(ceilingCheck.position, Vector3.up);
        Debug.DrawRay(ceilingCheck.position, Vector3.up * 2, Color.green);
        _ceiling = Physics.Raycast(ray, out hit, 2f, groundedMask);
        if (Input.GetKey(crouchKey))
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1.3f, _playerCrouchHeight, 1.3f), Time.deltaTime  * acceleration);
            
        }
        else if(!_ceiling && !Input.GetKey(crouchKey))
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1.3f, _playerHeight, 1.3f),Time.deltaTime  * acceleration);
        }
    }

    private void Jump()
    {
        _rb.velocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
        _rb.AddForce(transform.up.normalized * jumpForce, ForceMode.Impulse);
    }

    private void Strafe()
    {
        if (_enabledStrafe && _grounded)
        {
            if (Input.GetKeyDown(KeyCode.V) && _currentCooldownStrafe <= 0)
            {
                if (_strafeCount < playerStats.agility - 6)
                {
                    if (_moveDirection == Vector3.zero)
                    {
                        _rb.AddForce(orientation.forward * strafeForce, ForceMode.Impulse);
                    }
                    else
                    {
                        _rb.AddForce(_moveDirection.normalized * strafeForce, ForceMode.Impulse);
                    }
                    _strafeCount++;
                    _currentCooldownStrafeCount = 2f;
                    if (_strafeCount == playerStats.agility - 6)
                    {
                        _currentCooldownStrafe = 3f;
                    }
                }
               
            }
        }
        if (_currentCooldownStrafe > 0) _currentCooldownStrafe -= Time.deltaTime;
        if (_currentCooldownStrafeCount > 0)
        {
            _currentCooldownStrafeCount -= Time.deltaTime;
        }
        else
        {
            _strafeCount = 0;
        }
        
    }

    private bool OnSlope()
    {
        if (
            Physics
                .Raycast(groundCheck.position,
                Vector3.down,
                out slopeHit,
                0.5f, groundedMask)
        )
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    private void StepClimb()
    {
        RaycastHit hitLower;
        if (Physics.Raycast(stairRayLower.transform.position, stairRayLower.transform.forward, out hitLower, 0.7f))
        {
            if (Vector3.Angle(hitLower.normal, Vector3.up) >= 80 && Vector3.Angle(hitLower.normal, Vector3.up) <= 100)
            {
                RaycastHit hitUpper;
                if (!Physics.Raycast(stairRayUpper.transform.position, stairRayUpper.transform.forward, out hitUpper,
                    0.8f))
                {
                    _rb.position -= new Vector3(0f, -stepSmooths, 0f);
                }
            }
        }
        RaycastHit hitLower45;
        if (Physics.Raycast(stairRayLower.transform.position, stairRayLower.transform.TransformDirection(1.5f, 0f, 1f), out hitLower45, 0.4f))
        {
            if (Vector3.Angle(hitLower45.normal, Vector3.up) >= 80 && Vector3.Angle(hitLower45.normal, Vector3.up) <= 100)
            {
                RaycastHit hitUpper45;
                if (!Physics.Raycast(stairRayUpper.transform.position,
                    stairRayUpper.transform.TransformDirection(1.5f, 0f, 1f), out hitUpper45, 0.5f))
                {
                    _rb.position -= new Vector3(0f, -stepSmooths, 0f);
                }
            }
        }
        RaycastHit hitLowerMinus45;
        if (Physics.Raycast(stairRayLower.transform.position, stairRayLower.transform.TransformDirection(-1.5f, 0f, 1f), out hitLowerMinus45, 0.4f))
        {
            if (Vector3.Angle(hitLowerMinus45.normal, Vector3.up) >= 80 && Vector3.Angle(hitLowerMinus45.normal, Vector3.up) <= 100)
            {
                RaycastHit hitUpperMinus45;
                if (!Physics.Raycast(stairRayUpper.transform.position,
                    stairRayUpper.transform.TransformDirection(-1.5f, 0f, 1f), out hitUpperMinus45, 0.5f))
                {
                    _rb.position -= new Vector3(0f, -stepSmooths, 0f);
                }
            }
        }
        RaycastHit hitLowerBack;
        if (Physics.Raycast(stairRayLower.transform.position, -stairRayLower.transform.forward, out hitLowerBack, 0.7f))
        {
            if (Vector3.Angle(hitLowerBack.normal, Vector3.up) >= 80 && Vector3.Angle(hitLowerBack.normal, Vector3.up) <= 100)
            {
                RaycastHit hitUpperBack;
                if (!Physics.Raycast(stairRayUpper.transform.position, -stairRayUpper.transform.forward, out hitUpperBack,
                    0.8f))
                {
                    _rb.position -= new Vector3(0f, -stepSmooths, 0f);
                }
            }
        }
        RaycastHit hitLower45Back;
        if (Physics.Raycast(stairRayLower.transform.position, stairRayLower.transform.TransformDirection(1.5f, 0f, -1f), out hitLower45Back, 0.4f))
        {
            if (Vector3.Angle(hitLower45Back.normal, Vector3.up) >= 80 && Vector3.Angle(hitLower45Back.normal, Vector3.up) <= 100)
            {
                RaycastHit hitUpper45Back;
                if (!Physics.Raycast(stairRayUpper.transform.position,
                    stairRayUpper.transform.TransformDirection(1.5f, 0f, -1f), out hitUpper45Back, 0.5f))
                {
                    _rb.position -= new Vector3(0f, -stepSmooths, 0f);
                }
            }
        }
        RaycastHit hitLowerMinus45Back;
        if (Physics.Raycast(stairRayLower.transform.position, stairRayLower.transform.TransformDirection(-1.5f, 0f, -1f), out hitLowerMinus45Back, 0.4f))
        {
            if (Vector3.Angle(hitLowerMinus45Back.normal, Vector3.up) >= 80 && Vector3.Angle(hitLowerMinus45Back.normal, Vector3.up) <= 100)
            {
                RaycastHit hitUpperMinus45Back;
                if (!Physics.Raycast(stairRayUpper.transform.position,
                    stairRayUpper.transform.TransformDirection(-1.5f, 0f, -1f), out hitUpperMinus45Back, 0.5f))
                {
                    _rb.position -= new Vector3(0f, -stepSmooths, 0f);
                }
            }
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(stairRayLower.transform.position, stairRayLower.transform.forward * 0.7f);
        Gizmos.DrawRay(stairRayUpper.transform.position, stairRayUpper.transform.forward * 0.8f);
        Gizmos.DrawRay(stairRayLower.transform.position,
            stairRayLower.transform.TransformDirection(-1.5f, 0f, 1f) * 0.4f);
        Gizmos.DrawRay(stairRayLower.transform.position,
            stairRayLower.transform.TransformDirection(1.5f, 0f, 1f) * 0.4f);
        Gizmos.DrawRay(stairRayUpper.transform.position,
            stairRayUpper.transform.TransformDirection(-1.5f, 0f, 1f) * 0.5f);
        Gizmos.DrawRay(stairRayUpper.transform.position,
            stairRayUpper.transform.TransformDirection(1.5f, 0f, 1f) * 0.5f);
        Gizmos.DrawRay(stairRayLower.transform.position, -stairRayLower.transform.forward * 0.7f);
        Gizmos.DrawRay(stairRayUpper.transform.position, -stairRayUpper.transform.forward * 0.8f);
        Gizmos.DrawRay(stairRayLower.transform.position,
            stairRayLower.transform.TransformDirection(-1.5f, 0f, -1f) * 0.4f);
        Gizmos.DrawRay(stairRayLower.transform.position,
            stairRayLower.transform.TransformDirection(1.5f, 0f, -1f) * 0.4f);
        Gizmos.DrawRay(stairRayUpper.transform.position,
            stairRayUpper.transform.TransformDirection(-1.5f, 0f, -1f) * 0.5f);
        Gizmos.DrawRay(stairRayUpper.transform.position,
            stairRayUpper.transform.TransformDirection(1.5f, 0f, -1f) * 0.5f);
    }

    #endregion


    #region Public Methods

    public void UpdateStats()
    {

        _currentMoveMult = (playerStats.agility - 3) * playerStats.goodMultSpeed -
                           (playerStats.strength - 3) * playerStats.badMultSpeed;
        jumpForce = _jumpForce + _currentMoveMult * 0.384615f;
        crouchSpeed = _crouchSpeed + _currentMoveMult * 0.692308f;
        walkSpeed = _walkSpeed + _currentMoveMult;
        sprintSpeed = _sprintSpeed + _currentMoveMult;
        
        if(playerStats.agility > 4)
        {
            _enabledDoubleJump = true;
        }
        else
        {
            _enabledDoubleJump = false;
        }
        
        if(playerStats.agility > 6)
        {
            _enabledStrafe = true;
        }
        else
        {
            _enabledStrafe = false;
            
        }
    }

    #endregion
}
