using System;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using System.Collections;
using Random = UnityEngine.Random;

public class ChatAvatar : MonoBehaviour
{
    public ChatMessage messageData;
    
    [Header("Visuals")]
    [SerializeField] private Renderer avatarRenderer;
    [SerializeField] public Transform avatarTransform;
    [SerializeField] private BlendShapeController avatarBlendShape;
    [SerializeField] private float growFactor = 0.5f;
    [SerializeField] private float maxScale = 3f;
    
    [Header("Despawn Settings")]
    [SerializeField] private float despawnTimeMinutes = 10f;
    [SerializeField] private GameObject nameTagObject;
    
    private string username;
    private DateTime lastActivityTime;
    private FallingEmote detectedEmote;

    [SerializeField] private bool isDetectingEmote;
    
    private TMP_Text nameTag;
    private RectTransform nameTageRec;
    
    private GameObject cameraToLook;
    private WalkBehavior walkBehavior;
    private Transform vipRockTarget;
    private AvatarFamily avatarFamily;
    
    public string Username => username;
    public DateTime LastActivityTime => lastActivityTime;
    
    public void Initialize(string user, ChatMessage message, Collider walkBounds, GameObject cameraToLook, AvatarFamily family)
    {
        username = user;
        messageData = message;
        lastActivityTime = DateTime.Now;
        this.cameraToLook = cameraToLook;
        avatarFamily = family;
        
        ApplyUniqueColor();
        
        CreateNameTag();
        ApplyAvatarEffects();
        
        SetupWalkBehavior(walkBounds);
    }

    private void ApplyUniqueColor()
    {
        if (avatarRenderer == null)
        {
            Debug.LogWarning("Avatar Renderer is not assigned!", this);
            return;
        }
        
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        avatarRenderer.GetPropertyBlock(propBlock);
        
        Random.InitState(username.GetHashCode());
        Color randomColor = Random.ColorHSV(0f, 1f, 0.15f, 0.30f, 0.9f, 1f);
        
        propBlock.SetColor("_BaseColor", randomColor);
        avatarRenderer.SetPropertyBlock(propBlock);
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
        
        // Reset walk behavior
        if (walkBehavior != null)
        {
            walkBehavior.StopWalking();
        }
        
        // Reset scale
        transform.localScale = avatarFamily.Scale;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        nameTag.transform.position = nameTagObject.transform.position;
    }
    
    public void MoveToEmote(FallingEmote emote)
    {
        if (walkBehavior != null)
        {
            // Set destination to emote position
            isDetectingEmote = true;
            detectedEmote = emote;
            walkBehavior.EnqueueTarget(emote.transform.position);
            Debug.Log($"{username} moving to collect emote: {emote.EmoteData.emoteName}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDetectingEmote && other.TryGetComponent(out FallingEmote emote))
        {
            if (emote == detectedEmote)
            {
                CollectEmote(detectedEmote);
            }
        }
    }

    public void CollectEmote(FallingEmote emote)
    {
        // Start eating sequence
        StartCoroutine(EatingSequence(emote));
    }
    
    public void MoveToVipRock(Transform vipRock)
    {
        if (walkBehavior != null)
        {
            // Set destination to VIP rock position
            walkBehavior.StopWalking();
            transform.SetParent(vipRock);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            //walkBehavior.EnqueueTarget(vipRock.position);
            //walkBehavior.StartWalking();
            vipRockTarget = vipRock; // Store reference for when we arrive
            
            CommandsManager commandsManager = FindObjectOfType<CommandsManager>();
            if (commandsManager != null)
            {
                commandsManager.OnAvatarMountedVipRock(username);
            }
            
            vipRockTarget = null; // Clear the target
            
            Debug.Log($"{username} moving to VIP rock");
        }
    }
    
    public bool CheckIfReachedVipRock(Vector3 reachedPosition)
    {
        if (vipRockTarget != null)
        {
            float distance = Vector3.Distance(reachedPosition, vipRockTarget.position);
            if (distance < 1f) // Close enough to VIP rock
            {
                // We've reached the VIP rock - stop walking and mount it
                walkBehavior.StopWalking();
            
                // Parent to the rock and reset position
                transform.SetParent(vipRockTarget);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            
                Debug.Log($"{username} has mounted the VIP rock!");
            
                // Notify the command manager
                CommandsManager commandsManager = FindObjectOfType<CommandsManager>();
                if (commandsManager != null)
                {
                    commandsManager.OnAvatarMountedVipRock(username);
                }
            
                vipRockTarget = null; // Clear the target
                return true;
            }
        }
        return false;
    }
    
    private IEnumerator EatingSequence(FallingEmote emote)
    {
        int chewCount = Random.Range(2, 5); // Random number of chews
        float chewSpeed = 0.25f;
        
        // Get eating duration and pause walking for that long
        float eatingDuration = avatarBlendShape.GetEatingDuration(chewCount, chewSpeed);
        walkBehavior.PauseForDuration(eatingDuration);
        
        // Start eating animation
        yield return StartCoroutine(avatarBlendShape.EatAnimation(chewCount, chewSpeed));
        
        // Grow avatar slightly after eating, but clamp to maximum size
        Vector3 currentScale = avatarTransform.localScale;
        Vector3 newScale = currentScale + Vector3.one * growFactor;
        
        // Clamp each axis to the maximum scale
        newScale.x = Mathf.Min(newScale.x, maxScale);
        newScale.y = Mathf.Min(newScale.y, maxScale);
        newScale.z = Mathf.Min(newScale.z, maxScale);
        
        avatarTransform.localScale = newScale;
        Vector3 nameTagOffset = new Vector3(0, newScale.y, 0);
        nameTag.transform.position = avatarTransform.position + nameTagOffset;


        Debug.Log($"{username} ate emote: {emote.EmoteData.emoteName} with {chewCount} chews - New scale: {avatarTransform.localScale.x:F2}");
        
        // Update activity time and cleanup
        lastActivityTime = DateTime.Now;
        emote.OnCollected();
        isDetectingEmote = false;
        
        // Walking will automatically resume after the pause duration
    }
    
    private void SetupWalkBehavior(Collider walkBounds)
    {
        walkBehavior = GetComponent<WalkBehavior>();
        if (walkBehavior == null)
        {
            walkBehavior = gameObject.AddComponent<WalkBehavior>();
        }
        
        walkBehavior.Initialize(walkBounds);
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
    
    void CreateNameTag()
    {
        // Add TextMeshPro component
        nameTag = nameTagObject.GetComponent<TextMeshPro>();
        nameTag.text = username;
        // Color will be set in ApplyAvatarEffects() based on user status
        nameTageRec = nameTag.gameObject.transform as RectTransform;
        // Make name tag always face camera
        if (cameraToLook != null)
        {
            var lookAt = nameTagObject.GetComponent<LookAtConstraint>();
            lookAt.AddSource(new ConstraintSource { sourceTransform = cameraToLook.transform, weight = 1f });
            lookAt.constraintActive = true;
        }
    }
}