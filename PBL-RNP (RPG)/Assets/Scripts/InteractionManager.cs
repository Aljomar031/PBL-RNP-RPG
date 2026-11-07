using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [SerializeField] private Button interactButton;

    private readonly HashSet<InteractableBase> nearbyInteractables =
        new HashSet<InteractableBase>();

    private InteractableBase currentInteractable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        interactButton.interactable = false;
        interactButton.onClick.AddListener(Interact);
    }

    private void Update()
    {
        FindNearestInteractable();
    }

    public void AddNearbyInteractable(InteractableBase interactable)
    {
        nearbyInteractables.Add(interactable);
    }

    public void RemoveNearbyInteractable(InteractableBase interactable)
    {
        nearbyInteractables.Remove(interactable);
    }

    private void FindNearestInteractable()
    {
        currentInteractable = null;
        float nearestDistance = float.MaxValue;

        nearbyInteractables.RemoveWhere(item => item == null);

        foreach (InteractableBase item in nearbyInteractables)
        {
            float distance = Vector2.Distance(
                transform.position,
                item.transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                currentInteractable = item;
            }
        }

        bool dialogueOpen =
            DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialogueOpen;

        interactButton.interactable =
            currentInteractable != null && !dialogueOpen;
    }

    private void Interact()
    {
        if (currentInteractable == null) return;

        if (DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialogueOpen)
        {
            return;
        }

        currentInteractable.Interact();
    }

    private void OnDestroy()
    {
        if (interactButton != null)
        {
            interactButton.onClick.RemoveListener(Interact);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}