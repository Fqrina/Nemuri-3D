using System.Collections;
using UnityEngine;
using Nemuri.Player;
using Nemuri.Interactions;

namespace Nemuri.Dialogue
{
    public class WalkingDialogueManager : DialogueManager
    {
        protected override void ProceedToNextNode()
        {
            if (!canProceed)
            {
                if (_autoCloseCoroutine != null)
                {
                    StopCoroutine(_autoCloseCoroutine);
                }
                _autoCloseCoroutine = StartCoroutine(AutoCloseRoutine(0.5f));
                return;
            }

            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }

            _waitingForInput = false;

            if (_currentNode != null && string.Equals(_currentNode.speaker, "Objective", System.StringComparison.OrdinalIgnoreCase) &&
                WalkingSceneObjectiveManager.Instance != null)
            {
                PauseConversationForObjective(_currentNode.text);
            }
            else
            {
                DisplayNextNode();
            }
        }

        private void PauseConversationForObjective(string objectiveText)
        {
            StopDialogueAudio();
            SetSkipPromptVisible(false);
            SetDialoguePanelActive(false);

            SetPlayerMovementEnabled(true);

            if (WalkingSceneObjectiveManager.Instance != null)
            {
                WalkingSceneObjectiveManager.Instance.SetActiveObjective(objectiveText);
            }

            OnConversationEnd?.Invoke();
        }

        protected override void SetPlayerMovementEnabled(bool enabled)
        {
            var move = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
            foreach (var m in move)
            {
                m.SetCanMove(enabled);
            }
        }
    }
}
