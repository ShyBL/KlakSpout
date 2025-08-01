using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EmoteManager : MonoBehaviour
{
    [Header("Emote Settings")]
    [SerializeField] private GameObject emotePrefab;
    [SerializeField] private Collider spawnBounds;
    [SerializeField] private Transform emoteParent;
    
    [Header("Pool Settings")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxSize = 100;
    [SerializeField] private bool collectionCheck = true;
    
    [Header("Spawn Settings")]
    [SerializeField] private GameObject cameraToLook;
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private float emoteLifetime = 30f; // Cleanup emotes after 30 seconds
    
    private ObjectPool<GameObject> emotePool;
    private HashSet<GameObject> activeEmotes = new HashSet<GameObject>();
    private List<FallingEmote> landedEmotes = new List<FallingEmote>();
    
    private static EmoteManager instance;
    public static EmoteManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<EmoteManager>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePool();
    }
    
    private void Start()
    {
        // Subscribe to chat messages
        TwitchChatClient.OnMessageReceived += OnChatMessage;
    }
    
    private void OnDestroy()
    {
        TwitchChatClient.OnMessageReceived -= OnChatMessage;
        
        emotePool?.Clear();
        
        if (instance == this)
        {
            instance = null;
        }
    }
    
    private void InitializePool()
    {
        if (emoteParent == null)
        {
            GameObject emoteContainer = new GameObject("Emote Pool");
            emoteParent = emoteContainer.transform;
            emoteParent.SetParent(transform);
        }
        
        emotePool = new ObjectPool<GameObject>(
            createFunc: CreateEmote,
            actionOnGet: OnGetEmote,
            actionOnRelease: OnReleaseEmote,
            actionOnDestroy: OnDestroyEmote,
            collectionCheck: collectionCheck,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
        
        Debug.Log($"Emote pool initialized with capacity: {defaultCapacity}, max size: {maxSize}");
    }
    
    private GameObject CreateEmote()
    {
        if (emotePrefab == null)
        {
            Debug.LogError("Emote prefab is not assigned!");
            return null;
        }
        
        GameObject emote = Instantiate(emotePrefab, emoteParent);
        emote.SetActive(false);
        
        return emote;
    }
    
    private void OnGetEmote(GameObject emote)
    {
        if (emote != null)
        {
            emote.SetActive(true);
            activeEmotes.Add(emote);
        }
    }
    
    private void OnReleaseEmote(GameObject emote)
    {
        if (emote != null)
        {
            activeEmotes.Remove(emote);
            
            // Reset emote state
            FallingEmote fallingEmote = emote.GetComponent<FallingEmote>();
            if (fallingEmote != null)
            {
                fallingEmote.ResetEmote();
            }
            
            emote.SetActive(false);
            emote.transform.SetParent(emoteParent);
        }
    }
    
    private void OnDestroyEmote(GameObject emote)
    {
        if (emote != null)
        {
            activeEmotes.Remove(emote);
            Destroy(emote);
        }
    }
    
    private void OnChatMessage(ChatMessage message)
    {
        // Only spawn emotes for messages that have emotes
        if (message.hasEmotes && message.emotes != null && message.emotes.Length > 0)
        {
            SpawnEmotesFromMessage(message);
        }
    }
    
    private void SpawnEmotesFromMessage(ChatMessage message)
    {
        EmoteData[] emoteDataArray = EmoteData.FromEmoteInfoArray(message.emotes);
        
        foreach (EmoteData emoteData in emoteDataArray)
        {
            SpawnEmote(emoteData);
        }
        
        Debug.Log($"Spawned {emoteDataArray.Length} emotes from {message.username}");
    }
    
    public void SpawnEmote(EmoteData emoteData)
    {
        if (spawnBounds == null)
        {
            Debug.LogError("Spawn bounds not set!");
            return;
        }
        
        GameObject emoteObj = emotePool.Get();
        if (emoteObj == null) return;
        
        // Get random spawn position within bounds
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        // Initialize the falling emote
        FallingEmote fallingEmote = emoteObj.GetComponent<FallingEmote>();
        if (fallingEmote != null)
        {
            fallingEmote.Initialize(emoteData, spawnPosition, cameraToLook.transform);
            
            // Set parent for organization
            emoteObj.transform.SetParent(transform);
            
            // Schedule cleanup
            StartCoroutine(CleanupEmoteAfterTime(emoteObj, emoteLifetime));
        }
        else
        {
            Debug.LogError("FallingEmote component not found on emote prefab!");
            ReturnEmoteToPool(emoteObj);
        }
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        Bounds bounds = spawnBounds.bounds;
        
        // Random X and Z within bounds, Y at specified height above bounds
        Vector3 spawnPosition = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.max.y + spawnHeight,
            Random.Range(bounds.min.z, bounds.max.z)
        );
        
        return spawnPosition;
    }
    
    public void OnEmoteLanded(FallingEmote emote)
    {
        if (!landedEmotes.Contains(emote))
        {
            landedEmotes.Add(emote);
            
            // Find closest avatar and make it move to the emote
            ChatAvatar closestAvatar = FindClosestAvatar(emote.transform.position);
            if (closestAvatar != null)
            {
                closestAvatar.MoveToEmote(emote);
            }
        }
    }
    
    private ChatAvatar FindClosestAvatar(Vector3 position)
    {
        ChatAvatar[] allAvatars = FindObjectsOfType<ChatAvatar>();
        ChatAvatar closestAvatar = null;
        float closestDistance = float.MaxValue;
        
        foreach (ChatAvatar avatar in allAvatars)
        {
            float distance = Vector3.Distance(avatar.transform.position, position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestAvatar = avatar;
            }
        }
        
        return closestAvatar;
    }
    
    public void ReturnEmoteToPool(FallingEmote emote)
    {
        if (emote != null)
        {
            ReturnEmoteToPool(emote.gameObject);
        }
    }
    
    public void ReturnEmoteToPool(GameObject emoteObj)
    {
        if (emoteObj != null && activeEmotes.Contains(emoteObj))
        {
            // Remove from landed emotes list
            FallingEmote fallingEmote = emoteObj.GetComponent<FallingEmote>();
            if (fallingEmote != null)
            {
                landedEmotes.Remove(fallingEmote);
            }
            
            emotePool.Release(emoteObj);
        }
    }
    
    private System.Collections.IEnumerator CleanupEmoteAfterTime(GameObject emoteObj, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        
        // Check if emote still exists and hasn't been collected
        if (emoteObj != null && activeEmotes.Contains(emoteObj))
        {
            FallingEmote fallingEmote = emoteObj.GetComponent<FallingEmote>();
            if (fallingEmote != null && !fallingEmote.IsBeingCollected)
            {
                Debug.Log($"Cleaning up uncollected emote: {fallingEmote.EmoteData.emoteName}");
                ReturnEmoteToPool(emoteObj);
            }
        }
    }
    
    public int GetActiveEmoteCount()
    {
        return activeEmotes.Count;
    }
    
    public int GetLandedEmoteCount()
    {
        return landedEmotes.Count;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (spawnBounds != null)
        {
            Gizmos.color = Color.cyan;
            Bounds bounds = spawnBounds.bounds;
            
            // Draw spawn area
            Vector3 spawnCenter = new Vector3(bounds.center.x, bounds.max.y + spawnHeight, bounds.center.z);
            Vector3 spawnSize = new Vector3(bounds.size.x, 0.5f, bounds.size.z);
            Gizmos.DrawWireCube(spawnCenter, spawnSize);
            
            // Draw drop zone
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}