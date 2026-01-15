using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Define las propiedades de una habilidad
    /// </summary>
    [CreateAssetMenu(fileName = "New Ability", menuName = "MMO/Ability Data")]
    public class AbilityData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("ID único de la habilidad")]
        public int abilityID;
        
        [Tooltip("Nombre de la habilidad")]
        public string abilityName;
        
        [Tooltip("Descripción de la habilidad")]
        [TextArea(2, 4)]
        public string description;
        
        [Tooltip("Icono de la habilidad (para UI)")]
        public Sprite icon;

        [Header("Costs & Cooldown")]
        [Tooltip("Coste de maná")]
        public int manaCost;
        
        [Tooltip("Tiempo de cooldown en segundos")]
        public float cooldownTime;

        [Header("Effects")]
        [Tooltip("Daño base de la habilidad")]
        public int baseDamage;
        
        [Tooltip("Rango máximo en metros")]
        public float range = 5f;

        [Header("Type")]
        [Tooltip("Tipo de habilidad")]
        public AbilityType abilityType = AbilityType.Damage;

        [Tooltip("Tipo de cast")]
        public CastingType castingType = CastingType.Instant;

        [Tooltip("Tiempo de cast en segundos")]
        public float castTime = 0f;

        [Tooltip("Radio de efecto para daño en área (0 = single target)")]
        public float aoeRadius = 0f;
    }

    public enum CastingType
    {
        Instant,    // Lanzamiento instantáneo
        Casting,    // Requiere quedarse quieto X segundos
        Channel,    // Efecto continuo mientras dura el cast
        Movement    // Se puede lanzar en movimiento (como instantáneo pero explícito)
    }

    public enum AbilityType
    {
        Damage,     // Hace daño
        Heal,       // Cura
        Buff,       // Mejora stats (futuro)
        Debuff      // Reduce stats o aplica efectos negativos (futuro)
    }
}
