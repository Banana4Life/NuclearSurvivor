using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AutoTile : MonoBehaviour
{
    public MeshRenderer referenceTile;
    
    public bool walled;
    public bool[] connections;
    public bool connectionsCalculated;
    private Vector3 tileSize;
    public CubeCoord coord;
    public bool isTrigger;
    public bool isDoor;
    public bool isVoid;
    public string top;

    public GameObject voidPrefab;
    public GameObject triggerDoorPrefab;
    
    private static Dictionary<int[], TileType> tileMapping = new()
    {
        { new[] { 1, 0, 0, 0, 0 ,0}, TileType.WALL1_END },
        { new[] { 1, 1, 0, 0, 0, 0}, TileType.WALL2_SHARP },
        { new[] { 1, 0, 1, 0, 0, 0}, TileType.WALL2_OBTUSE },
        { new[] { 1, 0, 0, 1, 0, 0}, TileType.WALL2_STRAIGHT },
        { new[] { 1, 1, 1, 0, 0, 0}, TileType.WALL3_W },
        { new[] { 1, 1, 0, 1, 0, 0}, TileType.WALL3_Y_LEFT },
        { new[] { 1, 1, 0, 0, 1, 0}, TileType.WALL3_Y_RIGHT },
        { new[] { 1, 0, 1, 0, 1 ,0}, TileType.WALL3_Y },
        { new[] { 1, 1, 1, 1, 0, 0}, TileType.WALL4_V },
        { new[] { 1, 1, 1, 0, 1, 0}, TileType.WALL4_W },
        { new[] { 1, 1, 0, 1, 1, 0}, TileType.WALL4_X },
        { new[] { 1, 1, 1, 1, 1, 0}, TileType.WALL5},
        { new[] { 1, 1, 1, 1, 1, 1}, TileType.WALL6},
    };
    
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
        walled = Random.value > 0.5f;
        tileSize = referenceTile.bounds.size;
        if (isVoid)
        {
            Instantiate(voidPrefab, transform);
        }
        if (isTrigger)
        {
            Instantiate(triggerDoorPrefab, transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!connectionsCalculated)
        {
            connections = new bool[6];
            if (walled)
            {
                for (var i = 0; i < CubeCoord.FlatTopNeighbors.Length; i++)
                {
                    var cubeCoord = coord + CubeCoord.FlatTopNeighbors[i];
                    if (i == 0)
                    {
                        top = cubeCoord.ToString();
                    }
                    var autoTile = Game.TileAt(cubeCoord);
                    if (autoTile)
                    {
                        connections[i] = autoTile.walled;
                    }
                }
            }
            connectionsCalculated = true;
        }
    }

    public AutoTile Init(CubeCoord pos)
    {
        coord = pos;
        gameObject.name = $"{pos}";
        transform.position = pos.FlatTopToWorld(0, tileSize);
        return this;
    }

    private void OnDrawGizmos()
    {
        if (walled)
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
}

public enum TileType
{
    WALL1_END, 
    WALL2_SHARP, 
    WALL2_OBTUSE, 
    WALL2_STRAIGHT,
    WALL3_W,
    WALL3_Y,
    WALL3_Y_RIGHT,
    WALL3_Y_LEFT, 
    WALL4_X,
    WALL4_V,
    WALL4_W,
    WALL5,
    WALL6,
    DOOR_STRAIGHT,
}