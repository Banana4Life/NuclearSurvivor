using System;
using UnityEngine;

public class FogOfWarTile : MonoBehaviour
{
    private Vector3[] vertices;
    private Color[] colors;
    private FogOfWarMesh meshGenerator;
    private Mesh mesh;


    public void Init(FogOfWarMesh meshGenerator, Vector2 coord)
    {
        transform.position = new Vector3(coord.x * meshGenerator.gridTileSize, meshGenerator.gridHeight, coord.y * meshGenerator.gridTileSize);
        gameObject.name = $"Tile {coord.x}-{coord.y}";
        this.meshGenerator = meshGenerator;
    }
    private void Start()
    {
        var templateMesh = GetComponent<MeshFilter>().sharedMesh;
        mesh = new Mesh();
        mesh.name = "Mesh" + gameObject.name;
        vertices = templateMesh.vertices;
        mesh.vertices = vertices;
        mesh.triangles = templateMesh.triangles;
        colors = new Color[vertices.Length];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.black;
            colors[i].a = meshGenerator.maxObscure;
        }
        mesh.colors = colors;
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private float updateTime;
    private bool needUpdate;
    private void Update()
    {
        for (var i = 0; i < colors.Length; i++)
        {
            if (colors[i].a < meshGenerator.maxReObscure)
            {
                colors[i].a = Math.Min(meshGenerator.maxReObscure, colors[i].a + Time.deltaTime * meshGenerator.reobscureRate);
                needUpdate = true;
            }
            if (colors[i].a > meshGenerator.maxObscure)
            {
                colors[i].a = meshGenerator.maxObscure;
                needUpdate = true;
            }
        }

        updateTime -= Time.deltaTime;
        if (needUpdate && updateTime < 0)
        {
            updateTime = 1 / meshGenerator.refreshRate;
            mesh.colors = colors;
        }
    }

    public void ClearFogAround(Vector3 point)
    {
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            var vertexPos = transform.TransformPoint(vertex);
            var dist = (vertexPos - point).sqrMagnitude;
            if (dist < meshGenerator.lightRange * meshGenerator.lightRange)
            {
                // TODO probe texture instead
                colors[i].a = Mathf.Min(colors[i].a, dist / (meshGenerator.lightRange * meshGenerator.lightRange));
            }
        }
        needUpdate = true;
    }
}