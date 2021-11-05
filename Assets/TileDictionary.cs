using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileDictionary : MonoBehaviour
{
    public MeshRenderer referenceTile;
    private Vector3 tileSize;

    public GameObject[] prefabs;

    public GameObject triggerPrefab;
    public GameObject pickupPrefab;

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
        
    }
    

    public GameObject prefab(EdgeTileType type)
    {
        return prefabs[(int)type];
    }
    
    public enum EdgeTileType
    {
        NO_WALL,
        WALL1,
        WALL2,
        WALL2_Parallel,
        WALL2_U,
        WALL3_V,
        WALL3_J,
        WALL3_G,
        WALL3_O,
        WALL4_V,
        WALL4_V2,
        WALL4_Parallel,
        WALL5,
        WALL6
    }
    
    public static Dictionary<bool[], EdgeTileType> tilemap = new()
    {
        { new[] { false, false, false, false, false ,false}, EdgeTileType.NO_WALL },
        { new[] { true, false, false, false, false ,false}, EdgeTileType.WALL1 },
        { new[] { true, true, false, false, false, false}, EdgeTileType.WALL2 },
        { new[] { true, false, false, true, false, false}, EdgeTileType.WALL2_U },
        { new[] { true, false, true, false, false, false}, EdgeTileType.WALL2_Parallel },
        { new[] { true, true, true, false, false, false}, EdgeTileType.WALL3_V },
        { new[] { true, true, false, true, false, false}, EdgeTileType.WALL3_J },
        { new[] { true, true, false, false, true, false}, EdgeTileType.WALL3_G },
        { new[] { true, false, true, false, true ,false}, EdgeTileType.WALL3_O },
        { new[] { true, true, true, true, false, false}, EdgeTileType.WALL4_V },
        { new[] { true, true, true, false, true, false}, EdgeTileType.WALL4_V2 },
        { new[] { true, true, false, true, true, false}, EdgeTileType.WALL4_Parallel },
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
    
    
    

}
