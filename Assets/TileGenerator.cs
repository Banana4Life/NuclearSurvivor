using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.VisualScripting;
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

public abstract class CellRole
{
    public readonly AutoTile Tile;

    protected CellRole(AutoTile tile)
    {
        Tile = tile;
    }
}

sealed class RoomRole : CellRole
{
    public readonly Room Room;

    public RoomRole(Room room, AutoTile tile) : base(tile)
    {
        
        Room = room;
    }
}

sealed class HallwayRole : CellRole
{
    public readonly List<Hallway> Hallways;

    public HallwayRole(Hallway hallways, AutoTile autoTile) : base(autoTile)
    {
        Hallways = new List<Hallway> { hallways };
    }
}

class Hallway
{
    public readonly Room From;
    public readonly Room To;
    public readonly List<CubeCoord> Coords;
    public readonly List<(CubeCoord cell, List<CubeCoord> connections)> Connectors;

    public Hallway(Room @from, Room to, List<CubeCoord> coords, List<(CubeCoord cell, List<CubeCoord> connections)> connectors)
    {
        From = from;
        To = to;
        Coords = coords;
        Connectors = connectors;
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
    
    public GameObject roomPrefab;
    public int floorHeight = 0;

    void Start()
    {
        Debug.Log(Random.seed);
        currentRing = new List<Room> { generateAndSpawnRoom(CubeCoord.Origin, 0) };
        roomRing = 0;
        SpawnNextRingOfRooms(null);
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
        var parent = Instantiate(roomPrefab, transform);
        parent.name = "Room";
        parent.transform.position = roomCoord.FlatTopToWorld(floorHeight, tileSize);
        foreach (var cubeCoord in room.Coords)
        {
            _roles[cubeCoord] = new RoomRole(room, spawnFloor(parent.transform, cubeCoord));
        }
        foreach (var coord in room.Coords)
        {
            _roles[coord].Tile.Init(coord);
        }
        
        parent.GetComponent<NavMeshSurface>().BuildNavMesh();

        return room;
    }

    private bool isRoomCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is RoomRole;
    private bool isHallwayCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is HallwayRole;

    private Hallway generateHallway(Room from, Room to)
    {
        var fullPath = CubeCoord.SearchShortestPath(@from.Origin, to.Origin, pathCost, pathEstimation);
        var pathBetween = fullPath
            .Where(cell => !isRoomCell(cell))
            .ToList();
        foreach (var cell in pathBetween)
        {
            fullPath.Remove(cell);
        }

        var connectors = 
            fullPath.Select(roomCell => (roomCell, pathBetween.Where(coord => coord.IsAdjacent(roomCell)).ToList()))
                .Where(t => t.Item2.Count != 0)
                .ToList();
        return new Hallway(from, to, pathBetween, connectors);
    }

    private Hallway generateAndSpawnHallway(Room from, Room to)
    {
        var parent = Instantiate(roomPrefab, transform);
        parent.name = "Hallway";
        parent.transform.position = Vector3.Lerp(from.Origin.FlatTopToWorld(floorHeight, tileSize),
            to.Origin.FlatTopToWorld(floorHeight, tileSize), 0.5f);
        var hallway = generateHallway(from, to);
        foreach (var coord in hallway.Coords)
        {
            var autoTile = spawnFloor(parent.transform, coord);
            if (_roles.TryGetValue(coord, out var existingRole))
            {
                if (existingRole is HallwayRole existingHallwayRole)
                {
                    existingHallwayRole.Hallways.Add(hallway);
                }                
            }
            else
            {
                _roles[coord] = new HallwayRole(hallway, autoTile);
            }
        }
        
        foreach (var coord in hallway.Coords)
        {
            _roles[coord].Tile.Init(coord);
        }

        var triggerTiles = GenerateTriggerTiles(hallway, parent);
        parent.GetComponent<NavMeshSurface>().BuildNavMesh();
        foreach (var triggerTile in triggerTiles)
        {
            triggerTile.HideTrigger();
        }
        
        return hallway;
    }

    private IEnumerable<AutoTile> GenerateTriggerTiles(Hallway hallway, GameObject parent)
    {
        foreach (var (coord, connections) in hallway.Connectors)
        {
            var autoTile = spawnFloor(parent.transform, coord).Init(coord, true);
            foreach (var cubeCoord in connections)
            {
                var cubeDirection = cubeCoord - coord;

                var link = autoTile.AddComponent<NavMeshLink>();
                var direction = cubeDirection.FlatTopToWorld(floorHeight, tileSize);
                link.startPoint = direction / 2;
                link.endPoint = direction / 2 + direction.normalized * 0.1f;
                link.width = tileSize.x / 2;
            }

            yield return autoTile;
        }
    }

    public void SpawnNextRingOfRooms(AutoTile trigger)
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

    private AutoTile spawnFloor(Transform parent, CubeCoord at)
    {
        return Instantiate(floorTile, parent, true).GetComponent<AutoTile>();
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

    public AutoTile TileAt(CubeCoord cubeCoord)
    {
        return _roles.TryGetValue(cubeCoord, out var tile) ? tile.Tile : null;
    }
}
