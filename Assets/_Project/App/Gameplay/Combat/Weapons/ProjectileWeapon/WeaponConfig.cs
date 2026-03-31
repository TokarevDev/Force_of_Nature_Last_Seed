using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Weapon Config")]
public sealed class WeaponConfig : ScriptableObject
{
    public event Action OnModifiersChanged;

    [Header("Base")]
    public float FireRate = 1.4f;

    [Header("Projectile")]
    public ProjectileConfig Projectile;

    [Header("Modifiers")]
    [SerializeField] private List<ShotModifierData> _modifiers = new();

    public IReadOnlyList<ShotModifierData> Modifiers => _modifiers;

    public void AddModifier(ShotModifierData data)
    {
        if (data == null) return;

        _modifiers.Add(data);
        OnModifiersChanged?.Invoke();
    }

    public void RemoveModifier(ShotModifierData data)
    {
        if (data == null) return;

        _modifiers.Remove(data);
        OnModifiersChanged?.Invoke();
    }

    public void ClearModifiers()
    {
        _modifiers.Clear();
        OnModifiersChanged?.Invoke();
    }

    public WeaponConfig CreateRuntimeInstance()
    {
        var instance = Instantiate(this);

        instance._modifiers = new List<ShotModifierData>(_modifiers);

        return instance;
    }
}