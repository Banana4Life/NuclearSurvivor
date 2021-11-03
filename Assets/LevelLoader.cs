using UnityEngine;
using UnityEngine.AI;

public class LevelLoader : MonoBehaviour
{
    public GameObject thisTile;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<NavMeshAgent>())
        {
            Game.LoadNextLevel(thisTile.transform);
            Destroy(gameObject);
        }
    }
}
