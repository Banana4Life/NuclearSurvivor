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
        BuildFogMesh(tiles.transform, _knownTiles.Keys.ToList());
    }

    public Vector3[] Tessellate(Vector3[] input, int steps = 1)
    {
        if (steps == 0)
        {
            return input;
        }
        if (input.Length % 3 != 0)
        {
            throw new ArgumentException("Input Vertices must be a multiple of 3");
        }

        var tesselated = new Vector3[input.Length * 4];
        for (int i = 0; i < input.Length / 3; i++)
        {
            var a = input[i * 3 + 0];
            var b = input[i * 3 + 1];
            var c = input[i * 3 + 2];
            var d = Vector3.Lerp(a, b, 0.5f);
            var e = Vector3.Lerp(b, c, 0.5f);
            var f = Vector3.Lerp(c, a, 0.5f);

            tesselated[i * 12 + 0] = a;
            tesselated[i * 12 + 1] = d;
            tesselated[i * 12 + 2] = f;
            
            tesselated[i * 12 + 3] = b;
            tesselated[i * 12 + 4] = e;
            tesselated[i * 12 + 5] = d;
            
            tesselated[i * 12 + 6] = c;
            tesselated[i * 12 + 7] = f;
            tesselated[i * 12 + 8] = e;
            
            tesselated[i * 12 + 9] = d;
            tesselated[i * 12 + 10] = e;
            tesselated[i * 12 + 11] = f;
        }

        return Tessellate(tesselated, steps - 1);
    }
    
    public void BuildFogMesh(Transform room, IList<CubeCoord> coords)
    {
        // Offset Ring around Origin
        var offsets = CubeCoord.Neighbors.Select(coord => coord.FlatTopToWorld(0, tileSize)).ToArray();
        // Vertices in triples for triangles (clockwise order)
        offsets = Enumerable.Range(0, offsets.Length).SelectMany(i => new[] { offsets[i], offsets[(i + 1) % offsets.Length], Vector3.zero}).ToArray();
        // Tessellate offsets as needed
        offsets = Tessellate(offsets, 2);
        // Find all Hexes and apply tessellated offsets
        var allVerts = coords.Select(coord => coord.FlatTopToWorld(2, tileSize))
            .SelectMany(coord => offsets.Select(offset => offset + coord)).ToArray();

        // Build vertex mapping, deduplicate vertices, build triangles
        Dictionary<Vector3, int> dictionary = new();
        var triangles = new int[allVerts.Length];
        for (var i = 0; i < allVerts.Length; i++)
        {
            if (dictionary.TryAdd(allVerts[i], dictionary.Count))
            {
                // New Vertex
                triangles[i] = dictionary.Count - 1;
            }
            else
            {
                // Existing Vertex - reuse index for triangle
                triangles[i] = dictionary[allVerts[i]];
            }
        }
        var deduplicatedVerts = new Vector3[dictionary.Count];
        foreach (var (v, i) in dictionary)
        {
            deduplicatedVerts[i] = v;
        }
        // All Normals are Up
        var normals = Enumerable.Range(0, deduplicatedVerts.Length).Select(_ => Vector3.up).ToArray();

        // Finally Build the Mesh
        var mesh = new Mesh();
        mesh.vertices = deduplicatedVerts;
        // UVs?
        mesh.triangles = triangles;
        mesh.normals = normals;

        var go = Instantiate(fogOfWarMeshGeneratorPrefab);
        go.transform.parent = room;
        go.transform.position = Vector3.zero;
        mesh.name = $"Generated V{mesh.vertices.Length} T{mesh.triangles.Length}";
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
