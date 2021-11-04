using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FogOfWarMesh : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] currentVertices = Array.Empty<Vector3>();
    private Color[] currentColors = Array.Empty<Color>();
    
    public float lightRange = 5f;
    public float maxObscure = 0.9f;
    public float maxReObscure = 0.7f;
    public float reobscureRate = 0.02f;
    
    private void Awake()
    {
        mesh = new Mesh();
        // mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        // currentVertices = mesh.vertices;
        // currentColors = mesh.colors;
        gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        gameObject.GetComponent<MeshRenderer>().enabled = true;
    }
    
    public static Vector3[] Tessellate(Vector3[] input, int steps = 1)
    {
        if (steps == 0)
        {
            return input;
        }
        if (input.Length % 3 != 0)
        {
            throw new ArgumentException("Input Vertices must be a multiple of 3");
        }

        var tesselated = new Vector3[input.Length * 4];
        for (int i = 0; i < input.Length / 3; i++)
        {
            var a = input[i * 3 + 0];
            var b = input[i * 3 + 1];
            var c = input[i * 3 + 2];
            var d = Vector3.Lerp(a, b, 0.5f);
            var e = Vector3.Lerp(b, c, 0.5f);
            var f = Vector3.Lerp(c, a, 0.5f);

            tesselated[i * 12 + 0] = a;
            tesselated[i * 12 + 1] = d;
            tesselated[i * 12 + 2] = f;
            
            tesselated[i * 12 + 3] = b;
            tesselated[i * 12 + 4] = e;
            tesselated[i * 12 + 5] = d;
            
            tesselated[i * 12 + 6] = c;
            tesselated[i * 12 + 7] = f;
            tesselated[i * 12 + 8] = e;
            
            tesselated[i * 12 + 9] = d;
            tesselated[i * 12 + 10] = e;
            tesselated[i * 12 + 11] = f;
        }

        return Tessellate(tesselated, steps - 1);
    }

    public void BuildFogMesh(IList<CubeCoord> coords, Vector3 tileSize)
    {
        // Offset Ring around Origin
        var offsets = CubeCoord.Neighbors.Select(coord => coord.FlatTopToWorld(0, tileSize)).ToArray();
        // Vertices in triples for triangles (clockwise order)
        offsets = Enumerable.Range(0, offsets.Length).SelectMany(i => new[] { offsets[i], offsets[(i + 1) % offsets.Length], Vector3.zero}).ToArray();
        // Tessellate offsets as needed
        offsets = Tessellate(offsets, 2);
        // Find all Hexes and apply tessellated offsets
        var newVerts = coords.Select(coord => coord.FlatTopToWorld(2, tileSize))
            .SelectMany(coord => offsets.Select(offset => offset + coord)).ToArray();

        // Collect Mesh Data
        Dictionary<Vector3, int> dictionary = new();
        foreach (var prevVertex in mesh.vertices)
        {
            dictionary.Add(prevVertex, dictionary.Count); // Starting with old mesh vertices
        }

        var oldTriangleCount = mesh.triangles.Length;
        var newTriangles = new int[oldTriangleCount + newVerts.Length]; 
        // Copy Triangles from old mesh
        Array.Copy(mesh.triangles, newTriangles, oldTriangleCount);
        // Expand Vertex Mapping and add new triangles
        for (var i = 0; i < newVerts.Length; i++)
        {
            if (dictionary.TryAdd(newVerts[i], dictionary.Count))
            {
                newTriangles[oldTriangleCount + i] = dictionary.Count - 1; // New Vertex
            }
            else
            {
                newTriangles[oldTriangleCount + i] = dictionary[newVerts[i]]; // Existing Vertex - reuse index for triangle
            }
        }
        // Recreate new deduplicated verts array
        var deduplicatedVerts = new Vector3[dictionary.Count];
        foreach (var (v, i) in dictionary)
        {
            deduplicatedVerts[i] = v;
        }
        // All Normals are Up
        var normals = Enumerable.Range(0, deduplicatedVerts.Length).Select(_ => Vector3.up).ToArray();
        // Expand Colors and initialize new colors
        var newColors = new Color[dictionary.Count];
        Array.Copy(currentColors, newColors, currentColors.Length);
        for (int i = currentColors.Length; i < newColors.Length; i++)
        {
            newColors[i] = Color.black;
            newColors[i].a = maxObscure;
        }
        currentColors = newColors;
        currentVertices = deduplicatedVerts;

        // Finally Build the Mesh
        mesh.vertices = deduplicatedVerts;
        // UVs?
        mesh.triangles = newTriangles;
        mesh.normals = normals;
        mesh.name = $"Generated V{mesh.vertices.Length} T{mesh.triangles.Length}";
        mesh.colors = currentColors;
        
        Debug.Log($"Appending Fog Mesh for {coords.ToList().Count} Tiles");
        gameObject.GetComponent<MeshCollider>().sharedMesh = mesh; // Reapply for PhysicsRay detection to work
    }

    private void Update()
    {
        for (var i = 0; i < currentColors.Length; i++)
        {
            currentColors[i].a = Math.Min(maxReObscure, currentColors[i].a + Time.deltaTime * reobscureRate);
        }
        mesh.colors = currentColors;
    }

    public void ClearFogAround(RaycastHit hitInfo)
    {
        for (var i = 0; i < currentVertices.Length; i++)
        {
            var vertex = currentVertices[i];
            var vertexPos = hitInfo.transform.TransformPoint(vertex);
            var dist = (vertexPos - hitInfo.point).sqrMagnitude;
            if (dist < lightRange * lightRange)
            {
                // TODO probe texture instead
                currentColors[i].a = Mathf.Min(currentColors[i].a, dist / (lightRange * lightRange));
            }
        }
        mesh.colors = currentColors;
    }

}
