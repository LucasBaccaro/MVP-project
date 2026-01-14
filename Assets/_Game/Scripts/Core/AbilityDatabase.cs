using UnityEngine;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Base de datos centralizada de todas las habilidades del juego.
    /// Singleton para acceso global. Similar a ItemDatabase.
    /// </summary>
    public class AbilityDatabase : MonoBehaviour
    {
        public static AbilityDatabase Instance { get; private set; }

        [Header("Ability Database")]
        [Tooltip("Lista de TODAS las habilidades del juego. Asignar en Inspector.")]
        public List<AbilityData> allAbilities = new List<AbilityData>();

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
