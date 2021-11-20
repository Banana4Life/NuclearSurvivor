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
    private Dictionary<int, Dictionary<CubeCoord, CombineInstance>> wallsToCombine = new();

    public Mesh floorMesh;
    public Mesh wallMesh ;

    private GameObject areaPickups;
    private GameObject areaDecorations;

    public int textureIdx;

    private static Dictionary<CubeCoord, (float, float, float, float)> _colorInfluencedHexes = new();

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
            var floorMeshVertices = floorMesh.vertices;
            for (var i = 0; i < floorMeshVertices.Length; i++)
            {
                var vertex = floorMeshVertices[i] + transform.position;
                var uvs = floorMesh.uv;
                uvs[i] = new Vector2(vertex.x, vertex.z) / generator.uvFactor;
                floorMesh.uv = uvs;
            }
        }
        if (needsWallMeshCombining)
        {
            List<CombineInstance> subMeshes = new();
            foreach (var combineInstances in wallsToCombine.Values)
            {
                var subMesh = new Mesh();
                subMesh.CombineMeshes(combineInstances.Values.ToArray());
                subMeshes.Add(new CombineInstance() { mesh = subMesh});
            }
            wallMesh.Clear();
            wallMesh.CombineMeshes(subMeshes.ToArray(), false, false);
        }
        floorMeshFilter.sharedMesh = floorMesh;
        wallMeshFilter.sharedMesh = wallMesh;
        floorMeshFilter.gameObject.GetComponent<MeshCollider>().sharedMesh = floorMesh;
        wallMeshFilter.gameObject.GetComponent<MeshCollider>().sharedMesh = wallMesh;
        
        CalcVertexColor();
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

    public void CalcVertexColor()
    {
        textureIdx = Random.Range(0, 4);
        var color = (textureIdx == 0 ? 1 : 0, textureIdx == 1 ? 1 : 0, textureIdx == 2 ? 1 : 0, textureIdx == 3 ? 1 : 0);
        foreach (var cellData in cells.Values)
        {
            _colorInfluencedHexes[cellData.coord] = color;
        }
    }

    public void ApplyVertexColors()
    {
        float r = 0;
        float g = 0;
        float b = 0;
        float a = 0;

        var areaPos = transform.position;
        var newColors = new Color[floorMesh.vertexCount];
        for (int i = 0; i < newColors.Length; i++)
        {
            var vertex = floorMesh.vertices[i];
            var coord = CubeCoord.FlatTopFromWorld(vertex + areaPos, generator.tiledict.TileSize());
            if (_colorInfluencedHexes.TryGetValue(coord, out var tuple))
            {
                var (rr, gg, bb, aa) = tuple;
                r += rr * generator.paintInfluenceHex;
                g += gg * generator.paintInfluenceHex;
                b += bb * generator.paintInfluenceHex;
                a += aa * generator.paintInfluenceHex;
            }
            for (int ring = 1; ring < generator.paintInfluenceHex; ring++)
            {
                foreach (var ringCoord in CubeCoord.Ring(coord, ring))
                {
                    if (_colorInfluencedHexes.TryGetValue(ringCoord, out var t))
                    {
                        var (rr, gg, bb, aa) = t;
                        r += rr * (generator.paintInfluenceHex - ring + 1);
                        g += gg * (generator.paintInfluenceHex - ring + 1);
                        b += bb * (generator.paintInfluenceHex - ring + 1);
                        a += aa * (generator.paintInfluenceHex - ring + 1);
                    }
                }
            }

            // var normalizeFactor = (r + g + b + a) * 1.5f;
            var normalizeFactor = Mathf.Max(r, g, b, a);
            r /= normalizeFactor;
            g /= normalizeFactor;
            b /= normalizeFactor;
            a /= normalizeFactor;

            newColors[i] = new Color(r, g, b, a);
        }        

        floorMesh.colors = newColors;
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
        foreach (var cellData in cells.Values)
        {
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
            SpawnPickup(cells[cellCoord], Random.value > 0.5f ? generator.tiledict.barrelPickup : generator.tiledict.cubePickup);
        }
        
        foreach (var cellCoord in coords.ToList().Shuffled().Take(cellCnt * 2/3))
        {
            SpawnDecorations(cells[cellCoord]);
        }
    }

    private void SpawnPickup(CellData cellData, GameObject prefab)
    {
        SpawnDecoration(cellData, "FloorDeco", () => prefab, () => true, areaPickups);
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
                foreach (var walls in wallsToCombine.Values)
                {
                    walls.Remove(cellData.coord);
                }
            }
            else
            {
                var rot = Quaternion.Euler(0, type.rotation * 60, 0);
                var pos = cellData.position - transform.position;
                if (type.type == TileDictionary.EdgeTileType.WALL2_P && Random.value < 0.5f)
                {
                    var combined = generator.tiledict.CombinedMesh(TileDictionary.EdgeTileType.WALL2_P,
                        TileDictionary.EdgeTileType.DOOR);
                    for (int subMeshIdx = 0; subMeshIdx < combined.subMeshCount; subMeshIdx++)
                    {
                        if (!wallsToCombine.TryGetValue(subMeshIdx, out var meshes))
                        {
                            meshes = new Dictionary<CubeCoord, CombineInstance>();
                            wallsToCombine[subMeshIdx] = meshes;
                        }
                        meshes[cellData.coord] = MeshAsCombineInstance(combined, pos, rot, subMeshIdx);
                    }
                }
                else
                {
                    var variant = generator.tiledict.Variant(type.type);
                    cellData.variant = variant.Item1;
                    var wallMesh = variant.Item2;
                    var subMeshIdxMapping = variant.Item1.meshOrder;

                    for (int subMeshIdx = 0; subMeshIdx < wallMesh.subMeshCount; subMeshIdx++)
                    {
                        if (!wallsToCombine.TryGetValue(subMeshIdx, out var meshes))
                        {
                            meshes = new Dictionary<CubeCoord, CombineInstance>();
                            wallsToCombine[subMeshIdx] = meshes;
                        }
                        meshes[cellData.coord] = MeshAsCombineInstance(wallMesh, pos, rot, subMeshIdxMapping[subMeshIdx]);
                    }
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
