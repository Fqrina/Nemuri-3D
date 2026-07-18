using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HoverAnimationController : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        PlayHoverAnimation();
    }

    void OnEnable()
    {
        // Ensure animation plays when object is re-enabled
        PlayHoverAnimation();
    }

    public void PlayHoverAnimation()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            // Play Ferry on base layer (layer index 0)
            animator.Play("Ferry", 0, 0f);

            // If a second layer exists for Robe, set its weight and play the state
            if (animator.layerCount > 1)
            {
                animator.SetLayerWeight(1, 1f);
                animator.Play("Robe", 1, 0f);
            }
        }
    }
}
