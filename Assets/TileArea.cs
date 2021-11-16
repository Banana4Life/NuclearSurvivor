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
    private Dictionary<CubeCoord, GameObject> wallDecorations = new();

    private List<CombineInstance> floorsToAdd = new();
    private Dictionary<CubeCoord, CombineInstance> wallsToCombine = new();
    private Dictionary<CubeCoord, CombineInstance> wallsToCombineVoid = new();

    public Mesh floorMesh;
    public Mesh wallMesh ;

    private GameObject pickups;
    private GameObject decorations;

    public class CellData
    {
        public CubeCoord coord;
        public bool[] walls = new bool[6];
        public Vector3 position;
        public bool hasPickup = Random.value < 0.05f;
    }


    void UpdateCombinedMesh()
    {
        if (floorsToAdd.Count > 0)
        {
            floorMesh.CombineMeshes(floorsToAdd.ToArray());
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
        pickups = new GameObject("Pickups");
        pickups.transform.parent = transform;
        pickups.transform.position = transform.position;
        
        decorations = new GameObject("Decorations");
        decorations.transform.parent = transform;
        decorations.transform.position = transform.position;
        
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
            if (wallDecorations.TryGetValue(cellData.coord, out var deco))
            {
                wallDecorations.Remove(cellData.coord);
                Destroy(deco);
            }
            if (type.type == TileDictionary.EdgeTileType.WALL0)
            {
                wallsToCombine.Remove(cellData.coord);
                wallsToCombineVoid.Remove(cellData.coord);
            }
            else
            {
                var rot = Quaternion.Euler(0, 60 * type.rotation, 0);
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
                    SpawnWallDecoration(cellData, variant.Item1.prefab, rot, type);
                    
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

    private void SpawnWallDecoration(CellData cellData, GameObject prefab, Quaternion rot, TileDictionary.RotatedTileType type)
    {
        if (Random.value < 0.5f)
        {
            foreach (Transform child in prefab.transform)
            {
                if (child.CompareTag("WallDeco") && Random.value < 0.5f)
                {
                    if (!wallDecorations.TryGetValue(cellData.coord, out var wallDeco))
                    {
                        wallDeco = new GameObject(cellData.coord.ToString());
                        wallDeco.transform.parent = decorations.transform;
                        wallDeco.transform.position = cellData.position;
                        wallDecorations[cellData.coord] = wallDeco;
                    }

                    var decoPos = rot * child.position + cellData.position;
                    var decoration = Instantiate(generator.tiledict.DecorationPrefab(), wallDeco.transform);
                    decoration.transform.position = decoPos;
                    decoration.transform.rotation = child.rotation;
                    decoration.transform.Rotate(0, 60 * type.rotation, 0);
                }
            }
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
        floorsToAdd.Add(MeshAsCombineInstance(generator.tiledict.Variant(TileDictionary.EdgeTileType.WALL0).Item2, cellData.position - transform.position, Quaternion.identity));
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
