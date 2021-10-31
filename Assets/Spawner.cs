using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;

    private float timeUntil;
    public float delay = 0.1f;
    public Team spawnTeam;
    
    private static Queue<GameObject> free = new();
    private static List<GameObject> live = new();

    private GameObject pool;
    
    private void Start()
    {
        pool = new GameObject("UnitPool");
        pool.transform.parent = transform;
    }

    private GameObject PooledUnit(Vector3 pos)
    {
        if (!free.TryDequeue(out GameObject unit))
        {
            unit = Instantiate(prefab, pool.transform);
        }

        unit.transform.position = pos;
        live.Add(unit);
        return unit;
    }
    
    private void Update()
    {
        // Refill Pool with freed GameObjects
        var freed = live.Where(go => !go.activeSelf).ToList();
        foreach (var go in freed)
        {
            live.Remove(go);
            free.Enqueue(go);
        }

        timeUntil -= Time.deltaTime;
        if (timeUntil < 0)
        {
            timeUntil = delay;
            var pos = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)) + transform.position;
            var unit = PooledUnit(pos);
            var unitScript = unit.GetComponent<Unit>();
            unitScript.Init("Unit (" + spawnTeam + ")", spawnTeam, live);
        }
    }

}