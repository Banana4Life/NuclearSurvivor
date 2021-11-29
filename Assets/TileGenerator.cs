using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FlatTop;
using Unity.AI.Navigation;
using UnityEngine;
using static TileDictionary.TileType;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Game))]
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

    public GameObject editorTiles;
    public float uvFactor = 10f;

    public int paintInfluenceHex = 3;
    public float additionalHallwayChance = 0.1f;
    public int levelRings = 5;

    private Dictionary<CubeCoord, List<CubeCoord>> _connections;
    private Game _game;

    void Start()
    {
        _game = GetComponent<Game>();
        Debug.Log(Random.seed);
        editorTiles.SetActive(false);
        Destroy(editorTiles);
        SpawnLevel();
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
                roomCoord.ToWorld(floorHeight, tiledict.TileSize()) * RoomSize);
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
        return room;
    }

    private bool CanGenerateRoom(CubeCoord roomCoord)
    {
        return !_rooms.ContainsKey(roomCoord);
    }

    public bool IsCell(CubeCoord coord) => _roles.ContainsKey(coord);
    
    private bool IsRoomCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is RoomRole;
    private bool IsHallwayCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is HallwayRole;

    private Hallway GenerateHallway(Room from, Room to)
    {
        var fromCenter = from.Centers[Random.Range(0, from.Centers.Length)].Item1;
        var toCenter = to.Centers[Random.Range(0, to.Centers.Length)].Item1;

        var thinPath = SearchPath(fromCenter, toCenter);
        var fullPath = thinPath.SelectMany(cellCoord => cellCoord.Neighbors().Shuffled().Take(widePath).Concat(new []{cellCoord})).Distinct().ToList();
        var path = fullPath.ToLookup(cell => IsRoomCell(cell) || IsHallwayCell(cell));

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
            var next = @last.Neighbors().Where(coord => !path.Contains(coord))
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
            var next = @last.Neighbors().Where(coord => !path.Contains(coord))
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
        var hallway = GenerateHallway(from, to);
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

        foreach (var area in hallway.Intersecting.Concat(hallway.Coords).SelectMany(cell => cell.Neighbors()).Distinct().Where(cell => _areas.ContainsKey(cell)).Select(cell => _areas[cell]).Distinct())
        {
            area.UpdateWalls();
        }
        return hallway;
    }
  
    public void SpawnLevel()
    {
        Debug.Log("Spawning Level... " + Time.realtimeSinceStartup);
        var ringCoords = new []{CubeCoord.Origin}.Concat(CubeCoord.Spiral(CubeCoord.Origin, 1 , levelRings).ToList().Shuffled()).ToList();
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
        
        Debug.Log("Connecting Rooms... " + Time.realtimeSinceStartup);

        HashSet<CubeCoord> connectedSet = new();
        _connections = new();
        connectedSet.Add(CubeCoord.Origin);
        var newHallways = new List<Hallway>();
        while (connectedSet.Count < _rooms.Count)
        {
            var hallway = AddRoomConnection(connectedSet, _connections);
            if (hallway != null)
            {
                newHallways.Add(hallway);
            }
        }

        Debug.Log("Connecting More Rooms... " + Time.realtimeSinceStartup);
        foreach (var (startRoom, _) in _connections.Where(e => e.Value.Count == 1).ToList())
        {
            if (Random.value > additionalHallwayChance)
            {
                var cubeCoords = CubeCoord.Spiral(startRoom, 2, 4).Where(c => _rooms.ContainsKey(c));
                var distances = GetDistances(_connections, startRoom);
                foreach (var targetRoom in cubeCoords.Where(c => !distances.ContainsKey(c) || distances[c] >= 4d).ToList().Shuffled())
                {
                    if (HasDirectPathWithoutRoom(startRoom, targetRoom))
                    {
                        var hallway = GenerateAndSpawnHallway(_rooms[startRoom], _rooms[targetRoom]);
                        newHallways.Add(hallway);
                        AddConnection(hallway.From.RoomCoord, hallway.To.RoomCoord);
                    }
                    break;
                }
            }
        }
        
        Debug.Log("Decorating rooms and hallways... " + Time.realtimeSinceStartup);
        PopulateLevel(newRooms, newHallways);
        
        Debug.Log("Building Meshes and Spawning Area Content... " + Time.realtimeSinceStartup);
        foreach (var area in _areas.Values.Distinct())
        {
            area.FinalizeArea();
        }
        
        Debug.Log("Baking Navmesh... " + Time.realtimeSinceStartup);
        GetComponent<NavMeshSurface>().BuildNavMesh();
        
        Debug.Log("Done Spawning Level " + Time.realtimeSinceStartup);
        StartCoroutine(ApplyVertexColors());

        _game.OnLevelSpawned(newRooms, newHallways);
    }

    private void PopulateLevel(List<Room> rooms, List<Hallway> hallways)
    {
        var hideoutRooms = PlaceHideouts(rooms);

        foreach (var room in rooms)
        {
            PopulateRoom(room, hideoutRooms);
        }

        foreach (var hallway in hallways)
        {
            PopulateHallway(hallway);
        }
    }

    private HashSet<Room> PlaceHideouts(List<Room> rooms)
    {
        // only allow WALL1 tiles which have a visible face, rotations 3 and 4 are not generally visible
        var allowedRotations = new[] { 0, 1, 2, 5, 6 };
        var candidates = rooms.Where(r => r.RoomCoord != CubeCoord.Origin).Select(room =>
        {
            var walls = new List<CubeCoord>();
            foreach (var coord in room.Coords)
            {
                if (_areas.TryGetValue(coord, out var area) && area)
                {
                    var data = area.GetData(coord);
                    if (data.type.type == WALL1 && allowedRotations.Contains(data.type.rotation))
                    {
                        area.TransformIntoHideout(coord);
                        walls.Add(coord);
                    }
                }
            }

            return (room, walls);
        }).Where(c => c.Item2.Count > 0).ToList();

        var hideoutLimit = 8;
        var hideoutRooms = new HashSet<(Room, List<CubeCoord>)> { candidates[0] };
        candidates.RemoveAt(0);
        while (hideoutRooms.Count < hideoutLimit && candidates.Count > 0)
        {
            var next = candidates.MaxBy(candidate =>
                hideoutRooms.Min(existing => existing.Item1.RoomCoord.Distance(candidate.Item1.RoomCoord)));
            candidates.Remove(next);
            hideoutRooms.Add(next);
        }

        var finalRooms = new HashSet<Room>(hideoutRooms.Count);
        foreach (var (room, coords) in hideoutRooms)
        {
            finalRooms.Add(room);
            for (var i = 0; i < Random.Range(1, coords.Count); i++)
            {
                room.TileArea.TransformIntoHideout(coords[i]);
            }
        }

        return finalRooms;
    }

    private (int, Room) FindNearestHideoutRoom(Room room, HashSet<Room> hideoutRooms)
    {
        var hideoutRoomDistance = float.PositiveInfinity;
        Room nearestHideoutRoom = null;
        Graph.PathFindingResult<CubeCoord, float>? result = null;

        foreach (var hideoutRoom in hideoutRooms)
        {
            var newResult = Graph.FindPath(room.RoomCoord, hideoutRoom.RoomCoord, from => _connections[from], (_, from, to) => from.ManhattenDistance(to), (from, to) => from.Distance(to));
            if (newResult.TotalCost < hideoutRoomDistance)
            {
                hideoutRoomDistance = newResult.TotalCost;
                nearestHideoutRoom = hideoutRoom;
                result = newResult;
            }
        }

        var totalCost = result?.TotalCost ?? float.PositiveInfinity;
        return (Mathf.RoundToInt(totalCost), nearestHideoutRoom);
    }

    private void PopulateRoom(Room room, HashSet<Room> hideoutRooms)
    {
        var (distance, nearestHideoutRoom) = FindNearestHideoutRoom(room, hideoutRooms);

        var walls = new List<CubeCoord>();
        var rest = new List<CubeCoord>();
        foreach (var coord in room.Coords)
        {
            if (_areas.TryGetValue(coord, out var area) && area.IsWall(coord))
            {
                walls.Add(coord);
            }
            else
            {
                rest.Add(coord);
            }
        }

        var center = room.Centers
            .Select(a => a.Item1)
            .Aggregate(CubeCoord.Origin, (c, a) => c + a) * (1f/room.Centers.Length);

        float AngleAroundCenter(CubeCoord coord)
        {
            var diff = coord - center;
            return Mathf.Atan2(diff.Q, diff.R);
        }

        var wallsInWindingOrder = walls.OrderBy(AngleAroundCenter);
        foreach (var (cubeCoord, i) in wallsInWindingOrder.ZipWithIndex())
        {
            if (i % 2 == 0)
            {
                room.TileArea.SpawnOnWall(cubeCoord, WALL_DECO_CANDLE);
            }
            else
            {
                room.TileArea.SpawnOnWall(cubeCoord, WALL_DECO_CHAINS);
            }
        }

        var freeSlots = new HashSet<CubeCoord>(room.Coords);
        
        // spawn collectibles
        walls.Shuffle();
        rest.Shuffle();
        for (int i = 0; i < Random.Range(1, room.Centers.Length); i++)
        {
            room.TileArea.SpawnOnFloor(walls[i], PICKUP_CUBE);
            freeSlots.Remove(walls[i]);
        }
        for (int i = 0; i < Random.Range(3, rest.Count / 3); i++)
        {
            var type = WeightedRandom.ChooseWeighted(new[] { 10f, 10f }, new[] { (int)PICKUP_BATTERY, (int)FOOD });
            room.TileArea.SpawnOnFloor(rest[i], (TileDictionary.TileType)type);
            freeSlots.Remove(rest[i]);
        }

        // spawn wires
        foreach (var (roomCenter, _) in room.Centers)
        {
            if (freeSlots.Contains(roomCenter))
            {
                room.TileArea.SpawnOnFloor(roomCenter, CABLES);
                freeSlots.Remove(roomCenter);
            }
        }
        
        foreach (var coord in new List<CubeCoord>(freeSlots).Shuffled().Take(Mathf.Min(5, distance)))
        {
            room.TileArea.SpawnOnFloor(coord, ROCKS);
        }
    }

    private void PopulateHallway(Hallway hallway)
    {
        foreach (var coord in hallway.Coords)
        {
            if (_areas.TryGetValue(coord, out var area) && area.IsWall(coord))
            {
                area.SpawnOnWall(coord, WALL_DECO);
            }
        }


        foreach (var coord in hallway.Coords)
        {
            var type = WeightedRandom.ChooseWeighted(new[] { 5f, 1f, 30}, new[] { (int)PICKUP_BATTERY, (int)PICKUP_CUBE, -1 });
            if (type != -1)
            {
                hallway.TileArea.SpawnOnFloor(coord, (TileDictionary.TileType)type);
            }
        }
    }

    private IEnumerator ApplyVertexColors()
    {
        Debug.Log("[coroutine] Coloring Floors " + Time.realtimeSinceStartup);
        using var toBeProcessed = _areas.Values
            .Distinct()
            .OrderBy(a => a.transform.position.sqrMagnitude)
            .GetEnumerator();
        // load the first few eagerly to prevent visible pop-ins
        for (int i = 0; i < 5 && toBeProcessed.MoveNext(); i++)
        {
            toBeProcessed.Current.ApplyVertexColors();
        }
        // load the rest, each on a separate frame to reduce lag
        while (toBeProcessed.MoveNext())
        {
            toBeProcessed.Current.ApplyVertexColors();
            yield return null;
        }
        Debug.Log("[coroutine] Done Coloring Floors " + Time.realtimeSinceStartup);
    }

    private static Dictionary<CubeCoord, double> GetDistances(Dictionary<CubeCoord, List<CubeCoord>> connections, CubeCoord start, int depthToGo = 5)
    {
        var currentSet = new HashSet<CubeCoord>();
        var result = new Dictionary<CubeCoord, double>
        {
            [start] = 0
        };
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

    private void AddConnection(CubeCoord from, CubeCoord to)
    {
        if (!_connections.TryGetValue(from, out var fromConnections))
        {
            fromConnections = new();
            _connections[from] = fromConnections;
        }
        fromConnections.Add(to);
                        
        if (!_connections.TryGetValue(to, out var toConnections))
        {
            toConnections = new();
            _connections[to] = toConnections;
        }
        toConnections.Add(from);
    }

    private Hallway AddRoomConnection(HashSet<CubeCoord> connectedSet, Dictionary<CubeCoord, List<CubeCoord>> connections)
    {
        for (int r = 1; r < 5; r++)
        {
            foreach (var roomToConnect in connectedSet.ToList().Shuffled())
            {
                foreach (var potentialRoom in CubeCoord.Ring(roomToConnect, r).Shuffled())
                {
                    if (_rooms.ContainsKey(potentialRoom) && !connectedSet.Contains(potentialRoom))
                    {
                        var hallway = GenerateAndSpawnHallway(_rooms[roomToConnect], _rooms[potentialRoom]);
                        connectedSet.Add(potentialRoom);
                        AddConnection(roomToConnect, potentialRoom);
                        return hallway;
                    }
                }
            }
        }

        return null;
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
        var current = CubeCoordFlatTop.FromWorld(pos, tiledict.TileSize());
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

    public void ApplyVertexColorsCoroutined()
    {
        StartCoroutine(ApplyVertexColors());
    }

    private Dictionary<Color, IList<CubeCoord>> gizmoTiles = new();

    private void OnDrawGizmos()
    {
        var tileSize = tiledict.TileSize();
        var size = tileSize.z / 2;
        foreach (var (color, coords) in gizmoTiles)
        {
            Gizmos.color = color;
            foreach (var coord in coords)
            {
                Gizmos.DrawWireSphere(coord.ToWorld(0, tileSize), size);
            }
        }
    }
}
