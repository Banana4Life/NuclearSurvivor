using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using Random = UnityEngine.Random;

public class NavigatableTiles : MonoBehaviour
{
    public bool needsNewNavMesh;
    private bool needsMeshCombining;
    private NavMeshSurface navMesh;
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;

    private TileGenerator generator;
    private Dictionary<CubeCoord, CellData> cells = new();
    private Dictionary<CubeCoord, GameObject> walls = new();

    private Queue<CombineInstance> floorsToCombine = new();
    private Queue<CombineInstance> wallsToCombine = new();
    private Queue<CombineInstance> linksToCombine = new();

    public Mesh floorMesh;
    public Mesh wallMesh ;
    public Mesh linkMesh ;

    public Mesh baseLinkMesh;

    private GameObject wallsContainer;
    private GameObject pickups;
    private GameObject links;

    public class CellData
    {
        public CubeCoord coord;
        public bool[] walls = new bool[6];
        public Vector3 position;
        public bool hasPickup = Random.value < 0.05f;
    }

    private void Start()
    {
        navMesh = GetComponent<NavMeshSurface>();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        CombineMesh(floorMesh, floorsToCombine);
        CombineMesh(wallMesh, wallsToCombine);
        CombineMesh(linkMesh, linksToCombine);
      
        if (needsMeshCombining)
        {
            mesh.CombineMeshes(new []{new CombineInstance(){mesh = floorMesh}, new CombineInstance(){mesh = wallMesh}, new CombineInstance(){mesh = linkMesh}}, false, false);
            needsMeshCombining = false;
            needsNewNavMesh = true;
        }
        if (needsNewNavMesh)
        {
            needsNewNavMesh = false;
            navMesh.BuildNavMesh();
        }
    }

    private void CombineMesh(Mesh mesh, Queue<CombineInstance> toCombine)
    {
        if (toCombine.Count != 0)
        {
            mesh.CombineMeshes(toCombine.ToArray());
            toCombine.Clear();
            needsMeshCombining = true;
        }
    }

    public void InitMeshes(String coord)
    {
        mesh = new();
        mesh.subMeshCount = 3;
        floorMesh = new();
        wallMesh = new();
        linkMesh = new();
        baseLinkMesh = new();
        mesh.name = $"Main Mesh {coord}";
        floorMesh.name = $"Floor Mesh {coord}";
        wallMesh.name = $"Wall Mesh {coord}";
        linkMesh.name = $"Link Mesh {coord}";
        baseLinkMesh.name = "Base Link Mesh";
        mesh.subMeshCount = 3; // FloorTiles / Walls / NavMeshLinkTiles
    }
    
    public void Init(TileGenerator generator, Room room)
    {
        this.generator = generator;
        gameObject.name = "Room " + room.Origin;
        transform.position = room.RoomCoord.FlatTopToWorld(generator.floorHeight, generator.tiledict.TileSize()) * TileGenerator.RoomSize;
        InitMeshes(room.Origin.ToString());
        InitCells(room.Coords);
    }
    
    public bool[] GetEdgeWalls(CubeCoord coord)
    {
        var walls = new bool[6];
        var neighbors = coord.FlatTopNeighbors();
        for (var i = 0; i < neighbors.Length; i++)
        {
            var neighbor = neighbors[i];
            walls[i] = !generator.IsCell(neighbor);
        }
        return walls;
    }

    public void Init(TileGenerator generator, Hallway hallway)
    {
        this.generator = generator;
        gameObject.name = "Hallway";
        transform.position = Vector3.Lerp(hallway.From.Origin.FlatTopToWorld(generator.floorHeight,  generator.tiledict.TileSize()),
                                          hallway.To.Origin.FlatTopToWorld(generator.floorHeight,  generator.tiledict.TileSize()), 0.5f);
        InitMeshes($"{hallway.From.Origin}|{hallway.To.Origin}");
        InitCells(hallway.Coords);
        UpdateRooms(hallway);
        InitNavMeshLinks(hallway);
        InitLoadTriggers(hallway);
    }

    private void UpdateRooms(Hallway hallway)
    {
        hallway.From.Nav.UpdateWalls();
        hallway.To.Nav.UpdateWalls();
        // TODO intersecting hallways
    }

    private void UpdateWalls()
    {
        List<CellData> toRespawn = new();
        foreach (var wall in walls.ToList())
        {
            var cellData = cells[wall.Key];
            var oldWalls = cellData.walls;
            cellData.walls = GetEdgeWalls(cellData.coord);
            if (!oldWalls.SequenceEqual(cellData.walls))
            {
                Destroy(wall.Value);
                toRespawn.Add(cellData);
            }
        }
        foreach (var cellData in toRespawn)
        {
            SpawnWall(cellData);
        }
    }

