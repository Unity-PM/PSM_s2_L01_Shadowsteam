using UnityEngine;

public class NPCTestController : MonoBehaviour
{
    [SerializeField] private DynamicAnimator animator;

    void Awake()
    {
        animator ??= GetComponent<DynamicAnimator>();
    }

    void Start()
    {
        if (animator != null)
            animator.Play("Idle");
    }
}
