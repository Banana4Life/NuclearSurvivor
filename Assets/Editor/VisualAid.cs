using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
    public override void OnInspectorGUI()
    {
        var tileDict = ((TileDictionary)target);
        var currentEvent = Event.current;
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
            if (gameObject == null)
            {
                GUILayout.Box("Drag Prefab Here");
            }
            else
            {
                var assetPreview = AssetPreview.GetAssetPreview(gameObject);
                GUILayout.Box(assetPreview);
            }
            var lastRect = GUILayoutUtility.GetLastRect();

            // switch (currentEvent.type)
            // {
            //     case EventType.DragUpdated:
            //     case EventType.DragPerform:
            //         if (!lastRect.Contains (currentEvent.mousePosition + scrollPosition))
            //             return;
            //         DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            //         if (currentEvent.type == EventType.DragPerform) {
            //             DragAndDrop.AcceptDrag();
            //             if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is GameObject go)
            //             {
            //                 tileDict.prefabs[0] = go;
            //             }
            //         }
            //         break;
            // }
            
            var newObject = EditorGUILayout.ObjectField(gameObject, typeof(GameObject), false);
            tileDict.prefabs[i] = (GameObject)newObject;
            EditorGUILayout.EndVertical();
        }


        EditorGUILayout.EndHorizontal(); GUILayout.EndScrollView();
    }


}