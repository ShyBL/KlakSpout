using System.Collections;
using UnityEngine;

public class WalkBehavior : MonoBehaviour
{
    [Header("Walk Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float pauseMinTime = 0.5f;
    [SerializeField] private float pauseMaxTime = 2f;
    [SerializeField] private float targetReachDistance = 0.5f;
    
    private Collider walkBounds;
    private Vector3 targetPosition;
    private bool isWalking = true;
    private Coroutine walkCoroutine;
    
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
    
    private void SetNewTarget()
    {
        targetPosition = GetRandomPointInBounds();
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
                
                yield return null;
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