using System.Collections.Generic;
using UnityEngine;

public class ChatAvatarManager : MonoBehaviour
{
    [Header("Avatar Prefabs")]
    [Tooltip("Default avatar for regular users.")]
    public GameObject defaultAvatarPrefab;
    [Tooltip("Special avatar for the broadcaster.")]
    public GameObject broadcasterAvatarPrefab;
    [Tooltip("Special avatar for moderators.")]
    public GameObject moderatorAvatarPrefab;
    [Tooltip("Special avatar for VIPs.")]
    public GameObject vipAvatarPrefab;
    [Tooltip("Special avatar for subscribers.")]
    public GameObject subscriberAvatarPrefab;
    
    [Header("Pooling")]
    [Tooltip("The number of avatars of each type to create on startup.")]
    public int initialPoolSize = 10;
    
    [Header("Avatar Configuration")]
    [Tooltip("The maximum number of active avatars allowed in the scene at once.")]
    public int maxAvatars = 50;
    [Tooltip("The time in seconds before an inactive avatar is removed.")]
    public float avatarTimeout = 300f;

    [Header("Avatar Movement")]
    public float spawnRadius = 10f;
    public float walkSpeed = 2f;
    public float walkRadius = 15f;

    [Header("Avatar Display")]
    public float nameTagHeight = 2f;

    // --- Pooling System ---
    private Dictionary<GameObject, Queue<GameObject>> _avatarPools;
    private readonly Dictionary<string, GameObject> _activeAvatars = new Dictionary<string, GameObject>();
    private readonly List<string> _avatarSpawnOrder = new List<string>();
    private TwitchChatClient _chatClient;

    #region Unity Lifecycle & Pool Initialization
    void Start()
    {
        InitializePools();
        
        _chatClient = FindObjectOfType<TwitchChatClient>();
        if (_chatClient == null)
        {
            Debug.LogError("TwitchChatClient not found in the scene!");
            this.enabled = false;
            return;
        }
        TwitchChatClient.OnMessageReceived += OnChatMessage;
    }

    void InitializePools()
    {
        _avatarPools = new Dictionary<GameObject, Queue<GameObject>>();
        var prefabs = new List<GameObject> { defaultAvatarPrefab, broadcasterAvatarPrefab, moderatorAvatarPrefab, vipAvatarPrefab, subscriberAvatarPrefab };

        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;
            
            var pool = new Queue<GameObject>();
            for (int i = 0; i < initialPoolSize; i++)
            {
                var avatar = CreateNewAvatarForPool(prefab);
                pool.Enqueue(avatar);
            }
            _avatarPools[prefab] = pool;
        }
    }

    GameObject CreateNewAvatarForPool(GameObject prefab)
    {
        var avatar = Instantiate(prefab, transform);
        var identity = avatar.GetComponent<AvatarPoolIdentity>();
        if (identity == null)
        {
            identity = avatar.AddComponent<AvatarPoolIdentity>();
            Debug.LogWarning($"Prefab '{prefab.name}' was missing AvatarPoolIdentity component. It has been added automatically.");
        }
        identity.OriginalPrefab = prefab;
        avatar.SetActive(false);
        return avatar;
    }

    void OnDestroy()
    {
        if (_chatClient != null)
        {
            TwitchChatClient.OnMessageReceived -= OnChatMessage;
        }
    }
    #endregion

    void OnChatMessage(ChatMessage message)
    {
        if (string.IsNullOrEmpty(message.username)) return;

        string username = message.username.ToLower();

        if (_activeAvatars.TryGetValue(username, out GameObject existingAvatarGO))
        {
            var existingAvatar = existingAvatarGO.GetComponent<ChatAvatar>();
            if (existingAvatar != null)
            {
                existingAvatar.Refresh(message);
                _avatarSpawnOrder.Remove(username);
                _avatarSpawnOrder.Add(username);
            }
            return;
        }

        if (_activeAvatars.Count >= maxAvatars)
        {
            RemoveOldestAvatar();
        }

        ActivateAvatar(username, message);
    }

    void ActivateAvatar(string username, ChatMessage message)
    {
        GameObject prefabToUse = SelectAvatarPrefab(message);
        if (prefabToUse == null)
        {
            Debug.LogWarning("No suitable avatar prefab found. Cannot spawn avatar.");
            return;
        }

        GameObject avatarGO = GetFromPool(prefabToUse);
        
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        avatarGO.transform.position = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        avatarGO.transform.rotation = Quaternion.identity;
        avatarGO.name = $"Avatar_{username}";

        var avatarScript = avatarGO.GetComponent<ChatAvatar>();
        if (avatarScript != null)
        {
            avatarScript.Initialize(username, message, walkRadius, walkSpeed, nameTagHeight, this);
        }

        _activeAvatars[username] = avatarGO;
        _avatarSpawnOrder.Add(username);
    }
    
    #region Pooling Logic
    private GameObject GetFromPool(GameObject prefab)
    {
        if (!_avatarPools.ContainsKey(prefab))
        {
             Debug.LogError($"Prefab {prefab.name} does not have an initialized pool.");
             return null;
        }
        var pool = _avatarPools[prefab];
        GameObject avatar;

        if (pool.Count > 0)
        {
            avatar = pool.Dequeue();
        }
        else
        {
            avatar = CreateNewAvatarForPool(prefab);
        }

        avatar.SetActive(true);
        return avatar;
    }

    private void ReturnToPool(GameObject avatar)
    {
        var identity = avatar.GetComponent<AvatarPoolIdentity>();
        if (identity != null && _avatarPools.ContainsKey(identity.OriginalPrefab))
        {
            avatar.SetActive(false);
            _avatarPools[identity.OriginalPrefab].Enqueue(avatar);
        }
        else
        {
            Debug.LogWarning($"Avatar {avatar.name} is not a pooled object. Destroying instead.");
            Destroy(avatar);
        }
    }
    #endregion

    #region Avatar Management
    public void RemoveAvatar(string username)
    {
        username = username.ToLower();
        if (_activeAvatars.TryGetValue(username, out GameObject avatar))
        {
            ReturnToPool(avatar); 
            _activeAvatars.Remove(username);
            _avatarSpawnOrder.Remove(username);
        }
    }
    
    private void RemoveOldestAvatar()
    {
        if (_avatarSpawnOrder.Count > 0)
        {
            string oldestUser = _avatarSpawnOrder[0];
            Debug.Log($"Max avatars reached. Returning oldest avatar to pool: {oldestUser}");
            RemoveAvatar(oldestUser);
        }
    }

    public void ClearAllAvatars()
    {
        var usernames = new List<string>(_activeAvatars.Keys);
        foreach (var username in usernames)
        {
            RemoveAvatar(username);
        }
    }

    GameObject SelectAvatarPrefab(ChatMessage message)
    {
        if (message.isBroadcaster && broadcasterAvatarPrefab != null) return broadcasterAvatarPrefab;
        if (message.isModerator && moderatorAvatarPrefab != null) return moderatorAvatarPrefab;
        if (message.isVip && vipAvatarPrefab != null) return vipAvatarPrefab;
        if (message.isSubscriber && subscriberAvatarPrefab != null) return subscriberAvatarPrefab;
        return defaultAvatarPrefab;
    }
    #endregion
}