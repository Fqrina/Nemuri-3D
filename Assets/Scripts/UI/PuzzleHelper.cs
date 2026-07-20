using UnityEngine;

namespace Nemuri.UI
{
    public static class PuzzleHelper
    {
        /// <summary>
        /// Automatically checks the GameObject for any of the puzzle types (J, H, G, L, F) and opens it.
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
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var imgUnorderedPuzzle))
            {
                imgUnorderedPuzzle.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var imgRotatablePuzzle))
            {
                imgRotatablePuzzle.OpenPuzzleUI();
            }
            else
            {
                Debug.LogWarning("[PuzzleHelper] No recognized puzzle component found on " + puzzleGo.name);
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the puzzle types (J, H, G, L, F) and closes it.
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
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var imgUnorderedPuzzle))
            {
                imgUnorderedPuzzle.ClosePuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var imgRotatablePuzzle))
            {
                imgRotatablePuzzle.ClosePuzzleUI();
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the puzzle types (J, H, G, L, F) and subscribes to its OnPuzzleSolved callback.
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
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var imgUnorderedPuzzle))
            {
                imgUnorderedPuzzle.OnPuzzleSolved += callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var imgRotatablePuzzle))
            {
                imgRotatablePuzzle.OnPuzzleSolved += callback;
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the puzzle types (J, H, G, L, F) and unsubscribes from its OnPuzzleSolved callback.
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
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var imgUnorderedPuzzle))
            {
                imgUnorderedPuzzle.OnPuzzleSolved -= callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var imgRotatablePuzzle))
            {
                imgRotatablePuzzle.OnPuzzleSolved -= callback;
            }
        }
    }
}
