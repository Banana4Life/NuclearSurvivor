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

    public AreaFloorBaker[] areaFloorBakers;
    void Start()
    {
        Debug.Log(Random.seed);
        SpawnLevel();
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
    
    public bool HasDirectPathWithoutRoom(CubeCoord from, CubeCoord to)
    {
        List<CubeCoord> path = new List<CubeCoord>();
        var distance = from.Distance(to);
        var last = from;
        while (true)
        {
            var next = @last.FlatTopNeighbors().Where(coord => !path.Contains(coord))
                .Select(coord => (coord, coord.Distance(to))).Where(cd => cd.Item2 <= distance)
                .ToList().OrderBy(cd => (int)cd.Item2).First();
            
            path.Add(next.coord);
            last = next.coord;
            distance = next.Item2;
            if (next.coord.Equals(to))
            {
                break;
            }
            if (_rooms.ContainsKey(next.coord))
            {
                return false;
            }
        }

        return true;
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
  
    public void SpawnLevel()
    {
        var ringCoords = new []{CubeCoord.Origin}.Concat(CubeCoord.Spiral(CubeCoord.Origin, 1 , 5).ToList().Shuffled()).ToList();
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

        HashSet<CubeCoord> connectedSet = new();
        Dictionary<CubeCoord, List<CubeCoord>> connections = new();
        connectedSet.Add(CubeCoord.Origin);
        while (connectedSet.Count < _rooms.Count)
        {
            AddRoomConnection(connectedSet, connections);
        }

        foreach (var startRoom in connections.Where(e => e.Value.Count == 1))
        {
            if (Random.value < 0.9f)
            {
                var cubeCoords = CubeCoord.Spiral(startRoom.Key, 2, 4).Where(c => _rooms.ContainsKey(c));
                var distances = GetDistances(connections, startRoom.Key);
                foreach (var targetRoom in cubeCoords.Where(c => !distances.ContainsKey(c) || distances[c] >= 4d).ToList().Shuffled())
                {
                    if (HasDirectPathWithoutRoom(startRoom.Key, targetRoom))
                    {
                        GenerateAndSpawnHallway(_rooms[startRoom.Key], _rooms[targetRoom]);
                    }
                    break;
                }
            }
        }
    }

    private static Dictionary<CubeCoord, double> GetDistances(Dictionary<CubeCoord, List<CubeCoord>> connections, CubeCoord start, int depthToGo = 5)
    {
        var currentSet = new HashSet<CubeCoord>();
        var result = new Dictionary<CubeCoord, double>();
        result[start] = 0;
        var nextSet = new HashSet<CubeCoord>();
        currentSet.Add(start);
        for (int i = depthToGo; i > 0; i--)
        {
            foreach (var currentCoord in currentSet)
            {
                foreach (var nextCoord in connections[currentCoord])
                {
                    if (!result.ContainsKey(nextCoord))
                    {
                        var distance = nextCoord.Distance(currentCoord);
                        result[nextCoord] = distance + result[currentCoord];
                        nextSet.Add(nextCoord);    
                    }
                }
            }

            currentSet = nextSet;
            nextSet = new HashSet<CubeCoord>();
        }

        return result;
    }

    private void AddRoomConnection(HashSet<CubeCoord> connectedSet, Dictionary<CubeCoord, List<CubeCoord>> connections)
    {
        for (int r = 1; r < 5; r++)
        {
            foreach (var roomToConnect in connectedSet.ToList().Shuffled())
            {
                foreach (var potentialRoom in CubeCoord.Ring(roomToConnect, r).Shuffled())
                {
                    if (_rooms.ContainsKey(potentialRoom) && !connectedSet.Contains(potentialRoom))
                    {
                        GenerateAndSpawnHallway(_rooms[roomToConnect], _rooms[potentialRoom]);
                        connectedSet.Add(potentialRoom);
                        if (!connections.TryGetValue(roomToConnect, out var list))
                        {
                            list =new();
                            connections[roomToConnect] = list;
                        }
                        list.Add(potentialRoom);
                        
                        if (!connections.TryGetValue(potentialRoom, out var list2))
                        {
                            list2 = new();
                            connections[potentialRoom] = list2;
                        }
                        list2.Add(roomToConnect);
                        return;
                    }
                }
            }
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
