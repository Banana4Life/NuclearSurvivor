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

    public GameObject fogOfWarMeshGeneratorPrefab;

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

    public GameObject tilePrefab;
    public GameObject doorPrefab;
    private MeshRenderer tileMeshRenderer;
    private Vector3 tileSize;
    
    private Dictionary<CubeCoord, GameObject> _knownTiles = new();
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
        var objectScale = tilePrefab.transform.localScale;
        var tileSize = tilePrefab.GetComponentInChildren<MeshRenderer>().bounds.size;
        tileSize.Scale(objectScale);

        var coords = CubeCoord.Spiral(CubeCoord.Origin, 0, 8);
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
        tileMeshRenderer = tilePrefab.GetComponentInChildren<MeshRenderer>();
        tileSize = tileMeshRenderer.bounds.size;
        tileSize.Scale(tilePrefab.transform.localScale);
        rooms.Add(tiles.GetComponent<NavMeshSurface>());
        
        var coords = CubeCoord.Spiral(CubeCoord.Origin, 0, 8);
        foreach (Transform tile in tiles.transform)
        {
            if (coords.MoveNext())
            {
                _knownTiles[coords.Current] = tile.gameObject;
            }
            else
            {
                break;
            }
        }
        
    }
    
    private static readonly int[] Triangles =
    {
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 5,
        0, 5, 6,
        0, 6, 1
    };
    
    public void BuildFogMesh(Transform room, IList<CubeCoord> coords)
    {
        var verticesOffset = CubeCoord.Spiral(CubeCoord.Origin, 0, 2).ToList().Select(coord => coord.FlatTopToWorld(0, tileSize)).ToList();

        var mesh = new Mesh();
        mesh.vertices = coords.Select(coord => coord.FlatTopToWorld(5, tileSize))
            .SelectMany(coord => verticesOffset.Select(offset => offset + coord)).ToArray();
        // UVs?
        mesh.triangles = Enumerable.Range(0, coords.Count).SelectMany(i => Triangles.Select(j => i * 7 + j)).ToArray();
        mesh.normals = Enumerable.Range(0, mesh.vertices.Length).Select(_ => Vector3.up).ToArray();

        var go = Instantiate(fogOfWarMeshGeneratorPrefab);
        go.transform.parent = room;
        go.transform.position = Vector3.zero;
        mesh.name = $"Generated V{mesh.vertices.Length} T{mesh.triangles.Length} N{mesh.normals.Length} AR{verticesOffset.Count}";
        go.GetComponent<FogOfWarMesh>().Init(mesh);
        Debug.Log($"Building Fog Mesh with {coords.ToList().Count} Tiles");
    }

    public void BuildRoom(Transform prevExit)
    {
        var room = Instantiate(roomPrefab, transform);
        room.name = "Room " + rooms.Count;
        room.transform.position = prevExit.position;
        
        tileSize.Scale(tilePrefab.transform.localScale);
        var entry = CubeCoord.FlatTopFromWorld(prevExit.position, tileSize);
        var coords = CubeCoord.Spiral(entry, 0, 6).Where(coord => !_knownTiles.ContainsKey(coord)).ToList();
        
        // Reparent prevExit as entry and build NavMeshLink on same tile
        _knownTiles[entry].transform.parent = room.transform;
        var link = prevExit.AddComponent<NavMeshLink>();
        var direction = Vector3.left; // TODO direction based on tile
        link.startPoint = Vector3.zero;
        link.endPoint = direction.normalized * float.Epsilon;
        link.width = tileSize.z;

        foreach (var cubeCoord in coords)
        {
            var newTile = spawnTile(cubeCoord, room.transform);
        }
        room.GetComponent<NavMeshSurface>().BuildNavMesh();
        
        BuildFogMesh(room.transform, coords);
    }

    private GameObject spawnTile(CubeCoord pos, Transform room)
    {
        var tile = Instantiate(tilePrefab, room.transform, true);
        tile.name = $"{pos}";
        tile.transform.position = pos.FlatTopToWorld(0, tileSize);
        _knownTiles[pos] = tile;
        
        // var door = Instantiate(doorPrefab, tile.transform, true);
        // door.name = "Door";
        // door.transform.position = tile.transform.position;
        return tile;
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
}

[CustomEditor(typeof(Game))]
public class GameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Layout"))
        {
            ((Game)target).ArrangeTiles();
        }
    }
}
