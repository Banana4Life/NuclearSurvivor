using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public Transform player;
    public LayerMask layer;

    // Update is called once per frame
    void Update()
    {
        var ray = new Ray(transform.position, player.position - transform.position);
        if (Physics.Raycast(ray , out RaycastHit hit, 200, layer, QueryTriggerInteraction.Collide))
        {
            var fow = hit.transform.gameObject.GetComponent<FogOfWarMesh>();
            if (fow)
            {
                fow.ClearFogAround(hit);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(player.transform.position, transform.position);
        var ray = new Ray(transform.position, player.position - transform.position);
        if (Physics.Raycast(ray , out RaycastHit hit, 200, layer, QueryTriggerInteraction.Collide))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hit.point, 0.5f);
        }
    }
}
