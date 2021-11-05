using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AutoTile : MonoBehaviour
{
    public MeshRenderer referenceTile;
    
    public bool hasWall;
    public bool[] connections;
    private Vector3 tileSize;
    public CubeCoord coord;
    public bool isTrigger;
    public bool isDoor;
    public bool hasPickup;

    private GameObject feature;
    
    public GameObject triggerDoorPrefab;
    public GameObject pickupPrefab;

    public GameObject[] prefabs;
    
    private static Dictionary<bool[], TileType> meshTileMap = new()
    {
        { new[] { false, false, false, false, false ,false}, TileType.NO_WALL },
        { new[] { true, false, false, false, false ,false}, TileType.WALL1_END },
        { new[] { true, true, false, false, false, false}, TileType.WALL2_SHARP },
        { new[] { true, false, true, false, false, false}, TileType.WALL2_OBTUSE },
        { new[] { true, false, false, true, false, false}, TileType.WALL2_STRAIGHT },
        { new[] { true, true, true, false, false, false}, TileType.WALL3_W },
        { new[] { true, true, false, true, false, false}, TileType.WALL3_Y_RIGHT },
        { new[] { true, true, false, false, true, false}, TileType.WALL3_Y_LEFT },
        { new[] { true, false, true, false, true ,false}, TileType.WALL3_Y },
        { new[] { true, true, true, true, false, false}, TileType.WALL4_V },
        { new[] { true, true, true, false, true, false}, TileType.WALL4_W },
        { new[] { true, true, false, true, true, false}, TileType.WALL4_X },
        { new[] { true, true, true, true, true, false}, TileType.WALL5},
        { new[] { true, true, true, true, true, true}, TileType.WALL6},
    };
    
    private struct RotatedTileType
    {
        public TileType type;
        public int rotation;
    }

    private static Dictionary<bool[], RotatedTileType> tileMap = CalcRotatedTileMap();
    static Dictionary<bool[], RotatedTileType> CalcRotatedTileMap()
    {
        var tileMap = new Dictionary<bool[], RotatedTileType>(new BoolArrayEqualityComparer());
        foreach (var original in meshTileMap)
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

    
    public static bool[] rotate(bool[] original)
    {
        var rotated = new bool[original.Length];
        Array.Copy(original, 0, rotated, 1, original.Length -1);
        rotated[0] = original[^1];
        return rotated;
    }

    private void PlaceFeature()
    {
        if (isTrigger)
        {
            // TODO place trigger
        }
        if (hasPickup)
        {
            feature = Instantiate(pickupPrefab, transform);
            return;
        }
        if (tileMap.TryGetValue(connections, out var type))
        {
            if (type.type == TileType.WALL2_STRAIGHT)
            {
                if (Random.value < 0.1f)
                {
                    isDoor = true;
                    feature = Instantiate(triggerDoorPrefab, transform);
                    feature.transform.RotateAround(transform.position, Vector3.up, 60 * type.rotation);
                    return;
                }
            }
            
            feature = Instantiate(prefabs[(int)type.type], transform);
            feature.transform.RotateAround(transform.position, Vector3.up, 60 * type.rotation);
        }
        else
        {
            Debug.Log("Wall Type not found " + string.Join("|", connections));
        }
    }
    
    // Flat Top Hex Directions (multiply with tilesize) - Top first clockwise
    private static Vector2[] directions = {
        new(0, 1f / 2f), // top
        new(3f / 8f, 1f / 4f), // top-right
        new(3f / 8f, -1f / 4f), // bottom-right
        new(0, -1f / 2f), // bottom
        new(-3f / 8f, -1f / 4f), // bottom-left
        new(-3f / 8f, 1f / 4f), // top-left
    };
    
    // Start is called before the first frame update
    void Start()
    {
        hasWall = Random.value < 0.5f;
        hasPickup = !hasWall && Random.value < 0.05f;
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    public AutoTile Init(CubeCoord pos, bool isTrigger = false)
    {
        tileSize = referenceTile.bounds.size;
        coord = pos;
        gameObject.name = (isTrigger ? "Trigger" : "Tile") + $" {pos}";
        transform.position = pos.FlatTopToWorld(0, tileSize);
        this.isTrigger = isTrigger; 
        connections = new bool[6];
        if (!isTrigger)
        {
            if (hasWall)
            {
                for (var i = 0; i < CubeCoord.FlatTopNeighbors.Length; i++)
                {
                    var cubeCoord = coord + CubeCoord.FlatTopNeighbors[i];
                    var autoTile = Game.TileAt(cubeCoord);
                    if (autoTile)
                    {
                        connections[i] = autoTile.hasWall;
                    }
                }
            }
        }
        PlaceFeature();
        return this;
    }

    private void OnDrawGizmos()
    {
        if (hasWall)
        {
            Gizmos.color = Color.gray;
        }
        else
        {
            Gizmos.color = Color.black;
        }
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);


        if (connections != null)
        {
            for (var i = 0; i < connections.Length; i++)
            {
                if (connections[i])
                {
                    Gizmos.color = Color.green;
                    var dir = directions[i];
                    Gizmos.DrawLine(transform.position, transform.position + new Vector3(dir.x * tileSize.x, 0, dir.y * tileSize.z));
                }
            }    
        }
    }

    public void HideTrigger()
    {
        
    }
}

public enum TileType
{
    NO_WALL,
    WALL1_END, 
    WALL2_SHARP, 
    WALL2_OBTUSE, 
    WALL2_STRAIGHT,
    WALL3_W,
    WALL3_Y,
    WALL3_Y_LEFT,
    WALL3_Y_RIGHT, 
    WALL4_X,
    WALL4_V,
    WALL4_W,
    WALL5,
    WALL6,
    DOOR_STRAIGHT,
}