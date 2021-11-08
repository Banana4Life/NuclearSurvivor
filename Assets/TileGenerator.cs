using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;

public class Room
{
    public readonly CubeCoord RoomCoord;
    public readonly CubeCoord Origin;
    public readonly (CubeCoord, int)[] Centers;
    public readonly HashSet<CubeCoord> Coords;
    public readonly NavigatableTiles Nav;

    public Room(CubeCoord roomCoord, CubeCoord origin, (CubeCoord, int)[] centers, HashSet<CubeCoord> coords, NavigatableTiles nav)
    {
        RoomCoord = roomCoord;
        Origin = origin;
        Centers = centers;
        Coords = coords;
        Nav = nav;
    }
}

public abstract class CellRole
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

public class Hallway
{
    public readonly Room From;
    public readonly Room To;
    public readonly List<CubeCoord> Coords;
    public readonly (CubeCoord roomCell, CubeCoord[] hallwayCells)[] Connectors;
    public readonly NavigatableTiles Nav;

    public Hallway(Room from, Room to, List<CubeCoord> coords, (CubeCoord roomCell, CubeCoord[] hallwayCells)[] connectors, NavigatableTiles nav)
    {
        From = from;
        To = to;
        Coords = coords;
        Connectors = connectors;
        Nav = nav;
    }
}

public class TileGenerator : MonoBehaviour
{
    public TileDictionary tiledict;

    public const int RoomSize = 18;

    private Dictionary<CubeCoord, Room> _rooms = new();
    private Dictionary<CubeCoord, CellRole> _roles = new();
    
    public GameObject roomPrefab;
    public int floorHeight = 0;

    public bool showPathFindingResults;
    
    private readonly List<ShortestPath.PathFindingResult<CubeCoord, float>> _recentPathSearches = new();

