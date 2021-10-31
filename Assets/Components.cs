using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Speed : IComponentData
{
    public float value;
    public bool up;

    public static Speed Of(float value) => new()
    {
        value = value
    };
}

public struct Ttl : IComponentData
{
    public float value;
}

public struct Target : IComponentData
{
    public Vector3 value;
}

public struct PotentialTargetTag : IComponentData
{
}