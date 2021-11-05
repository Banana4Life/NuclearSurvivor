using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class Game : MonoBehaviour
{
    public Floaty floatyPrefab;
    public GameObject canvas;

    public FogOfWarMesh fogOfWar;

    public AudioSourcePool audioSourcePool;

    private static Game INSTANCE;

    private Dictionary<GameObject, GameObjectPool> pools = new();

    public GameObject cr;
    public GameObject camCart;
    
    public float maxPanSpeed = 2f;
    public float panAcceleration = 5f;
    public float panBreaking = 2f;
    public float panLimit = 30;
    public float panStopLimit = 9;
    private bool panning;
    private float panSpeed;

    public AutoTile autoTilePrefab;
    public MeshRenderer referenceTile;
    private MeshRenderer tileMeshRenderer;
    private Vector3 tileSize;
    
    private Dictionary<CubeCoord, AutoTile> _knownTiles = new();
    public GameObject tiles;
    public GameObject roomPrefab;

    public List<NavMeshSurface> rooms = new();

    private void Awake()
    {
        INSTANCE = this;
    }

    public static AudioSourcePool audioPool()
    {
        return INSTANCE.audioSourcePool;
    }
    
    public static void SpawnFloaty(string text, Vector3 pos)
    {
        var floaty = Instantiate(INSTANCE.floatyPrefab, INSTANCE.canvas.transform, false);
        floaty.Init(text, pos);
    }

    public void ArrangeTiles()
    {
        var tileSize = referenceTile.bounds.size;
        using var coords = CubeCoord.Spiral(CubeCoord.Origin, 0, 8).GetEnumerator();
        foreach (Transform tile in tiles.transform)
        {
            if (coords.MoveNext())
            {
                tile.gameObject.name = $"{coords.Current}";
                tile.position = coords.Current.FlatTopToWorld(0, tileSize);
            }
            else
            {
                break;
            }
        }
        tiles.GetComponent<NavMeshSurface>().BuildNavMesh();
    }
    
    private void Start()
    {
        tileMeshRenderer = autoTilePrefab.GetComponentInChildren<MeshRenderer>();
        tileSize = tileMeshRenderer.bounds.size;
        rooms.Add(tiles.GetComponent<NavMeshSurface>());
        
        using var coords = CubeCoord.Spiral(CubeCoord.Origin, 0, 8).GetEnumerator();
        foreach (Transform tile in tiles.transform)
        {
            if (coords.MoveNext())
            {
                _knownTiles[coords.Current] = tile.gameObject.GetOrAddComponent<AutoTile>().Init(coords.Current);
            }
            else
            {
                break;
            }
        }
        fogOfWar.BuildFogMesh(_knownTiles.Keys.ToList(), tileSize);
    }

    public void BuildRoom(Transform prevExit)
    {
        var room = Instantiate(roomPrefab, transform);
        room.name = "Room " + rooms.Count;
        room.transform.position = prevExit.position;
        var entry = CubeCoord.FlatTopFromWorld(prevExit.position, tileSize);
        var coords = CubeCoord.Spiral(entry, 0, 9).Where(coord => !_knownTiles.ContainsKey(coord)).ToList();
        
        // Reparent prevExit as entry and build NavMeshLink on same tile
        _knownTiles[entry].transform.parent = room.transform;
        var link = prevExit.AddComponent<NavMeshLink>();
        var direction = Vector3.left; // TODO direction based on tile
        link.startPoint = Vector3.zero;
        link.endPoint = direction.normalized * float.Epsilon;
        link.width = tileSize.z;

        foreach (var coord in coords)
        {
            spawnTile(coord, room.transform);
        }
        room.GetComponent<NavMeshSurface>().BuildNavMesh();
        fogOfWar.BuildFogMesh(coords, tileSize);
    }

    private void spawnTile(CubeCoord pos, Transform room)
    {
        _knownTiles[pos] = Instantiate(autoTilePrefab, room.transform, true).Init(pos);
    }

    private void Update()
    {
        foreach (var pool in pools.Values)
        {
            pool.Reclaim();
        }

        Ray worldPoint = Camera.main.ScreenPointToRay(Input.mousePosition);
        const int selectableTileLayerMask = 1 << 11;
        if (Physics.Raycast(worldPoint, out RaycastHit hit, Mathf.Infinity, selectableTileLayerMask))
        {
            cr.GetComponent<NavMeshAgent>().destination = hit.point;
        }

        var camDelta = (camCart.transform.position - cr.transform.position).sqrMagnitude;
        if (camDelta > panLimit * panLimit)
        {
            panning = true;
        }
        else if (camDelta < panStopLimit * panStopLimit)
        {
            panning = false;
        }
        
        panSpeed = Math.Max(0, Math.Min(panSpeed + (panning ? panAcceleration : -panBreaking) * Time.deltaTime, maxPanSpeed));
        camCart.transform.position = Vector3.Lerp(camCart.transform.position, cr.transform.position, panSpeed * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        // Ray worldPoint = Camera.main.ScreenPointToRay(Input.mousePosition);
        // const int selectableTileLayerMask = 1 << 11;
        // if (Physics.Raycast(worldPoint, out RaycastHit hit, Mathf.Infinity, selectableTileLayerMask))
        // {
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawWireSphere(hit.point, 1f);;
        //     Gizmos.color = Color.red;
        //     
        //     var tileSize = tilePrefab.GetComponentInChildren<MeshRenderer>().bounds.size;
        //     tileSize.Scale(tilePrefab.transform.localScale);
        //     
        //     var cubeCoord = CubeCoord.FromWorld(hit.point, tileSize);
        //     
        //     Gizmos.DrawWireSphere(cubeCoord.ToWorld(0, tileSize), 1f);;
        // }
        //
    }
    

    private static GameObjectPool PoolFor(GameObject prefab)
    {
        if (!INSTANCE.pools.TryGetValue(prefab, out GameObjectPool pool))
        {
            var poolGo = new GameObject("Empty Pool");
            pool = poolGo.GetOrAddComponent<GameObjectPool>().Init(prefab);
            poolGo.transform.parent = INSTANCE.transform;
            INSTANCE.pools[prefab] = pool;
        }

        return pool;
    }

    public static T Pooled<T>(GameObject prefab)
    {
        return PoolFor(prefab).Pooled<T>();
    }

    public static void LoadNextLevel(Transform prevExit)
    {
        INSTANCE.BuildRoom(prevExit);
    }

    public static AutoTile TileAt(CubeCoord cubeCoord)
    {
        return INSTANCE._knownTiles.TryGetValue(cubeCoord, out var tile) ? tile : null;
    }
}
