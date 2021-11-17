using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class TileArea : MonoBehaviour
{
    public MeshFilter floorMeshFilter;
    public MeshFilter wallMeshFilter;
    
    private bool needsWallMeshCombining;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private TileGenerator generator;
    private Dictionary<CubeCoord, CellData> cells = new();
    private Dictionary<CubeCoord, GameObject> cellDecorations = new();

    private List<CombineInstance> floorsToAdd = new();
    private Dictionary<CubeCoord, CombineInstance> wallsToCombine = new();
    private Dictionary<CubeCoord, CombineInstance> wallsToCombineVoid = new();

    public Mesh floorMesh;
    public Mesh wallMesh ;

    private GameObject areaPickups;
    private GameObject areaDecorations;

    public class CellData
    {
        public CubeCoord coord;
        public bool[] walls = new bool[6];
        public Vector3 position;
        public TileVariant variant;
        public TileDictionary.RotatedTileType type;
    }


    void UpdateCombinedMesh()
    {
        if (floorsToAdd.Count > 0)
        {
            floorMesh.CombineMeshes(floorsToAdd.ToArray());
            for (var i = 0; i < floorMesh.vertices.Length; i++)
            {
                var vertex = floorMesh.vertices[i] + transform.position;
                var uvs = floorMesh.uv;
                uvs[i] = new Vector2(vertex.x, vertex.z);
                floorMesh.uv = uvs;
            }

            floorsToAdd.Clear();
        }
        if (needsWallMeshCombining)
        {
            var wallMeshWall = new Mesh();
            var wallMeshVoid = new Mesh();
            if (wallsToCombine.Count > 0)
            {
                wallMeshWall.CombineMeshes(wallsToCombine.Values.ToArray());
            }
            if (wallsToCombineVoid.Count > 0)
            {
                wallMeshVoid.CombineMeshes(wallsToCombineVoid.Values.ToArray());
            }
            wallMesh.Clear();
            wallMesh.CombineMeshes(new []{new CombineInstance(){mesh = wallMeshWall}, new CombineInstance(){mesh = wallMeshVoid}}, false, false);
        }
        floorMeshFilter.sharedMesh = floorMesh;
        wallMeshFilter.sharedMesh = wallMesh;
        floorMeshFilter.gameObject.GetComponent<MeshCollider>().sharedMesh = floorMesh;
        wallMeshFilter.gameObject.GetComponent<MeshCollider>().sharedMesh = wallMesh;
    }

    public void InitMeshes(String coord)
    {
        floorMesh = new();
        wallMesh = new();
        floorMesh.name = $"Floor Mesh {coord}";
        wallMesh.name = $"Wall Mesh {coord}";
    }
    
    public void Init(TileGenerator generator, Room room)
    {
        this.generator = generator;
        gameObject.name = "Room " + room.Origin;
        transform.position = room.WorldCenter;
        InitMeshes(room.Origin.ToString());
        InitCells(room.Coords);
        UpdateCombinedMesh();
    }

    public void Init(TileGenerator generator, Hallway hallway)
    {
        this.generator = generator;
        gameObject.name = "Hallway";
        transform.position = Vector3.Lerp(hallway.From.Origin.FlatTopToWorld(generator.floorHeight,  generator.tiledict.TileSize()),
            hallway.To.Origin.FlatTopToWorld(generator.floorHeight,  generator.tiledict.TileSize()), 0.5f);
        InitMeshes($"{hallway.From.Origin}|{hallway.To.Origin}");
        InitCells(hallway.Coords);
        InitLoadTriggers(hallway);
        UpdateCombinedMesh();
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

    public void UpdateWalls()
    {
        List<CellData> toRespawn = new();
        foreach (var wall in wallsToCombine.ToList())
        {
            var cellData = cells[wall.Key];
            var oldWalls = cellData.walls;
            cellData.walls = GetEdgeWalls(cellData.coord);
            if (!oldWalls.SequenceEqual(cellData.walls))
            {
                toRespawn.Add(cellData);
            }   
        }
        foreach (var cellData in toRespawn)
        {
            SpawnWall(cellData);
        }
        UpdateCombinedMesh();
    }

    private void InitLoadTriggers(Hallway hallway)
    {
        // TODO remove loadtriggers
        // var mainTrigger = new GameObject("TriggerArea");
        // mainTrigger.transform.parent = transform;
        // mainTrigger.transform.position = transform.position + Vector3.up;
        // mainTrigger.AddComponent<LevelLoaderTrigger>().Init(generator, hallway.To, floorMesh);
    }

    private void InitCells(IEnumerable<CubeCoord> coords)
    {
        var cellCnt = coords.ToList().Count; 
        
        areaPickups = new GameObject("Pickups");
        areaPickups.transform.parent = transform;
        areaPickups.transform.position = transform.position;
        
        areaDecorations = new GameObject("Decorations");
        areaDecorations.transform.parent = transform;
        areaDecorations.transform.position = transform.position;

        // Init Cell Floor & Walls + store CellData
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
        }
        
        foreach (var cellCoord in coords.ToList().Shuffled().Take(cellCnt / 20))
        {
            SpawnPickup(cells[cellCoord]);
        }
        
        foreach (var cellCoord in coords.ToList().Shuffled().Take(cellCnt * 2/3))
        {
            SpawnDecorations(cells[cellCoord]);
        }
    }

    private void SpawnPickup(CellData cellData)
    {
        SpawnDecoration(cellData, "FloorDeco", () => generator.tiledict.pickupPrefab, () => true, areaPickups);
    }

    private void SpawnDecorations(CellData cellData)
    {
        SpawnDecoration(cellData, "WallDeco", () => generator.tiledict.WallDecorationPrefab(), () => Random.value < 0.3f, areaDecorations);
        SpawnDecoration(cellData, "FloorDeco", () => generator.tiledict.FloorDecorationPrefab(), () => Random.value < 0.3f, areaDecorations);
    }

    private void SpawnWall(CellData cellData)
    {
        if (TileDictionary.edgeTileMap.TryGetValue(cellData.walls, out var type))
        {
            cellData.type = type;
            if (cellDecorations.TryGetValue(cellData.coord, out var deco))
            {
                cellDecorations.Remove(cellData.coord);
                Destroy(deco);
            }
            if (type.type == TileDictionary.EdgeTileType.WALL0)
            {
                cellData.variant = generator.tiledict.Variant(TileDictionary.EdgeTileType.WALL0).Item1;
                wallsToCombine.Remove(cellData.coord);
                wallsToCombineVoid.Remove(cellData.coord);
            }
            else
            {
                var rot = Quaternion.Euler(0, type.rotation * 60, 0);
                var pos = cellData.position - transform.position;
                if (type.type == TileDictionary.EdgeTileType.WALL2_P && Random.value < 0.5f)
                {
                    var combined = generator.tiledict.CombinedMesh(TileDictionary.EdgeTileType.WALL2_P,
                        TileDictionary.EdgeTileType.DOOR);
                    wallsToCombine[cellData.coord] = MeshAsCombineInstance(combined, pos, rot, 0);
                    wallsToCombineVoid[cellData.coord] = MeshAsCombineInstance(combined, pos, rot, 1);
                }
                else
                {
                    var variant = generator.tiledict.Variant(type.type);
                    cellData.variant = variant.Item1;
                    var wallMesh = variant.Item2;
                    var wallSubMeshes = generator.tiledict.SubMeshs(type.type);
                    wallsToCombine[cellData.coord] = MeshAsCombineInstance(wallMesh, pos, rot, wallSubMeshes[0]);
                    wallsToCombineVoid[cellData.coord] = MeshAsCombineInstance(wallMesh, pos, rot, wallSubMeshes[1]);
                }
            }
            needsWallMeshCombining = true;
        }
        else
        {
            Debug.Log("Wall Type not found " + string.Join("|", cellData.walls));
        }
    }

    private void SpawnDecoration(CellData cellData, String decoType, Func<GameObject> decorationSupplier, Func<Boolean> shouldPlace, GameObject parent)
    {
        if (cellData.variant.prefab == null) // No Variant set
        {
            return;
        }
        var rot = Quaternion.Euler(0, cellData.type.rotation * 60, 0);
        foreach (Transform mountPoint in cellData.variant.prefab.transform)
        {
            if (mountPoint.CompareTag(decoType) && shouldPlace())
            {
                if (!cellDecorations.TryGetValue(cellData.coord, out var cellDeco))
                {
                    cellDeco = new GameObject(cellData.coord.ToString());
                    cellDeco.transform.parent = parent.transform;
                    cellDeco.transform.position = cellData.position;
                    cellDecorations[cellData.coord] = cellDeco;
                }
                
                var decoPos = rot * mountPoint.position + cellData.position;
                var decoration = Instantiate(decorationSupplier(), cellDeco.transform);
                var initialRot = decoration.transform.rotation;
                decoration.transform.position = decoration.transform.localPosition + decoPos;
                decoration.transform.localRotation = mountPoint.rotation * rot;
                decoration.transform.rotation *= initialRot;
            }
        }
    }

    private void SpawnFloor(CellData cellData)
    {
        var floorMesh = generator.tiledict.Variant(TileDictionary.EdgeTileType.WALL0).Item2;
        floorsToAdd.Add(MeshAsCombineInstance(floorMesh, cellData.position - transform.position, Quaternion.identity));
    }

    private CombineInstance MeshAsCombineInstance(Mesh baseMesh, Vector3 position, Quaternion rotation, int subMesh = 0)
    {
        return new CombineInstance
        {
            mesh = baseMesh,
            transform = Matrix4x4.TRS(position, rotation, Vector3.one),
            subMeshIndex = subMesh
        };
    }
}
