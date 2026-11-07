using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject gameplayUI;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Text Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    private InteractableNPC activeNPC;
    private InteractableNPC.DialogueSection activeSection;
    private string[] currentDialogueLines;

    private int currentLine;
    private bool dialogueOpen;
    private bool isTyping;
    private Coroutine typingCoroutine;

    public bool IsDialogueOpen => dialogueOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(
        InteractableNPC npc,
        InteractableNPC.DialogueSection section)
    {
        if (dialogueOpen || npc == null || section == null)
            return;

        activeNPC = npc;
        activeSection = section;
        currentDialogueLines = npc.GetDialogueLines(section);

        if (currentDialogueLines == null ||
            currentDialogueLines.Length == 0)
        {
            activeNPC = null;
            activeSection = null;
            return;
        }

        currentLine = 0;
        dialogueOpen = true;

        gameplayUI.SetActive(false);
        dialoguePanel.SetActive(true);

        nameText.text = activeNPC.NPCName;

        ShowCurrentLine();
    }

    public void AdvanceDialogue()
    {
        if (!dialogueOpen || activeNPC == null) return;

        // If text is still typing, show the whole line immediately.
        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            dialogueText.text = GetCurrentLineWithPlayerName();

            typingCoroutine = null;
            isTyping = false;
            return;
        }

        currentLine++;

        if (currentLine >= currentDialogueLines.Length)
        {
            activeNPC.ApplyDialogueAction(activeSection);
            CloseDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        typingCoroutine = StartCoroutine(
            TypeLine(GetCurrentLineWithPlayerName())
        );
    }

    private string GetCurrentLineWithPlayerName()
    {
        string line = currentDialogueLines[currentLine];
        string playerName = AuthSession.Username;

        if (string.IsNullOrEmpty(playerName))
            playerName = "Traveler";

        return line.Replace("[playername]", playerName);
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in line)
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        dialogueText.text = line;
        isTyping = false;
        typingCoroutine = null;
    }

    private void CloseDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;
        isTyping = false;
        dialogueOpen = false;

        activeNPC = null;
        activeSection = null;
        currentDialogueLines = null;

        dialoguePanel.SetActive(false);
        gameplayUI.SetActive(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}