    private void InitNavMeshLinks(Hallway hallway)
    {
        links = new GameObject("Links");
        links.transform.parent = transform;
        var tileSize = generator.tiledict.TileSize();
        baseLinkMesh.vertices = new[]
        {
            new Vector3(-tileSize.x / 4, 0, tileSize.z / 4), new Vector3(-tileSize.x / 4, 0, tileSize.z / 2), 
            new Vector3(tileSize.x / 4, 0, tileSize.z / 2), new Vector3(tileSize.x / 4, 0, tileSize.z / 4)
        };
        baseLinkMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        
        foreach (var (coord, connections) in hallway.Connectors)
        {
            var linkTile = new GameObject("LinkTile");
            linkTile.transform.parent = links.transform;
            var pos = coord.FlatTopToWorld(generator.floorHeight, tileSize);
            linkTile.transform.position = pos;
            foreach (var cubeCoord in connections)
            {
                var cubeDirection = cubeCoord - coord;
                var link = linkTile.AddComponent<NavMeshLink>();
                var direction = cubeDirection.FlatTopToWorld(generator.floorHeight, tileSize);
                link.startPoint = direction / 2 - direction.normalized * 0.5f;
                link.endPoint = direction / 2 - direction.normalized * 0.4999f;
                link.width = tileSize.x / 2;
                
                ExtendMesh(linksToCombine, baseLinkMesh, pos - transform.position, Quaternion.LookRotation(direction, Vector3.up));
            }

            foreach (var navigatable in generator.NavigatableAt(coord))
            {
                navigatable.needsNewNavMesh = true;
            }
        }

        // Always update this/source/target rooms
        needsNewNavMesh = true;
        hallway.From.Nav.needsNewNavMesh = true;
        hallway.To.Nav.needsNewNavMesh = true;
    }

    private void InitLoadTriggers(Hallway hallway)
    {
        var mainTrigger = new GameObject("TriggerArea");
        mainTrigger.transform.parent = transform;
        mainTrigger.transform.position = transform.position + Vector3.up;
        mainTrigger.AddComponent<LevelLoaderTrigger>().Init(generator, hallway.To, floorMesh);
    }

    private void InitCells(IEnumerable<CubeCoord> coords)
    {
        wallsContainer = new GameObject("Walls");
        wallsContainer.transform.parent = transform;
        pickups = new GameObject("Pickups");
        pickups.transform.parent = transform;
        
        foreach (var cellCoord in coords)
        {
            var cellData = new CellData()
            {
                coord = cellCoord,
                position = cellCoord.FlatTopToWorld(generator.floorHeight, generator.tiledict.TileSize()),
                walls = GetEdgeWalls(cellCoord)
            };
            cells[cellCoord] = cellData;
            SpawnFloor(cellData);
            SpawnWall(cellData);
            SpawnPickups(cellData);
        }
    }

    private void SpawnWall(CellData cellData)
    {
        if (TileDictionary.edgeTileMap.TryGetValue(cellData.walls, out var type))
        {
            if (type.type != TileDictionary.EdgeTileType.WALL0)
            {
                var wall = Instantiate(generator.tiledict.Prefab(type.type), wallsContainer.transform);
                wall.name = $"{type.type} {cellData.coord}";
                wall.transform.position = cellData.position;
                wall.transform.RotateAround(cellData.position, Vector3.up, 60 * type.rotation);
                walls[cellData.coord] = wall;
            }
        }
        else
        {
            Debug.Log("Wall Type not found " + string.Join("|", cellData.walls));
        }
    }

    private void SpawnPickups(CellData cellData)
    {
        if (cellData.hasPickup)
        {
            var pickup = Instantiate(generator.tiledict.pickupPrefab, pickups.transform);
            pickup.transform.position = cellData.position;
        }
    }

    private void SpawnFloor(CellData cellData)
    {
        var baseMesh = generator.tiledict.baseHexMesh;
        ExtendMesh(floorsToCombine, baseMesh, cellData.position - transform.position, Quaternion.identity);
    }

    private void ExtendMesh(Queue<CombineInstance> toCombine, Mesh baseMesh, Vector3 position, Quaternion rotation)
    {
        var combine = new CombineInstance
        {
            mesh = baseMesh,
            transform = Matrix4x4.TRS(position, rotation, Vector3.one)
        };
        toCombine.Enqueue(combine);
    }
}
