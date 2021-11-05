using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class NavigatableTiles : MonoBehaviour
{
    public bool navMeshCalculated;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (navMeshCalculated)
        {
            navMeshCalculated = false;
            GetComponent<NavMeshSurface>().BuildNavMesh();
        }
    }
}
