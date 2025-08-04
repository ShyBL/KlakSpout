using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatAvatarManager : MonoBehaviour
{
    [Header("Bounds Settings")]
    [SerializeField] private Collider spawnBounds;
    [SerializeField] private Collider walkBounds;
    
    [Header("Avatar Settings")]
    [SerializeField] private GameObject cameraToLook;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float nameTagHeight = 2f;
    
    [Header("Despawn Management")]
    [SerializeField] private float despawnCheckInterval = 30f; // Check every 30 seconds
    
    private Dictionary<string, ChatAvatar> activeAvatars = new Dictionary<string, ChatAvatar>();
    private TwitchChatClient chatClient;
    private AvatarPoolManager poolManager;
    
    void Start()
    {
        chatClient = FindObjectOfType<TwitchChatClient>();
        if (chatClient == null)
        {
            Debug.LogError("TwitchChatClient not found!");
            return;
        }
        
        poolManager = FindObjectOfType<AvatarPoolManager>();
        if (poolManager == null)
        {
            Debug.LogError("AvatarPoolManager not found!");
            return;
        }
        
        // Use walkBounds as spawnBounds if not set
        if (spawnBounds == null)
        {
            spawnBounds = walkBounds;
        }
        
        chatClient.OnMessageReceived += OnChatMessage;
        
        // Start despawn management coroutine
        StartCoroutine(DespawnManagementCoroutine());
    }
    
    void OnDestroy()
    {
        chatClient.OnMessageReceived -= OnChatMessage;
    }
    
    void OnChatMessage(ChatMessage message)
    {
        string username = message.username.ToLower();
        
        // Check if avatar already exists
        if (activeAvatars.ContainsKey(username))
        {
            // Update existing avatar activity
            activeAvatars[username].UpdateActivity(message);
            Debug.Log($"Updated activity for existing avatar: {username}");
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
                SpawnAvatar(username, message);
                break;
                
            case MessageType.BitsCheer:
                // TODO: Spawn avatar with bits celebration effects
                SpawnAvatar(username, message);
                Debug.Log($"{username} cheered {message.bitsAmount} bits!");
                break;
                
            case MessageType.UserNotice:
                HandleUserNotice(message);
                break;
                
            case MessageType.System:
                // TODO: System messages might not need avatars
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
                SpawnAvatar(username, message);
                Debug.Log($"{username} just subscribed!");
                break;
                
            case UserNoticeType.Resub:
                SpawnAvatar(username, message);
                Debug.Log($"{username} resubscribed for {message.subMonths} months!");
                break;
                
            case UserNoticeType.SubGift:
                SpawnAvatar(username, message);
                Debug.Log($"{username} gifted a subscription!");
                break;
                
            case UserNoticeType.Raid:
                SpawnAvatar(username, message);
                Debug.Log($"Raid from {message.raidFrom} with {message.raidViewers} viewers!");
                break;
                
            case UserNoticeType.BitsBadgeTier:
                SpawnAvatar(username, message);
                Debug.Log($"{username} earned a new bits badge!");
                break;
                
            case UserNoticeType.Other:
                SpawnAvatar(username, message);
                break;
        }
    }
    
    void SpawnAvatar(string username, ChatMessage message)
    {
        if (spawnBounds == null || walkBounds == null)
        {
            Debug.LogError("Spawn bounds or walk bounds not set!");
            return;
        }
        
        // Get avatar from pool
        GameObject avatarObj = poolManager.GetAvatar();
        if (avatarObj == null)
        {
            Debug.LogWarning("Could not get avatar from pool!");
            return;
        }
        
        // Position avatar within spawn bounds
        Vector3 spawnPosition = GetRandomPointInBounds(spawnBounds);
        avatarObj.transform.position = spawnPosition;
        avatarObj.transform.SetParent(transform);
        avatarObj.name = $"Avatar_{username}";
        
        // Initialize avatar component
        ChatAvatar avatarScript = avatarObj.GetComponent<ChatAvatar>();
        if (avatarScript == null)
        {
            avatarScript = avatarObj.AddComponent<ChatAvatar>();
        }
        
        avatarScript.Initialize(username, message, walkBounds, walkSpeed, nameTagHeight, cameraToLook);
        
        // Store reference
        activeAvatars[username] = avatarScript;
        
        Debug.Log($"Spawned avatar for {username} at {spawnPosition}. Active avatars: {activeAvatars.Count}");
    }
    
    private Vector3 GetRandomPointInBounds(Collider bounds)
    {
        Bounds boundsBox = bounds.bounds;
        
        // Generate random point within bounds
        Vector3 randomPoint = new Vector3(
            Random.Range(boundsBox.min.x, boundsBox.max.x),
            boundsBox.center.y,
            Random.Range(boundsBox.min.z, boundsBox.max.z)
        );
        
        // Ensure the point is actually inside the collider
        Vector3 closestPoint = bounds.ClosestPoint(randomPoint);
        
        // If the closest point is significantly different, use it instead
        if (Vector3.Distance(randomPoint, closestPoint) > 0.1f)
        {
            randomPoint = closestPoint;
        }
        
        return randomPoint;
    }
    
    private IEnumerator DespawnManagementCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(despawnCheckInterval);
            
            // Check for avatars that should be despawned
            List<string> avatarsToRemove = new List<string>();
            
            foreach (var kvp in activeAvatars)
            {
                if (kvp.Value == null || kvp.Value.ShouldDespawn())
                {
                    avatarsToRemove.Add(kvp.Key);
                }
            }
            
            // Remove avatars that should be despawned
            foreach (string username in avatarsToRemove)
            {
                RemoveAvatar(username);
            }
            
            if (avatarsToRemove.Count > 0)
            {
                Debug.Log($"Despawned {avatarsToRemove.Count} inactive avatars. Active avatars: {activeAvatars.Count}");
            }
        }
    }
    
    public void RemoveAvatar(string username)
    {
        username = username.ToLower();
        if (activeAvatars.ContainsKey(username))
        {
            ChatAvatar avatar = activeAvatars[username];
            if (avatar != null)
            {
                // Return to pool instead of destroying
                poolManager.ReturnAvatar(avatar.gameObject);
            }
            
            activeAvatars.Remove(username);
        }
    }
    
    public void ClearAllAvatars()
    {
        List<string> allUsernames = new List<string>(activeAvatars.Keys);
        foreach (string username in allUsernames)
        {
            RemoveAvatar(username);
        }
        
        Debug.Log("Cleared all avatars");
    }
    
    public int GetActiveAvatarCount()
    {
        return activeAvatars.Count;
    }
    
    // Gizmos for visualizing bounds in scene view
    private void OnDrawGizmosSelected()
    {
        if (spawnBounds != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnBounds.bounds.center, spawnBounds.bounds.size);
        }
        
        if (walkBounds != null && walkBounds != spawnBounds)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(walkBounds.bounds.center, walkBounds.bounds.size);
        }
    }
}