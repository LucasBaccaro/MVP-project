using UnityEngine;
using UnityEngine.UIElements;
using Game.Combat;
using Game.Player;
using Game.Core;

namespace Game.UI
{
    /// <summary>
    /// Controlador de UI para debugear el sistema de combate (Casteo, AoE, etc.)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CombatDebugUI : MonoBehaviour
    {
        private VisualElement root;
        private Label selectedAbilityLabel;
        private Label abilityTypeLabel;
        private Label descriptionLabel;
        private Label castTypeBadge;
        private Label targetLabel;
        private Label castTimeLabel;
        private Label aoeRadiusLabel;
        private Label baseDamageLabel;
        private VisualElement progressFill;
        private VisualElement progressContainer;

        private PlayerCombat playerCombat;
        private TargetingSystem targetingSystem;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            CacheElements();
        }

        private void CacheElements()
        {
            selectedAbilityLabel = root.Q<Label>("selected-ability");
            abilityTypeLabel = root.Q<Label>("ability-type-value");
            descriptionLabel = root.Q<Label>("ability-description");
            castTypeBadge = root.Q<Label>("cast-type-badge");
            targetLabel = root.Q<Label>("combat-target");
            castTimeLabel = root.Q<Label>("cast-time-value");
            aoeRadiusLabel = root.Q<Label>("aoe-radius-value");
            baseDamageLabel = root.Q<Label>("base-damage-value");
            progressFill = root.Q<VisualElement>("progress-fill");
            progressContainer = root.Q<VisualElement>("progress-container");
        }

        private void Update()
        {
            if (playerCombat == null)
            {
                FindPlayer();
                return;
            }

            UpdateUI();
        }

        private void FindPlayer()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null && player.isLocalPlayer)
            {
                playerCombat = player.GetComponent<PlayerCombat>();
                targetingSystem = player.GetComponent<TargetingSystem>();
                Debug.Log("[CombatDebugUI] Local player found and linked.");
            }
        }

        private void UpdateUI()
        {
            // Selected Ability
            AbilityData selected = playerCombat.GetSelectedAbility();
            selectedAbilityLabel.text = selected != null ? selected.abilityName : "None";
            abilityTypeLabel.text = selected != null ? selected.abilityType.ToString() : "None";
            descriptionLabel.text = selected != null ? selected.description : "";
            descriptionLabel.style.display = selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (selected != null)
            {
                castTimeLabel.text = $"{selected.castTime:F1}s";
                aoeRadiusLabel.text = $"{selected.aoeRadius:F1}m";
                baseDamageLabel.text = selected.baseDamage.ToString();
            }
            else
            {
                castTimeLabel.text = "0.0s";
                aoeRadiusLabel.text = "0m";
                baseDamageLabel.text = "0";
            }

            // Target
            var target = targetingSystem != null ? targetingSystem.currentTarget : null;
            targetLabel.text = target != null ? target.name : "None";

            // Casting Status
            if (playerCombat.IsCasting)
            {
                castTypeBadge.text = "CASTING";
                castTypeBadge.RemoveFromClassList("status-idle");
                castTypeBadge.RemoveFromClassList("status-instant");
                castTypeBadge.AddToClassList("status-casting");
                
                progressContainer.style.display = DisplayStyle.Flex;
                progressFill.style.width = Length.Percent(playerCombat.CastProgress * 100f);
            }
            else
            {
                progressContainer.style.display = DisplayStyle.None;
                
                if (selected != null)
                {
                    if (selected.castingType == CastingType.Instant)
                    {
                        castTypeBadge.text = "INSTANT";
                        castTypeBadge.RemoveFromClassList("status-idle");
                        castTypeBadge.AddToClassList("status-instant");
                        castTypeBadge.RemoveFromClassList("status-casting");
                    }
                    else
                    {
                        castTypeBadge.text = "READY";
                        castTypeBadge.AddToClassList("status-idle");
                        castTypeBadge.RemoveFromClassList("status-instant");
                        castTypeBadge.RemoveFromClassList("status-casting");
                    }
                }
                else
                {
                    castTypeBadge.text = "IDLE";
                    castTypeBadge.AddToClassList("status-idle");
                    castTypeBadge.RemoveFromClassList("status-instant");
                    castTypeBadge.RemoveFromClassList("status-casting");
                }
            }
        }
    }
}
