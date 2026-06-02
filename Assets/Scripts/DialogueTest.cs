using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTest : MonoBehaviour
{
    public DialogueManager dialogueManager;

    void Start()
    {
        List<DialogueLine> dialogues = new List<DialogueLine>
        {
            new DialogueLine { characterName = "角色A", dialogueText = "你好，我是角色A。" },
            new DialogueLine { characterName = "角色B", dialogueText = "你好，角色A。我是角色B。" },
            new DialogueLine { characterName = "角色C", dialogueText = "大家好，我是角色C。" },
            new DialogueLine { characterName = "角色D", dialogueText = "很高兴见到大家，我是角色D。" },
            new DialogueLine { characterName = "角色A", dialogueText = "让我们开始对话吧！" },
            new DialogueLine { characterName = "角色B", dialogueText = "好的，听起来不错。" },
            new DialogueLine { characterName = "角色C", dialogueText = "我同意。" },
            new DialogueLine { characterName = "角色D", dialogueText = "开始吧！" }
        };

        dialogueManager.StartDialogue(dialogues);
    }
}