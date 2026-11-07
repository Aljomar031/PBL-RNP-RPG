using UnityEngine;

public abstract class InteractableBase : MonoBehaviour
{
    public abstract void Interact();

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            InteractionManager.Instance?.AddNearbyInteractable(this);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            InteractionManager.Instance?.RemoveNearbyInteractable(this);
        }
    }
}