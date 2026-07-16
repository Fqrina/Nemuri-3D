using System.Collections;
using UnityEngine;
using Nemuri.Player;
using Nemuri.Interactions;

namespace Nemuri.Dialogue
{
    public class WalkingDialogueManager : DialogueManager
    {
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
