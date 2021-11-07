using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.VisualScripting;
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
    public readonly (CubeCoord roomCell, CubeCoord[] hallwayCells)[] Connectors;

    public Hallway(Room from, Room to, List<CubeCoord> coords, (CubeCoord roomCell, CubeCoord[] hallwayCells)[] connectors)
    {
        From = from;
        To = to;
        Coords = coords;
        Connectors = connectors;
    }
}

public class TileGenerator : MonoBehaviour
{
    public TileDictionary tiledict;

    public GameObject floorTile;
    private const int RoomSize = 18;

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

    private Room generateRoom(CubeCoord roomCoord, NavigatableTiles nav)
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


        var room = new Room(roomCoord, origin, centers, coords, nav);
        nav.needsNewNavMesh = true;
        _rooms[room.RoomCoord] = room;
        return room;
    }

    private Room GenerateAndSpawnRoom(CubeCoord roomCoord)
    {
        if (!CanGenerateRoom(roomCoord))
        {
            return null;
        }
        var parent = Instantiate(roomPrefab, transform);
        var room = generateRoom(roomCoord, parent.GetComponent<NavigatableTiles>());
        parent.name = "Room " + roomCoord;
        parent.transform.position = roomCoord.FlatTopToWorld(floorHeight, tiledict.TileSize()) * RoomSize;
        foreach (var cubeCoord in room.Coords)
        {
            _roles[cubeCoord] = new RoomRole(room, spawnFloor(parent.transform, cubeCoord));
        }
        foreach (var coord in room.Coords)
        {
            _roles[coord].Tile.Init(coord);
        }

        return room;
    }

    private bool CanGenerateRoom(CubeCoord roomCoord)
    {
        return !_rooms.ContainsKey(roomCoord);
    }

    private bool isRoomCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is RoomRole;
    private bool isHallwayCell(CubeCoord coord) => _roles.TryGetValue(coord, out var role) && role is HallwayRole;

    private Hallway generateHallway(Room from, Room to)
    {
        var fromCenter = from.Centers[Random.Range(0, from.Centers.Length)].Item1;
        var toCenter = to.Centers[Random.Range(0, to.Centers.Length)].Item1;
        var pathResult = CubeCoord.SearchShortestPath(fromCenter, toCenter, pathCost, pathEstimation);
        
        _recentPathSearches.Add(pathResult);

        var pathBetween = pathResult.Path.SelectMany(cellCoord =>
            {
                var neighbors = cellCoord.FlatTopNeighbors();
                neighbors.Shuffle();
                return new[] { cellCoord, neighbors[0], neighbors[1] };
            })
            .Where(cell => !isRoomCell(cell))
            .Distinct()
            .ToList();

        // TODO FIX NAVMESH - we need to include hallways connecting to those rooms as they could intersect
        var connectors = from.Coords.Concat(to.Coords)
            .Select(roomCell => (roomCell, pathBetween.Where(coord => coord.IsAdjacent(roomCell)).ToArray()))
            .Where(t => t.Item2.Length != 0)
            .ToArray();

        return new Hallway(from, to, pathBetween, connectors);
    }

    private Hallway generateAndSpawnHallway(Room from, Room to)
    {
        var parent = Instantiate(roomPrefab, transform);
        parent.name = "Hallway";
        parent.transform.position = Vector3.Lerp(from.Origin.FlatTopToWorld(floorHeight, tiledict.TileSize()),
            to.Origin.FlatTopToWorld(floorHeight, tiledict.TileSize()), 0.5f);
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
        
        var mainTrigger = new GameObject("TriggerArea");
        mainTrigger.transform.parent = parent.transform;
        mainTrigger.AddComponent<LevelLoaderTrigger>().Init(this, to);
        foreach (var coord in hallway.Coords)
        {
            var pos = coord.FlatTopToWorld(0, tiledict.TileSize());
            var subTrigger = Instantiate(tiledict.triggerPrefab, mainTrigger.transform);
            subTrigger.GetComponent<SubTrigger>().Init(mainTrigger.GetComponent<LevelLoaderTrigger>(), pos);
        }

        var triggerTiles = GenerateNavMeshLinkTiles(hallway, parent).ToList();
        
        foreach (var linkTiles in triggerTiles)
        {
            var tile = _roles[linkTiles.coord].Tile.PlaceTile();
            // Update Rooms/Hallway Intersection
            tile.transform.parent.GetComponent<NavigatableTiles>().needsNewNavMesh = true;
        }

        // Always update source/target rooms
        from.Nav.needsNewNavMesh = true;  
        to.Nav.needsNewNavMesh = true;  
        
        parent.GetComponent<NavMeshSurface>().BuildNavMesh();

        foreach (var linkTiles in triggerTiles)
        {
            linkTiles.HideNavMeshLinkPlate();
        }
        
        return hallway;
    }

    private IEnumerable<AutoTile> GenerateNavMeshLinkTiles(Hallway hallway, GameObject parent)
    {
        foreach (var (coord, connections) in hallway.Connectors)
        {
            // TODO FIX NAVMESH - link tiles actually need the walls anyways
            var autoTile = spawnFloor(parent.transform, coord).Init(coord, true);
            foreach (var cubeCoord in connections)
            {
                var cubeDirection = cubeCoord - coord;

                var link = autoTile.AddComponent<NavMeshLink>();
                var direction = cubeDirection.FlatTopToWorld(floorHeight, tiledict.TileSize());
                link.startPoint = direction / 2 - direction.normalized * 0.5f;
                link.endPoint = direction / 2 - direction.normalized * 0.4999f;
                link.width = tiledict.TileSize().x / 2;
            }

            yield return autoTile;
        }
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

            generateAndSpawnHallway(room, newRoom);
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

    private AutoTile spawnFloor(Transform parent, CubeCoord at)
    {
        return Instantiate(floorTile, parent, true).GetComponent<AutoTile>();
    }

    public AutoTile TileAt(CubeCoord cubeCoord)
    {
        return _roles.TryGetValue(cubeCoord, out var tile) ? tile.Tile : null;
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
}
