using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Speed : IComponentData
{
    public float speed;
    public bool up;
}

public struct Ttl : IComponentData
{
    public float value;
}

public struct Target : IComponentData
{
    public Vector3 value;
}