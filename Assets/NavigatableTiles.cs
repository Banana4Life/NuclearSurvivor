using Unity.AI.Navigation;
using UnityEngine;

public class NavigatableTiles : MonoBehaviour
{
    public bool needsNewNavMesh;

    void Update()
    {
        if (needsNewNavMesh)
        {
            needsNewNavMesh = false;
            GetComponent<NavMeshSurface>().BuildNavMesh();
        }
    }
}
