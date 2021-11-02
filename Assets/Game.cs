using System;
using System.Collections.Generic;
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

        if ((camCart.transform.position - cr.transform.position).sqrMagnitude > 100)
        {
            camCart.transform.position = Vector3.Lerp(camCart.transform.position, cr.transform.position, Time.deltaTime * 3f);
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
