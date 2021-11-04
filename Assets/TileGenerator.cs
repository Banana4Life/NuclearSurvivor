using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class Room
{
    public readonly CubeCoord RoomCoord;
    public readonly CubeCoord Origin;
    public readonly (CubeCoord, int)[] Centers;
    public readonly HashSet<CubeCoord> Coords;
    private readonly HashSet<CubeCoord> _outlineCoords;
    public readonly int Connections;

    public Room(CubeCoord roomCoord, CubeCoord origin, (CubeCoord, int)[] centers, HashSet<CubeCoord> coords, HashSet<CubeCoord> outlineCoords, int connections)
    {
        RoomCoord = roomCoord;
        Origin = origin;
        Centers = centers;
        Coords = coords;
        _outlineCoords = outlineCoords;
        Connections = connections;
    }
}

class Hallway
{
    public readonly Room From;
    public readonly Room To;

    public Hallway(Room from, Room to)
    {
        From = from;
        To = to;
    }
}

class WorldNetwork
{
    private List<Room> _rooms;
    private List<Hallway> _hallways;
}

public class TileGenerator : MonoBehaviour
{
    public MeshRenderer referenceTile;

    public GameObject floorTile;
    private const int RoomSize = 32;

    private Vector3 tileSize;
    private int roomRing = 0;
    private Dictionary<CubeCoord, Room> _rooms = new();

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Random.seed);
        StartCoroutine(nameof(SpawnNextRingOfRooms));
    }

    private Room generateRoom(CubeCoord roomCoord)
    {
        var origin = roomCoord * RoomSize;
        var coords = new HashSet<CubeCoord>();
        tileSize = referenceTile.bounds.size;
        var max = Random.Range(2, 4);
        var centers = new (CubeCoord, int)[max];
        var center = origin;
        coords.Add(center);
        var centerCandidates = new List<CubeCoord>();
        for (var i = 0; i < max; ++i)
        {
            var radius = Random.Range(3, 6 - i);
            centers[i] = (center, radius);
            var innerTiles = CubeCoord.Spiral(center, 1, radius - 2);
            var outerTiles = CubeCoord.Spiral(center, radius - 2, radius).ToList();
            foreach (var c in innerTiles)
            {
                coords.Add(c);
                centerCandidates.Remove(c);
            }
            foreach (var c in outerTiles)
            {
                coords.Add(c);
                centerCandidates.Add(c);
            }
            center = centerCandidates[Random.Range(0, outerTiles.Count)];
        }

        var outline = CubeCoord.Outline(coords);

        var room = new Room(roomCoord, origin, centers, coords, outline.ToHashSet(), 0);
        _rooms[room.RoomCoord] = room;
        return room;
    }

    private IEnumerator SpawnNextRingOfRooms()
    {
        while (true)
        {
            var ringCoords = CubeCoord.Ring(CubeCoord.Origin, roomRing++).ToList().Shuffled();
            for (var i = 0; i < Mathf.CeilToInt(ringCoords.Count / 2f); i++)
            {
                var ringCoord = ringCoords[i];
                foreach (var cubeCoord in generateRoom(ringCoord).Coords)
                {
                    spawnFloor(cubeCoord);
                }
            }

            yield return new WaitForSeconds(2);
        }
    }

    private void spawnFloor(CubeCoord at)
    {
        var worldPos = at.FlatTopToWorld(0, tileSize);
        var tileGo = Instantiate(floorTile, transform, true);
        tileGo.name = $"tile: {at}";
        tileGo.transform.position = worldPos;
    }
}
