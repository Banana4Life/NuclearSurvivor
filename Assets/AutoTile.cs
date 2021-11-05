using UnityEngine;
using Random = UnityEngine.Random;

public class AutoTile : MonoBehaviour
{
    
    public bool[] connections = new bool[6];
    public CubeCoord coord;
    public bool isTrigger;
    public bool isDoor;
    public bool hasPickup;
    public bool isLinkOnly;

    public TileDictionary tileDict;
    public GameObject thisTile;

    public AutoTile PlaceTile()
    {
        GetEdgeWalls();
        Destroy(thisTile);
        
        thisTile = Instantiate(tileDict.prefab(TileDictionary.EdgeTileType.NO_WALL), transform);
        if (TileDictionary.edgeTileMap.TryGetValue(connections, out var type))
        {
            if (type.type != TileDictionary.EdgeTileType.NO_WALL)
            {
                var wall = Instantiate(tileDict.prefabs[(int)type.type], thisTile.transform);
                wall.transform.RotateAround(transform.position, Vector3.up, 60 * type.rotation);
            }

            // TODO Doors
            if (type.type == TileDictionary.EdgeTileType.WALL2_Parallel)
            {
                if (Random.value < 0.1f)
                {
                    isDoor = true;
                    thisTile = Instantiate(tileDict.prefabs[(int)TileDictionary.EdgeTileType.WALL2_Parallel], transform);
                    thisTile.transform.RotateAround(transform.position, Vector3.up, 60 * type.rotation);
                    return this;
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
            Instantiate(tileDict.pickupPrefab, thisTile.transform);
        }

        return this;

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

    private void Awake()
    {
        hasPickup = Random.value < 0.05f;
    }

    public AutoTile Init(CubeCoord pos, bool isNavMeshLink = false)
    {
        
        coord = pos;
        gameObject.name = $"Tile {pos}";
        transform.position = pos.FlatTopToWorld(0, tileDict.TileSize());
        isLinkOnly = isNavMeshLink;
        PlaceTile();
        return this;
    }

    public void GetEdgeWalls()
    {
        connections = new bool[6];
        if (!isLinkOnly)
        {
            for (var i = 0; i < CubeCoord.FlatTopNeighbors.Length; i++)
            {
                var cubeCoord = coord + CubeCoord.FlatTopNeighbors[i];
                var autoTile = Game.TileAt(cubeCoord);
                connections[i] = autoTile == null;
            }    
        }
    }

    public void SetTrigger()
    {
        isTrigger = true;
        gameObject.name = $"Trigger {coord}";
        Instantiate(tileDict.triggerPrefab, thisTile.transform).GetComponent<LevelLoader>().thisTile = this;
    }

    public void HideNavMeshLinkPlate()
    {
        Destroy(thisTile);
    }
}
