using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class LevelLoaderTrigger : MonoBehaviour
{
    private TileGenerator generator;
    private Room room;
    private MeshCollider collider;
    private Mesh mesh;
    public void Init(TileGenerator generator, Room room, Mesh mesh)
    {
        this.generator = generator;
        this.room = room;
        gameObject.name = "Trigger for " + room.RoomCoord;
        collider = this.AddComponent<MeshCollider>();
        collider.sharedMesh = null;
        collider.convex = true;
        this.mesh = mesh;
    }

    private void Update()
    {
        if (mesh != null)
        {
            collider.sharedMesh = mesh;
            collider.isTrigger = true;
            mesh = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<NavMeshAgent>())
        {
            generator.SpawnRoomRing(room);
            Destroy(gameObject);
        }
    }
}
