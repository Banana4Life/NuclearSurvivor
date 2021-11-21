using System;
using System.Collections.Generic;
using System.Linq;
using FlatTop;
using UnityEngine;
using Random = UnityEngine.Random;

enum MountType
{
    WALL,
    FLOOR,
}

public class TileArea : MonoBehaviour
{
    public MeshFilter floorMeshFilter;
    public MeshFilter wallMeshFilter;
    
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private TileGenerator generator;
    private Dictionary<CubeCoord, CellData> cells = new();
    private Dictionary<CubeCoord, GameObject> cellDecorations = new();

    private PrefabCombiner<CubeCoord> floorPrefabCombiner;
    private PrefabCombiner<CubeCoord> wallPrefabCombiner;
    
    public Mesh floorMesh ;
    public Mesh wallMesh ;

    private GameObject areaInteractables;
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

        public bool IsWall => walls.Any(x => x);
    }

    public bool IsWall(CubeCoord coord)
    {
        if (cells.TryGetValue(coord, out var cell))
        {
            return cell.IsWall;
        }

        return false;
    }

    public void InitMeshes(String coord)
    {
        wallMesh = new();
        wallMesh.name = $"Wall Mesh {coord}";
    }
    
    public void Init(TileGenerator generator, Room room)
    {
        this.generator = generator;
        gameObject.name = "Room " + room.Origin;
        transform.position = room.WorldCenter;
        InitMeshes(room.Origin.ToString());
        InitCells(room.Coords);
    }

    public void Init(TileGenerator generator, Hallway hallway)
    {
        this.generator = generator;
        gameObject.name = "Hallway";
        transform.position = Vector3.Lerp(hallway.From.Origin.ToWorld(generator.floorHeight,  generator.tiledict.TileSize()),
            hallway.To.Origin.ToWorld(generator.floorHeight,  generator.tiledict.TileSize()), 0.5f);
        InitMeshes($"{hallway.From.Origin}|{hallway.To.Origin}");
        InitCells(hallway.Coords);
    }

    private void InitVertexColors()
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
            var coord = CubeCoordFlatTop.FromWorld(vertex + areaPos, generator.tiledict.TileSize());
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
        var neighbors = coord.Neighbors();
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
            SpawnTile(cellData);
        }
    }

    private void InitCells(IEnumerable<CubeCoord> coords)
    {
        var thisPos = transform.position;

        areaInteractables = new GameObject("Interactables");
        areaInteractables.transform.parent = transform;
        areaInteractables.transform.position = thisPos;
        
        areaDecorations = new GameObject("Decorations");
        areaDecorations.transform.parent = transform;
        areaDecorations.transform.position = thisPos;

        floorPrefabCombiner = new PrefabCombiner<CubeCoord>(thisPos, true, generator.uvFactor);
        wallPrefabCombiner = new PrefabCombiner<CubeCoord>(thisPos);

        // Init Cell Floor & Walls + store CellData
        foreach (var cellCoord in coords)
        {
            var cellData = new CellData()
            {
                coord = cellCoord,
                position = cellCoord.ToWorld(generator.floorHeight, generator.tiledict.TileSize()),
                walls = GetEdgeWalls(cellCoord)
            };
            SpawnTile(cellData);
        }
      
    }

    private void SpawnInstance(CellData cellData, TileDictionary.TileType tileType, MountType mountType)
    {
        var prefab = generator.tiledict.Variant(tileType).prefab;
        SpawnDecoration(cellData, MountType.FLOOR, () => prefab, () => true, areaInteractables);
    }

    private void WithCellData(CubeCoord coord, Action<CellData> f)
    {
        if (!cells.TryGetValue(coord, out var data))
        {
            throw new Exception("no tiledata");
        }
        f(data);
    }

    public void SpawnOnFloor(CubeCoord coord, TileDictionary.TileType type)
    {
        WithCellData(coord, data => SpawnInstance(data, type, MountType.FLOOR));
    }

    public void SpawnOnWall(CubeCoord coord, TileDictionary.TileType type)
    {
        WithCellData(coord, data => SpawnInstance(data, type, MountType.WALL));
    }

    private (Vector3, Quaternion) GetPositionAndRotation(CellData data) => GetPositionAndRotation(data, data.type);

    private (Vector3, Quaternion) GetPositionAndRotation(CellData data, TileDictionary.RotatedTileType type)
    {
        var rot = Quaternion.Euler(0, type.rotation * 60, 0);
        var pos = data.position - transform.position;
        return (pos, rot);
    }

    public void TransformIntoHideout(CubeCoord coord)
    {
        WithCellData(coord, data =>
        {
            if (data.type.type != TileDictionary.TileType.WALL1)
            {
                throw new Exception("Hideouts can only exist at WALL1 tiles!");
            }
            var (pos, rot) = GetPositionAndRotation(data);
            var hideoutFloorVariant = generator.tiledict.Variant(TileDictionary.TileType.FLOOR_HIDEOUT);
            data.variant = generator.tiledict.Variant(TileDictionary.TileType.WALL1_HIDEOUT);
            wallPrefabCombiner.SetPrefab(data.coord, pos, rot, data.variant.prefab);
            floorPrefabCombiner.AddPrefab(data.coord, pos, rot, hideoutFloorVariant.prefab);
        });
    }

    private void SpawnTile(CellData cellData)
    {
        if (TileDictionary.edgeTileMap.TryGetValue(cellData.walls, out var type))
        {
            cellData.type = type;
            var floorVariant = generator.tiledict.Variant(TileDictionary.TileType.FLOOR);
            cellData.variant = floorVariant;
            wallPrefabCombiner.Remove(cellData.coord);
            floorPrefabCombiner.SetPrefab(cellData.coord, cellData.position - transform.position, Quaternion.identity, floorVariant.prefab);

            var (pos, rot) = GetPositionAndRotation(cellData, type);
            
            if (type.type != TileDictionary.TileType.FLOOR)
            {
                cellData.variant = generator.tiledict.Variant(type.type);
                wallPrefabCombiner.SetPrefab(cellData.coord, pos, rot, cellData.variant.prefab);
                if (type.type == TileDictionary.TileType.WALL2_P && Random.value < 0.5f)
                {
                    var doorVariant = generator.tiledict.Variant(TileDictionary.TileType.DOOR);
                    wallPrefabCombiner.AddPrefab(cellData.coord, pos, rot, doorVariant.prefab);
                }
            }
            cells[cellData.coord] = cellData;
            
            if (type.type == TileDictionary.TileType.WALL1) // TODO randomly spawn hiding spot
            {
                TransformIntoHideout(cellData.coord);
            }
        }
        else
        {
            Debug.Log("Wall Type not found " + string.Join("|", cellData.walls));
        }
    }

    private void SpawnDecoration(CellData cellData, MountType mountType, Func<GameObject> decorationSupplier, Func<Boolean> shouldPlace, GameObject parent)
    {
        var mountTag = mountType switch
        {
            MountType.WALL => "WallDeco",
            MountType.FLOOR => "FloorDeco",
            _ => ""
        };
        if (cellData.variant.prefab == null) // No Variant set
        {
            return;
        }
        var rot = Quaternion.Euler(0, cellData.type.rotation * 60, 0);
        foreach (Transform mountPoint in cellData.variant.prefab.transform)
        {
            if (mountPoint.CompareTag(mountTag) && shouldPlace())
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
    
    private void CombineMeshes()
    {       
        floorMesh = floorPrefabCombiner.CombineMeshes();
        wallMesh = wallPrefabCombiner.CombineMeshes();
        floorMeshFilter.sharedMesh = floorMesh;
        wallMeshFilter.sharedMesh = wallMesh;
        floorMeshFilter.gameObject.GetComponent<MeshCollider>().sharedMesh = floorMesh;
        floorMeshFilter.gameObject.GetComponent<MeshRenderer>().materials = floorPrefabCombiner.Materials();
        wallMeshFilter.gameObject.GetComponent<MeshCollider>().sharedMesh = wallMesh;
        wallMeshFilter.gameObject.GetComponent<MeshRenderer>().materials = wallPrefabCombiner.Materials();

        InitVertexColors();
    }

    public void FinalizeArea()
    {
        CombineMeshes();
        floorPrefabCombiner.SpawnAdditionalGo(floorMeshFilter.transform);
        wallPrefabCombiner.SpawnAdditionalGo(wallMeshFilter.transform);
    }
}
