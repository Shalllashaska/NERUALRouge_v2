using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class PlayerHits : MonoBehaviour
{
    public PlayerHealth health;
    public float partOfBodyMult = 1f;
    public ResourcesArmor currentResourcesArmor;
    public ArmorHolder armH;

    public void OnHit(float damage)
    {
       
        if (currentResourcesArmor)
        {
            float i = Random.Range(0f, 1f);
            if (i > currentResourcesArmor.GetCurHealthArmor())
            {
                health.Damage(damage * partOfBodyMult);
            }
            else
            {
                float j = Random.Range(0f, 1f);
                if (j > i)
                {
                   
                    health.Damage(damage * partOfBodyMult * j);
                }
                if (currentResourcesArmor.GetCurHealthArmor() > 0.1)
                {
                    float dam = Mathf.Abs(currentResourcesArmor.GetCurHealthArmor() - i);
                    if (dam > currentResourcesArmor.GetCurHealthArmor() / 4)
                    {
                        currentResourcesArmor.HitDamageArmor(currentResourcesArmor.GetCurHealthArmor() / 4);
                    }
                    else
                    {
                        currentResourcesArmor.HitDamageArmor(dam);
                    }
                    armH.UpdateStats();
                }
            }
        }
        else
        {
            health.Damage(damage * partOfBodyMult);
        }
    }
}
