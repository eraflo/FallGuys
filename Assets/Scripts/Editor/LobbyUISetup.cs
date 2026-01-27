using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class LobbyUISetup : EditorWindow
{
    [MenuItem("Tools/FallGuys/Fix Lobby UI")]
    public static void FixLobbyUI()
    {
        // 1. Fix Lobby UI List
        LobbyUI lobby = FindFirstObjectByType<LobbyUI>();
        if (lobby != null)
        {
            // We need to access the private field via SerializedObject since we can't change code visibility easily
            // But for now, let's assume the user can drag the container or we find it by name if possible.
            // Since we can't reliably find private fields without reflection, we'll try to find the likely container by name or structure.
            
            Transform container = lobby.transform.Find("LobbyPanel/PlayerList/Container"); // Guessing structure
            if (container == null) container = lobby.transform.Find("LobbyPanel/PlayerList"); 
            
            // Fallback: Ask user to select it, or try to find where the rows are. 
            // Better strategy: Add components to the SELECTED object.
            
            if (Selection.activeGameObject != null)
            {
                SetupContainer(Selection.activeGameObject);
                Debug.Log($"Setup UI on selected object: {Selection.activeGameObject.name}");
            }
            else
            {
                 Debug.LogWarning("Please select the 'Container' object in your Hierarchy and run this command again.");
            }
        }
        else
        {
             if (Selection.activeGameObject != null)
            {
                SetupContainer(Selection.activeGameObject);
                Debug.Log($"Setup UI on selected object: {Selection.activeGameObject.name}");
            }
        }
    }
    
    private static void SetupContainer(GameObject go)
    {
        // Add Layout Group
        var vlg = go.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = go.AddComponent<VerticalLayoutGroup>();
        
        vlg.childControlWidth = true;
        vlg.childControlHeight = false; // Let children decide their height
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        
        // Add Content Size Fitter (optional, good for scrolling)
        var csf = go.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Clean children
        foreach(Transform child in go.transform)
        {
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }
    }
}
