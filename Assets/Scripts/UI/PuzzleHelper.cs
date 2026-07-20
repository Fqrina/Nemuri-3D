using UnityEngine;

namespace Nemuri.UI
{
    public static class PuzzleHelper
    {
        /// <summary>
        /// Automatically checks the GameObject for any of the 3 puzzle types (J, H, G) and opens it.
        /// </summary>
        public static void OpenPuzzle(GameObject puzzleGo)
        {
            if (puzzleGo == null)
            {
                Debug.LogWarning("[PuzzleHelper] Cannot open puzzle because GameObject is null.");
                return;
            }

            if (puzzleGo.TryGetComponent<OverlayPinPuzzleUI>(out var pinPuzzle))
            {
                pinPuzzle.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayUnorderedPuzzleUI>(out var unorderedPuzzle))
            {
                unorderedPuzzle.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayRotatablePuzzleUI>(out var rotatablePuzzle))
            {
                rotatablePuzzle.OpenPuzzleUI();
            }
            else
            {
                Debug.LogWarning("[PuzzleHelper] No recognized puzzle component (OverlayPinPuzzleUI, OverlayUnorderedPuzzleUI, or OverlayRotatablePuzzleUI) found on " + puzzleGo.name);
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the 3 puzzle types (J, H, G) and closes it.
        /// </summary>
        public static void ClosePuzzle(GameObject puzzleGo)
        {
            if (puzzleGo == null) return;

            if (puzzleGo.TryGetComponent<OverlayPinPuzzleUI>(out var pinPuzzle))
            {
                pinPuzzle.ClosePuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayUnorderedPuzzleUI>(out var unorderedPuzzle))
            {
                unorderedPuzzle.ClosePuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayRotatablePuzzleUI>(out var rotatablePuzzle))
            {
                rotatablePuzzle.ClosePuzzleUI();
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the 3 puzzle types (J, H, G) and subscribes to its OnPuzzleSolved callback.
        /// </summary>
        public static void RegisterOnPuzzleSolved(GameObject puzzleGo, System.Action callback)
        {
            if (puzzleGo == null || callback == null) return;

            if (puzzleGo.TryGetComponent<OverlayPinPuzzleUI>(out var pinPuzzle))
            {
                pinPuzzle.OnPuzzleSolved += callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayUnorderedPuzzleUI>(out var unorderedPuzzle))
            {
                unorderedPuzzle.OnPuzzleSolved += callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayRotatablePuzzleUI>(out var rotatablePuzzle))
            {
                rotatablePuzzle.OnPuzzleSolved += callback;
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the 3 puzzle types (J, H, G) and unsubscribes from its OnPuzzleSolved callback.
        /// </summary>
        public static void UnregisterOnPuzzleSolved(GameObject puzzleGo, System.Action callback)
        {
            if (puzzleGo == null || callback == null) return;

            if (puzzleGo.TryGetComponent<OverlayPinPuzzleUI>(out var pinPuzzle))
            {
                pinPuzzle.OnPuzzleSolved -= callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayUnorderedPuzzleUI>(out var unorderedPuzzle))
            {
                unorderedPuzzle.OnPuzzleSolved -= callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayRotatablePuzzleUI>(out var rotatablePuzzle))
            {
                rotatablePuzzle.OnPuzzleSolved -= callback;
            }
        }
    }
}
