#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Game.Core;

namespace Game.Editor
{
    /// <summary>
    /// Editor window para crear habilidades por defecto
    /// </summary>
    public class AbilityCreator : EditorWindow
    {
        [MenuItem("MMO/Create Default Abilities")]
        public static void CreateDefaultAbilities()
        {
            string path = "Assets/_Game/ScriptableObjects/Abilities";
            
            // Crear carpeta si no existe
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "Abilities");
            }

            // GUERRERO
            CreateAbility(path, "GuerreroAtaqueBasico", 0, "Ataque Básico", 
                "Golpe básico con el arma", 10, 0, 0f, 3f, AbilityType.Damage);
            
            CreateAbility(path, "GolpePesado", 1, "Golpe Pesado", 
                "Un golpe poderoso que consume maná", 25, 10, 5f, 3f, AbilityType.Damage);

            // MAGO
            CreateAbility(path, "BolaDeFuego", 2, "Bola de Fuego", 
                "Lanza una bola de fuego ardiente", 30, 20, 3f, 10f, AbilityType.Damage);
            
            CreateAbility(path, "RayoHelado", 3, "Rayo Helado", 
                "Dispara un rayo de hielo", 20, 15, 2f, 8f, AbilityType.Damage);

            // CAZADOR
            CreateAbility(path, "Flecha", 4, "Flecha", 
                "Dispara una flecha rápida", 15, 5, 1f, 15f, AbilityType.Damage);
            
            CreateAbility(path, "DisparoMultiple", 5, "Disparo Múltiple", 
                "Dispara múltiples flechas", 35, 25, 8f, 12f, AbilityType.Damage);

            // SACERDOTE
            CreateAbility(path, "GolpeSagrado", 6, "Golpe Sagrado", 
                "Golpe imbuido con energía sagrada", 12, 8, 2f, 5f, AbilityType.Damage);
            
            CreateAbility(path, "PalabraSagrada", 7, "Palabra Sagrada", 
                "Palabra de poder que daña a los enemigos", 20, 15, 4f, 8f, AbilityType.Damage);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[AbilityCreator] 8 habilidades creadas exitosamente en " + path);
        }

        private static void CreateAbility(string path, string fileName, int id, string name, 
            string description, int damage, int manaCost, float cooldown, float range, AbilityType type)
        {
            AbilityData ability = ScriptableObject.CreateInstance<AbilityData>();
            
            ability.abilityID = id;
            ability.abilityName = name;
            ability.description = description;
            ability.baseDamage = damage;
            ability.manaCost = manaCost;
            ability.cooldownTime = cooldown;
            ability.range = range;
            ability.abilityType = type;

            string fullPath = $"{path}/{fileName}.asset";
            AssetDatabase.CreateAsset(ability, fullPath);
            
            Debug.Log($"[AbilityCreator] Creada: {name} ({fullPath})");
        }
    }
}
#endif
