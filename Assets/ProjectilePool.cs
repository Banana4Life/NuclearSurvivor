using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public GameObject prefab;
    
    private GameObject pool;
    
    private static Queue<GameObject> free = new();
    public static List<GameObject> live = new();
    
    private void Start()
    {
        pool = new GameObject("ProjectilePool");
        pool.transform.parent = transform;
    }

    void Update()
    {
        var freed = live.Where(a => !a.activeSelf).ToList();
        foreach (var audioSource in freed)
        {
            live.Remove(audioSource);
            free.Enqueue(audioSource);
            audioSource.gameObject.name = "(Free)";
        }
    }

    private GameObject PooledProjectile(GameObject source, Vector3 target)
    {
        if (!free.TryDequeue(out GameObject projectile))
        {
            projectile = Instantiate(prefab, pool.transform);
        }

        projectile.transform.position = source.transform.position;
        projectile.name = "Projectile (_)";
        projectile.GetComponent<Projectile>().Init(target, source);
        live.Add(projectile);
        return projectile;
    }

    public void LaunchProjectile(GameObject source, Vector3 target)
    {
        PooledProjectile(source, target);
    }
}
