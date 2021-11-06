using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class FogOfWarMesh : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] currentVertices = Array.Empty<Vector3>();
    private Color[] currentColors = Array.Empty<Color>();

    private HashSet<CubeCoord> covered = new(); 
    
    public float lightRange = 5f;
    public float maxObscure = 0.9f;
    public float maxReObscure = 0.7f;
    public float reobscureRate = 0.02f;

    private Vector3 tileSize = new(4f, 0, 3.46f);
    
    private Vector3 camMinRayHit = Vector3.zero;
    private Vector3 camMaxRayHit = Vector3.zero;
    
    private Vector3 minCamArea = Vector3.zero;
    private Vector3 maxCamArea = Vector3.zero;

    public Transform player;
    
    public int tessellate = 1;
    private Plane plane = new(Vector3.up, Vector3.zero);
    
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

    private Vector3[] corners = {
        new(1f / -2f, 0, 0),
        new(1f / -4f, 0, 1f / 2),
        new(1f / 4f, 0, 1f / 2),
        new(1f / 2f, 0, 0),
        new(1f / 4f, 0, 1f / -2),
        new(1f / -4f, 0, 1f / -2),
    };
    
    public void BuildFogMesh(IList<CubeCoord> coords, Vector3 tileSize)
    {
        this.tileSize = tileSize;
        // coords = coords.SelectMany(coord => CubeCoord.Spiral(coord, 1, 8).ToList()).Distinct().ToList();
        foreach (var coord in covered)
        {
            coords.Remove(coord);
        }
        covered.AddRange(coords);
        
        // Offset around Origin
        var offsets = corners.Select(c => new Vector3(c.x * tileSize.x, 0, c.z * tileSize.z)).ToArray();
        // Vertices in triples for triangles (clockwise order)
        offsets = Enumerable.Range(0, offsets.Length).SelectMany(i => new[] { offsets[i], offsets[(i + 1) % offsets.Length], Vector3.zero}).ToArray();
        // Tessellate offsets as needed
        var tesselated = offsets = Tessellate(offsets, tessellate);
        // Find all Hexes and apply tessellated offsets
        var newVerts = coords.Select(coord => coord.FlatTopToWorld(2, tileSize))
            .SelectMany(coord => tesselated.Select(offset => offset + coord)).ToArray();

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
        mesh.uv = deduplicatedVerts.Select(v => new Vector2(v.x, v.z) / tileSize.x).ToArray();
        mesh.triangles = newTriangles;
        mesh.normals = normals;
        mesh.name = $"Generated V{mesh.vertices.Length} T{mesh.triangles.Length}";
        mesh.colors = currentColors;
        
        // Debug.Log($"Appending Fog Mesh for {coords.ToList().Count} Tiles");
        gameObject.GetComponent<MeshCollider>().sharedMesh = mesh; // Reapply for PhysicsRay detection to work
    }

    private void Update()
    {
        for (var i = 0; i < currentColors.Length; i++)
        {
            if (currentColors[i].a < maxReObscure)
            {
                currentColors[i].a = Math.Min(maxReObscure, currentColors[i].a + Time.deltaTime * reobscureRate);
            }
        }
        mesh.colors = currentColors;

       
        ClearFog();
        UpdateCamArea();
    }

    private void ClearFog()
    {
        var camPos = Camera.main.transform.position;
        var ray = new Ray(camPos, player.position - camPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200, 1 << gameObject.layer, QueryTriggerInteraction.Collide))
        {
            ClearFogAround(hit.point);
        }
    }

    public void ClearFogAround(Vector3 point)
    {
        for (var i = 0; i < currentVertices.Length; i++)
        {
            var vertex = currentVertices[i];
            var vertexPos = transform.TransformPoint(vertex);
            var dist = (vertexPos - point).sqrMagnitude;
            if (dist < lightRange * lightRange)
            {
                // TODO probe texture instead
                currentColors[i].a = Mathf.Min(currentColors[i].a, dist / (lightRange * lightRange));
            }
        }
        mesh.colors = currentColors;
    }

    
    
    public void UpdateCamArea()
    {
        // Detect min/max View
        var cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0,0,0));
        if (plane.Raycast(ray, out var botLeftDist))
        {
            var botLeft = ray.GetPoint(botLeftDist);
            ray = cam.ViewportPointToRay(new Vector3(0,1,0));
            if (plane.Raycast(ray, out var topLeftDist))
            {
                var topLeft = ray.GetPoint(topLeftDist);
                camMinRayHit = Vector3.Min(camMinRayHit, Vector3.Min(botLeft, topLeft));
            }
        }
        ray = cam.ViewportPointToRay(new Vector3(1,1,0));
        if (plane.Raycast(ray, out var topRightDist))
        {
            camMaxRayHit = Vector3.Max(camMaxRayHit, ray.GetPoint(topRightDist));
        }
        
        // 2 Tiles Offset
        var min = camMinRayHit - tileSize * 2;
        var max = camMaxRayHit + tileSize * 2;
        
        var newMin = minCamArea;
        var newMax = maxCamArea;
        if (min.x < minCamArea.x - tileSize.x / 2) // Left rect
        {
            // min.x = min.x - tileSize.x;
            BuildFogMesh(getGrid(min, new Vector3(minCamArea.x, 0, max.z)).Distinct().ToList(), tileSize);
            newMin.x = min.x;
        }
        if (min.z < minCamArea.z - tileSize.z / 2) // Bot rect
        {
            // min.z = min.z - tileSize.z * 2;
            BuildFogMesh(getGrid(min, new Vector3(max.x, 0, minCamArea.z)).Distinct().ToList(), tileSize);
            newMin.z = min.z;
        }
        if (max.x > maxCamArea.x + tileSize.x / 2) // Right rect
        {
            // max.x = max.x + tileSize.x;
            BuildFogMesh(getGrid(new Vector3(maxCamArea.x, 0, min.z), max).Distinct().ToList(), tileSize);
            newMax.x = max.x;
        }
        if (max.z > maxCamArea.z + tileSize.z / 2) // Top rect 
        {
            // max.z = max.z + tileSize.z * 2;
            BuildFogMesh(getGrid(new Vector3(min.x, 0, maxCamArea.z), max).Distinct().ToList(), tileSize);
            newMax.z = max.z;
        }
        // Update min/max
        minCamArea = newMin;
        maxCamArea = newMax;
    }

    private IEnumerable<CubeCoord> getGrid(Vector3 min, Vector3 max)
    {
        for (float x = min.x; x < max.x; x += tileSize.x / 2)
        {
            for (float z = min.z; z < max.z; z += tileSize.z / 2)
            {
                yield return CubeCoord.FlatTopFromWorld(new Vector3(x, 0, z), tileSize);
            }
        }
    }

    
    private void OnDrawGizmos()
    {
        // // Camera to Player Line
        // Gizmos.color = Color.gray;
        // Gizmos.DrawLine(player.transform.position, Camera.main.transform.position);
        //
        // // Camera to Player FogOfWar Hit
        // var ray = new Ray(Camera.main.transform.position, player.position - Camera.main.transform.position);
        // if (Physics.Raycast(ray , out RaycastHit hit, 200, 1 << gameObject.layer, QueryTriggerInteraction.Collide))
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawWireSphere(hit.point, 0.5f);
        // }
        
        // // Camera View (for FogOfWar Mesh Generation)
        // Gizmos.color = Color.yellow;
        // Gizmos.DrawLine(min, max);
        //
        // // Camera Bounds
        // Gizmos.color = Color.green;
        // var cam = Camera.main;
        // ray = cam.ViewportPointToRay(new Vector3(0,0,0));
        // var plane = new Plane(Vector3.up, Vector3.zero);
        // if (plane.Raycast(ray, out var botLeftDist))
        // {
        //     var botLeft = ray.GetPoint(botLeftDist);
        //     ray = cam.ViewportPointToRay(new Vector3(0,1,0));
        //     if (plane.Raycast(ray, out var topLeftDist))
        //     {
        //         var topLeft = ray.GetPoint(topLeftDist);
        //         min = Vector3.Min(min, Vector3.Min(botLeft, topLeft));
        //         Gizmos.DrawLine(cam.transform.position, botLeft);
        //         Gizmos.DrawLine(cam.transform.position, topLeft);
        //     }
        // }
        // ray = cam.ViewportPointToRay(new Vector3(1,1,0));
        // if (plane.Raycast(ray, out var topRightDist))
        // {
        //     var topRight = ray.GetPoint(topRightDist);
        //     max = Vector3.Max(max, topRight);
        //     Gizmos.DrawLine(cam.transform.position, topRight);
        // }
    }
 
}
