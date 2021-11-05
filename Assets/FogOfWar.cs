using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public Transform player;
    public LayerMask layer;
    public FogOfWarMesh fow;

    public Vector3 min = Vector3.zero;
    public Vector3 max = Vector3.zero;
    
    // Update is called once per frame
    void Update()
    {
        var ray = new Ray(transform.position, player.position - transform.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 200, layer, QueryTriggerInteraction.Collide))
        {
            fow.ClearFogAround(hit.point);
        }
        
        // Detect min/max View
        var cam = Camera.main;
        var plane = new Plane(Vector3.up, Vector3.zero);
        ray = cam.ViewportPointToRay(new Vector3(0,0,0));
        if (plane.Raycast(ray, out var botLeftDist))
        {
            var botLeft = ray.GetPoint(botLeftDist);
            ray = cam.ViewportPointToRay(new Vector3(0,1,0));
            if (plane.Raycast(ray, out var topLeftDist))
            {
                var topLeft = ray.GetPoint(topLeftDist);
                min = Vector3.Min(min, Vector3.Min(botLeft, topLeft));
            }
        }
        ray = cam.ViewportPointToRay(new Vector3(1,1,0));
        if (plane.Raycast(ray, out var topRightDist))
        {
            max = Vector3.Max(max, ray.GetPoint(topRightDist));
        }
        fow.UpdateCamArea(min, max);
        
    }

    private void OnDrawGizmos()
    {
        // Camera to Player Line
        // Gizmos.color = Color.gray;
        // Gizmos.DrawLine(player.transform.position, transform.position);
        
        // Camera to Player FogOfWar Hit
        // var ray = new Ray(transform.position, player.position - transform.position);
        // if (Physics.Raycast(ray , out RaycastHit hit, 200, layer, QueryTriggerInteraction.Collide))
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawWireSphere(hit.point, 0.5f);
        // }
        
        // Camera View (for FogOfWar Mesh Generation)
        // Gizmos.color = Color.yellow;
        // Gizmos.DrawLine(min, max);

        // Camera Bounds
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
