using System.Collections;
using UnityEngine;

public class WalkBehavior : MonoBehaviour
{
    [Header("Walk Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float pauseMinTime = 0.5f;
    [SerializeField] private float pauseMaxTime = 2f;
    [SerializeField] private float targetReachDistance = 0.5f;
    
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationSmoothing = 0.1f;
    
    private Collider walkBounds;
    private Vector3 targetPosition;
    private bool isWalking = true;
    public bool isDetectingEmote = false;
    private Coroutine walkCoroutine;
    
    // Animation components
    private float currentSpeed = 0f;
    private Vector3 lastPosition;
    
    // Animation parameter names (make sure these match your Animator Controller)
    private const string SPEED_PARAM = "Speed";
    private const string IS_WALKING_PARAM = "IsWalking";
    
    private void Awake()
    {
        if (animator == null)
        {
            Debug.LogWarning($"No Animator found on {gameObject.name}. Animation will not work.");
        }
        
        lastPosition = transform.position;
    }
    

    
    public void Initialize(Collider bounds, float speed = 2f)
    {
        walkBounds = bounds;
        walkSpeed = speed;
        
        // Set initial position within bounds
        SetRandomPositionInBounds();
        
        // Start walking behavior
        StartWalking();
    }
    
    public void StartWalking()
    {
        if (walkCoroutine == null && walkBounds != null)
        {
            isWalking = true;
            walkCoroutine = StartCoroutine(WalkBehaviorCoroutine());
        }
    }
    
    public void StopWalking()
    {
        isWalking = false;
        if (walkCoroutine != null)
        {
            StopCoroutine(walkCoroutine);
            walkCoroutine = null;
        }
        
        // Ensure animation goes to idle
        if (animator != null)
        {
            animator.SetFloat(SPEED_PARAM, 0f);
            animator.SetBool(IS_WALKING_PARAM, false);
        }
    }
    
    public void SetWalkSpeed(float speed)
    {
        walkSpeed = speed;
    }
    
    private void SetRandomPositionInBounds()
    {
        if (walkBounds == null) return;
        
        Vector3 randomPoint = GetRandomPointInBounds();
        transform.position = randomPoint;
        lastPosition = transform.position;
        SetNewTarget();
    }
    
    private Vector3 GetRandomPointInBounds()
    {
        if (walkBounds == null) return transform.position;
        
        Bounds bounds = walkBounds.bounds;
        
        // Generate random point within bounds
        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y, // Keep Y at bounds center
            Random.Range(bounds.min.z, bounds.max.z)
        );
        
        // Ensure the point is actually inside the collider
        Vector3 closestPoint = walkBounds.ClosestPoint(randomPoint);
        
        // If the closest point is significantly different, use it instead
        if (Vector3.Distance(randomPoint, closestPoint) > 0.1f)
        {
            randomPoint = closestPoint;
        }
        
        return randomPoint;
    }

    public void SetNewTarget(Transform target = null)
    {
        targetPosition = target == null ? GetRandomPointInBounds() : target.transform.position;
    }
    
    private IEnumerator WalkBehaviorCoroutine()
    {
        while (isWalking && walkBounds != null)
        {
            // Move towards target
            while (Vector3.Distance(transform.position, targetPosition) > targetReachDistance)
            {
                if (!isWalking) yield break;
                
                Vector3 direction = (targetPosition - transform.position).normalized;
                Vector3 newPosition = transform.position + direction * walkSpeed * Time.deltaTime;
                
                // Ensure we stay within bounds
                Vector3 clampedPosition = walkBounds.ClosestPoint(newPosition);
                transform.position = clampedPosition;
                
                // Rotate to face movement direction
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
                
                // Update animation parameters while moving
                if (animator != null)
                {
                    // Calculate actual movement speed
                    float actualSpeed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
                    lastPosition = transform.position;
                    
                    // Smooth the speed value for better animation blending
                    currentSpeed = Mathf.Lerp(currentSpeed, actualSpeed, animationSmoothing);
                    
                    // Normalize speed (0 = idle, 1 = full walk speed)
                    float normalizedSpeed = Mathf.Clamp01(currentSpeed / walkSpeed);
                    
                    // Update animator parameters
                    animator.SetFloat(SPEED_PARAM, normalizedSpeed);
                    animator.SetBool(IS_WALKING_PARAM, true);
                }
                
                yield return null;
            }
            
            // Set animation to idle during pause
            if (animator != null)
            {
                animator.SetFloat(SPEED_PARAM, 0f);
                animator.SetBool(IS_WALKING_PARAM, false);
            }
            
            // Brief pause at destination
            float pauseTime = Random.Range(pauseMinTime, pauseMaxTime);
            yield return new WaitForSeconds(pauseTime);
            
            // Set new target
            SetNewTarget();
        }
    }
    
    private void OnDestroy()
    {
        StopWalking();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (walkBounds != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(walkBounds.bounds.center, walkBounds.bounds.size);
        }
        
        // Draw target position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
    }
}