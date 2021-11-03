using System;
using UnityEngine;

public class FogOfWarMesh : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    public float lightRange = 5f;

    private void Start()
    {
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;
        vertices = mesh.vertices;
        colors = new Color[vertices.Length];
        for (var i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.black;
        }

        mesh.colors = colors;
    }

    public void Init(Mesh mesh)
    {
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void UpdateFog(RaycastHit hitInfo)
    {
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            var vertexPos = hitInfo.transform.TransformPoint(vertex);
            var dist = (vertexPos - hitInfo.point).sqrMagnitude;
            colors[i] = Color.yellow;
            colors[i].a = 0.6f;
            if (dist < lightRange * lightRange)
            {
                var alpha = Mathf.Min(colors[i].a, dist / (lightRange * lightRange)); // TODO probe texture instead
                colors[i].a = alpha;
            }
        }
        mesh.colors = colors;
    }

    // private void OnDrawGizmos()
    // {
    //     var meshFilter = gameObject.GetComponent<MeshFilter>();
    //     if (meshFilter)
    //     {
    //         if (this.mesh != meshFilter.sharedMesh)
    //         {
    //             Init(meshFilter.sharedMesh);
    //         }
    //         var mesh = meshFilter.sharedMesh;
    //         for (var i = 0; i < mesh.vertices.Length; i++)
    //         {
    //             var vector3 = mesh.vertices[i];
    //             var color = mesh.colors[i];
    //             Gizmos.color = color;
    //             Gizmos.DrawWireCube(vector3, Vector3.one * 0.5f);
    //         }
    //     }
    // }
}
