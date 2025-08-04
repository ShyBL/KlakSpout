using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class AvatarPoolManager : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 100;
    [SerializeField] private bool collectionCheck = true;
    [SerializeField] private Transform poolParent;
    
    private ObjectPool<GameObject> avatarPool;
    private HashSet<GameObject> activeAvatars = new HashSet<GameObject>();
    
    private void Awake()
    {
        InitializePool();
    }
    
    private void InitializePool()
    {
        if (poolParent == null)
        {
            GameObject poolContainer = new GameObject("Avatar Pool");
            poolParent = poolContainer.transform;
            poolParent.SetParent(transform);
        }
        
        // Initialize Unity's ObjectPool
        avatarPool = new ObjectPool<GameObject>(
            createFunc: CreateAvatar,
            actionOnGet: OnGetAvatar,
            actionOnRelease: OnReleaseAvatar,
            actionOnDestroy: OnDestroyAvatar,
            collectionCheck: collectionCheck,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
        
        Debug.Log($"Avatar pool initialized with capacity: {defaultCapacity}, max size: {maxSize}");
    }
    
    // Pool callback: Create new avatar instance
    private GameObject CreateAvatar()
    {
        if (avatarPrefab == null)
        {
            Debug.LogError("Avatar prefab is not assigned!");
            return null;
        }
        
        GameObject avatar = Instantiate(avatarPrefab, poolParent);
        avatar.SetActive(false);
        
        return avatar;
    }
    
    // Pool callback: When avatar is retrieved from pool
    private void OnGetAvatar(GameObject avatar)
    {
        if (avatar != null)
        {
            avatar.SetActive(true);
            activeAvatars.Add(avatar);
        }
    }
    
    // Pool callback: When avatar is returned to pool
    private void OnReleaseAvatar(GameObject avatar)
    {
        if (avatar != null)
        {
            activeAvatars.Remove(avatar);
            ResetAvatarState(avatar);
            avatar.SetActive(false);
            avatar.transform.SetParent(poolParent);
        }
    }
    
    // Pool callback: When avatar is destroyed (pool overflow)
    private void OnDestroyAvatar(GameObject avatar)
    {
        if (avatar != null)
        {
            activeAvatars.Remove(avatar);
            Destroy(avatar);
        }
    }
    
    public GameObject GetAvatar()
    {
        return avatarPool.Get();
    }
    
    public void ReturnAvatar(GameObject avatar)
    {
        if (avatar == null) return;
        
        // Only release if it's actually from our pool
        if (activeAvatars.Contains(avatar))
        {
            avatarPool.Release(avatar);
        }
        else
        {
            Debug.LogWarning("Trying to return avatar that's not from this pool!");
        }
    }
    
    private void ResetAvatarState(GameObject avatar)
    {
        // Reset position and rotation
        avatar.transform.localPosition = Vector3.zero;
        avatar.transform.localRotation = Quaternion.identity;
        
        // Stop any walking behavior
        WalkBehavior walkBehavior = avatar.GetComponent<WalkBehavior>();
        if (walkBehavior != null)
        {
            walkBehavior.StopWalking();
        }
        
        // Reset ChatAvatar component if it exists
        ChatAvatar chatAvatar = avatar.GetComponent<ChatAvatar>();
        if (chatAvatar != null)
        {
            chatAvatar.ResetAvatar();
        }
    }
    
    public int GetActiveCount()
    {
        return activeAvatars.Count;
    }
    
    public int GetInactiveCount()
    {
        return avatarPool.CountInactive;
    }
    
    public int GetTotalCount()
    {
        return avatarPool.CountAll;
    }
    
    public void ClearPool()
    {
        // Return all active avatars to pool first
        var activeAvatarsCopy = new HashSet<GameObject>(activeAvatars);
        foreach (var avatar in activeAvatarsCopy)
        {
            ReturnAvatar(avatar);
        }
        
        // Clear the pool
        avatarPool.Clear();
        
        Debug.Log("Avatar pool cleared");
    }
    
    private void OnDestroy()
    {
        // Clean up pool when manager is destroyed
        avatarPool?.Clear();
    }
    
    // Debug info for inspector
    [System.Serializable]
    public struct PoolDebugInfo
    {
        public int activeCount;
        public int inactiveCount;
        public int totalCount;
    }
    
    [Header("Debug Info (Read Only)")]
    [SerializeField] private PoolDebugInfo debugInfo;
    
    private void Update()
    {
        // Update debug info in inspector
        if (avatarPool != null)
        {
            debugInfo.activeCount = activeAvatars.Count;
            debugInfo.inactiveCount = avatarPool.CountInactive;
            debugInfo.totalCount = avatarPool.CountAll;
        }
    }
}