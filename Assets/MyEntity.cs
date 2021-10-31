using System;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct MyEntity : IComponentData
{
    public Entity prefab;
    public float timeUntil;
    public float deltaTime;

}