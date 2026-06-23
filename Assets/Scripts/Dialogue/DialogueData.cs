using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nemuri.Dialogue
{
    [Serializable]
    public class DialogueNode
    {
        public string speaker;
        [TextArea(3, 10)]
        public string text;
        public string portraitName; // Optional: ID or filename of the sprite
        public float typingSpeed = 0.05f;
    }

    [Serializable]
    public class DialogueSequence
    {
        public List<DialogueNode> nodes = new List<DialogueNode>();
    }
}
