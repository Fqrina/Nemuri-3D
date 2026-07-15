using System.Collections;
using UnityEngine;
using Nemuri.Player;

namespace Nemuri.Dialogue
{
    public class Chapt1DialogueManager : DialogueManager
    {
        protected override void SetPlayerMovementEnabled(bool enabled)
        {
            var move = FindObjectsByType<PlayerMovementChapt1>(FindObjectsInactive.Include);
            foreach (var m in move)
            {
                m.SetCanMove(enabled);
            }
        }
    }
}
