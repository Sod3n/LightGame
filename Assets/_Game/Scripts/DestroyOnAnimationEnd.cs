using UnityEngine;

public class DestroyOnAnimationEnd : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private float _previousNormalizedTime;
    private bool _hasStarted;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null)
        {
            Destroy(gameObject);
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime;

        if (_hasStarted)
        {
            // Detect completion: either reached end (non-looping) or wrapped around (looping)
            if ((normalizedTime >= 0.95f && !animator.IsInTransition(0)) ||
                normalizedTime < _previousNormalizedTime)
            {
                Destroy(gameObject);
                return;
            }
        }

        _previousNormalizedTime = normalizedTime;
        _hasStarted = true;
    }
}
