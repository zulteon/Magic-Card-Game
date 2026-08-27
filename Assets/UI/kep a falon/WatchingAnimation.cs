using System.Collections;
using UnityEngine;

public class WatchingAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Random idõ")]
    [SerializeField] private float minDelay = 5f;
    [SerializeField] private float maxDelay = 12f;

    [Header("Animáció")]
    [SerializeField] private float animationSpeed = 1f;

    private IEnumerator Start()
    {
        animator.speed = animationSpeed;

        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minDelay, maxDelay)
            );

            animator.SetBool("Look", true);

            yield return null;

            animator.SetBool("Look", false);
        }
    }
}