using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public TMP_Text dialogueText;
    public TMP_Text characterNameText;
    public Image characterPortrait;
    public Button nextButton;

    private Queue<DialogueLine> dialogueQueue;
    private bool isDialogueActive = false;

    void Start()
    {
        dialogueQueue = new Queue<DialogueLine>();
        nextButton.onClick.AddListener(ShowNextDialogue);
        nextButton.gameObject.SetActive(false);
    }

    public void StartDialogue(List<DialogueLine> dialogues)
    {
        dialogueQueue.Clear();
        foreach (DialogueLine line in dialogues)
        {
            dialogueQueue.Enqueue(line);
        }
        isDialogueActive = true;
        nextButton.gameObject.SetActive(true);
        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = dialogueQueue.Dequeue();
        characterNameText.text = currentLine.characterName;
        dialogueText.text = currentLine.dialogueText;
        if (characterPortrait != null && currentLine.characterPortrait != null)
        {
            characterPortrait.sprite = currentLine.characterPortrait;
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        nextButton.gameObject.SetActive(false);
        // Optionally hide the dialogue UI
    }
}