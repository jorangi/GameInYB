using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class AbilityConfig : ScriptableObject
{
    public float WDist = 0.6f, WFace = 0.3f;
    public int animIndex = 0;
    public float cooldown;
    public float enter;
    public float exit;
    public abstract IAbility Build(NonPlayableCharacter npc);
}