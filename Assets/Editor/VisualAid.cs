using UnityEditor;
using UnityEngine;

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
    public override void OnInspectorGUI()
    {
        var tileDict = ((TileDictionary)target);
        base.OnInspectorGUI();
        
        GUILayout.Space(10);
        if (GUILayout.Button("Place Tiles"))
        {
            ((TileDictionary)target).PlaceTiles();
        }
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.BeginHorizontal();
        for (var i = 0; i < tileDict.prefabs.Length; i++)
        {
            var gameObject = tileDict.prefabs[i];
            EditorGUILayout.BeginVertical();
            GUILayout.Label(((TileDictionary.EdgeTileType)i).ToString());
            Rect dragDropRect;
            if (gameObject == null)
            {
                var content = new GUIContent("Drag Prefab Here");
                dragDropRect = GUILayoutUtility.GetRect(content, GUI.skin.box);
                GUI.Box(dragDropRect, content, GUI.skin.box);
            }
            else
            {
                var assetPreview = AssetPreview.GetAssetPreview(gameObject);
                
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
                        gameObject = (GameObject)DragAndDrop.objectReferences[0];    
                    }
                    Event.current.Use();
                }
            }
            gameObject = (GameObject) EditorGUILayout.ObjectField(gameObject, typeof(GameObject), false);
            tileDict.prefabs[i] = gameObject;
            
            EditorGUILayout.EndVertical();
        }


        EditorGUILayout.EndHorizontal(); 
        GUILayout.EndScrollView();
    }


}