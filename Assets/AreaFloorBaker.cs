using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NavMeshBuilder = UnityEngine.AI.NavMeshBuilder;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class AreaFloorBaker : MonoBehaviour
{
    private NavMeshSurface navSurface;
    public float movementThreshold = 3;
    public float updateRate = 0.1f;
    public Vector3 navMeshSize = new(20, 1, 20);
    
    private Vector3 worldAnchor;
    private NavMeshData data;
    private Dictionary<TileArea, List<NavMeshBuildMarkup>> markup = new();
    private List<NavMeshBuildMarkup> markups = new();
    private List<NavMeshBuildSource> navSources = new();
    public bool active;
    public bool built;
    private LeaderAgent agent;
    
    private void Start()
    {
        agent = GetComponent<LeaderAgent>();
        navSurface = GetComponent<NavMeshSurface>();
        data = new NavMeshData();
        NavMesh.AddNavMeshData(data);
        StartCoroutine(CheckPlayerMovement());
    }

    private IEnumerator CheckPlayerMovement()
    {
        var waitForSeconds = new WaitForSeconds(updateRate);
        while (true)
        {
            if (active && (!built || Vector3.Distance(worldAnchor, transform.position) > movementThreshold))
            {
                BuildNavMesh(true);
                worldAnchor = transform.position;
                built = true;
            }

            yield return waitForSeconds;
        }
    }

    public void BuildNavMesh(bool async)
    {
        var bounds = new Bounds(transform.position, navMeshSize);
        markups.RemoveAll(m => m.root == null); // TODO this is not efficient
        NavMeshBuilder.CollectSources(agent.generator.transform, navSurface.layerMask, navSurface.useGeometry, navSurface.defaultArea, markups, navSources);
        if (async)
        {
            NavMeshBuilder.UpdateNavMeshDataAsync(data, navSurface.GetBuildSettings(), navSources, bounds);
        }
        else
        {
            NavMeshBuilder.UpdateNavMeshData(data, navSurface.GetBuildSettings(), navSources, bounds);
        }
    }

    public void UpdateNavMesh(TileArea navTiles)
    {
        // Collect all modifiers 
        var newModifiers = navTiles.GetComponentsInChildren<NavMeshModifier>();
        if (markup.ContainsKey(navTiles)) // If markup data was collected previously
        {
            var oldMarkups = markup[navTiles];
            // remove all previous markup data from global list
            markups.RemoveAll(e => oldMarkups.Contains(e));
        }
        // Collect markup data
        var newMarkups = new List<NavMeshBuildMarkup>();
        foreach (var modifier in newModifiers)
        {
            if ((navSurface.layerMask & (1 << modifier.gameObject.layer)) != 0 && modifier.AffectsAgentType(navSurface.agentTypeID))
            {
                newMarkups.Add(new NavMeshBuildMarkup()
                {
                    root = modifier.transform,
                    overrideArea = modifier.overrideArea,
                    area = modifier.area,
                    ignoreFromBuild = modifier.ignoreFromBuild,
                });
            }
        }

        markup[navTiles] = newMarkups;
        markups.AddRange(newMarkups); // Finally add markup data to global list
    }

}
