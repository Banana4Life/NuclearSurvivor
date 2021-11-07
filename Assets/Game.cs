using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Game : MonoBehaviour
{
    private static Game INSTANCE;
    
    public Floaty floatyPrefab;
    public GameObject canvas;

    public AudioSourcePool audioSourcePool;


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

    public float timeLeft = 60;
    public float points = 0;

    public GameObject tiles;

    public TileGenerator generator;
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 50, 20), ((int)(1.0f / Time.smoothDeltaTime)).ToString());        
        GUI.Label(new Rect(10, 30, 50, 20), timeLeft.ToString());        
        GUI.Label(new Rect(10, 50, 50, 20), points.ToString());        
    }
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
        Destroy(tiles);
    }
    
    private Plane plane = new Plane(Vector3.up, Vector3.zero);

    
    private void Update()
    {
        timeLeft -= Time.deltaTime;
        foreach (var pool in pools.Values)
        {
            pool.Reclaim();
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out var dist)) // We have no height at the moment
        {
            cr.GetComponent<NavMeshAgent>().destination = ray.GetPoint(dist);  
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

}
