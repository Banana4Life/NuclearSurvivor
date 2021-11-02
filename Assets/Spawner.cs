using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;

    private float timeUntil;
    public float delay = 0.1f;
    public Team spawnTeam;
    public bool spawningActive;

    private SpawnPoint[] spawnPoints;

    public GameObject mainTarget;
    
    private void Start()
    {
        spawnPoints = gameObject.GetComponentsInChildren<SpawnPoint>();
        foreach (var projTarget in GetComponentsInChildren<ProjectileTarget>())
        {
            projTarget.Init(spawnTeam, gameObject, true);
        }
    }

    private void Update()
    {
        timeUntil -= Time.deltaTime;
        if (timeUntil < 0 && spawningActive)
        {
            timeUntil = delay;
            Vector3 spawnPos;
            if (spawnPoints.Length == 0)
            {
                 spawnPos = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)) + transform.position;    
            }
            else
            {
                spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position + new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
            }
            Game.Pooled<Unit>(prefab).Init(spawnTeam, spawnPos, mainTarget);
        }
    }


}