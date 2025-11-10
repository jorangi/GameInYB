using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;

[Flags]
public enum HitPosition
{
    None = 0,
    AttackPosition = 1 << 0,
    Body = 1 << 1,
    All = ~0
}

public abstract class AbilityConfig : ScriptableObject
{
    public float WDist = 0.6f, WFace = 0.3f;
    public int animIndex = 0;
    public float cooldown;
    public float enter;
    public float exit;
    public bool backAttack;
    public bool rootDuring = true;
    public float speed = 0f;
    public float AttackSize = 1.0f;
    private readonly HitPosition hit = HitPosition.AttackPosition;
    public HitPosition Hit = HitPosition.AttackPosition;
    public abstract IAbility Build(NonPlayableCharacter npc);
}