    void Start()
    {
        Debug.Log(Random.seed);
        var room = GenerateAndSpawnRoom(CubeCoord.Origin);
        SpawnRoomRing(room);
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

        var room = new Room(roomCoord, origin, centers, coords, Instantiate(roomPrefab, transform).GetComponent<NavigatableTiles>());
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
        }
        room.Nav.Init(this, room);
        return room;
    }

    private bool CanGenerateRoom(CubeCoord roomCoord)
    {
        return !_rooms.ContainsKey(roomCoord);
    }

    public bool IsCell(CubeCoord coord) => _roles.ContainsKey(coord);
    
    private bool isRoomCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is RoomRole;
    private bool isHallwayCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is HallwayRole;

    public struct GenerateHallwayJob : IJob
    {
        public CubeCoord fromRoom;
        public CubeCoord toRoom;
        public CubeCoord fromCenter;
        public CubeCoord toCenter;
        public PathCostSettings settings;
        // public Dictionary<CubeCoord, CellRole> roles;
        public NativeArray<CubeCoord> resultList;
        public NativeArray<int> result;
        public JobHandle handle;
        
        public void Execute()
        {
            var cubeCoords = CubeCoord.SearchShortestPath(fromCenter, toCenter, pathCost, pathEstimation).Path;
            for (int i = 0; i < cubeCoords.Count; i++)
            {
                resultList[i] = cubeCoords[i];
            }
            result[0] = cubeCoords.Count;
        }
        
        private float pathCost(Dictionary<CubeCoord, CubeCoord> prevMap, CubeCoord from, CubeCoord to)
        {
            // var emptyTileCost = Random.Range(emptyTileCostMin, emptyTileCostMax);
            var emptyTileCost = settings.emptyTileCostMin;
            if (prevMap.TryGetValue(from, out var prev))
            {
                if (from - prev == to - from)
                {
                    // emptyTileCost = Random.Range(straightTileCostMin, straightTileCostMax);
                    emptyTileCost = settings.straightTileCostMin;
                }    
            }
        
            // if (!roles.TryGetValue(to, out var toRole))
            // {
            //     return emptyTileCost;
            // }
            //
            // if (toRole is RoomRole)
            // {
            //     return settings.roomCost;
            // }
            //
            // if (toRole is HallwayRole)
            // {
            //     return settings.hallwayCost;
            // }
        
            return emptyTileCost;
            
        }
    }

    private Queue<GenerateHallwayJob> jobs = new();

    public struct FromToRoom
    {
        public Room fromRoom;
        public Room toRoom;
    }
    
    private void Update()
    {
        if (jobs.Count != 0)
        {
            if (jobs.Peek().handle.IsCompleted)
            {
                GenerateAndSpawnHallway(jobs.Dequeue());
            }
        }
    }

    private void GenerateAndSpawnHallway(GenerateHallwayJob jobData)
    {
        jobData.handle.Complete();
        var pathResult = new CubeCoord[100];
        jobData.resultList.CopyTo(pathResult);
        Array.Resize(ref pathResult, jobData.result[0]);

        // _recentPathSearches.Add(pathResult.ToArray());

        Debug.Log($"path found {jobData.result[0]} {pathResult.Length}" );
        for (int i = 0; i < pathResult.Length; i++)
        {
            Debug.Log(pathResult[i]);
        }
            
        jobData.resultList.Dispose();
        jobData.result.Dispose();


        var pathBetween = pathResult.SelectMany(cellCoord =>
            {
                var neighbors = cellCoord.FlatTopNeighbors();
                neighbors.Shuffle();
                return new[] { cellCoord, neighbors[0], neighbors[1] };
            })
            .Where(cell => !isRoomCell(cell))
            .Distinct()
            .ToList();

        var fromRoom = _rooms[jobData.fromRoom];
        var toRoom = _rooms[jobData.toRoom];
        // TODO FIX NAVMESH - we need to include hallways connecting to those rooms as they could intersect
        var connectors = fromRoom.Coords.Concat(toRoom.Coords)
            .Select(roomCell => (roomCell, pathBetween.Where(coord => coord.IsAdjacent(roomCell)).ToArray()))
            .Where(t => t.Item2.Length != 0)
            .ToArray();

        var nav = Instantiate(roomPrefab, transform).GetComponent<NavigatableTiles>();
        var hallway = new Hallway(fromRoom, toRoom, pathBetween, connectors, nav);
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
            }
        }
        
        hallway.Nav.Init(this, hallway);
    }

    private void ScheduleHallwayGeneration(Room from, Room to)
    {
        var fromCenter = from.Centers[Random.Range(0, from.Centers.Length)].Item1;
        var toCenter = to.Centers[Random.Range(0, to.Centers.Length)].Item1;
        var jobData = new GenerateHallwayJob()
        {
            fromRoom = from.RoomCoord,
            toRoom = to.RoomCoord,
            fromCenter = fromCenter,
            toCenter = toCenter,
            settings = pathCostSettings,
            resultList = new NativeArray<CubeCoord>(100, Allocator.Persistent),
            result = new NativeArray<int>(1, Allocator.Persistent),
            // roles = _roles
        };
        jobData.handle = jobData.Schedule();
        jobs.Enqueue(jobData);
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
        foreach (var newRoom in newRooms)
        {
            ScheduleHallwayGeneration(room, newRoom);
        }
    }

    [Serializable]
    public struct PathCostSettings
    {
        public int emptyTileCostMin;
        public int emptyTileCostMax;
        public int straightTileCostMin;
        public int straightTileCostMax;
        public int roomCost;
        public int hallwayCost;
    }

    public PathCostSettings pathCostSettings = new PathCostSettings()
    {
        emptyTileCostMin = 1,
        emptyTileCostMax = 10,
        straightTileCostMin = 4,
        straightTileCostMax = 20,
        roomCost = 4,
        hallwayCost = 2
    };

    

    private static float pathEstimation(CubeCoord from, CubeCoord destination)
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

    public IEnumerable<NavigatableTiles> NavigatableAt(CubeCoord coord)
    {
        if (_roles.TryGetValue(coord, out var role))
        {
            if (role is HallwayRole hallways)
            {
                return hallways.Hallways.Select(h => h.Nav);
            }

            if (role is RoomRole room)
            {
                return new List<NavigatableTiles>() { room.Room.Nav };
            }
        }

        return Array.Empty<NavigatableTiles>();
    }
}
