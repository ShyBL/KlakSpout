using UnityEngine;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class FallingEmote : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private EmoteData emoteData;
    private bool hasLanded = false;
    private bool isBeingCollected = false;
    
    public EmoteData EmoteData => emoteData;
    public bool HasLanded => hasLanded;
    public bool IsBeingCollected => isBeingCollected;
    
    private EmoteManager emoteManager;
    
    private void Awake()
    {
        emoteManager = FindObjectOfType<EmoteManager>();
        if (emoteManager == null)
        {
            Debug.LogError("EmoteManager not found!");
            return;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void Initialize(EmoteData data, Vector3 spawnPosition, Transform cameraToLook)
    {
        emoteData = data;
        transform.position = spawnPosition;
        hasLanded = false;
        isBeingCollected = false;
        
        // Load emote image
        emoteManager.LoadEmoteImage(emoteData.imageUrl, emoteData.emoteName, spriteRenderer);
        
        // Set up camera look constraint
        var lookAt = GetComponentInChildren<LookAtConstraint>();
        if (lookAt != null)
        {
            lookAt.AddSource(new ConstraintSource { sourceTransform = cameraToLook.transform, weight = 1f });
            lookAt.constraintActive = true;
        }
    }
    
    public void ResetEmote()
    {
        // Reset state for pooling
        emoteData = new EmoteData();
        hasLanded = false;
        isBeingCollected = false;
        
        // Clear sprite
        emoteManager.ClearSprite(spriteRenderer);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check if we hit the ground
        if (!hasLanded && collision.gameObject.CompareTag("Ground"))
        {
            hasLanded = true;
            OnLanded();
        }
    }
    
    private void OnLanded()
    {
        // Notify the emote manager that we've landed
        emoteManager.OnEmoteLanded(this);
        
        Debug.Log($"Emote {emoteData.emoteName} landed at {transform.position}");
    }
    
    // private void OnTriggerEnter(Collider other)
    // {
    //     // Check if an avatar is collecting this emote
    //     ChatAvatar avatar = other.GetComponent<ChatAvatar>();
    //     if (avatar != null && !isBeingCollected)
    //     {
    //         isBeingCollected = true;
    //         avatar.CollectEmote(this);
    //     }
    // }
    
    public void OnCollected()
    {
        // Called when avatar successfully collects this emote
        emoteManager.ReturnEmoteToPool(this);
    }
}