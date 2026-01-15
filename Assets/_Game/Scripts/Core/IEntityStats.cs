using UnityEngine;

namespace Game.Core
{
    public interface IEntityStats
    {
        string EntityName { get; }
        int CurrentHealth { get; }
        int MaxHealth { get; }
        string ClassName { get; } // Players have class, NPCs might not (return "NPC" or type)
        void TakeDamage(int damage, IEntityStats attacker); // Unified damage method
    }
}
