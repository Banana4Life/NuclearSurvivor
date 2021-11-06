using System;
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

[CustomEditor(typeof(TileDictionary))]
public class TileDictionaryEditor : Editor
{
    Vector2 scrollPosition;
    private Vector2 mouse;
    private int[] sliders;
    public override void OnInspectorGUI()
    {
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
            var prefabList = tileDict.tilePrefabs[i].list;
            if (prefabList.Length == 0)
            {
                tileDict.tilePrefabs[i].list = new GameObject[] { null };
            }
            
            EditorGUILayout.BeginHorizontal();
            sliders[i] = EditorGUILayout.IntSlider(sliders[i], 0, prefabList.Length - 1);
            var sliderPos = sliders[i];
            GUILayout.Label("(" + prefabList.Length + ")");
            EditorGUILayout.EndHorizontal();
       
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(((TileDictionary.EdgeTileType)i).ToString());
            int deleteAt = -1;
            var gameObject = prefabList[sliderPos];
            if (GUILayout.Button("+"))
            {
                Array.Resize(ref prefabList, prefabList.Length + 1);
                prefabList[prefabList.Length - 1] = null;
                sliders[i] = prefabList.Length - 1;
            }
            if (GUILayout.Button("X"))
            {
                deleteAt = sliderPos;
                gameObject = null;
                sliders[i] = Math.Min(sliderPos, prefabList.Length - 1);
            }
            EditorGUILayout.EndHorizontal();
            gameObject = (GameObject) EditorGUILayout.ObjectField(gameObject, typeof(GameObject), false);
            gameObject = (GameObject) DragDropBox(gameObject);
            prefabList[sliderPos] = gameObject;

            // Delete null except last
            tileDict.tilePrefabs[i].list = prefabList.Where((e, idx) => idx != deleteAt).ToArray();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal(); 
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