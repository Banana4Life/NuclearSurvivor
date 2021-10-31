using System;
using Unity.Entities;

[Serializable]
public struct Speed : IComponentData
{
    public float speed;
    public bool up;
}