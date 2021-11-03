using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Game : MonoBehaviour
{
    public Floaty floatyPrefab;
    public GameObject canvas;

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

    public bool needsNewNavMesh;
    
    private NavMeshSurface navMesh;

    public GameObject tilePrefab;
    private MeshRenderer tileMeshRenderer;
    
    private Dictionary<CubeCoord, GameObject> _knownTiles = new();

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

    private void Start()
    {
        navMesh = GetComponent<NavMeshSurface>();
        tileMeshRenderer = tilePrefab.GetComponentInChildren<MeshRenderer>();

        BuildRoom();
        UpdateNavMesh();
    }

    public void BuildRoom()
    {
        spawnTile(CubeCoord.Origin);
        var coords = CubeCoord.ShuffledRings(CubeCoord.Origin, 1, 8);
        while (coords.MoveNext())
        {
            spawnTile(coords.Current);
        }
    }

    private GameObject spawnTile(CubeCoord pos)
    {
        var tile = Instantiate(tilePrefab, transform, true);
        tile.name = $"{pos}";
        var objectScale = tilePrefab.transform.localScale;
        var tileSize = tileMeshRenderer.bounds.size;
        tileSize.Scale(objectScale);
        tile.transform.position = pos.ToWorld(0, tileSize);
        _knownTiles[pos] = tile;
        return tile;
    }

    private void Update()
    {
        UpdateNavMesh();

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


    public void UpdateNavMesh()
    {
        if (needsNewNavMesh)
        {
            navMesh.BuildNavMesh();
            needsNewNavMesh = false;
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cr.GetComponent<NavMeshAgent>().destination, 0.1f);
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

}
