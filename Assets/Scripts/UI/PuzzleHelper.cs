using UnityEngine;

namespace Nemuri.UI
{
    public static class PuzzleHelper
    {
        /// <summary>
        /// Automatically checks the GameObject for any of the puzzle types (J, H, G, L, F, B) and opens it.
        /// </summary>
        public static void OpenPuzzle(GameObject puzzleGo)
        {
            if (puzzleGo == null)
            {
                Debug.LogWarning("[PuzzleHelper] Cannot open puzzle because GameObject is null.");
                return;
            }

            if (puzzleGo.TryGetComponent<OverlayImageMultiRotatablePuzzleUI>(out var imgMultiRotatablePuzzle) && imgMultiRotatablePuzzle.enabled)
            {
                imgMultiRotatablePuzzle.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var imgRotatablePuzzle) && imgRotatablePuzzle.enabled)
            {
                imgRotatablePuzzle.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var imgUnorderedPuzzle) && imgUnorderedPuzzle.enabled)
            {
                imgUnorderedPuzzle.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayPinPuzzleUI>(out var pinPuzzle) && pinPuzzle.enabled)
            {
                pinPuzzle.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayUnorderedPuzzleUI>(out var unorderedPuzzle) && unorderedPuzzle.enabled)
            {
                unorderedPuzzle.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayRotatablePuzzleUI>(out var rotatablePuzzle) && rotatablePuzzle.enabled)
            {
                rotatablePuzzle.OpenPuzzleUI();
            }
            // Fallback for components that might not be explicitly enabled
            else if (puzzleGo.TryGetComponent<OverlayImageMultiRotatablePuzzleUI>(out var fallbackB))
            {
                fallbackB.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var fallbackF))
            {
                fallbackF.OpenPuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var fallbackL))
            {
                fallbackL.OpenPuzzleUI();
            }
            else
            {
                Debug.LogWarning("[PuzzleHelper] No recognized active puzzle component found on " + puzzleGo.name);
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the puzzle types (J, H, G, L, F, B) and closes it.
        /// </summary>
        public static void ClosePuzzle(GameObject puzzleGo)
        {
            if (puzzleGo == null) return;

            if (puzzleGo.TryGetComponent<OverlayImageMultiRotatablePuzzleUI>(out var imgMultiRotatablePuzzle) && imgMultiRotatablePuzzle.enabled)
            {
                imgMultiRotatablePuzzle.ClosePuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var imgRotatablePuzzle) && imgRotatablePuzzle.enabled)
            {
                imgRotatablePuzzle.ClosePuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var imgUnorderedPuzzle) && imgUnorderedPuzzle.enabled)
            {
                imgUnorderedPuzzle.ClosePuzzleUI();
            }
            else if (puzzleGo.TryGetComponent<OverlayPinPuzzleUI>(out var pinPuzzle))
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
            else if (puzzleGo.TryGetComponent<OverlayImageMultiRotatablePuzzleUI>(out var fallbackB))
            {
                fallbackB.ClosePuzzleUI();
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the puzzle types (J, H, G, L, F, B) and subscribes to its OnPuzzleSolved callback.
        /// </summary>
        public static void RegisterOnPuzzleSolved(GameObject puzzleGo, System.Action callback)
        {
            if (puzzleGo == null || callback == null) return;

            if (puzzleGo.TryGetComponent<OverlayImageMultiRotatablePuzzleUI>(out var imgMultiRotatablePuzzle) && imgMultiRotatablePuzzle.enabled)
            {
                imgMultiRotatablePuzzle.OnPuzzleSolved += callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var imgRotatablePuzzle) && imgRotatablePuzzle.enabled)
            {
                imgRotatablePuzzle.OnPuzzleSolved += callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var imgUnorderedPuzzle) && imgUnorderedPuzzle.enabled)
            {
                imgUnorderedPuzzle.OnPuzzleSolved += callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayPinPuzzleUI>(out var pinPuzzle))
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
            else if (puzzleGo.TryGetComponent<OverlayImageMultiRotatablePuzzleUI>(out var fallbackB))
            {
                fallbackB.OnPuzzleSolved += callback;
            }
        }

        /// <summary>
        /// Automatically checks the GameObject for any of the puzzle types (J, H, G, L, F, B) and unsubscribes from its OnPuzzleSolved callback.
        /// </summary>
        public static void UnregisterOnPuzzleSolved(GameObject puzzleGo, System.Action callback)
        {
            if (puzzleGo == null || callback == null) return;

            if (puzzleGo.TryGetComponent<OverlayImageMultiRotatablePuzzleUI>(out var imgMultiRotatablePuzzle) && imgMultiRotatablePuzzle.enabled)
            {
                imgMultiRotatablePuzzle.OnPuzzleSolved -= callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayImageRotatablePuzzleUI>(out var imgRotatablePuzzle) && imgRotatablePuzzle.enabled)
            {
                imgRotatablePuzzle.OnPuzzleSolved -= callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayImageUnorderedPuzzleUI>(out var imgUnorderedPuzzle) && imgUnorderedPuzzle.enabled)
            {
                imgUnorderedPuzzle.OnPuzzleSolved -= callback;
            }
            else if (puzzleGo.TryGetComponent<OverlayPinPuzzleUI>(out var pinPuzzle))
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
            else if (puzzleGo.TryGetComponent<OverlayImageMultiRotatablePuzzleUI>(out var fallbackB))
            {
                fallbackB.OnPuzzleSolved -= callback;
            }
        }
    }
}
