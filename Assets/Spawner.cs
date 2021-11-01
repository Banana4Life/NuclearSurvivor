using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;

    private float timeUntil;
    public float delay = 0.1f;
    public Team spawnTeam;
  
    private void Update()
    {
        timeUntil -= Time.deltaTime;
        if (timeUntil < 0)
        {
            timeUntil = delay;
            var pos = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)) + transform.position;
            Game.Pooled<Unit>(prefab).Init(spawnTeam, pos);
        }
    }

}