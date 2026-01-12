using UnityEngine;
using Game.UI;

/// <summary>
/// Script temporal para diagnosticar problemas con AbilityBarUI
/// Ejecutar en la escena GameWorld para ver cuántos hay
/// </summary>
public class DiagnosticAbilityBar : MonoBehaviour
{
    void Start()
    {
        var abilityBars = FindObjectsOfType<AbilityBarUI>();
        Debug.LogWarning($"[DIAGNOSTIC] Se encontraron {abilityBars.Length} AbilityBarUI en la escena:");
        
        for (int i = 0; i < abilityBars.Length; i++)
        {
            Debug.LogWarning($"  [{i}] {abilityBars[i].gameObject.name} - Path: {GetGameObjectPath(abilityBars[i].gameObject)}", abilityBars[i].gameObject);
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}
