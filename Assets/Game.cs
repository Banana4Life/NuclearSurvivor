using UnityEngine;

public class Game : MonoBehaviour
{
    public Floaty floatyPrefab;
    public GameObject canvas;

    public AudioSourcePool audioSourcePool;
    public ProjectilePool projectilePool;

    private static Game INSTANCE;
    private void Awake()
    {
        INSTANCE = this;
    }

    public static AudioSourcePool audioPool()
    {
        return INSTANCE.audioSourcePool;
    }
    
    public static ProjectilePool projPool()
    {
        return INSTANCE.projectilePool;
    }
    
    public static void SpawnFloaty(string text, Vector3 pos)
    {
        var floaty = Instantiate(INSTANCE.floatyPrefab, INSTANCE.canvas.transform, false);
        floaty.Init(text, pos);
    }
}
