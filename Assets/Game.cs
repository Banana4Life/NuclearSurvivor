using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Game : MonoBehaviour
{
    public Floaty floatyPrefab;
    public GameObject canvas;

    public AudioSourcePool audioSourcePool;

    private static Game INSTANCE;

    private Dictionary<GameObject, GameObjectPool> pools = new();
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
    }

    public static T Pooled<T>(GameObject prefab)
    {
        if (!INSTANCE.pools.TryGetValue(prefab, out GameObjectPool pool))
        {
            var poolGo = new GameObject("Empty Pool");
            pool = poolGo.GetOrAddComponent<GameObjectPool>().Init(prefab);
            poolGo.transform.parent = INSTANCE.transform;
            INSTANCE.pools[prefab] = pool;
        }

        return pool.Pooled<T>();
    }
}
