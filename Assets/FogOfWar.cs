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
        Gizmos.DrawLine(player.transform.position, transform.position);
        var ray = new Ray(transform.position, player.position - transform.position);
        if (Physics.Raycast(ray , out RaycastHit hit, 1000, layer, QueryTriggerInteraction.Collide))
        {
            var vertices = hit.transform.GetComponent<MeshFilter>().sharedMesh.vertices;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hit.point, 0.5f);
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var vertexPos = hit.transform.TransformPoint(vertex);
                var dist = (vertexPos - hit.point).sqrMagnitude;
                if (dist < 5 * 5)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(vertexPos, 0.2f);
                }
            }
        }
        
    }
}
