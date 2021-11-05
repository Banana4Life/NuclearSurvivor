using UnityEngine;
using UnityEngine.AI;

public class LevelLoader : MonoBehaviour
{
    public AutoTile thisTile;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<NavMeshAgent>())
        {
            Game.LoadNextLevel(thisTile);
            Destroy(gameObject);
        }
    }
}
