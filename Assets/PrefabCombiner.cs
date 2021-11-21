using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrefabCombiner<T>
{
    private Dictionary<GameObject, MeshFilter> prefabMeshes = new();
    private Dictionary<GameObject, Material[]> prefabMaterials = new();
    private Dictionary<GameObject, List<GameObject>> prefabTaggedGameobjects = new();
    private List<Material> allMaterials = new();
    
    private Vector3 baseOffset;
    private Dictionary<T, List<GameObjectCombineInstance>> toCombine = new();

    private Mesh combinedMesh = new();
    private bool tiledUv;
    private float uvFactor;

    public PrefabCombiner(Vector3 baseOffset, bool tiledUv = false, float uvFactor = 0)
    {
        this.baseOffset = baseOffset;
        this.tiledUv = tiledUv;
        this.uvFactor = uvFactor;
    }

    public Mesh CombineMeshes()
    {
        Dictionary<int, Dictionary<T, List<CombineInstance>>> meshCombineInstances = new();
        foreach (var (k, l) in toCombine)
        {
            foreach (var c in l)
            {
                if (c.mesh == null) // Has no Mesh to combine
                {
                    continue;
                }

                for (int subMeshIdx = 0; subMeshIdx < c.mesh.subMeshCount; subMeshIdx++)
                {
                    var remappedIdx = c.remappedMaterials[subMeshIdx];
                    if (!meshCombineInstances.TryGetValue(remappedIdx, out var mc))
                    {
                        mc = new Dictionary<T, List<CombineInstance>>();
                        meshCombineInstances[remappedIdx] = mc;
                    }
                    if (!mc.TryGetValue(k, out var list))
                    {
                        list = new List<CombineInstance>();
                        mc[k] = list;
                    }
                    list.Add(new CombineInstance
                    {
                        mesh = c.mesh,
                        transform = Matrix4x4.TRS(c.offset, c.rotation, Vector3.one),
                        subMeshIndex = subMeshIdx
                    });
                }
            }
        }
        
        List<CombineInstance> subMeshes = new();
        foreach (var (_, combineInstances) in meshCombineInstances.OrderBy(key => key.Key))
        {
            var subMesh = new Mesh();
            subMesh.CombineMeshes(combineInstances.Values.SelectMany(l => l).ToArray());
            subMeshes.Add(new CombineInstance() { mesh = subMesh});
        }
        combinedMesh.Clear();
        combinedMesh.CombineMeshes(subMeshes.ToArray(), false, false);
        
        if (tiledUv)
        {
            var vertices = combinedMesh.vertices;
            var uvs = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i] + baseOffset;
                uvs[i] = new Vector2(vertex.x, vertex.z) / uvFactor;
            }
            combinedMesh.uv = uvs;
        }
        
        return combinedMesh;
    }
    
    public void AddInstance(T key, GameObjectCombineInstance instance)
    {
        if (!toCombine.TryGetValue(key, out var list))
        {
            list = new List<GameObjectCombineInstance>();
            toCombine[key] = list;
        }
        list.Add(instance);
    }
    
    public void AddPrefab(T key, Vector3 offset, Quaternion rotation, GameObject prefab)
    {
        if (prefab.CompareTag("SpawnAsGo"))
        {
            AddInstance(key, new GameObjectCombineInstance()
            {
                offset = offset,
                rotation = rotation,
                prefab = prefab,
                taggedGos = new List<GameObject>{prefab}
            });        
        }
        else
        {
            var meshFilter = PrefabMesh(prefab);
            var materials = PrefabMaterials(prefab, meshFilter);
            var remappedMaterials = RemapMaterials(materials);
            var taggedGos = TaggedGameObjects(prefab);
        
            AddInstance(key, new GameObjectCombineInstance()
            {
                offset = offset,
                rotation = rotation,
                prefab = prefab,
                mesh = meshFilter.sharedMesh,
                materials = materials,
                remappedMaterials = remappedMaterials,
                taggedGos = taggedGos
            });
        }
    }

    public List<GameObject> TaggedGameObjects(GameObject prefab)
    {
        if (!prefabTaggedGameobjects.TryGetValue(prefab, out var list))
        {
            list = new();
            prefabTaggedGameobjects[prefab] = list;

            foreach (Transform go in prefab.transform)
            {
                if (go.CompareTag("SpawnAsGo"))
                {
                    list.Add(go.gameObject);
                }
            }
        }
        return list;
    }

    public void SetPrefab(T key, Vector3 offset, Quaternion rotation, GameObject prefab)
    {
        toCombine.Remove(key);
        AddPrefab(key, offset, rotation, prefab);
    }

    private int[] RemapMaterials(Material[] materials)
    {
        var materialRemapping = new int[materials.Length];
        for (int i = 0; i < materialRemapping.Length; i++)
        {
            var idx = allMaterials.IndexOf(materials[i]);
            if (idx == -1)
            {
                allMaterials.Add(materials[i]);
                idx = allMaterials.Count - 1;
            }
            materialRemapping[i] = idx;
        }
        return materialRemapping;
    }

    private MeshFilter PrefabMesh(GameObject prefab)
    {
        if (!prefabMeshes.TryGetValue(prefab, out var meshFilter))
        {
            meshFilter = prefab.GetComponentInChildren<MeshFilter>();
            prefabMeshes[prefab] = meshFilter;
        }
        return meshFilter;
    }
    
    private Material[] PrefabMaterials(GameObject prefab, MeshFilter meshFilter)
    {
        if (!prefabMaterials.TryGetValue(prefab, out var materials))
        {
            var renderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
            materials = renderer.sharedMaterials;
            prefabMaterials[prefab] = materials;
        }
        return materials;
    }

    public Material[] Materials()
    {
        return allMaterials.ToArray();
    }

    public void Remove(T key)
    {
        toCombine.Remove(key);
    }

    public void SpawnAdditionalGo(Transform parent)
    {
        foreach (var c in toCombine.Values.SelectMany(v => v))
        {
            foreach (var gameObject in c.taggedGos)
            {
                Object.Instantiate(gameObject, c.offset + baseOffset, c.rotation, parent);
            }
        }
    }
}

public record GameObjectCombineInstance
{
    public Vector3 offset;
    public Quaternion rotation;
    public GameObject prefab;
    public Mesh mesh;
    public Material[] materials;
    public int[] remappedMaterials;
    public List<GameObject> taggedGos;
}