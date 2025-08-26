using UnityEngine;

public class AnimationCenter : MonoBehaviour
{
    public static AnimationCenter Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlaySummonAnimation(string minionName)
    {
        Debug.Log($"Summon animation: {minionName}");
        // TODO: Animator trigger, particle effect, UI update
    }

    public void PlayDeathAnimation(string minionName)
    {
        Debug.Log($"Death animation: {minionName}");
        // TODO: Animator trigger, particle effect
    }

    public void PlayBuffAnimation(string minionName, string buffName)
    {
        Debug.Log($"Buff animation: {minionName} gets {buffName}");
        // TODO: Animator trigger, VFX
    }
}
