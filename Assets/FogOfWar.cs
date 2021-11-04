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
                fow.UpdateFog(hit);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(player.transform.position, transform.position);
    }
}
