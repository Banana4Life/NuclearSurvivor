using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FlatTop;
using Unity.AI.Navigation;
using UnityEngine;
using static TileDictionary.TileType;
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

    public GameObject editorTiles;
    public float uvFactor = 10f;

    public int paintInfluenceHex = 3;
    public float additionalHallwayChance = 0.1f;
    public int levelRings = 5;

    private Dictionary<CubeCoord, List<CubeCoord>> connections;

    void Start()
    {
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
    
    private bool isRoomCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is RoomRole;
    private bool isHallwayCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is HallwayRole;

    private Hallway GenerateHallway(Room from, Room to)
    {
        var fromCenter = from.Centers[Random.Range(0, from.Centers.Length)].Item1;
        var toCenter = to.Centers[Random.Range(0, to.Centers.Length)].Item1;

        var thinPath = SearchPath(fromCenter, toCenter);
        var fullPath = thinPath.SelectMany(cellCoord => cellCoord.Neighbors().Shuffled().Take(widePath).Concat(new []{cellCoord})).Distinct().ToList();
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
        connections = new();
        connectedSet.Add(CubeCoord.Origin);
        var hallways = new List<Hallway>();
        while (connectedSet.Count < _rooms.Count)
        {
            var hallway = AddRoomConnection(connectedSet, connections);
            if (hallway != null)
            {
                hallways.Add(hallway);
            }
        }

        Debug.Log("Connecting More Rooms... " + Time.realtimeSinceStartup);
        foreach (var (startRoom, _) in connections.Where(e => e.Value.Count == 1).ToList())
        {
            if (Random.value > additionalHallwayChance)
            {
                var cubeCoords = CubeCoord.Spiral(startRoom, 2, 4).Where(c => _rooms.ContainsKey(c));
                var distances = GetDistances(connections, startRoom);
                foreach (var targetRoom in cubeCoords.Where(c => !distances.ContainsKey(c) || distances[c] >= 4d).ToList().Shuffled())
                {
                    if (HasDirectPathWithoutRoom(startRoom, targetRoom))
                    {
                        var hallway = GenerateAndSpawnHallway(_rooms[startRoom], _rooms[targetRoom]);
                        hallways.Add(hallway);
                        if (connections.TryGetValue(hallway.From.RoomCoord, out var neighbors))
                        {
                            neighbors.Add(hallway.To.RoomCoord);
                        }
                        else
                        {
                            connections[hallway.From.RoomCoord] = new List<CubeCoord> { hallway.To.RoomCoord };
                        }
                    }
                    break;
                }
            }
        }
        
        Debug.Log("Decorating rooms and hallways... " + Time.realtimeSinceStartup);
        PopulateLevel(newRooms, hallways);
        
        Debug.Log("Building Meshes and Spawning Area Content... " + Time.realtimeSinceStartup);
        foreach (var area in _areas.Values.Distinct())
        {
            area.FinalizeArea();
        }
        
        Debug.Log("Baking Navmesh... " + Time.realtimeSinceStartup);
        GetComponent<NavMeshSurface>().BuildNavMesh();
        
        Debug.Log("Done Spawning Level " + Time.realtimeSinceStartup);
        StartCoroutine(ApplyVertexColors());
    }

    private void PopulateLevel(List<Room> rooms, List<Hallway> hallways)
    {
        // only allow WALL1 which have a visible face, rotations 3 and 4 are not generally visible
        var allowedRotations = new[] { 0, 1, 2, 5, 6 };
        var candidates = rooms.Select(room =>
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
        }).ToList();

        foreach (var room in rooms)
        {
            PopulateRoom(room);
        }

        foreach (var hallway in hallways)
        {
            PopulateHallway(hallway);
        }
    }

    private void PopulateRoom(Room room)
    {
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
        for (int i = 0; i < Random.Range(1, rest.Count / 3); i++)
        {
            room.TileArea.SpawnOnFloor(rest[i], PICKUP_BARREL);
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
        
        // final floor decorations on still free tiles
        foreach (var coord in freeSlots)
        {
            if (Random.value < 0.1)
            {
                room.TileArea.SpawnOnFloor(coord, FLOOR_DECO);
            }
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
            var type = WeightedRandom.ChooseWeighted(new[] { 5f, 1f, 30}, new[] { (int)PICKUP_BARREL, (int)PICKUP_CUBE, -1 });
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
                        if (!connections.TryGetValue(roomToConnect, out var list))
                        {
                            list = new();
                            connections[roomToConnect] = list;
                        }
                        list.Add(potentialRoom);
                        
                        if (!connections.TryGetValue(potentialRoom, out var list2))
                        {
                            list2 = new();
                            connections[potentialRoom] = list2;
                        }
                        list2.Add(roomToConnect);
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
