using UnityEngine;
using System.Collections.Generic; // Required for using a Queue

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
    private Vector3 currentTargetPosition;
    private bool isWalkingEnabled = true;
    private bool isCurrentlyMoving = false;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    private Queue<Vector3> targetQueue = new ();
    private const string IS_WALKING_PARAM = "isWalking";

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
    
    public void Initialize(Collider bounds)
    {
        walkBounds = bounds;
        transform.position = GetRandomPointInBounds(); // Set initial position
        EnqueueRandomTarget(); // Add the first target to the queue
        StartWalking();
    }

    private void Update()
    {
        if (!isWalkingEnabled || walkBounds == null) return;

        // 1. Handle the pause state after reaching a target
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                // Pause is over, get the next target from the queue
                PrepareAndSetNextTarget();
            }
            return; // Don't do anything else while paused
        }

        if (!isCurrentlyMoving) return; // If we aren't moving, exit

        // 2. Handle the movement logic (your original code)
        float distanceToTarget = Vector3.Distance(transform.position, currentTargetPosition);

        if (distanceToTarget > targetReachDistance)
        {
            // Move towards target
            Vector3 direction = (currentTargetPosition - transform.position).normalized;
            transform.position = walkBounds.ClosestPoint(transform.position + direction * (walkSpeed * Time.deltaTime));

            // Rotate to face movement direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), animationTransitionSpeed * Time.deltaTime);
            }
        }
        else
        {
            // 3. Reached destination: stop moving and start the pause
            isCurrentlyMoving = false;
            UpdateAnimation(false);

            isPaused = true;
            pauseTimer = Random.Range(pauseMinTime, pauseMaxTime);
        }
    }
    

    /// <summary>
    /// PUBLIC: Adds a specific destination to the end of the queue.
    /// Call this from other scripts to give the character a new task without interrupting it.
    /// </summary>
    public void EnqueueTarget(Vector3 newTargetPosition)
    {
        // We clamp the position to the bounds to ensure it's a valid location
        targetQueue.Enqueue(walkBounds.ClosestPoint(newTargetPosition));
    }
    
    public void StartWalking()
    {
        isWalkingEnabled = true;
        if (!isCurrentlyMoving && !isPaused)
        {
            PrepareAndSetNextTarget();
        }
    }

    public void StopWalking()
    {
        isWalkingEnabled = false;
        isCurrentlyMoving = false;
        isPaused = false;
        targetQueue.Clear(); // Empty the queue of any pending tasks
        UpdateAnimation(false);
    }
    
    public void SetWalkSpeed(float speed)
    {
        walkSpeed = speed;
    }



    /// <summary>
    /// Prepares the system for the next target by managing the queue.
    /// </summary>
    private void PrepareAndSetNextTarget()
    {
        // If the queue is empty, add a new random wander target.
        // This creates the continuous wandering behavior.
        if (targetQueue.Count == 0)
        {
            EnqueueRandomTarget();
        }

        // Dequeue the next target and begin moving towards it.
        if (targetQueue.Count > 0)
        {
            currentTargetPosition = targetQueue.Dequeue();
            isCurrentlyMoving = true;
            UpdateAnimation(true);
        }
    }

    /// <summary>
    /// Adds a random point within the bounds to the queue.
    /// </summary>
    private void EnqueueRandomTarget()
    {
        if (walkBounds != null)
        {
            targetQueue.Enqueue(GetRandomPointInBounds());
        }
    }
    
    
    private void UpdateAnimation(bool walking)
    {
        if (animator != null)
        {
            animator.SetBool(IS_WALKING_PARAM, walking);
        }
    }

    private Vector3 GetRandomPointInBounds()
    {
        if (walkBounds == null) return transform.position;

        Bounds bounds = walkBounds.bounds;
        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y, // Keep Y at bounds center for simplicity
            Random.Range(bounds.min.z, bounds.max.z)
        );

        return walkBounds.ClosestPoint(randomPoint);
    }

    private void OnDrawGizmosSelected()
    {
        if (walkBounds != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(walkBounds.bounds.center, walkBounds.bounds.size);
        }

        // Draw the current target position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentTargetPosition, 0.5f);

        // Draw a sphere over the character showing its state
        Gizmos.color = isCurrentlyMoving ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}