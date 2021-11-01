using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameObjectPool : MonoBehaviour
{
    private List<GameObject> live = new();
    private Queue<GameObject> free = new();

    private GameObject prefab;
    private string type;

    public T Pooled<T>()
    {
        if (!free.TryDequeue(out GameObject go))
        {
            go = Instantiate(prefab, gameObject.transform);
            gameObject.name = type + "Pool (" + (live.Count + free.Count) + ")";
        }
        live.Add(go);
        return go.GetComponent<T>();
    }

    public void Reclaim()
    {
        var freed = live.Where(a => !a.activeSelf).ToList();
        foreach (var audioSource in freed)
        {
            live.Remove(audioSource);
            free.Enqueue(audioSource);
            audioSource.gameObject.name = "(Free)";
        }
    }

    public GameObjectPool Init(GameObject prefab)
    {
        this.prefab = prefab;
        type = prefab.name;
        name = type + "Pool";
        return this;
    }

}