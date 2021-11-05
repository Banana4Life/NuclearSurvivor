using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

class Room
{
    public readonly CubeCoord RoomCoord;
    public readonly int Ring;
    public readonly CubeCoord Origin;
    public readonly (CubeCoord, int)[] Centers;
    public readonly HashSet<CubeCoord> Coords;

    public Room(CubeCoord roomCoord, int ring, CubeCoord origin, (CubeCoord, int)[] centers, HashSet<CubeCoord> coords)
    {
        RoomCoord = roomCoord;
        Ring = ring;
        Origin = origin;
        Centers = centers;
        Coords = coords;
    }
}

interface CellRole
{
}

sealed class RoomRole : CellRole
{
    public readonly Room Room;

    public RoomRole(Room room)
    {
        Room = room;
    }
}

sealed class HallwayRole : CellRole
{
    public readonly List<Hallway> Hallways;

    public HallwayRole(Hallway hallways)
    {
        Hallways = new List<Hallway> { hallways };
    }
}

class Hallway
{
    public readonly Room From;
    public readonly Room To;
    public readonly List<CubeCoord> Coords;

    public Hallway(Room from, Room to, List<CubeCoord> coords)
    {
        From = from;
        To = to;
        Coords = coords;
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
    private const int RoomSize = 18;

    private Vector3 tileSize;
    private int roomRing;
    private List<Room> currentRing;
    private Dictionary<CubeCoord, Room> _rooms = new();
    private Dictionary<CubeCoord, CellRole> _roles = new();

    void Start()
    {
        Debug.Log(Random.seed);
        currentRing = new List<Room> { generateAndSpawnRoom(CubeCoord.Origin, 0) };
        roomRing = 0;
        StartCoroutine(nameof(SpawnNextRingOfRooms));
    }

    private Room generateRoom(CubeCoord roomCoord, int ring)
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
            var radius = Random.Range(3, 5 - i);
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


        var room = new Room(roomCoord, ring, origin, centers, coords);
        _rooms[room.RoomCoord] = room;
        return room;
    }

    private Room generateAndSpawnRoom(CubeCoord roomCoord, int ring)
    {
        var room = generateRoom(roomCoord, ring);
        var role = new RoomRole(room);
        foreach (var cubeCoord in room.Coords)
        {
            _roles[cubeCoord] = role;
            spawnFloor(cubeCoord);
        }

        return room;
    }

    private bool isRoomCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is RoomRole;
    private bool isHallwayCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is HallwayRole;

    private Hallway generateHallway(Room from, Room to)
    {
        var pathBetween = CubeCoord.SearchShortestPath(from.Origin, to.Origin, pathCost, pathEstimation)
            .Where(cell => !isRoomCell(cell))
            .ToList();

        return new Hallway(from, to, pathBetween);
    }

    private Hallway generateAndSpawnHallway(Room from, Room to)
    {
        var hallway = generateHallway(from, to);
        foreach (var coord in hallway.Coords)
        {
            spawnFloor(coord);
            if (_roles.TryGetValue(coord, out var existingRole))
            {
                if (existingRole is HallwayRole existingHallwayRole)
                {
                    existingHallwayRole.Hallways.Add(hallway);
                }                
            }
            else
            {
                _roles[coord] = new HallwayRole(hallway);
            }
        }

        return hallway;
    }

    private IEnumerator SpawnNextRingOfRooms()
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            roomRing++;
            var ringCoords = CubeCoord.Ring(CubeCoord.Origin, roomRing).ToList().Shuffled();
            var newRooms = new List<Room>();
            for (var i = 0; i < Mathf.CeilToInt(ringCoords.Count / 2f); i++)
            {
                newRooms.Add(generateAndSpawnRoom(ringCoords[i], roomRing));
            }

            var possibleConnections = CartesianProductWithoutDuplicated(currentRing, newRooms);
            var newRingDestinations = new HashSet<Room>();
            foreach (var (a, b) in possibleConnections)
            {
                if (newRingDestinations.Contains(a) || newRingDestinations.Contains(b))
                {
                    continue;
                }
                generateAndSpawnHallway(a, b);

                if (newRooms.Contains(a))
                {
                    newRingDestinations.Add(a);
                }
                else if (newRooms.Contains(b))
                {
                    newRingDestinations.Add(b);
                }
            }

            currentRing = newRooms;

            if (roomRing < 3)
            {
                yield return new WaitForSeconds(1);
            }
            else
            {
                break;
            }
        }
    }

    private float pathCost(CubeCoord from, CubeCoord to)
    {
        const int emptyTileCost = 4;
        //var fromRole = _roles[from];
        if (!_roles.TryGetValue(to, out var toRole))
        {
            return emptyTileCost;
        }
        
        if (toRole is RoomRole)
        {
            return 1;
        }

        if (toRole is HallwayRole)
        {
            return 1;
        }
        
        return emptyTileCost;
    }

    private float pathEstimation(CubeCoord from, CubeCoord destination)
    {
        return (float)from.Distance(destination);
    }

    private static IEnumerable<(T, T)> CartesianProductWithoutDuplicated<T>(IEnumerable<T> a, IEnumerable<T> b)
    {
        var bItems = b as T[] ?? b.ToArray();
        var seen = new HashSet<(T, T)>();
        foreach (var aItem in a)
        {
            foreach (var bItem in bItems)
            {
                var tuple = (aItem, bItem);
                if (!seen.Contains(tuple))
                {
                    seen.Add(tuple);
                    seen.Add((bItem, aItem));
                    yield return tuple;
                }
            }
        }
    }

    private void spawnFloor(CubeCoord at)
    {
        var worldPos = at.FlatTopToWorld(0, tileSize);
        var tileGo = Instantiate(floorTile, transform, true);
        tileGo.name = $"tile: {at}";
        tileGo.transform.position = worldPos;
    }

    private void OnDrawGizmos()
    {
        var colors = new[] { Color.black, Color.blue, Color.yellow, Color.red, Color.magenta };
        foreach (var (coord, role) in _roles)
        {
            if (role is RoomRole roomRole)
            {
                var ring = roomRole.Room.Ring;
                if (ring < colors.Length)
                {
                    Gizmos.color = colors[ring];
                }
                else
                {
                    Gizmos.color = Color.white;
                }
            }
            else
            {
                Gizmos.color = Color.gray;
            }
            Gizmos.DrawWireSphere(coord.FlatTopToWorld(0, tileSize), tileSize.z * 0.5f);
        }

    }
}
