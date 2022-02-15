using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class DoorScript : MonoBehaviour
{

    public Animator anim;
    private bool _isOpened;
    

    public void Open()
    {
        anim.SetBool("isOpened", _isOpened);
        _isOpened = true;
    }
}
