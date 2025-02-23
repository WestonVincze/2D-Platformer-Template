﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    private Animator _Animator;
    private Character _Character;

    private bool _IsDead;
    private bool _IsFalling;
    private bool _IsRunning;

    private float _StaticAnimationTime;
    private float _PriorityAnimationTime;


    private Dictionary<AnimationState, float> _AnimationTimes = new Dictionary<AnimationState, float>();
    private AnimationState _CurrentAnimation;
    private bool _InMineCart;

    public bool _IsBoss;
    // shield enemy 
    public bool _HoldsShield = false;
    private bool _ShieldBroken = false;


    public Dictionary<AnimationState, float> AnimationTimes => _AnimationTimes;

    // Dynamic animations are fluid and handeled within the UpdateAnimations function
    // Static animations will prevent dynamic animation logic until it has played through
    // Priority animations will prevent ALL other animations until it has played through
    private enum AnimationType
    {
        Dynamic,
        Static,
        Priority
    }

    /* TODO: store animation clips as Hash ID's (Animator.StringToHash("your_clip_name") to improve performance */
    public enum AnimationState {
        None,
        Idle,
        RunStart,
        Run,
        RunStop,
        Jump,
        JumpToFall,
        Fall,
        Landing,
        Dodge,
        Hurt,
        Death,
        ShieldHurt,
        ShieldBreak,
        MineCart,
        RunNoShield,
        IdleNoShield,
        Scream,
        Attack1,
        Attack2,
        Puke,
        RunWithRevolver,
        RunStartWithRevolver,
        JumpWithRevolver,
        IdleWithRevolver,
        FallWithRevolver,
        RunWithShotgun,
        RunStartWithShotgun,
        JumpWithShotgun,
        IdleWithShotgun,
        FallWithShotgun
    }

    void Start()
    {
        _Animator = GetComponentInChildren<Animator>();
        _Character = GetComponent<Character>();
        _InMineCart = false;

        if (_Character == null) print("CharacterAnimation couldn't find Character to assign to _Character.");
        if (_Animator == null) print("CharacterAnimation couldn't find Animator component to assign to _Animator.");

        UpdateAnimationTimes();
    }

    // Populates _AnimationTime Dict with animation states and their respective durations
    private void UpdateAnimationTimes()
    {
        AnimationClip[] clips = _Animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            // TODO: (potentially) update logic... nested looping is ineffecient.
            foreach (AnimationState state in Enum.GetValues(typeof(AnimationState)))
            {
                if (state.ToString() == clip.name)
                {
                    if (_AnimationTimes.ContainsKey(state)) break;
                    _AnimationTimes.Add(state, clip.length);
                }
            }
        }
    }

    void Update()
    {
        UpdateAnimationCooldowns();
        DynamicAnimations();
    }

    // handles dynamic animations
    public void DynamicAnimations()
    {
        if (PriorityAnimationPlaying() || StaticAnimationPlaying() || _IsDead) return;

        if (_InMineCart) {
            ChangeAnimationState(AnimationState.MineCart);
            return;
        }
        if (_IsBoss)
        {
            ChangeAnimationState(AnimationState.Idle);
            return;
        }
        if (_Character.IsGrounded)
        {
            if (_IsFalling)
            {
                _IsFalling = false;
                Landing();
            }
            else if (_IsRunning && !_Character.IsLocked)
            {
                if (_HoldsShield && _ShieldBroken)
                {
                    ChangeAnimationState(AnimationState.RunNoShield);
                }
                else
                {
                    Run();
                }
            }
            else
            {
                if (_HoldsShield && _ShieldBroken)
                {
                    ChangeAnimationState(AnimationState.IdleNoShield);
                }
                else if (_Character.HoldingRevolver)
                {
                    ChangeAnimationState(AnimationState.IdleWithRevolver);
                }
                else if (_Character.HoldingShotgun)
                {
                    ChangeAnimationState(AnimationState.IdleWithShotgun);
                }
                else
                {
                    ChangeAnimationState(AnimationState.Idle);
                }
            }
        }
        else if (_Character.RigidBody2D.velocity.y < 0)
        {
            if (_IsFalling)
            {
                if (_Character.HoldingRevolver)
                {
                    ChangeAnimationState(AnimationState.FallWithRevolver);
                }
                else if (_Character.HoldingShotgun)
                {
                    ChangeAnimationState(AnimationState.FallWithShotgun);
                }
                else
                {
                    ChangeAnimationState(AnimationState.Fall);
                }
            }
            else
            {
                _IsFalling = true;
                ChangeAnimationState(AnimationState.JumpToFall);
            }
        }
        else
        {
            Jump();
        }
    }


    private void UpdateAnimationCooldowns()
    {
        if (StaticAnimationPlaying()) _StaticAnimationTime -= Time.deltaTime;
        if (PriorityAnimationPlaying()) _PriorityAnimationTime -= Time.deltaTime;
    }

    private bool StaticAnimationPlaying()
    {
        return _StaticAnimationTime > 0;
    }

    private bool PriorityAnimationPlaying()
    {
        return _PriorityAnimationTime > 0;
    }

    private void ChangeAnimationState(AnimationState newState, AnimationType animationType = AnimationType.Dynamic)
    {
        if (!_AnimationTimes.ContainsKey(newState)) return;
        if (_CurrentAnimation == newState || (PriorityAnimationPlaying() && animationType != AnimationType.Priority)) return;

        // oof I dun like this, but it must be done to sync those sexy run animations
        if ((_CurrentAnimation == AnimationState.Run || _CurrentAnimation == AnimationState.RunWithRevolver || _CurrentAnimation == AnimationState.RunStartWithShotgun)
              && (newState == AnimationState.Run || newState == AnimationState.RunWithRevolver || newState == AnimationState.RunWithShotgun))
        {
            var normalizedTime = _Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            _Animator.Play(newState.ToString(), -1, normalizedTime - Mathf.Floor(normalizedTime));
        }
        else 
        {
            _Animator.Play(newState.ToString());
        }


        _CurrentAnimation = newState;

        switch (animationType)
        {
            case (AnimationType.Dynamic):
                break;
            case (AnimationType.Static):
                SetStaticAnimationDelay(_AnimationTimes[newState]);
                break;
            case (AnimationType.Priority):
                SetPriorityAnimationDelay(_AnimationTimes[newState]);
                break;
        }
    }

    public void Movement()
    {
        if (_Character.IsMoving)
        {
            RunStart();
        }
        else
        {
            RunStop();
        }
    }

    private void Run()
    {
        if (_Character.HoldingRevolver)
        {
            ChangeAnimationState(AnimationState.RunWithRevolver);
        }
        else if (_Character.HoldingShotgun)
        {
            ChangeAnimationState(AnimationState.RunWithShotgun);
        }
        else 
        {
            ChangeAnimationState(AnimationState.Run);
        }
    }

    private void RunStart()
    {
        if (!_Character.IsGrounded || _IsRunning) return;
        _IsRunning = true;

        if (_Character.HoldingRevolver)
        {
            ChangeAnimationState(AnimationState.RunStartWithRevolver, AnimationType.Static);
        }
        else if (_Character.HoldingShotgun)
        {
            ChangeAnimationState(AnimationState.RunStartWithShotgun, AnimationType.Static);
        }
        else
        {
            ChangeAnimationState(AnimationState.RunStart, AnimationType.Static);
        }
    }

    private void RunStop()
    {
        if (!_Character.IsGrounded || !_IsRunning) return;
        _IsRunning = false;

        if (_CurrentAnimation != AnimationState.Run) return;
        ChangeAnimationState(AnimationState.RunStop, AnimationType.Static);
    }

    public void Jump()
    {
        if (_Character.HoldingRevolver)
        {
            ChangeAnimationState(AnimationState.JumpWithRevolver, AnimationType.Priority);
        }
        else if (_Character.HoldingShotgun)
        {
            ChangeAnimationState(AnimationState.JumpWithShotgun, AnimationType.Priority);
        }
        else
        {
            ChangeAnimationState(AnimationState.Jump, AnimationType.Priority);
        }
    }

    private void Landing()
    {
        ChangeAnimationState(AnimationState.Landing, AnimationType.Static);
    }

    public void Dodge()
    {
        ChangeAnimationState(AnimationState.Dodge, AnimationType.Priority);
    }

    public void Hurt()
    {
        ChangeAnimationState(AnimationState.Hurt, AnimationType.Priority);
    }

    public void Die()
    {
        _IsDead = true;

            ChangeAnimationState(AnimationState.Death, AnimationType.Priority); 
    }

    public void ShieldHurt()
    {
        ChangeAnimationState(AnimationState.ShieldHurt, AnimationType.Priority); 
    }

    public void ShieldBreak()
    {
        _ShieldBroken = true;
        ChangeAnimationState(AnimationState.ShieldBreak, AnimationType.Priority); 
    }

    public void Attack1()
    {
        ChangeAnimationState(AnimationState.Attack1, AnimationType.Priority); 
    }

    public void Attack2()
    {
        ChangeAnimationState(AnimationState.Attack2, AnimationType.Priority); 
    }

    public void Scream()
    {
        ChangeAnimationState(AnimationState.Scream, AnimationType.Priority);
    }

    public void Puke()
    {
        ChangeAnimationState(AnimationState.Puke, AnimationType.Priority);
    }


    public void EnterMineCart()
    {
        _InMineCart = true;
    }

    public void ExitMineCart()
    {
        _InMineCart = false;
    }

    private void SetStaticAnimationDelay(float delay)
    {
        _StaticAnimationTime = delay;
    }

    private void SetPriorityAnimationDelay(float delay)
    {
        _PriorityAnimationTime = delay;
    }
}
