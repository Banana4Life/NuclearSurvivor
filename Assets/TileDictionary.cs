using System;
using System.Collections.Generic;
using System.Linq;
using FlatTop;
using UnityEngine;
using Random = UnityEngine.Random;

public class TileDictionary : MonoBehaviour
{
    public MeshRenderer referenceTile;
    public TileVariants[] tilePrefabs;

    private Vector3 tileSize;
 
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

                var pos = coords.Current.ToWorld(0, TileSize());
                var floor = Instantiate(tilePrefabs[(int)TileType.FLOOR].prefabs[0].prefab, transform);
                floor.transform.position = pos;
                var wall = Instantiate(tilePrefab.prefab, floor.transform);
                floor.gameObject.name = ((TileType)i).ToString();
            }
        }
    }

    public enum TileType
    {
        FLOOR,
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
        DOOR,
        WALL1_HIDEOUT,
        FLOOR_HIDEOUT,
        FLOOR_DECO,
        WALL_DECO,
        PICKUP_BATTERY,
        PICKUP_CUBE,
        CABLES,
        WALL_DECO_CANDLE,
        WALL_DECO_CHAINS,
        FOOD,
        ROCKS,
    }
    
    public static Dictionary<bool[], TileType> tilemap = new()
    {
        { new[] { false, false, false, false, false ,false}, TileType.FLOOR },
        { new[] { true, false, false, false, false ,false}, TileType.WALL1 },
        { new[] { true, true, false, false, false, false}, TileType.WALL2 },
        { new[] { true, false, false, true, false, false}, TileType.WALL2_P },
        { new[] { true, false, true, false, false, false}, TileType.WALL2_U },
        { new[] { true, true, true, false, false, false}, TileType.WALL3_V },
        { new[] { true, true, false, true, false, false}, TileType.WALL3_J },
        { new[] { true, true, false, false, true, false}, TileType.WALL3_G },
        { new[] { true, false, true, false, true ,false}, TileType.WALL3_O },
        { new[] { true, true, true, true, false, false}, TileType.WALL4_V },
        { new[] { true, true, true, false, true, false}, TileType.WALL4_V2 },
        { new[] { true, true, false, true, true, false}, TileType.WALL4_P },
        { new[] { true, true, true, true, true, false}, TileType.WALL5},
        { new[] { true, true, true, true, true, true}, TileType.WALL6},
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
        public TileType type;
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

    public TileVariant Variant(TileType type)
    {
        var allVariants = tilePrefabs[(int)type].prefabs;
        var variantIndex = Random.Range(0, allVariants.Length);
        return allVariants[variantIndex];;
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
} 