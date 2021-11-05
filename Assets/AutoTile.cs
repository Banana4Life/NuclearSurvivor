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

    public GameObject thisTile;
    
    public GameObject triggerPrefab;
    public GameObject pickupPrefab;

    public GameObject[] edgePrefabs;
    
    private static Dictionary<bool[], EdgeTileType> edgeMeshTileMap = new()
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
    
    private struct RotatedTileType<T>
    {
        public T type;
        public int rotation;
    }
    
    private static Dictionary<bool[], RotatedTileType<EdgeTileType>> edgeTileMap = CalcEdgeRotatedTileMap();
    static Dictionary<bool[], RotatedTileType<EdgeTileType>> CalcEdgeRotatedTileMap()
    {
        var tileMap = new Dictionary<bool[], RotatedTileType<EdgeTileType>>(new BoolArrayEqualityComparer());
        foreach (var original in edgeMeshTileMap)
        {
            var rot = original.Key;
            tileMap[original.Key] = new RotatedTileType<EdgeTileType> { rotation = 0, type = original.Value };
            for (int i = 1; i < 6; i++)
            {
                rot = rotate(rot);
                tileMap[rot] = new RotatedTileType<EdgeTileType> { rotation = i, type = original.Value };
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

    public void PlaceTile()
    {
        Destroy(thisTile);
        thisTile = Instantiate(edgePrefabs[(int)EdgeTileType.NO_WALL], transform);
        if (edgeTileMap.TryGetValue(connections, out var type))
        {
            if (type.type != EdgeTileType.NO_WALL)
            {
                var wall = Instantiate(edgePrefabs[(int)type.type], thisTile.transform);
                wall.transform.RotateAround(transform.position, Vector3.up, 60 * type.rotation);
            }

            // TODO Doors
            if (type.type == EdgeTileType.WALL2_Parallel)
            {
                if (Random.value < 0.1f)
                {
                    isDoor = true;
                    thisTile = Instantiate(edgePrefabs[(int)EdgeTileType.WALL2_Parallel], transform);
                    thisTile.transform.RotateAround(transform.position, Vector3.up, 60 * type.rotation);
                    return;
                }
            }
        }
        else
        {
            Debug.Log("Wall Type not found " + string.Join("|", connections));
        }
        
        // Tile Features
        if (hasPickup)
        {
            Instantiate(pickupPrefab, thisTile.transform);
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
        
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    private void Awake()
    {
        hasWall = Random.value < 0.5f;
        hasPickup = !hasWall && Random.value < 0.05f;
    }

    public AutoTile Init(CubeCoord pos, bool isNavMeshLink = false)
    {
        tileSize = referenceTile.bounds.size;
        coord = pos;
        gameObject.name = $"Tile {pos}";
        transform.position = pos.FlatTopToWorld(0, tileSize);
        connections = new bool[6];
        // Edge Walls
        if (!isNavMeshLink)
        {
            for (var i = 0; i < CubeCoord.FlatTopNeighbors.Length; i++)
            {
                var cubeCoord = coord + CubeCoord.FlatTopNeighbors[i];
                var autoTile = Game.TileAt(cubeCoord);
                connections[i] = autoTile == null;
            }
            // TODO update edge walls when generating new rooms
        }
        // Center Walls
        // if (!isNavMeshLink && hasWall)
        // {
        //     for (var i = 0; i < CubeCoord.FlatTopNeighbors.Length; i++)
        //     {
        //         var cubeCoord = coord + CubeCoord.FlatTopNeighbors[i];
        //         var autoTile = Game.TileAt(cubeCoord);
        //         if (autoTile)
        //         {
        //             connections[i] = autoTile.hasWall;
        //         }
        //     }
        // }
        PlaceTile();
        return this;
    }

    public void SetTrigger()
    {
        isTrigger = true;
        gameObject.name = $"Trigger {coord}";
        Instantiate(triggerPrefab, thisTile.transform).GetComponent<LevelLoader>().thisTile = this;
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

    public void HideNavMeshLinkPlate()
    {
        Destroy(thisTile);
    }
    
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
