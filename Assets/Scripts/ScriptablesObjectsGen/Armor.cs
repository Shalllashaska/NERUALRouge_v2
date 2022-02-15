using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Armor", menuName = "Armor")]
public class Armor : ScriptableObject
{
   public int typeOfArmor; // 0 helmets, 1 body
   public GameObject armorPrefab;
   public float speedMult;
   public float healthMult;
   public float damageMult;
   public int level;
}
