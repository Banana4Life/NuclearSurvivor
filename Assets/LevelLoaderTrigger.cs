using UnityEngine;
using UnityEngine.AI;

public class LevelLoaderTrigger : MonoBehaviour
{
    private TileGenerator generator;
    private Room room;
  

    public void Init(TileGenerator generator, Room room)
    {
        this.generator = generator;
        this.room = room;
        gameObject.name = "Trigger for " + room.RoomCoord;
    }

    public void triggerEnter(Collider other)
    {
        if (other.GetComponent<NavMeshAgent>())
        {
            generator.SpawnRoomRing(room);
            Destroy(gameObject);
        }
    }
}
