using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// ScriptableObject que define los stats base de una clase de personaje
    /// Se crean assets para cada clase (Guerrero, Mago, Cazador, Sacerdote)
    /// </summary>
    [CreateAssetMenu(fileName = "New Class", menuName = "MMO/Class Data", order = 0)]
    public class ClassData : ScriptableObject
    {
        [Header("Class Info")]
        [Tooltip("Nombre de la clase")]
        public string className = "Guerrero";

        [Tooltip("Descripción de la clase")]
        [TextArea(3, 5)]
        public string description = "Un guerrero valiente...";

        [Tooltip("Color del material de la clase")]
        public Color classColor = Color.white;

        [Header("Base Stats")]
        [Tooltip("Vida base de la clase")]
        public int baseHP = 100;

        [Tooltip("Maná base de la clase")]
        public int baseMana = 50;

        [Tooltip("Daño base de la clase")]
        public int baseDamage = 10;

        [Header("Regeneration")]
        [Tooltip("HP regenerado por segundo")]
        public float hpRegenRate = 1f;

        [Tooltip("Maná regenerado por segundo")]
        public float manaRegenRate = 2f;

        [Header("Starting Resources")]
        [Tooltip("Oro inicial de la clase")]
        public int startingGold = 100;

        [Tooltip("Nivel inicial (normalmente 1)")]
        public int startingLevel = 1;

        [Header("Abilities")]
        [Tooltip("Habilidades de esta clase")]
        public AbilityData[] classAbilities;

        /// <summary>
        /// Valida que los stats tengan valores razonables
        /// </summary>
        private void OnValidate()
        {
            baseHP = Mathf.Max(1, baseHP);
            baseMana = Mathf.Max(0, baseMana);
            baseDamage = Mathf.Max(1, baseDamage);
            hpRegenRate = Mathf.Max(0, hpRegenRate);
            manaRegenRate = Mathf.Max(0, manaRegenRate);
            startingGold = Mathf.Max(0, startingGold);
            startingLevel = Mathf.Max(1, startingLevel);
        }
    }
}
