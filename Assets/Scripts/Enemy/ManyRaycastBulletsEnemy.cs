using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManyRaycastBulletsEnemy : MonoBehaviour
{
    public float speed;
   public float gravity = 0.5f;
   public float LifeTime = 20f;
   public LayerMask canBeShot;
   private Vector3 startPosition;
   private Vector3 startForward;
   
   private bool isInitialized = false;
   private float startTime = -1;
   private float _damage;
   private Vector3 _currentAttackPoint;

   public void Initialize(Transform startPoint, float damage, Vector3 attackPoint)
   {
      startPosition = startPoint.position;
      startForward = startPoint.forward.normalized;
      isInitialized = true;
      _damage = damage;
      _currentAttackPoint = attackPoint;
   }
   
   private Vector3 FindPointOnParabola(float time)
   {
      Vector3 point = startPosition + (startForward * speed * time);
      Vector3 gravityVec = Vector3.down * gravity * time * time;
      return point + gravityVec;
   }

   private bool CastRayBetweenPoints(Vector3 startPoint, Vector3 endPoint, out RaycastHit hit)
   {
      return Physics.Raycast(startPoint, endPoint - startPoint, out hit, (endPoint - startPoint).magnitude, canBeShot);
   }

   private void OnHit(RaycastHit hit)
   {
      WallHits wall = hit.transform.GetComponent<WallHits>();
      EnemyHits enemy = hit.transform.GetComponent<EnemyHits>();
      PlayerHits player = hit.collider.transform.GetComponent<PlayerHits>();
     
      if (wall)
      {
         wall.OnHit(hit);
         Destroy(gameObject);
      }
      else if (enemy)
      {
         enemy.OnHit(hit, _damage, _currentAttackPoint);
      }
      else if (player)
      {
        player.OnHit(_damage);
      }
      Destroy(gameObject);
   }

   private void FixedUpdate()
   {
      if (!isInitialized) return;
      if (startTime < 0) startTime = Time.time;
      RaycastHit hit;
      float currentTime = Time.time - startTime;
      float prevTime = currentTime - Time.fixedDeltaTime;
      float nextTime = currentTime + Time.fixedDeltaTime;
      Vector3 currentPoint = FindPointOnParabola(currentTime);
      Vector3 nextPoint = FindPointOnParabola(nextTime);
      if (prevTime > 0)
      {
         Vector3 prevPoint = FindPointOnParabola(prevTime);
         if (CastRayBetweenPoints(prevPoint, currentPoint, out hit))
         {
            OnHit(hit);
         }
      }

      if (CastRayBetweenPoints(currentPoint, nextPoint, out hit))
      {
         OnHit(hit);
      }
      
   }

   
   private void Update()
   {
      if (!isInitialized || startTime < 0) return;
      float currentTime = Time.time - startTime;
      Vector3 currentPoint = FindPointOnParabola(currentTime);
      transform.position = currentPoint;
   }
}
