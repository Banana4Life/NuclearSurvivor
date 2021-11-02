using UnityEngine;
using UnityEngine.AI;

public class Interactable : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<NavMeshAgent>())
        {
            Destroy(gameObject);
        }
    }
}
