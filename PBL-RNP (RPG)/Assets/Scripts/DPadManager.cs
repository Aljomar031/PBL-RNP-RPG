using UnityEngine;
using UnityEngine.EventSystems;

public class DPadManager : MonoBehaviour
{
    public CharacterController player;

    public GameObject upButton;
    public GameObject downButton;
    public GameObject leftButton;
    public GameObject rightButton;

    void Awake()
    {
        SetupButton(upButton, CharacterController.Direction.Up);
        SetupButton(downButton, CharacterController.Direction.Down);
        SetupButton(leftButton, CharacterController.Direction.Left);
        SetupButton(rightButton, CharacterController.Direction.Right);
    }

    void SetupButton(GameObject button, CharacterController.Direction direction)
    {
        if (button == null)
        {
            Debug.LogWarning("A D-pad button has not been assigned.");
            return;
        }

        EventTrigger trigger = button.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = button.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers =
                new System.Collections.Generic.List<EventTrigger.Entry>();
        }

        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener(
            (eventData) => PressDirection(direction));

        trigger.triggers.Add(pointerDown);

        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener(
            (eventData) => ReleaseDirection(direction));

        trigger.triggers.Add(pointerUp);
    }

    void PressDirection(CharacterController.Direction direction)
    {
        if (player != null)
        {
            player.PressDirection(direction);
        }
    }

    void ReleaseDirection(CharacterController.Direction direction)
    {
        if (player != null)
        {
            player.ReleaseDirection(direction);
        }
    }

    void OnDisable()
    {
        if (player != null)
        {
            player.ReleaseDirection(CharacterController.Direction.Up);
            player.ReleaseDirection(CharacterController.Direction.Down);
            player.ReleaseDirection(CharacterController.Direction.Left);
            player.ReleaseDirection(CharacterController.Direction.Right);
        }
    }
}