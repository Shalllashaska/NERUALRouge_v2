using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.SocialPlatforms;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [HideInInspector]
    public Transform Player;
    
    [Header("---Activate---")]
    public bool activateScript = true;
    public bool activateHide = true;
    public bool activateCover = true;

    [Header("---Room---")] 
    public Transform floorRoom;
    
    [Header("---Settings---")]
    public LayerMask HidableLayers;
    public LayerMask ViewMask;
    public EnemyLineOfSightChecker LineOfSightChecker;
    public NavMeshAgent Agent;
    [Range(-1, 1)]
    [Tooltip("Lower is a better hiding spot")]
    public float HideSensitivity = 0;
    [Range(-1, 1)]
    public float CoverSensitivity = 0;
    [Range(1, 20)]
    public float MinPlayerDistance = 5f;
    [Range(1, 20)]
    public float MaxPlayerDistance = 3f;
    [Range(0, 10f)]
    public float MinObstacleHeight = 1.25f;
    [Range(0, 10f)]
    public float MaxObstacleHeight = 0.5f;
    [Range(0.01f, 10f)]
    public float UpdateFrequency = 0.25f;
    [Range(0.01f, 10f)]
    public float UpdateFrequencyHide = 0.25f;
    [Range(0.01f, 10f)]
    public float UpdateFrequencyChase = 0.25f;

    private Coroutine MovementCoroutine;
    private Collider[] Colliders = new Collider[20]; // more is less performant, but more options
    private Vector3 prevColliderPosition  = Vector3.zero;
    private Vector3 prevPosition = Vector3.zero;
    private Vector3 nextPosition = Vector3.zero;
    private Vector3 testDir = Vector3.zero;
    
    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();

        if (activateHide)
        {
            ActivateHide();
        }
        else if (activateCover)
        {
            ActivateCover();
        }
        
    }

    private void HandleGainSight(Transform Target)
    {
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        Player = Target;
        MovementCoroutine = StartCoroutine(Hide(Target));
    }

    private void HandleLoseSight(Transform Target)
    {
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        Player = null;
    }
    
    private void HandleGainSightCover(Transform Target)
    {
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        Player = Target;
        MovementCoroutine = StartCoroutine(Cover(Target));
    }

    private void HandleLoseSightCover(Transform Target)
    {
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        Player = Target;
        MovementCoroutine = StartCoroutine(Chase(Target));
    }

    private IEnumerator Hide(Transform Target)
    {
        if (!activateScript)
        {
            DisactivateSript();
            yield return null;
        }
        WaitForSeconds Wait = new WaitForSeconds(UpdateFrequencyHide);
        while (true)
        {
            if (!activateScript)
            {
                DisactivateSript();
                yield return null;
            }
            for (int i = 0; i < Colliders.Length; i++)
            {
                Colliders[i] = null;
            }

            int hits = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius, Colliders, HidableLayers);

            int hitReduction = 0;
            for (int i = 0; i < hits; i++)
            {
                if (activateHide)
                {
                    if (Vector3.Distance(Colliders[i].transform.position, Target.position) < MinPlayerDistance ||
                        Colliders[i].bounds.size.y < MinObstacleHeight)
                    {
                        Colliders[i] = null;
                        hitReduction++;
                    }
                }
            }
            hits -= hitReduction;

            System.Array.Sort(Colliders, ColliderArraySortComparer);

            
            bool foundPlace = false;
            for (int i = 0; i < hits; i++)
            {
                if (NavMesh.SamplePosition(Colliders[i].transform.position, out NavMeshHit hit, 2f, Agent.areaMask))
                {
                    if (!NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
                    {
                        Debug.LogError($"Unable to find edge close to {hit.position}");
                    }

                    if (activateHide)
                    {
                        if (Vector3.Dot(hit.normal, (Target.position - hit.position).normalized) < HideSensitivity)
                        {
                            foundPlace = true;
                            Agent.SetDestination(hit.position);
                            break;
                        }
                        else
                        {
                            // Since the previous spot wasn't facing "away" enough from teh target, we'll try on the other side of the object
                            if (NavMesh.SamplePosition(
                                Colliders[i].transform.position - (Target.position - hit.position).normalized * 2,
                                out NavMeshHit hit2, 2f, Agent.areaMask))
                            {
                                if (!NavMesh.FindClosestEdge(hit2.position, out hit2, Agent.areaMask))
                                {
                                    Debug.LogError(
                                        $"Unable to find edge close to {hit2.position} (second attempt)");
                                }

                                if (Vector3.Dot(hit2.normal, (Target.position - hit2.position).normalized) <
                                    HideSensitivity)
                                {
                                    foundPlace = true;
                                    Agent.SetDestination(hit2.position);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError(
                        $"Unable to find NavMesh near object {Colliders[i].name} at {Colliders[i].transform.position}");
                }
            }
            if (!foundPlace)
            {
                RunAwayFromPlayer();
            }
            yield return Wait;
        }
    }

    private IEnumerator Cover(Transform Target)
    {
        if (!activateScript)
        {
            DisactivateSript();
            yield return null;
        }
        WaitForSeconds Wait = new WaitForSeconds(UpdateFrequency);
        while (true)
        {
            if (!activateScript)
            {
                DisactivateSript();
                yield return null;
            }
            for (int i = 0; i < Colliders.Length; i++)
            {
                Colliders[i] = null;
            }

            int hits = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius,
                Colliders, HidableLayers);

            int hitReduction = 0;
            for (int i = 0; i < hits; i++)
            {
                if (activateCover)
                {
                    if (Vector3.Distance(Colliders[i].transform.position, Target.position) < MaxPlayerDistance ||
                        Colliders[i].bounds.size.y > MaxObstacleHeight)
                    {
                        Colliders[i] = null;
                        hitReduction++;
                    }
                }
            }

            hits -= hitReduction;

            System.Array.Sort(Colliders, ColliderArraySortComparer);
            bool foundPlace = false;
            for (int i = 0; i < hits; i++)
            {
                if (prevColliderPosition != Colliders[i].transform.position)
                {
                    prevColliderPosition = Colliders[i].transform.position;
                    if (NavMesh.SamplePosition(Colliders[i].transform.position, out NavMeshHit hit, 2f, Agent.areaMask))
                    {
                        if (!NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
                        {
                            Debug.LogError($"Unable to find edge close to {hit.position}");
                        }

                        if (activateCover)
                        {
                            if (Vector3.Dot(hit.normal, (Target.position - hit.position).normalized) < CoverSensitivity)
                            {
                                nextPosition = hit.position;
                                if (hit.position != prevPosition && CoverBetweenToPoints(Player.position, hit.position))
                                {
                                    Agent.SetDestination(hit.position);
                                    prevPosition = hit.position;
                                    foundPlace = true;
                                    break;
                                }
                            }
                            else
                            {
                                // Since the previous spot wasn't facing "away" enough from teh target, we'll try on the other side of the object
                                if (NavMesh.SamplePosition(
                                    Colliders[i].transform.position - (Target.position - hit.position).normalized * 2,
                                    out NavMeshHit hit2, 2f, Agent.areaMask))
                                {
                                    if (!NavMesh.FindClosestEdge(hit2.position, out hit2, Agent.areaMask))
                                    {
                                        Debug.LogError(
                                            $"Unable to find edge close to {hit2.position} (second attempt)");
                                    }

                                    if (Vector3.Dot(hit2.normal, (Target.position - hit2.position).normalized) <
                                        CoverSensitivity)
                                    {
                                        nextPosition = hit2.position;
                                        if (hit2.position != prevPosition &&
                                            CoverBetweenToPoints(Player.position, hit2.position))
                                        {
                                            Agent.SetDestination(hit2.position);
                                            prevPosition = hit2.position;
                                            foundPlace = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError(
                            $"Unable to find NavMesh near object {Colliders[i].name} at {Colliders[i].transform.position}");
                    }
                }
            }
            if (!foundPlace)
            {
                float x = Random.Range(-13.5f, 13.5f);
                float z = Random.Range(-13.5f, 13.5f);
                Agent.SetDestination(Target.position + new Vector3(x, 0, z));
                nextPosition = Target.position + new Vector3(x, 0, z);
            }
            yield return Wait;
        }
    }
    
    private IEnumerator Chase(Transform Target)
    {
        if (!activateScript)
        {
            DisactivateSript();
            yield return null;
        }
        WaitForSeconds Wait = new WaitForSeconds(UpdateFrequencyChase);
        float x = Random.Range(-6.5f, 6.5f);
        float z = Random.Range(-6.5f, 6.5f);
        Agent.SetDestination(Target.position + new Vector3(x, 0, z));
        nextPosition = Target.position + new Vector3(x, 0, z);
        yield return Wait;
    }

    private bool CoverBetweenToPoints(Vector3 targetPos, Vector3 coverPosition) //есть ли приптсвия между врагом и игроком
    {
        if (!Physics.Linecast(coverPosition + new Vector3(0, 1.5f*MaxObstacleHeight, 0), targetPos,  ViewMask))
        {
            return true;
        }
        return false;
    }

    public void RunAwayFromPlayer()
    {
        if (Vector3.Distance(transform.position, Player.position) > MinPlayerDistance) return;
        float x = Random.Range(-1.5f, 0.5f);
        float z = Random.Range(-0.5f, 1.5f);
        float ch = Random.Range(0f, 1f);
        float dir = -1;
        if (ch > 0.85f)
        {
            dir = ch;
        }
        Vector3 direction = dir*(Player.position - (transform.position + new Vector3(x, 0, z))).normalized;
        testDir = direction;
        Vector3 point = transform.position + direction * MinPlayerDistance;
        prevPosition = point;
        Agent.SetDestination(point);


    }


    public int ColliderArraySortComparer(Collider A, Collider B)
    {
        if (A == null && B != null)
        {
            return 1;
        }
        else if (A != null && B == null)
        {
            return -1;
        }
        else if (A == null && B == null)
        {
            return 0;
        }
        else
        {
            return Vector3.Distance(Agent.transform.position, A.transform.position).CompareTo(Vector3.Distance(Agent.transform.position, B.transform.position));
        }
    }


    public void DisactivateSript()
    {
        LineOfSightChecker.OnGainSight -= HandleGainSight;
        LineOfSightChecker.OnLoseSight -= HandleLoseSight;
        LineOfSightChecker.OnGainSight -= HandleGainSightCover;
        LineOfSightChecker.OnLoseSight -= HandleLoseSightCover;
        Agent.SetDestination(transform.position);
        activateScript = false;
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
    }
    
    public void ActivateSript()
    {
        activateScript = true;
        if(activateCover) ActivateCover();
        else if(activateHide) ActivateHide();
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
    }

    public void ActivateCover()
    {
        LineOfSightChecker.OnGainSight -= HandleGainSight;
        LineOfSightChecker.OnLoseSight -= HandleLoseSight;
        LineOfSightChecker.OnGainSight += HandleGainSightCover;
        LineOfSightChecker.OnLoseSight += HandleLoseSightCover;
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        activateCover = true;
        activateHide = false;
    }
    
    public void ActivateHide()
    {
        LineOfSightChecker.OnGainSight -= HandleGainSightCover;
        LineOfSightChecker.OnLoseSight -= HandleLoseSightCover;
        LineOfSightChecker.OnGainSight += HandleGainSight;
        LineOfSightChecker.OnLoseSight += HandleLoseSight;
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        activateCover = false;
        activateHide = true;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(prevPosition, 1f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(nextPosition + new Vector3(0, 1.5f*MaxObstacleHeight, 0), Player.position);
        Gizmos.DrawRay(transform.position, testDir * 10f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(nextPosition, 1f);
    }
}
