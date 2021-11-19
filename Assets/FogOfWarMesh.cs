using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class FogOfWarMesh : MonoBehaviour
{
    private HashSet<Vector2> covered = new(); 
    private Queue<Vector2> toBeCovered = new(); 
    
    public float lightRange = 5f;
    public float maxLightRange = 60f;
    public float maxObscure = 0.9f;
    public float maxReObscure = 0.7f;
    public float reobscureRate = 0.02f;

    public Transform player;
    
    public int tessellate = 1;
    private Plane plane = new(Vector3.up, Vector3.zero);
    public float gridTileSize = 5;
    public float gridHeight;
    public GameObject template;
    public Material fogMaterial;
    public float refreshRate = 1f;

    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
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

        var tessellated = new Vector3[input.Length * 4];
        for (int i = 0; i < input.Length / 3; i++)
        {
            var a = input[i * 3 + 0];
            var b = input[i * 3 + 1];
            var c = input[i * 3 + 2];
            var d = Vector3.Lerp(a, b, 0.5f);
            var e = Vector3.Lerp(b, c, 0.5f);
            var f = Vector3.Lerp(c, a, 0.5f);

            tessellated[i * 12 + 0] = a;
            tessellated[i * 12 + 1] = d;
            tessellated[i * 12 + 2] = f;
            
            tessellated[i * 12 + 3] = b;
            tessellated[i * 12 + 4] = e;
            tessellated[i * 12 + 5] = d;
            
            tessellated[i * 12 + 6] = c;
            tessellated[i * 12 + 7] = f;
            tessellated[i * 12 + 8] = e;
            
            tessellated[i * 12 + 9] = d;
            tessellated[i * 12 + 10] = e;
            tessellated[i * 12 + 11] = f;
        }

        return Tessellate(tessellated, steps - 1);
    }

 
    public void BuildFogMesh(IList<Vector2> coords)
    {
        covered.AddRange(coords);

        foreach (var coord in coords)
        {
            Instantiate(template, transform).GetComponent<FogOfWarTile>().Init(this, coord);
        }
        
        // gameObject.GetComponent<MeshCollider>().sharedMesh = mesh; // Reapply for PhysicsRay detection to work
    }

    private void Update()
    {
        ClearFog();
        UpdateCamArea();
    }

    private void ClearFog()
    {
        if (lightRange > maxLightRange)
        {
            gameObject.SetActive(false);
        }
        var camPos = _mainCam.transform.position;
        var ray = new Ray(camPos, player.position - camPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200, 1 << gameObject.layer, QueryTriggerInteraction.Collide))
        {
            ClearFogAround(hit.point);
        }
    }

    public void ClearFogAround(Vector3 point)
    {
        foreach (var fogOfWarTiles in Physics.OverlapSphere(point, Math.Min(lightRange, maxLightRange), 1 << 13))
        {
            fogOfWarTiles.gameObject.GetComponent<FogOfWarTile>().ClearFogAround(point);
        }
    }

    public void UpdateCamArea()
    {
        var min = Vector3.zero;
        var max = Vector3.zero;
        
        // Detect min/max View
        Ray ray = _mainCam.ViewportPointToRay(new Vector3(0,0,0));
        if (plane.Raycast(ray, out var botLeftDist))
        {
            var botLeft = ray.GetPoint(botLeftDist);
            ray = _mainCam.ViewportPointToRay(new Vector3(0,1,0));
            if (plane.Raycast(ray, out var topLeftDist))
            {
                var topLeft = ray.GetPoint(topLeftDist);
                min = Vector3.Min(botLeft, topLeft);
            }
        }
        ray = _mainCam.ViewportPointToRay(new Vector3(1,1,0));
        if (plane.Raycast(ray, out var topRightDist))
        {
            max = ray.GetPoint(topRightDist);
        }

        foreach (var coord in getGrid(min, max)
            .Where(coord => !covered.Contains(coord))
            .Where(coord => !toBeCovered.Contains(coord)))
        {
            toBeCovered.Enqueue(coord);
        }


        var nextup = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            if (toBeCovered.Count == 0)
            {
                break;
            }

            nextup.Add(toBeCovered.Dequeue());
        }
        BuildFogMesh(nextup);

       
    }

    private IEnumerable<Vector2> getGrid(Vector3 min, Vector3 max)
    {
        for (int x = Mathf.FloorToInt(min.x / gridTileSize); x < Mathf.CeilToInt(max.x / gridTileSize); x++)
        {
            for (int z = Mathf.FloorToInt(min.z / gridTileSize); z < Mathf.CeilToInt(max.z / gridTileSize); z++)
            {
                yield return new Vector2(x, z);
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