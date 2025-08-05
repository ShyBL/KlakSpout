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
    [SerializeField] private float animationTransitionSpeed = 5f;
    
    private Collider walkBounds;
    private Vector3 targetPosition;
    private bool isWalkingEnabled = true;
    private bool isCurrentlyMoving = false;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    
    // Animation parameter names
    private const string IS_WALKING_PARAM = "isWalking";
    
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"No Animator found on {gameObject.name}. Animation will not work.");
            }
        }
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
    
    private void Update()
    {
        if (!isWalkingEnabled || walkBounds == null) return;
        
        // Handle pause state
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                SetNewTarget();
            }
            return;
        }
        
        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // Check if we should be moving
        if (distanceToTarget > targetReachDistance)
        {
            // We should be moving
            if (!isCurrentlyMoving)
            {
                isCurrentlyMoving = true;
                UpdateAnimation(true);
            }
            
            // Move towards target
            Vector3 direction = (targetPosition - transform.position).normalized;
            Vector3 newPosition = transform.position + direction * (walkSpeed * Time.deltaTime);
            
            // Ensure we stay within bounds
            Vector3 clampedPosition = walkBounds.ClosestPoint(newPosition);
            transform.position = clampedPosition;
            
            // Rotate to face movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, animationTransitionSpeed * Time.deltaTime);
            }
        }
        else
        {
            // We've reached the target
            if (isCurrentlyMoving)
            {
                isCurrentlyMoving = false;
                UpdateAnimation(false);
                
                // Start pause
                isPaused = true;
                pauseTimer = Random.Range(pauseMinTime, pauseMaxTime);
            }
        }
    }
    
    public void StartWalking()
    {
        isWalkingEnabled = true;
        if (walkBounds != null)
        {
            SetNewTarget();
        }
    }
    
    public void StopWalking()
    {
        isWalkingEnabled = false;
        isCurrentlyMoving = false;
        isPaused = false;
        
        // Set animation to idle
        UpdateAnimation(false);
    }
    
    public void SetWalkSpeed(float speed)
    {
        walkSpeed = speed;
    }
    
    
    
    private void UpdateAnimation(bool walking)
    {
        if (animator != null)
        {
            animator.SetBool(IS_WALKING_PARAM, walking);
        }
    }
    
    private void SetRandomPositionInBounds()
    {
        if (walkBounds == null) return;
        
        Vector3 randomPoint = GetRandomPointInBounds();
        transform.position = randomPoint;
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
        
        // Draw current state
        Gizmos.color = isCurrentlyMoving ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}