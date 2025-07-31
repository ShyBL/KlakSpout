using System;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using Random = UnityEngine.Random;

public class ChatAvatar : MonoBehaviour
{
    [Header("Despawn Settings")]
    [SerializeField] private float despawnTimeMinutes = 10f;
    [SerializeField] private GameObject nameTagObject;
    
    private string username;
    private ChatMessage messageData;
    private DateTime lastActivityTime;
    
    private TMP_Text nameTag;
    private GameObject cameraToLook;
    private WalkBehavior walkBehavior;
    
    public string Username => username;
    public DateTime LastActivityTime => lastActivityTime;
    
    public void Initialize(string user, ChatMessage message, Collider walkBounds, float walkSpeed, float nameHeight, GameObject cameraToLook)
    {
        username = user;
        messageData = message;
        lastActivityTime = DateTime.Now;
        this.cameraToLook = cameraToLook;
        
        SetupNameTag(nameHeight);
        ApplyAvatarEffects();
        SetupWalkBehavior(walkBounds, walkSpeed);
    }
    
    public void UpdateActivity(ChatMessage newMessage)
    {
        messageData = newMessage;
        lastActivityTime = DateTime.Now;
        
        // Update visual effects based on new message
        ApplyAvatarEffects();
        
        Debug.Log($"Updated activity for {username}");
    }
    
    public bool ShouldDespawn()
    {
        TimeSpan timeSinceLastActivity = DateTime.Now - lastActivityTime;
        return timeSinceLastActivity.TotalMinutes >= despawnTimeMinutes;
    }
    
    public void ResetAvatar()
    {
        // Reset all avatar state for pooling
        username = "";
        lastActivityTime = DateTime.MinValue;
        
        // Destroy name tag if it exists
        if (nameTagObject != null)
        {
            nameTagObject = null;
            nameTag = null;
        }
        
        // Reset walk behavior
        if (walkBehavior != null)
        {
            walkBehavior.StopWalking();
        }
    }
    
    private void SetupWalkBehavior(Collider walkBounds, float walkSpeed)
    {
        walkBehavior = GetComponent<WalkBehavior>();
        if (walkBehavior == null)
        {
            walkBehavior = gameObject.AddComponent<WalkBehavior>();
        }
        
        walkBehavior.Initialize(walkBounds, walkSpeed);
    }
    
    void ApplyAvatarEffects()
    {
        // Handle different message types
        switch (messageData.type)
        {
            case MessageType.RegularChat:
                // TODO: Standard avatar appearance
                break;
                
            case MessageType.EmoteOnly:
                // TODO: Add emote effects to avatar
                // Example: Floating emote particles, bounce animation
                break;
                
            case MessageType.BitsCheer:
                // TODO: Add bits celebration effects
                // Example: Golden glow, coin particles, celebration animation
                // Could scale effects based on messageData.bitsAmount
                break;
                
            case MessageType.UserNotice:
                HandleUserNoticeEffects();
                break;
        }
        
        // Apply badge-based effects
        ApplyBadgeEffects();
        
        // Handle emotes if present
        if (messageData.hasEmotes)
        {
            // TODO: Add emote-specific effects
            // Example: Display emotes above avatar, emote trail
            Debug.Log($"{username} used {messageData.emotes.Length} emotes");
        }
    }
    
    void HandleUserNoticeEffects()
    {
        switch (messageData.noticeType)
        {
            case UserNoticeType.Sub:
            case UserNoticeType.Resub:
                // TODO: Add subscription celebration effects
                // Example: Confetti particles, crown effect, special animation
                // For resub, could show month count: messageData.subMonths
                break;
                
            case UserNoticeType.SubGift:
                // TODO: Add gift celebration effects
                // Example: Present box animation, gift particles
                break;
                
            case UserNoticeType.Raid:
                // TODO: Add raid effects
                // Example: Invasion particles, army banner
                // Could scale based on messageData.raidViewers
                break;
                
            case UserNoticeType.BitsBadgeTier:
                // TODO: Add bits badge tier celebration
                // Example: Badge upgrade animation, achievement effect
                break;
        }
    }
    
    void ApplyBadgeEffects()
    {
        if (nameTag == null) return;
        
        if (messageData.isBroadcaster)
        {
            // TODO: Apply broadcaster effects
            // Example: Crown above nameTag, special color, larger size
            nameTag.color = Color.red; // Temporary broadcaster indicator
        }
        else if (messageData.isModerator)
        {
            // TODO: Apply moderator effects
            // Example: Sword icon, mod badge, green nameTag
            nameTag.color = Color.green; // Temporary moderator indicator
        }
        else if (messageData.isVip)
        {
            // TODO: Apply VIP effects
            // Example: Diamond icon, purple nameTag, special glow
            nameTag.color = Color.magenta; // Temporary VIP indicator
        }
        else if (messageData.isSubscriber)
        {
            // TODO: Apply subscriber effects
            // Example: Sub badge, special color, subscriber perks
            nameTag.color = Color.cyan; // Temporary subscriber indicator
        }
        else
        {
            // Regular user - random color based on username for consistency
            Random.InitState(username.GetHashCode());
            nameTag.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        }
        
        // TODO: Handle additional badges from messageData.badges array
        // Example: Parse custom badges, channel-specific badges, etc.
    }
    
    void SetupNameTag(float height)
    {
        // Add TextMeshPro component
        nameTag = nameTagObject.GetComponent<TextMeshPro>();
        nameTag.text = username;
        
        // Make name tag always face camera
        if (cameraToLook != null)
        {
            var lookAt = nameTagObject.GetComponent<LookAtConstraint>();
            lookAt.AddSource(new ConstraintSource { sourceTransform = cameraToLook.transform, weight = 1f });
            lookAt.constraintActive = true;
        }
    }
}