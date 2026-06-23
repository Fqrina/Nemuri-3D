using System.Collections.Generic;
using UnityEngine;

namespace Nemuri.Dialogue
{
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private TextAsset _dialogueJson;

        public void TriggerDialogue()
        {
            if (_dialogueJson == null)
            {
                Debug.LogWarning("[DialogueTrigger] No dialogue JSON assigned.", this);
                return;
            }

            DialogueSequence sequence = JsonUtility.FromJson<DialogueSequence>(_dialogueJson.text);
            if (sequence == null || sequence.nodes == null)
            {
                Debug.LogWarning("[DialogueTrigger] Dialogue JSON could not be parsed.", this);
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[DialogueTrigger] DialogueManager is not available.", this);
                return;
            }

            DialogueManager.Instance.StartConversation(sequence.nodes);
        }
    }
}
