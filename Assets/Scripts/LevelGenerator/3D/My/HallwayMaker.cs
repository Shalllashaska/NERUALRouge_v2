using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HallwayMaker : MonoBehaviour
{
    public Transform[] checkers;
    
    public LayerMask hallwayWallLayer;
    public LayerMask roomWallLayer;

    public GameObject doorPrefab;

    public bool makeRoomDoor = false;
    void Start()
    {
        foreach (Transform check in checkers)
        {
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(check.position, check.forward, out hit,  1.5f,hallwayWallLayer))
            {
                HallwayWallDetected(hit, check);
            }
            else if (Physics.Raycast(check.position, check.forward,out RaycastHit hit2,  1.5f, roomWallLayer))
            {
                RoomWallDetected(hit2, check);
            }
        }
        makeRoomDoor = false;
    }


    private void HallwayWallDetected(RaycastHit hit, Transform checker)
    {
        Destroy(hit.collider.gameObject);
        Destroy(checker.parent.gameObject);
    }

    private void RoomWallDetected(RaycastHit hit, Transform checker)
    {
        if (makeRoomDoor)
        {
            Destroy(hit.transform.gameObject);
            GameObject door = Instantiate(doorPrefab, hit.collider.transform.position, Quaternion.LookRotation(hit.normal));
            Destroy(checker.parent.gameObject);
            
        }
    }
    
}
