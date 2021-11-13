using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class TileGenerator : MonoBehaviour
{
    public TileDictionary tiledict;

    public const int RoomSize = 18;

    private Dictionary<CubeCoord, TileArea> _areas = new();
    private Dictionary<CubeCoord, Room> _rooms = new();
    private Dictionary<CubeCoord, CellRole> _roles = new();
    
    public GameObject roomPrefab;
    public int floorHeight = 0;
    public int widePath = 2;

    public bool showPathFindingResults;
    
    private readonly List<ShortestPath.PathFindingResult<CubeCoord, float>> _recentPathSearches = new();

    public AreaFloorBaker[] areaFloorBakers;
    void Start()
    {
        Debug.Log(Random.seed);
        var room = GenerateAndSpawnRoom(CubeCoord.Origin);
        SpawnRoomRing(room);
        foreach (var areaFloorBaker in areaFloorBakers)
        {
            areaFloorBaker.Activate();
        }
    }

    private Room generateRoom(CubeCoord roomCoord)
    {
        var origin = roomCoord * RoomSize;
        var coords = new HashSet<CubeCoord>();
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

        var room = new Room(roomCoord, origin, centers, coords, Instantiate(roomPrefab, transform).GetComponent<TileArea>(),
                roomCoord.FlatTopToWorld(floorHeight, tiledict.TileSize()) * RoomSize);
        _rooms[room.RoomCoord] = room;
        return room;
    }

    private Room GenerateAndSpawnRoom(CubeCoord roomCoord)
    {
        if (!CanGenerateRoom(roomCoord))
        {
            return null;
        }
        var room = generateRoom(roomCoord);
        foreach (var cellCoord in room.Coords)
        {
            _roles[cellCoord] = new RoomRole(room);
            _areas[cellCoord] = room.TileArea;
        }
        room.TileArea.Init(this, room);
        foreach (var areaFloorBaker in areaFloorBakers)
        {
            areaFloorBaker.UpdateNavMesh(room.TileArea);
        }
        return room;
    }

    private bool CanGenerateRoom(CubeCoord roomCoord)
    {
        return !_rooms.ContainsKey(roomCoord);
    }

    public bool IsCell(CubeCoord coord) => _roles.ContainsKey(coord);
    
    private bool isRoomCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is RoomRole;
    private bool isHallwayCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is HallwayRole;

    private Hallway generateHallway(Room from, Room to)
    {
        var fromCenter = from.Centers[Random.Range(0, from.Centers.Length)].Item1;
        var toCenter = to.Centers[Random.Range(0, to.Centers.Length)].Item1;
        // var pathResult = CubeCoord.SearchShortestPath(fromCenter, toCenter, pathCost, pathEstimation);
        // _recentPathSearches.Add(pathResult);

        var thinPath = SearchPath(fromCenter, toCenter);
        var fullPath = thinPath.SelectMany(cellCoord => cellCoord.FlatTopNeighbors().Shuffled().Take(widePath).Concat(new []{cellCoord})).Distinct().ToList();
        var path = fullPath.ToLookup(cell => isRoomCell(cell) || isHallwayCell(cell));

        var nav = Instantiate(roomPrefab, transform).GetComponent<TileArea>();
        var hallway = new Hallway(@from, to, path, nav);
        return hallway;
    }
    
    
    public static List<CubeCoord> SearchPath(CubeCoord from, CubeCoord to)
    {
        List<CubeCoord> path = new List<CubeCoord>();
        var distance = from.Distance(to);
        var last = from;
        while (true)
        {
            var next = @last.FlatTopNeighbors().Where(coord => !path.Contains(coord))
                .Select(coord => (coord, coord.Distance(to))).Where(cd => cd.Item2 <= distance)
                .ToList().Shuffled().First();
            path.Add(next.coord);
            last = next.coord;
            distance = next.Item2;
            if (next.coord.Equals(to))
            {
                break;
            }
        }

        return path;
    }

    private Hallway GenerateAndSpawnHallway(Room from, Room to)
    {
        var hallway = generateHallway(from, to);
        foreach (var coord in hallway.Coords)
        {
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
                _areas[coord] = hallway.TileArea;
            }
        }
        hallway.TileArea.Init(this, hallway);

        foreach (var nav in hallway.Intersecting.Concat(hallway.Coords).SelectMany(cell => cell.FlatTopNeighbors()).Distinct().Where(cell => _areas.ContainsKey(cell)).Select(cell => _areas[cell]).Distinct())
        {
            nav.UpdateWalls();
            foreach (var areaFloorBaker in areaFloorBakers)
            {
                areaFloorBaker.UpdateNavMesh(nav);
            }
        }
        return hallway;
    }
  
    public void SpawnRoomRing(Room room)
    {
        Debug.Log("Loading next Rooms around " + room);
        var ringCoords = CubeCoord.Ring(room.RoomCoord, 1).ToList().Shuffled();
        var newRooms = new List<Room>();
        int generated = 0;
        var maxToGenerate = Mathf.CeilToInt(ringCoords.Count / 2f);
        foreach (var ringCoord in ringCoords)
        {
            if (generated >= maxToGenerate)
            {
                break;
            }

            var newRoom = GenerateAndSpawnRoom(ringCoord);
            if (newRoom != null)
            {
                newRooms.Add(newRoom);
                generated++;
            }
        }

        _recentPathSearches.Clear();
        var newRingDestinations = new HashSet<Room>();
        foreach (var newRoom in newRooms)
        {
            if (newRingDestinations.Contains(room) || newRingDestinations.Contains(newRoom))
            {
                continue;
            }

            GenerateAndSpawnHallway(room, newRoom);
            if (newRooms.Contains(room))
            {
                newRingDestinations.Add(room);
            }
            else if (newRooms.Contains(newRoom))
            {
                newRingDestinations.Add(newRoom);
            }
        }
    }

    public int emptyTileCostMin = 1;
    public int emptyTileCostMax = 10;
    public int straightTileCostMin = 4;
    public int straightTileCostMax = 20;
    public int roomCost = 4;
    public int hallwayCost = 2;

    private float pathCost(Dictionary<CubeCoord, CubeCoord> prevMap, CubeCoord from, CubeCoord to)
    {
        // var emptyTileCost = Random.Range(emptyTileCostMin, emptyTileCostMax);
        var emptyTileCost = emptyTileCostMin;
        if (prevMap.TryGetValue(from, out var prev))
        {
            if (from - prev == to - from)
            {
                // emptyTileCost = Random.Range(straightTileCostMin, straightTileCostMax);
                emptyTileCost = straightTileCostMin;
            }    
        }
        
        if (!_roles.TryGetValue(to, out var toRole))
        {
            return emptyTileCost;
        }
        
        if (toRole is RoomRole)
        {
            return roomCost;
        }

        if (toRole is HallwayRole)
        {
            return hallwayCost;
        }
        
        return emptyTileCost;
    }

    private float pathEstimation(CubeCoord from, CubeCoord destination)
    {
        return from.ManhattenDistance(destination);
    }

    private void OnDrawGizmos()
    {
        void ShowResult(ShortestPath.PathFindingResult<CubeCoord, float> result)
        {
            foreach (var cubeCoord in result.Path)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(cubeCoord.FlatTopToWorld(0, tiledict.TileSize()), tiledict.TileSize().x / 2f);
            }

            foreach (var open in result.OpenQueue)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(open.FlatTopToWorld(0, tiledict.TileSize()), tiledict.TileSize().x / 2f);
            }

            
            float worstDistance = result.ClosedSet.Max(c => pathEstimation(c, result.To));

            foreach (var closed in result.ClosedSet)
            {
                if (result.Path.Contains(closed))
                {
                    continue;
                }

                Gizmos.color = Color.Lerp(Color.green, Color.red, pathEstimation(closed, result.To) / worstDistance);
                Gizmos.DrawSphere(closed.FlatTopToWorld(0, tiledict.TileSize()), tiledict.TileSize().x / 2f);
            }
        }
        if (showPathFindingResults && _recentPathSearches.Count > 0)
        {
            ShowResult(_recentPathSearches.Last());
        }
    }

    public IEnumerable<TileArea> NavigatableAt(CubeCoord coord)
    {
        if (_roles.TryGetValue(coord, out var role))
        {
            if (role is HallwayRole hallways)
            {
                return hallways.Hallways.Select(h => h.TileArea);
            }

            if (role is RoomRole room)
            {
                return new List<TileArea>() { room.Room.TileArea };
            }
        }

        return Array.Empty<TileArea>();
    }

    public Room FindNearbyRoom(Vector3 pos, HashSet<Room> visited, float maxDistance)
    {
        if (visited.Count == 0)
        {
            return _rooms[CubeCoord.Origin];
        }
        var current = CubeCoord.FlatTopFromWorld(pos, tiledict.TileSize());
        if (_roles.TryGetValue(current, out var currentCell))
        {
            if (currentCell is RoomRole roomCell)
            {
                var roomCoord = roomCell.Room.RoomCoord;
                var potentialRooms = CubeCoord.Spiral(roomCoord, 0, 2)
                    .Where(c => _rooms.ContainsKey(c))
                    .Select(c => _rooms[c])
                    .Where(r => !visited.Contains(r))
                    .ToList();
                if (potentialRooms.Count != 0)
                {
                    return potentialRooms.Shuffled().First();        
                }
            }
            if (currentCell is HallwayRole hallwayCell)
            {
                foreach (var hallway in hallwayCell.Hallways)
                {
                    if (!visited.Contains(hallway.To))
                    {
                        return hallway.To;
                    }
                    if (!visited.Contains(hallway.From))
                    {
                        return hallway.From;
                    }
                }
            }

        }
        Debug.Log("Failed to find nearby Room. Picking a random room");
        return _rooms.Select(r => r.Value)
            .Where(r => !visited.Contains(r))
            .Where(r => (r.WorldCenter - pos).sqrMagnitude < maxDistance * maxDistance)
            .ToList().Shuffled().FirstOrDefault();
    }
}
