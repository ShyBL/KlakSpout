using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;

public class ChatAvatar : MonoBehaviour
{
    private string username;
    private ChatMessage messageData;
    private float walkRadius;
    private float walkSpeed;
    private Vector3 originPosition;
    private Vector3 targetPosition;
    private bool isMoving = true;
    
    private TMP_Text nameTag;
    private GameObject nameTagObject;
    
    public void Initialize(string user, ChatMessage message, float radius, float speed, float nameHeight)
    {
        username = user;
        messageData = message;
        walkRadius = radius;
        walkSpeed = speed;
        originPosition = transform.position;
        
        CreateNameTag(nameHeight);
        ApplyAvatarEffects();
        SetNewTarget();
        StartCoroutine(WalkBehavior());
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
            // Regular user - random color as before
            nameTag.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        }
        
        // TODO: Handle additional badges from messageData.badges array
        // Example: Parse custom badges, channel-specific badges, etc.
    }
    
    void CreateNameTag(float height)
    {
        // Create name tag above avatar
        nameTagObject = new GameObject($"NameTag_{username}");
        nameTagObject.transform.SetParent(transform);
        nameTagObject.transform.localPosition = Vector3.up * height;
        
        // Add TextMeshPro component
        nameTag = nameTagObject.AddComponent<TextMeshPro>();
        nameTag.text = username;
        nameTag.fontSize = 2f;
        nameTag.alignment = TextAlignmentOptions.Center;
        // Color will be set in ApplyAvatarEffects() based on user status
        
        // Make name tag always face camera
        var lookAt = nameTagObject.AddComponent<LookAtConstraint>();
        lookAt.AddSource(new ConstraintSource { sourceTransform = Camera.main.transform, weight = 1f });
        lookAt.constraintActive = true;
    }
    
    void SetNewTarget()
    {
        // Generate random point within walk radius from origin
        Vector2 randomCircle = Random.insideUnitCircle * walkRadius;
        targetPosition = originPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
    
    IEnumerator WalkBehavior()
    {
        while (isMoving)
        {
            // Move towards target
            while (Vector3.Distance(transform.position, targetPosition) > 0.5f)
            {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * walkSpeed * Time.deltaTime;
                
                // Rotate to face movement direction
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
                
                yield return null;
            }
            
            // Brief pause at destination
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));
            
            // Set new target
            SetNewTarget();
        }
    }
    
    void OnDestroy()
    {
        isMoving = false;
    }
}