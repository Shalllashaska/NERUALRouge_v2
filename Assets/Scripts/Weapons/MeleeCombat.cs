using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

public class MeleeCombat : MonoBehaviour
{
        [Header("---Weapon---")]
        public Gun gunData;
        public bool holdToApplyDamage;
        public float cooldownOfDamage = 0.1f;
        
        [Header("---Animation---")]
        public Animator currentAnimator;
        public string shortAttackName;
        public string longAttackName;
        public string backAttackName;
        
        [Header("---Attack---")]
        public Transform attackPoint;
        public float bladeHeight = 1f;
        public LayerMask enemyLayers;
        public LayerMask backLayer;

        private Stack<EnemyHits> _allHits = new Stack<EnemyHits>();

        private bool _alreadyHits = false;
        private float _currentCooldown;
        
        private void Update()
        {
                MyInput();
                if (gameObject.layer == 13)
                {
                        currentAnimator.enabled = false;
                }
                else
                {
                        currentAnimator.enabled = true;
                }
        }

        private void MyInput()
        {
                if (!IsAnimationPlaying(shortAttackName) && !IsAnimationPlaying(longAttackName) &&
                    !IsAnimationPlaying(backAttackName))
                {
                        _alreadyHits = false;
                        if (Input.GetKey(KeyCode.Mouse0))
                        {
                                if (ThisIsBack() && backAttackName != "")
                                {
                                        Attack("Back");
                                }
                                else
                                {
                                        Attack("Short");
                                }
                        }
                        else if (Input.GetKey(KeyCode.Mouse1))
                        {
                                Attack("Long");
                        }
                }

                if (IsAnimationPlaying(shortAttackName) || IsAnimationPlaying(longAttackName) ||
                    IsAnimationPlaying(backAttackName))
                {
                        if (holdToApplyDamage)
                        {
                                if (_currentCooldown <= 0)
                                {
                                        DrawRay();
                                }
                                else
                                {
                                        _currentCooldown -= Time.deltaTime;
                                }
                        }
                        else
                        {
                                DrawRay();
                        }
                }
        }

        private void Attack(string typeOfAttack)
        { 
                currentAnimator.SetTrigger(typeOfAttack);
        }

        private bool IsAnimationPlaying(string nameOfAttack)
        {
                AnimatorStateInfo animationStateInfo = currentAnimator.GetCurrentAnimatorStateInfo(0);
                if (animationStateInfo.IsName(nameOfAttack))
                {
                        return true;
                }
                return false;
        }


        private bool ThisIsBack()
        {
                Ray ray = new Ray(transform.position, transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 3.3f))
                {
                        if (hit.collider.tag == "Back")
                        {
                                return true;
                        }
                       
                }

                return false;
        }
        private void DrawRay()
        {
                RaycastHit hit;
                if (Physics.Raycast(attackPoint.position, attackPoint.up, out hit, bladeHeight, enemyLayers))
                {
                        if (holdToApplyDamage)
                        {
                                _alreadyHits = true;
                                _currentCooldown = cooldownOfDamage;
                                hit.collider.GetComponent<EnemyHits>().OnHit(hit, gunData.damage, attackPoint.position);
                                
                        }
                        else
                        {
                                if (!_alreadyHits)
                                {
                                        if (_allHits.Count > 0)
                                        {
                                                _allHits.Clear();
                                        }
                                        _alreadyHits = true;
                                        _currentCooldown = cooldownOfDamage;
                                        EnemyHits en = hit.collider.GetComponent<EnemyHits>();
                                        en.OnHit(hit, gunData.damage, attackPoint.position);
                                        _allHits.Push(en);
                                }
                                else
                                {
                                        _currentCooldown = cooldownOfDamage;
                                        EnemyHits en = hit.collider.GetComponent<EnemyHits>();
                                        if (!_allHits.Contains(en))
                                        {
                                                en.OnHit(hit, gunData.damage, attackPoint.position);
                                                _allHits.Push(en);
                                        }
                                }
                        }
                }
        }

        private void OnDrawGizmos()
        {
                Gizmos.DrawLine(attackPoint.position, attackPoint.position + attackPoint.up * bladeHeight);
        }
}
