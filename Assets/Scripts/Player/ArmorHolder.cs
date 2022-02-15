using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArmorHolder : MonoBehaviour
{
    public PlayerHits headPlHits;
    public PlayerHits bodyPlHits;
    public LayerMask canTake;

    private CanTake _cntk;
    private Transform _cam;
    private Text _nameHead;
    private Text _nameBody;
    private Text _currentHealthHead;
    private Text _currentHealthBody;
    

    private void Start()
    {
        _nameHead = GameObject.Find("Canvas/Armor/ArmorNameHead").GetComponent<Text>();
        _nameBody = GameObject.Find("Canvas/Armor/ArmorNameBody").GetComponent<Text>();
        _currentHealthHead = GameObject.Find("Canvas/Armor/ArmorHealthHead").GetComponent<Text>();
        _currentHealthBody = GameObject.Find("Canvas/Armor/ArmorHealthBody").GetComponent<Text>();
        _cntk = GameObject.Find("CameraHolder/PlayerCamera").GetComponent<CanTake>();
        _cam = GameObject.Find("CameraHolder/PlayerCamera").transform;
        headPlHits.currentResourcesArmor.Initialize();
        bodyPlHits.currentResourcesArmor.Initialize();
        UpdateStats();
    }

    private void Update()
    {
        MyInput();
    }

    private void MyInput()
    {
        if (_cntk.CanTakeFunc() && Input.GetKeyDown(KeyCode.F))
        {
            Ray ray = new Ray(_cam.position, _cam.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3.3f, canTake))
            {
                ResourcesArmor newArmor = hit.collider.gameObject.GetComponent<ResourcesArmor>();
                ResourcesArmor prevArm;
                if (newArmor)
                {
                    if (newArmor.typeOfArmor == 1)
                    {
                        prevArm = headPlHits.currentResourcesArmor;
                        GameObject lastArm = Instantiate(prevArm.prefabOfArmor.armorPrefab, _cam.position + _cam.forward * 2f,
                            _cam.rotation);
                        lastArm.GetComponent<ResourcesArmor>().NewArmor(prevArm);
                        lastArm.GetComponent<Rigidbody>().AddForce(_cam.forward * 5f, ForceMode.Impulse);
                        if (!newArmor.init)
                        {
                            newArmor.Initialize();
                        }
                        headPlHits.currentResourcesArmor.NewArmor(newArmor);
                        UpdateStats();
                       
                    }
                    else if (newArmor.typeOfArmor == 2)
                    {
                        prevArm = bodyPlHits.currentResourcesArmor;
                        GameObject lastArm = Instantiate(prevArm.prefabOfArmor.armorPrefab, _cam.position + _cam.forward * 2f,
                            _cam.rotation);
                        lastArm.GetComponent<ResourcesArmor>().NewArmor(prevArm);
                        lastArm.GetComponent<Rigidbody>().AddForce(_cam.forward * 5f, ForceMode.Impulse);
                        if (!newArmor.init)
                        {
                            newArmor.Initialize();
                        }
                        bodyPlHits.currentResourcesArmor.NewArmor(newArmor);
                        UpdateStats();
                        //Debug.Log( lastArm.GetComponent<ResourcesArmor>().GetCurHealthArmor());
                    }
                    Destroy(hit.collider.gameObject);
                    
                }
            }
        }
    }
    
    public void UpdateStats()
    {
        _nameHead.text = headPlHits.currentResourcesArmor.nameOfArmor + ":H";
        _nameBody.text = bodyPlHits.currentResourcesArmor.nameOfArmor + ":B";
        int bodyPr = (int) (bodyPlHits.currentResourcesArmor.GetCurHealthArmor() * 100 /
                            bodyPlHits.currentResourcesArmor.maxHealthArmor);
        _currentHealthBody.text =  bodyPr.ToString() + "%";
        int headPr = (int) (headPlHits.currentResourcesArmor.GetCurHealthArmor() * 100 /
                            headPlHits.currentResourcesArmor.maxHealthArmor);
        _currentHealthHead.text =  headPr.ToString() + "%";
    }
}
