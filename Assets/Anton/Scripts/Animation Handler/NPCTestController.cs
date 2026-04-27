using Unity.VisualScripting;
using UnityEngine;

public class NPCTestController : MonoBehaviour
{
    [SerializeField] private UniversalClipAnimator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Reset()
    {
        animator = GetComponent<UniversalClipAnimator>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        animator.Play("Idle");
    }
}
