using UnityEngine;

namespace Nemuri.Dialogue
{
    public class DialogueManagerChapter3 : DialogueManager
    {
        public DialogueManagerChapter3()
        {
            SetChapterDefaults();
        }

        private void Reset()
        {
            SetChapterDefaults();
        }

        private void SetChapterDefaults()
        {
            _defaultTypingSpeed = 0.01f;
            _audioVolume = 10f;

            _dialogueTextSize = new Vector2(1400f, 600f);
            _dialogueTextPosition = new Vector2(20f, -475f);
            _narrationTextSize = new Vector2(1700f, 600f);
            _narrationTextPosition = new Vector2(30f, -530f);
            _objectiveTextSize = new Vector2(1350f, 600f);
            _objectiveTextPosition = new Vector2(-100f, 220f);

            _dialogueNameTextLayout = new NameTextLayoutSettings
            {
                AnchoredPosition = new Vector2(530f, -700f),
                SizeDelta = new Vector2(850f, 500f)
            };
            _narrationNameTextLayout = new NameTextLayoutSettings
            {
                AnchoredPosition = new Vector2(90f, -22f),
                SizeDelta = new Vector2(650f, 350f)
            };
            _objectiveNameTextLayout = new NameTextLayoutSettings
            {
                AnchoredPosition = new Vector2(90f, 0f),
                SizeDelta = new Vector2(650f, 350f)
            };

            _skipPromptLabel = "Hold E to skip";
            _skipPromptAnchoredPosition = new Vector2(-120f, -80f);
            _skipPromptSizeDelta = new Vector2(400f, 300f);
            _skipPromptFontSize = 42;
        }
    }
}
