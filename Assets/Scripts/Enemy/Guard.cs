using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Guard : MonoBehaviour
{
    [Header("---Path and agent---")]
    public Transform pathHolder;
    public NavMeshAgent npcNavMeshAgent;
    public ArmorMenager armorOfGuard;
    public int typeOfEnemy = 1; // 0 - глупый, пугливый(убегает и прячеться), 1 - более сложный(преследует), 2 - сложный(патрулирует и затем преследует), 3 - оч. сложный(патрулирает, укрывается за укрытиями и может отступать)

    public float distanceToPlayer = 20f;
    public EnemyMovement enemyMovement;
    
    
    [Header("---Room---")] 
    public int numberOfRoom = 1;

    [Header("---Speed---")]
    public float patrolSpeed = 6f;
    public float chaseSpeed = 11f;
    
    [Header("---Time---")]
    public float waitTimeAtPoint = 0.7f;
    public float timeToSpotPlayer = 0.5f;

    [Header("---View---")] 
    public Transform headTransform;
    public Light spotLight;
    public float viewDistance;
    public LayerMask viewMask;
    

    private Vector3[] _waypoints;
    private Vector3 _targetWaypoint;
    private int _targetInd;
    private float _currentTime;
    private bool _atPoint = false;
    private float _viewAngle;
    private Transform _player;
    private Color _spotLightColor;
    private bool _foundPlayer = false;
    private float _playerVisibleTimer;
    private float _currentPatrolSpeed;
    private float _currentChaseSpeed;
    

    private AimTestScript _aimTest;

    private bool scriptInit = false;
    private bool _reachThePlayer = true;
    [HideInInspector]
    public bool _isDead = false;
    [HideInInspector]
    public bool _isToLowHealth = false;
    private bool _isToLowHealthActive = false;

    private bool activateEnemyMovement = false;
    
    
    private Vector3 testdir = Vector3.zero;

    private void Awake()
    {
        CalculateMind();
    }

    private void Start()
    {
        if (typeOfEnemy == 0)
        {
            enemyMovement.ActivateHide();
            enemyMovement.ActivateSript();
            activateEnemyMovement = true;
        }
        else if (typeOfEnemy == 1)
        {
            _spotLightColor = spotLight.color;
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _viewAngle = spotLight.spotAngle;
            _foundPlayer = true;
            _aimTest = gameObject.GetComponent<AimTestScript>();
            _aimTest.playerFound = true;
        }
        else if (typeOfEnemy >= 2)
        {
            _spotLightColor = spotLight.color;
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _viewAngle = spotLight.spotAngle;
            _waypoints = new Vector3[pathHolder.childCount];
            for (int i = 0; i < _waypoints.Length; i++)
            {
                _waypoints[i] = pathHolder.GetChild(i).position;
            }
            _targetWaypoint = _waypoints[0];
            _targetInd = 1;
            _aimTest = gameObject.GetComponent<AimTestScript>();
            SetNewPointDestination(_targetWaypoint);
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            enemyMovement.DisactivateSript();
            activateEnemyMovement = false;
            SetNewPointDestination(transform.position);
            spotLight.gameObject.SetActive(false);
            return;
        }

        if (_isToLowHealth && typeOfEnemy == 3 && !_isToLowHealthActive)
        {
            _isToLowHealthActive = true;
            enemyMovement.ActivateHide();
            enemyMovement.LineOfSightChecker.CheckMyself(_player);
        }

        if (!scriptInit)
        {
            if (armorOfGuard.IsInit())
            {
                if ((patrolSpeed + armorOfGuard.GetSpeedMult()) > 0)
                {
                    _currentPatrolSpeed = patrolSpeed + armorOfGuard.GetSpeedMult();
                }
                else
                {
                    _currentPatrolSpeed = patrolSpeed;
                }
                
                if ((chaseSpeed + armorOfGuard.GetSpeedMult()) > 0)
                {
                    _currentChaseSpeed = chaseSpeed + armorOfGuard.GetSpeedMult();
                }
                else
                {
                    _currentChaseSpeed = chaseSpeed;
                }
                npcNavMeshAgent.speed =  _currentPatrolSpeed;
                scriptInit = true;
            }
            else
            {
                return;
            }
        }

        
        if (typeOfEnemy == 1)
        {
            npcNavMeshAgent.speed = _currentChaseSpeed;
            ChasePlayer();
        }
        else if (typeOfEnemy == 2)
        {
            if (!_foundPlayer)
            {
                if (CanSeePlayer())
                {
                    _playerVisibleTimer += Time.deltaTime;
                }
                else
                {
                    _playerVisibleTimer -= Time.deltaTime;
                
                }
                FollowingPath();
            }
            else
            {
                ChasePlayer();
            }
            _playerVisibleTimer = Mathf.Clamp(_playerVisibleTimer, 0, timeToSpotPlayer);
            spotLight.color = Color.Lerp(_spotLightColor, Color.red, _playerVisibleTimer / timeToSpotPlayer);

            if (_playerVisibleTimer >= timeToSpotPlayer)
            {
                _foundPlayer = true;
                _aimTest.playerFound = true;
                npcNavMeshAgent.speed = _currentChaseSpeed;
            }
        }
        else if (typeOfEnemy == 3)
        {
            if (!_foundPlayer)
            {
                if (CanSeePlayer())
                {
                    _playerVisibleTimer += Time.deltaTime;
                }
                else
                {
                    _playerVisibleTimer -= Time.deltaTime;
                
                }
                FollowingPath();
            }
            else
            {
                if (!activateEnemyMovement)
                {
                    enemyMovement.ActivateCover();
                    enemyMovement.ActivateSript();
                    
                    activateEnemyMovement = true;
                }
                
                
            }
            _playerVisibleTimer = Mathf.Clamp(_playerVisibleTimer, 0, timeToSpotPlayer);
            spotLight.color = Color.Lerp(_spotLightColor, Color.red, _playerVisibleTimer / timeToSpotPlayer);

            if (_playerVisibleTimer >= timeToSpotPlayer)
            {
                _foundPlayer = true;
                _aimTest.playerFound = true;
                npcNavMeshAgent.speed = _currentChaseSpeed;
            }
        }

    }

    private void ChasePlayer()
    {
        float distToPl = Vector3.Distance(transform.position, _player.position);

        if (!CanSeePlayer() && _reachThePlayer)
        {
            _reachThePlayer = false;
        }

        if (CanSeePlayer() && _reachThePlayer)
        {
            if (distToPl <= (distanceToPlayer + 2f) && 
                 distToPl >= (distanceToPlayer - 2f))
            {
                SetNewPointDestination(transform.position);
            }
            else if (distToPl < distanceToPlayer - 2f)
            {
                Vector3 dir = -(_player.position - transform.position).normalized;
                testdir = dir;
                SetNewPointDestination(transform.position + dir * distanceToPlayer);
            }
            else if(distToPl > (distanceToPlayer + 2f))
            {
                SetNewPointDestination(_player.position);
            }
        }
        else if(!CanSeePlayer() && !_reachThePlayer)
        {
            if (distToPl <= 2.5f)
            {
                Vector3 dir = -(_player.position - transform.position).normalized;
                testdir = dir;
                SetNewPointDestination(transform.position + dir * distanceToPlayer);
                _reachThePlayer = true;
            }
            else
            {
                SetNewPointDestination(_player.position);
            }
        }
    }
    
    private bool CanSeePlayer()
    {
        if (Vector3.Distance(headTransform.position, _player.position) <= viewDistance)
        {
            Vector3 dirToPlayer = (_player.position - headTransform.position).normalized;
            float angleBetweenGuardAndPlayer = Vector3.Angle(headTransform.forward, dirToPlayer);
            if (angleBetweenGuardAndPlayer <= _viewAngle / 2f)
            {
                if (!Physics.Linecast(headTransform.position, _player.position,  viewMask))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private void FollowingPath()
    {
        if (npcNavMeshAgent.remainingDistance <= 0.1f)
        {
            if (!_atPoint)
            {
                _currentTime = waitTimeAtPoint;
                _atPoint = true;
                npcNavMeshAgent.isStopped = true;
            }
           
        }
        if (_currentTime <= 0 && _atPoint)
        {
            _atPoint = false;
            _targetWaypoint = _waypoints[_targetInd];
            SetNewPointDestination(_targetWaypoint);
            _targetInd = (_targetInd + 1) % _waypoints.Length;
        }
        else
        {
            _currentTime -= Time.deltaTime;
        }
    }

    private void SetNewPointDestination(Vector3 waypoint)
    {
        npcNavMeshAgent.isStopped = false;
        npcNavMeshAgent.SetDestination(waypoint);
    }

    public void CalculateMind()
    {
        if (numberOfRoom >= 1 && numberOfRoom <= 3)
        {
            float dice = Random.Range(0f, 1f);
            if (dice <= 0.8f)
            {
                typeOfEnemy = 0;
            }
            else
            {
                typeOfEnemy = 1;
            }
        }
        else if (numberOfRoom >= 4 && numberOfRoom <= 6)
        {
            float dice = Random.Range(0f, 1f);
            if (dice <= 0.4f)
            {
                typeOfEnemy = 0;
            }
            else if (dice > 0.4f && dice <= 0.8f)
            {
                typeOfEnemy = 1;
            }
            else
            {
                typeOfEnemy = 2;
            }
        }
        else if (numberOfRoom >= 7 && numberOfRoom <= 9)
        {
            float dice = Random.Range(0f, 1f);
            if (dice <= 0.1f)
            {
                typeOfEnemy = 0;
            }
            else if (dice > 0.1f && dice <= 0.3f)
            {
                typeOfEnemy = 1;
            }
            else if (dice > 0.3f && dice <= 0.7f)
            {
                typeOfEnemy = 2;
            }
            else 
            {
                typeOfEnemy = 3;
            }
        }
        else if (numberOfRoom == 99)
        {
            typeOfEnemy = 3;
        }
        else if (numberOfRoom >= 9)
        {
            
            float dice = Random.Range(0f, 1f);
            Debug.Log(dice);
            if (dice <= 0.1f)
            {
                typeOfEnemy = 1;
            }
            else if (dice > 0.1f && dice <= 0.5f)
            {
                typeOfEnemy = 2;
            }
            else 
            {
                typeOfEnemy = 3;
            }
        }
        
    }
    
    private void OnDrawGizmos()
    {
        Vector3 startPosition = pathHolder.GetChild(0).position;
        Vector3 previousPosition = startPosition;
        foreach (Transform waypoint in pathHolder)
        {
            Gizmos.DrawSphere(waypoint.position, 0.5f); 
            Gizmos.DrawLine(previousPosition, waypoint.position);
            previousPosition = waypoint.position;
        }
        Gizmos.DrawLine(previousPosition, startPosition);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(headTransform.position, headTransform.forward * viewDistance);
        Gizmos.color = Color.blue;
        if (_player)
        {
            Vector3 dir = _player.position - headTransform.position;
            Gizmos.DrawRay(headTransform.position, dir.normalized * viewDistance);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, testdir * distanceToPlayer);
    }
}
