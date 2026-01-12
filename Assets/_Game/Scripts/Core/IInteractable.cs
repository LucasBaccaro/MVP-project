using UnityEngine;

namespace Game.Core
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(GameObject player);
    }
}
