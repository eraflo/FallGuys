using UnityEngine;
using System.Text;

public class UIHierarchyPrinter : MonoBehaviour
{
    [ContextMenu("Print Hierarchy")]
    void Start()
    {
        PrintHierarchy();
    }

    public void PrintHierarchy()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"HIERARCHY OF {gameObject.name}:");
        Traverse(transform, 0, sb);
        Debug.Log(sb.ToString());
    }

    private void Traverse(Transform current, int depth, StringBuilder sb)
    {
        string indent = new string('-', depth * 2);
        RectTransform rt = current.GetComponent<RectTransform>();
        string info = "";
        if (rt != null)
        {
            info = $" [Pos: {rt.anchoredPosition}, Size: {rt.sizeDelta}, AnchorMin: {rt.anchorMin}, AnchorMax: {rt.anchorMax}]";
        }
        sb.AppendLine($"{indent}{current.name}{info}");

        foreach (Transform child in current)
        {
            Traverse(child, depth + 1, sb);
        }
    }
}
