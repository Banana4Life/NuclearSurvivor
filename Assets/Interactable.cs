using UnityEngine;
using UnityEngine.AI;

public class Interactable : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<Player>();
        if (player)
        {
            player.SetIrradiated();
            Destroy(gameObject);
        }
    }
}
