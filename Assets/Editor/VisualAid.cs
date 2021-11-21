using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof(Game))]
public class GameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
    
}

[CustomEditor(typeof(TileGenerator))]
public class TileGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Recolor Vertices"))
        {
            ((TileGenerator)target).ApplyVertexColorsCoroutined();
        }
        base.OnInspectorGUI();

    }
}

[CustomEditor(typeof(FogOfWarMesh))]
public class FogOfWarMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Generate Mesh Template"))
        {
            GenerateMeshTemplate((FogOfWarMesh)target);
        }
    }

    public void GenerateMeshTemplate(FogOfWarMesh fogOfWarMesh)
    {
        var templateMesh = new Mesh();
        fogOfWarMesh.template.GetComponent<MeshFilter>().sharedMesh = templateMesh;
        fogOfWarMesh.template.GetComponent<MeshCollider>().sharedMesh = templateMesh;
        fogOfWarMesh.template.layer = 13;
      
        Vector2[] quadTriangles =
        {
            new(0,0), // ct
            new(-1f / 2f, -1f / 2f), // bl
            new(-1f / 2f, 1f / 2f), // tl
            
            new(0,0), // ct
            new(-1f / 2f, 1f / 2f), // tl
            new(1f / 2f, 1f / 2f), // tr
            
            new(0,0), // ct
            new(1f / 2f, 1f / 2f), // tr
            new(1f / 2f, -1f / 2f), // br
            
            new(0,0), // ct
            new(1f / 2f, -1f / 2f), // br
            new(-1f / 2f, -1f / 2f), // bl
        };
        
        // Vertices in triples for triangles (clockwise order)
        var allVerts = quadTriangles.Select(c => new Vector3(c.x * fogOfWarMesh.gridTileSize, fogOfWarMesh.gridHeight, c.y * fogOfWarMesh.gridTileSize)).ToArray();
        // Tessellate offsets as needed
        allVerts = FogOfWarMesh.Tessellate(allVerts, fogOfWarMesh.tessellate);
        
        // Collect Mesh Data
        Dictionary<Vector3, int> dictionary = new();
        var triangles = new int[ allVerts.Length]; 
        // Build Vertex Mapping and add triangles
        for (var i = 0; i < allVerts.Length; i++)
        {
            if (dictionary.TryAdd(allVerts[i], dictionary.Count))
            {
                triangles[i] = dictionary.Count - 1; // New Vertex
            }
            else
            {
                triangles[i] = dictionary[allVerts[i]]; // Existing Vertex - reuse index for triangle
            }
        }
        // Deduplicated verts array
        var deduplicatedVerts = new Vector3[dictionary.Count];
        foreach (var (v, i) in dictionary)
        {
            deduplicatedVerts[i] = v;
        }
        templateMesh.vertices = deduplicatedVerts;
        templateMesh.triangles = triangles;
        templateMesh.uv = deduplicatedVerts.Select(v => new Vector2(v.x, v.z) * fogOfWarMesh.gridTileSize).ToArray();
        // All Normals are Up
        templateMesh.normals = Enumerable.Range(0, deduplicatedVerts.Length).Select(_ => Vector3.up).ToArray();

        Debug.Log($"Generated Fog Mesh with {templateMesh.vertices.Length} Verticies");
        
        AssetDatabase.DeleteAsset("Assets/fog.asset");
        AssetDatabase.CreateAsset(templateMesh, "Assets/fog.asset");
        AssetDatabase.SaveAssets();
    }
}


[CustomEditor(typeof(TileArea))]
public class TileAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Save As EditorRoom"))
        {
            AssetDatabase.DeleteAsset("Assets/Editor/EditorRoom.prefab");
            AssetDatabase.DeleteAsset("Assets/Editor/editorFloor.asset");
            AssetDatabase.DeleteAsset("Assets/Editor/editorWalls.asset");
            AssetDatabase.CreateAsset(((TileArea)target).floorMesh, "Assets/Editor/editorFloor.asset");
            AssetDatabase.CreateAsset(((TileArea)target).wallMesh, "Assets/Editor/editorWalls.asset");
            PrefabUtility.SaveAsPrefabAsset(((TileArea)target).gameObject, "Assets/Editor/EditorRoom.prefab");
            AssetDatabase.SaveAssets();
        }
    }

}

