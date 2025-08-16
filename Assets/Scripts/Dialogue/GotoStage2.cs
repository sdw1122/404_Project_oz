using UnityEngine;

public class GotoStage2 : DialogueManager
{
    public GameObject teleport;

    public override void EndDialogue()
    {
        base.EndDialogue();
        if (currentConversationName == "First")
        {
            teleport.SetActive(true);
        }
    }
}
