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

            _dialoguePanelLayout = new PanelLayoutSettings
            {
                AnchoredPosition = new Vector2(0f, -50f),
                SizeDelta = new Vector2(2150f, 1300f)
            };
            _narrationPanelLayout = new PanelLayoutSettings
            {
                AnchoredPosition = new Vector2(0f, 760f),
                SizeDelta = new Vector2(1920f, 1080f)
            };
            _objectivePanelLayout = new PanelLayoutSettings
            {
                AnchoredPosition = new Vector2(0f, 60f),
                SizeDelta = new Vector2(1920f, 1080f)
            };

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

            _kaelSplashArtSettings = new CharacterSplashArtSettings
            {
                SplashSprite = _kaelSplashArtSettings?.SplashSprite,
                AnchoredPosition = new Vector2(350f, -100f),
                SizeDelta = new Vector2(800f, 1000f),
                RenderBehindPanel = true
            };
            _feanorSplashArtSettings = new CharacterSplashArtSettings
            {
                SplashSprite = _feanorSplashArtSettings?.SplashSprite,
                AnchoredPosition = new Vector2(400f, 0f),
                SizeDelta = new Vector2(1000f, 1200f),
                RenderBehindPanel = true
            };
            _keikoSplashArtSettings = new CharacterSplashArtSettings
            {
                SplashSprite = _keikoSplashArtSettings?.SplashSprite,
                AnchoredPosition = new Vector2(400f, 0f),
                SizeDelta = new Vector2(1000f, 1200f),
                RenderBehindPanel = true
            };
            _murialSplashArtSettings = new CharacterSplashArtSettings
            {
                SplashSprite = _murialSplashArtSettings?.SplashSprite,
                AnchoredPosition = new Vector2(400f, 0f),
                SizeDelta = new Vector2(1000f, 1200f),
                RenderBehindPanel = true
            };
            _ronaSplashArtSettings = new CharacterSplashArtSettings
            {
                SplashSprite = _ronaSplashArtSettings?.SplashSprite,
                AnchoredPosition = new Vector2(400f, 0f),
                SizeDelta = new Vector2(1000f, 1200f),
                RenderBehindPanel = true
            };
        }
    }
}
