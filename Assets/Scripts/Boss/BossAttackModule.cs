using UnityEngine;
using System.Collections;

public abstract class BossAttackModule : MonoBehaviour
{
    [Header("Base Attack Settings")]
    public string attackName = "Boss Attack";
    public float cooldown = 10f;
    public float castDuration = 2.5f;
    public float damage = 20f;
    public bool isEnabled = true;

    [HideInInspector] public float lastCastTime = -999f;

    public virtual bool CanExecute()
    {
        return isEnabled && (Time.time >= lastCastTime + cooldown);
    }

    public abstract IEnumerator ExecuteAttackRoutine(Transform boss, Transform targetPlayer, System.Action onComplete);
}