[CustomEditor(typeof(TileDictionary))]
public class TileDictionaryEditor : Editor
{
    Vector2 scrollPosition;
    private Vector2 mouse;
    private int[] sliders;
    private bool unfoldedPrefabs = true;
    public override void OnInspectorGUI()
    {
        bool modified = false;
        var tileDict = ((TileDictionary)target);
        base.OnInspectorGUI();

        if (sliders == null || sliders.Length != tileDict.tilePrefabs.Length)
        {
            sliders = new int[tileDict.tilePrefabs.Length];
        }
        
        GUILayout.Space(10);
        
        if (Application.isEditor)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Place Tiles"))
            {
                ((TileDictionary)target).PlaceTiles();
            }    
            if (GUILayout.Button("Clear Tiles"))
            {
                ((TileDictionary)target).ClearTiles();
            } 
            EditorGUILayout.EndHorizontal();
        }

        if (tileDict.tilePrefabs.Length == 0)
        {
            tileDict.tilePrefabs = new TileVariants[Enum.GetNames(typeof(TileDictionary.TileType)).Length];
            modified = true;
        }

        unfoldedPrefabs = EditorGUILayout.BeginFoldoutHeaderGroup(unfoldedPrefabs, "Prefabs");
        if (unfoldedPrefabs)
        {
            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < tileDict.tilePrefabs.Length; i++)
            {
                if (i % 3 == 0 && i != 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(10);
                    EditorGUILayout.BeginHorizontal();
                }
                EditorGUILayout.BeginVertical();
                var prefabList = tileDict.tilePrefabs[i].prefabs;
                if (prefabList.Length == 0)
                {
                    prefabList = new []{ new TileVariant() };
                    modified = true;
                }
                
                EditorGUILayout.BeginHorizontal();
                sliders[i] = EditorGUILayout.IntSlider(sliders[i], 0, prefabList.Length - 1);
                var sliderPos = sliders[i];
                GUILayout.Label("(" + prefabList.Length + ")");
                EditorGUILayout.EndHorizontal();
           
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(((TileDictionary.TileType)i).ToString());
                int deleteAt = -1;
                var meshPrefab = prefabList[sliderPos];
                var prefab = meshPrefab.prefab;
                if (GUILayout.Button("+"))
                {
                    Array.Resize(ref prefabList, prefabList.Length + 1);
                    prefabList[prefabList.Length - 1] = new TileVariant();
                    sliders[i] = prefabList.Length - 1;
                    modified = true;
                }
                if (GUILayout.Button("X"))
                {
                    deleteAt = sliderPos;
                    prefab = null;
                    sliders[i] = Math.Min(sliderPos, prefabList.Length - 1);
                    modified = true;
                }
                EditorGUILayout.EndHorizontal();
                prefab = (GameObject) EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                prefab = (GameObject) DragDropBox(prefab);
                
                meshPrefab.prefab = prefab;
                
                prefabList[sliderPos] = meshPrefab;

                tileDict.tilePrefabs[i].prefabs = prefabList.Where((e, idx) => idx != deleteAt).ToArray();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal(); 
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        if (modified)
        {
            EditorUtility.SetDirty(tileDict);
        }
    }

    public Object DragDropBox(Object gameObject)
    {
        Rect dragDropRect;
        var assetPreview = AssetPreview.GetAssetPreview(gameObject);
        if (gameObject == null || assetPreview == null)
        {
            var content = new GUIContent("Drag Asset Here");
            dragDropRect = GUILayoutUtility.GetRect(content, GUI.skin.box, GUILayout.Width(130), GUILayout.Height(130));
            GUI.Box(dragDropRect, content, GUI.skin.box);
        }
        else
        {
            var content = new GUIContent(assetPreview);
            dragDropRect = GUILayoutUtility.GetRect(content, GUI.skin.box);
            GUI.Box(dragDropRect, content, GUI.skin.box);
        }
        if (dragDropRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }   
            else if (Event.current.type == EventType.DragPerform)
            {
                if (DragAndDrop.objectReferences.Length == 1)
                {
                    gameObject = DragAndDrop.objectReferences[0];    
                }
                Event.current.Use();
            }
        }

        return gameObject;
    }
    
    


}