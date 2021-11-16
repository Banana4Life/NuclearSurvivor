using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class TileDictionary : MonoBehaviour
{
    public MeshRenderer referenceTile;
    public TileVariants[] tilePrefabs;
    public GameObject pickupPrefab;
    public GameObject[] wallDecorations;

    private Vector3 tileSize;
    private Dictionary<int, Mesh> combinedMeshes = new();
    private Dictionary<GameObject, Mesh> prefabMeshes = new();
 
    public Vector3 TileSize()
    {
        if (tileSize == Vector3.zero)
        {
            tileSize = referenceTile.bounds.size;
        }
        return tileSize;
    }

    public void PlaceTiles()
    {
        ClearTiles();
        if (!Application.isEditor)
        {
            return;
        }

        var coords = CubeCoord.Spiral(CubeCoord.Origin, 0, 3).GetEnumerator();
        for (var i = 0; i < tilePrefabs.Length; i++)
        {
            var tilePrefabList = tilePrefabs[i];
            foreach (var tilePrefab in tilePrefabList.prefabs)
            {
                if (!coords.MoveNext())
                {
                    return;
                }

                var pos = coords.Current.FlatTopToWorld(0, TileSize());
                var floor = Instantiate(tilePrefabs[(int)EdgeTileType.WALL0].prefabs[0].prefab, transform);
                floor.transform.position = pos;
                var wall = Instantiate(tilePrefab.prefab, floor.transform);
                floor.gameObject.name = ((EdgeTileType)i).ToString();
            }
        }
    }

    public enum EdgeTileType
    {
        WALL0,
        WALL1,
        WALL2,
        WALL2_P,
        WALL2_U,
        WALL3_G,
        WALL3_J,
        WALL3_O,
        WALL3_V,
        WALL4_P,
        WALL4_V,
        WALL4_V2,
        WALL5,
        WALL6,
        DOOR
    }
    
    public static Dictionary<bool[], EdgeTileType> tilemap = new()
    {
        { new[] { false, false, false, false, false ,false}, EdgeTileType.WALL0 },
        { new[] { true, false, false, false, false ,false}, EdgeTileType.WALL1 },
        { new[] { true, true, false, false, false, false}, EdgeTileType.WALL2 },
        { new[] { true, false, false, true, false, false}, EdgeTileType.WALL2_P },
        { new[] { true, false, true, false, false, false}, EdgeTileType.WALL2_U },
        { new[] { true, true, true, false, false, false}, EdgeTileType.WALL3_V },
        { new[] { true, true, false, true, false, false}, EdgeTileType.WALL3_J },
        { new[] { true, true, false, false, true, false}, EdgeTileType.WALL3_G },
        { new[] { true, false, true, false, true ,false}, EdgeTileType.WALL3_O },
        { new[] { true, true, true, true, false, false}, EdgeTileType.WALL4_V },
        { new[] { true, true, true, false, true, false}, EdgeTileType.WALL4_V2 },
        { new[] { true, true, false, true, true, false}, EdgeTileType.WALL4_P },
        { new[] { true, true, true, true, true, false}, EdgeTileType.WALL5},
        { new[] { true, true, true, true, true, true}, EdgeTileType.WALL6},
    };
    


    
    public static Dictionary<bool[], RotatedTileType> edgeTileMap = CalcEdgeRotatedTileMap();
    static Dictionary<bool[], RotatedTileType> CalcEdgeRotatedTileMap()
    {
        var tileMap = new Dictionary<bool[], RotatedTileType>(new BoolArrayEqualityComparer());
        foreach (var original in TileDictionary.tilemap)
        {
            var rot = original.Key;
            tileMap[original.Key] = new RotatedTileType { rotation = 0, type = original.Value };
            for (int i = 1; i < 6; i++)
            {
                rot = rotate(rot);
                tileMap[rot] = new RotatedTileType { rotation = i, type = original.Value };
            }
        }
        return tileMap;
    }
    
    public static bool[] rotate(bool[] original)
    {
        var rotated = new bool[original.Length];
        Array.Copy(original, 0, rotated, 1, original.Length -1);
        rotated[0] = original[^1];
        return rotated;
    }
    
    public struct RotatedTileType
    {
        public EdgeTileType type;
        public int rotation;
    }
    private class BoolArrayEqualityComparer : IEqualityComparer<bool[]>
    {
        public bool Equals(bool[] x, bool[] y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(bool[] x)
        {
            int result = 29;
            foreach (bool b in x)
            {
                if (b) { result++; }
                result *= 23;
            }
            return result;
        }
    }


    public void ClearTiles()
    {
        if (!Application.isEditor)
        {
            return;
        }

        while (transform.childCount != 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    public (TileVariant, Mesh, int) Variant(EdgeTileType type)
    {
        var allVariants = tilePrefabs[(int)type].prefabs;
        var variantIndex = Random.Range(0, allVariants.Length);
        var variant = allVariants[variantIndex];
        if (prefabMeshes.TryGetValue(variant.prefab, out var mesh))
        {
            return (variant, mesh, variantIndex);
        }
        prefabMeshes[variant.prefab] = variant.prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
        return (variant, prefabMeshes[variant.prefab], variantIndex);
    }
    
    public int[] SubMeshs(EdgeTileType type)
    {
        return tilePrefabs[(int)type].prefabs[0].meshOrder;
    }

    public Mesh CombinedMesh(params EdgeTileType[] tileTypes)
    {
        var key = tileTypes.Select(tt => 1 << (int)tt).Aggregate(0, (prev, next) => prev | next);
        if (combinedMeshes.TryGetValue(key, out var combinedMesh))
        {
            return combinedMesh;
        }
        
        Dictionary<int, List<CombineInstance>> combineInstances = new();
        foreach (var tileType in tileTypes)
        {
            var subMeshes = SubMeshs(tileType);
            var mesh = Variant(tileType).Item2;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (!combineInstances.TryGetValue(i, out var list))
                {
                    list = new();
                    combineInstances[i] = list;
                }
                list.Add(new CombineInstance()
                {
                    mesh = mesh,
                    subMeshIndex = subMeshes[i]
                });
            }

        }

        List<CombineInstance> combinedSubMeshes = new();
        foreach (var list in combineInstances.Values)
        {
            var combinedSubMesh = new Mesh();
            combinedSubMesh.CombineMeshes(list.ToArray(), true, false);
            combinedSubMeshes.Add(new CombineInstance()
            {
                mesh = combinedSubMesh
            });
        }

        var combined = new Mesh();
        combined.CombineMeshes(combinedSubMeshes.ToArray(), false, false);
        combined.name = String.Join("+", tileTypes.Select(t => t.ToString()));
        combinedMeshes[key] = combined;
        return combined;
    }

    public GameObject DecorationPrefab()
    {
        return wallDecorations[Random.Range(0, wallDecorations.Length)];
    }
}

[Serializable]
public struct TileVariants
{
    public TileVariant[] prefabs;
} 

[Serializable]
public struct TileVariant
{
    public GameObject prefab;
    public int[] meshOrder;
} 