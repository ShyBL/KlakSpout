using System.Collections.Generic;
using UnityEngine;

public class ChatAvatarManager : MonoBehaviour
{
    [Header("Avatar Prefabs")]
    public GameObject avatarPrefab;
    // TODO: Add special avatar prefabs
    // public GameObject subscriberAvatarPrefab;
    // public GameObject moderatorAvatarPrefab;
    // public GameObject vipAvatarPrefab;
    // public GameObject broadcasterAvatarPrefab;
    // public GameObject bitsCheerAvatarPrefab;
    public float spawnRadius = 10f;
    public float walkSpeed = 2f;
    public float walkRadius = 15f;
    
    [Header("Avatar Display")]
    public float nameTagHeight = 2f;
    
    private Dictionary<string, GameObject> activeAvatars = new Dictionary<string, GameObject>();
    private TwitchChatClient chatClient;
    
    void Start()
    {
        chatClient = FindObjectOfType<TwitchChatClient>();
        if (chatClient == null)
        {
            Debug.LogError("TwitchChatClient not found!");
            return;
        }
        
        TwitchChatClient.OnMessageReceived += OnChatMessage;
    }
    
    void OnDestroy()
    {
        TwitchChatClient.OnMessageReceived -= OnChatMessage;
    }
    
    void OnChatMessage(ChatMessage message)
    {
        string username = message.username.ToLower();
        
        // Check if avatar already exists
        if (activeAvatars.ContainsKey(username))
        {
            Debug.Log($"Avatar for {username} already exists, ignoring");
            // TODO: Could update existing avatar based on new message data
            // Example: Change avatar appearance if user gained subscriber status
            return;
        }
        
        // Handle different message types
        switch (message.type)
        {
            case MessageType.RegularChat:
                SpawnAvatar(username, message);
                break;
                
            case MessageType.EmoteOnly:
                // TODO: Spawn avatar with special emote-focused appearance
                // Example: Larger avatar, emote particles, different color
                SpawnAvatar(username, message);
                break;
                
            case MessageType.BitsCheer:
                // TODO: Spawn avatar with bits celebration effects
                // Example: Golden avatar, coin particles, special animation
                SpawnAvatar(username, message);
                Debug.Log($"{username} cheered {message.bitsAmount} bits!");
                break;
                
            case MessageType.UserNotice:
                HandleUserNotice(message);
                break;
                
            case MessageType.System:
                // TODO: System messages might not need avatars
                // Example: Display system notification UI instead
                Debug.Log($"System message: {message.message}");
                break;
        }
    }
    
    void HandleUserNotice(ChatMessage message)
    {
        string username = message.username.ToLower();
        
        switch (message.noticeType)
        {
            case UserNoticeType.Sub:
                // TODO: Spawn special subscriber celebration avatar
                // Example: Crown effect, confetti, special subscriber prefab
                SpawnAvatar(username, message);
                Debug.Log($"{username} just subscribed!");
                break;
                
            case UserNoticeType.Resub:
                // TODO: Spawn avatar with resub celebration
                // Example: Larger crown, month counter, loyalty effects
                SpawnAvatar(username, message);
                Debug.Log($"{username} resubscribed for {message.subMonths} months!");
                break;
                
            case UserNoticeType.SubGift:
                // TODO: Spawn avatar with gift celebration
                // Example: Present box effect, gifting animation
                SpawnAvatar(username, message);
                Debug.Log($"{username} gifted a subscription!");
                break;
                
            case UserNoticeType.Raid:
                // TODO: Spawn multiple raider avatars or special raid leader
                // Example: Army of mini-avatars, raid banner, invasion effect
                SpawnAvatar(username, message);
                Debug.Log($"Raid from {message.raidFrom} with {message.raidViewers} viewers!");
                break;
                
            case UserNoticeType.BitsBadgeTier:
                // TODO: Spawn avatar with new bits badge celebration
                // Example: Badge upgrade animation, achievement popup
                SpawnAvatar(username, message);
                Debug.Log($"{username} earned a new bits badge!");
                break;
                
            case UserNoticeType.Other:
                // TODO: Handle other notice types
                SpawnAvatar(username, message);
                break;
        }
    }
    
    void SpawnAvatar(string username, ChatMessage message)
    {
        // Random spawn position within spawn radius
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // TODO: Select avatar prefab based on user status
        GameObject prefabToUse = avatarPrefab;
        
        // Check user badges and status
        if (message.isBroadcaster)
        {
            // TODO: Use special broadcaster avatar prefab
            // Example: Crown, special colors, larger size
            prefabToUse = avatarPrefab; // broadcasterAvatarPrefab;
        }
        else if (message.isModerator)
        {
            // TODO: Use moderator avatar prefab
            // Example: Sword icon, mod badge, special effects
            prefabToUse = avatarPrefab; // moderatorAvatarPrefab;
        }
        else if (message.isVip)
        {
            // TODO: Use VIP avatar prefab
            // Example: Diamond badge, premium effects
            prefabToUse = avatarPrefab; // vipAvatarPrefab;
        }
        else if (message.isSubscriber)
        {
            // TODO: Use subscriber avatar prefab
            // Example: Sub badge, different color, subscriber perks
            prefabToUse = avatarPrefab; // subscriberAvatarPrefab;
        }
        
        GameObject avatar = Instantiate(prefabToUse, spawnPosition, Quaternion.identity, transform);
        avatar.name = $"Avatar_{username}";
        
        // Add walking behavior
        ChatAvatar avatarScript = avatar.GetComponent<ChatAvatar>();
        if (avatarScript == null)
        {
            avatarScript = avatar.AddComponent<ChatAvatar>();
        }
        
        avatarScript.Initialize(username, message, walkRadius, walkSpeed, nameTagHeight);
        
        // Store reference
        activeAvatars[username] = avatar;
        
        Debug.Log($"Spawned avatar for {username} at {spawnPosition}");
    }
    
    public void RemoveAvatar(string username)
    {
        username = username.ToLower();
        if (activeAvatars.ContainsKey(username))
        {
            Destroy(activeAvatars[username]);
            activeAvatars.Remove(username);
        }
    }
    
    public void ClearAllAvatars()
    {
        foreach (var avatar in activeAvatars.Values)
        {
            if (avatar != null)
                Destroy(avatar);
        }
        activeAvatars.Clear();
    }
}