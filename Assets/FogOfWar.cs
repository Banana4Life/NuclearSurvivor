using System;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public Transform player;
    public GameObject plane;

    public LayerMask layer;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    void Start()
    {
        mesh = plane.GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        colors = new Color[vertices.Length];
        for (var i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.black;
        }

        mesh.colors = colors;
    }

    // Update is called once per frame
    void Update()
    {
        for (var i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.yellow;
        }

        mesh.colors = colors;
        
        var ray = new Ray(transform.position, player.position - transform.position);
        if (Physics.Raycast(ray , out RaycastHit hit, 1000, layer, QueryTriggerInteraction.Collide))
        {
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var vertexPos = plane.transform.TransformPoint(vertex);
                var dist = (vertexPos - hit.point).sqrMagnitude;
                if (dist < 5 * 5)
                {
                    var alpha = Mathf.Min(colors[i].a, dist / (5f * 5f)); // TODO probe texture instead
                    colors[i].a = alpha;
                }
            }
            mesh.colors = colors;
        }
    }

    private void OnDrawGizmos()
    {
        if (vertices != null)
        {
            Gizmos.DrawLine(player.transform.position, transform.position);
            var ray = new Ray(transform.position, player.position - transform.position);
            if (Physics.Raycast(ray , out RaycastHit hit, 1000, layer, QueryTriggerInteraction.Collide))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hit.point, 0.5f);
                for (var i = 0; i < vertices.Length; i++)
                {
                    var vertex = vertices[i];
                    var vertexPos = plane.transform.TransformPoint(vertex);
                    var dist = (vertexPos - hit.point).sqrMagnitude;
                    if (dist < 5 * 5)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(vertexPos, 0.2f);
                    }
                }
            }
        }
        
    }
}
