using UnityEngine;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Base de datos centralizada de todas las habilidades del juego.
    /// Singleton para acceso global. Similar a ItemDatabase.
    /// Carga automáticamente desde Resources/Abilities o desde la lista del Inspector.
    /// </summary>
    public class AbilityDatabase : MonoBehaviour
    {
        public static AbilityDatabase Instance { get; private set; }

        [Header("Ability Database")]
        [Tooltip("Lista de habilidades. Si está vacía, carga automáticamente desde Resources/Abilities")]
        public List<AbilityData> allAbilities = new List<AbilityData>();

        [Header("Auto-Load Settings")]
        [Tooltip("Si está activo, carga todas las habilidades desde Resources/Abilities")]
        public bool autoLoadFromResources = true;

        private Dictionary<int, AbilityData> abilityDictionary = new Dictionary<int, AbilityData>();

        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDatabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Inicializa el diccionario para búsquedas rápidas por ID
        /// </summary>
        void InitializeDatabase()
        {
            abilityDictionary.Clear();

            // Si autoLoad está activo y la lista está vacía, cargar desde Resources
            if (autoLoadFromResources)
            {
                LoadAbilitiesFromResources();
            }

            foreach (AbilityData ability in allAbilities)
            {
                if (ability != null)
                {
                    if (!abilityDictionary.ContainsKey(ability.abilityID))
                    {
                        abilityDictionary.Add(ability.abilityID, ability);
                    }
                    else
                    {
                        Debug.LogWarning($"AbilityDatabase: Habilidad duplicada con ID {ability.abilityID} ({ability.abilityName}). Ignorada.");
                    }
                }
            }

            Debug.Log($"[AbilityDatabase] Inicializado con {abilityDictionary.Count} habilidades.");
        }

        /// <summary>
        /// Carga todas las habilidades desde Resources/Abilities
        /// </summary>
        private void LoadAbilitiesFromResources()
        {
            AbilityData[] loadedAbilities = Resources.LoadAll<AbilityData>("Abilities");

            if (loadedAbilities.Length > 0)
            {
                Debug.Log($"[AbilityDatabase] Cargadas {loadedAbilities.Length} habilidades desde Resources/Abilities");

                foreach (var ability in loadedAbilities)
                {
                    if (!allAbilities.Contains(ability))
                    {
                        allAbilities.Add(ability);
                    }
                }
            }
            else
            {
                Debug.Log("[AbilityDatabase] No se encontraron habilidades en Resources/Abilities, usando lista del Inspector");
            }
        }

        /// <summary>
        /// Obtiene una habilidad por su ID
        /// </summary>
        public AbilityData GetAbility(int abilityID)
        {
            if (abilityID < 0) return null;

            if (abilityDictionary.TryGetValue(abilityID, out AbilityData ability))
            {
                return ability;
            }
            else
            {
                Debug.LogWarning($"[AbilityDatabase] Habilidad con ID {abilityID} no encontrada.");
                return null;
            }
        }

        /// <summary>
        /// Verifica si existe una habilidad con este ID
        /// </summary>
        public bool AbilityExists(int abilityID)
        {
            return abilityDictionary.ContainsKey(abilityID);
        }

        /// <summary>
        /// Obtiene todas las habilidades de un tipo específico
        /// </summary>
        public List<AbilityData> GetAbilitiesByType(AbilityType type)
        {
            List<AbilityData> result = new List<AbilityData>();

            foreach (AbilityData ability in allAbilities)
            {
                if (ability != null && ability.abilityType == type)
                {
                    result.Add(ability);
                }
            }

            return result;
        }
    }
}
