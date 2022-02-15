using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHits : MonoBehaviour
{
    public GameObject particlesPrefab;
    public EnemyHealth enemyHealth;
    
    public float damageMultByPartOfBody = 1f;
    
    public void OnHit(RaycastHit hit, float damage, Vector3 attackPoint)
    {
        GameObject particles = Instantiate(particlesPrefab, hit.point,
            Quaternion.LookRotation(hit.normal), hit.collider.transform);
        float distance = Vector3.Distance(attackPoint, hit.point);
        float damageDist;
        if (distance > damage)
        {
            damageDist= damage / distance;
        }
        else
        {
            damageDist = 1;
        }

        enemyHealth.Damage(damage * damageMultByPartOfBody * damageDist);
    }
}
