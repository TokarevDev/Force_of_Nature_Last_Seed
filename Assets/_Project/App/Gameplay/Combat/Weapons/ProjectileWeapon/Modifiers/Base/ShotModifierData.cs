using UnityEngine;

public abstract class ShotModifierData : ScriptableObject
{
    public abstract IShotModifier CreateRuntime();
}