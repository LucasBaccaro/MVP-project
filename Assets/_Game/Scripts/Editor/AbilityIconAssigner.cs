using UnityEngine;
using UnityEditor;
using Game.Core;
using System.IO;

namespace Game.Editor
{
    /// <summary>
    /// Editor tool para asignar iconos a las habilidades automáticamente
    /// </summary>
    public class AbilityIconAssigner : EditorWindow
    {
        [MenuItem("MMO/Asignar Iconos de Habilidades")]
        public static void ShowWindow()
        {
            GetWindow<AbilityIconAssigner>("Asignar Iconos");
        }

        private void OnGUI()
        {
            GUILayout.Label("Asignar Iconos Automáticamente", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Este tool asigna automáticamente los iconos de habilidades basándose en el nombre.\n\n" +
                "Mapeo de nombres:\n" +
                "- BolaDeFuego → Ab_UI_Firebolt\n" +
                "- RayoHelado → Ab_UI_Frostbeam\n" +
                "- GuerreroAtaqueBasico → Ab_UI_Autoattack\n" +
                "- GolpePesado → Ab_UI_GuantletStrike",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Asignar Iconos a Todas las Habilidades", GUILayout.Height(40)))
            {
                AssignAllIcons();
            }
        }

        private void AssignAllIcons()
        {
            // Cargar todas las habilidades
            string[] guids = AssetDatabase.FindAssets("t:AbilityData");
            int assignedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AbilityData ability = AssetDatabase.LoadAssetAtPath<AbilityData>(path);

                if (ability == null) continue;

                // Intentar asignar icono según el nombre
                bool assigned = TryAssignIconByName(ability);
                if (assigned)
                {
                    assignedCount++;
                    EditorUtility.SetDirty(ability);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AbilityIconAssigner] {assignedCount} iconos asignados exitosamente");
            EditorUtility.DisplayDialog("Completado", $"Se asignaron {assignedCount} iconos.", "OK");
        }

        private bool TryAssignIconByName(AbilityData ability)
        {
            string iconPath = GetIconPathForAbility(ability.name);

            if (string.IsNullOrEmpty(iconPath))
            {
                Debug.LogWarning($"[AbilityIconAssigner] No se encontró mapeo para: {ability.name}");
                return false;
            }

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            if (icon == null)
            {
                Debug.LogWarning($"[AbilityIconAssigner] No se encontró icono en: {iconPath}");
                return false;
            }

            ability.icon = icon;
            Debug.Log($"[AbilityIconAssigner] Asignado {icon.name} a {ability.abilityName}");
            return true;
        }

        /// <summary>
        /// Mapeo manual de nombres de habilidades a rutas de iconos
        /// </summary>
        private string GetIconPathForAbility(string abilityName)
        {
            // Mago
            if (abilityName.Contains("BolaDeFuego") || abilityName.Contains("Firebolt"))
                return "Assets/UI/Art/Abilities/Mage/Ab_UI_Firebolt.png";

            if (abilityName.Contains("RayoHelado") || abilityName.Contains("Frost"))
                return "Assets/UI/Art/Abilities/Mage/Ab_UI_Frostbeam.png";

            // Guerrero
            if (abilityName.Contains("GuerreroAtaqueBasico") || abilityName.Contains("Autoattack"))
                return "Assets/UI/Art/Abilities/War/Ab_UI_Autoattack.png";

            if (abilityName.Contains("GolpePesado") || abilityName.Contains("Strike"))
                return "Assets/UI/Art/Abilities/War/Ab_UI_GuantletStrike.png";

            // Añadir más mapeos aquí según necesites
            // ...

            return null;
        }
    }
}
