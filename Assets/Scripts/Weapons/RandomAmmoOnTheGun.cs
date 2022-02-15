using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAmmoOnTheGun : MonoBehaviour
{
    public Gun curGun;
    
    private int _stash;
    private int _clip;
    
    void Start()
    {
        _stash = Random.Range(0, curGun.ammo);
        _clip = Random.Range(0, curGun.clipsize);

        ResourcesHolderMono holder = gameObject.AddComponent<ResourcesHolderMono>();
        holder.clip = _clip;
        holder.stash = _stash;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
