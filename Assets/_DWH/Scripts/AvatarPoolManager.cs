using System.Collections.Generic;
using UnityEngine;

public class AvatarPoolManager : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private int initialPoolSize = 50;
    [SerializeField] private int maxPoolSize = 100;
    [SerializeField] private Transform poolParent;
    
    private Queue<GameObject> availableAvatars = new Queue<GameObject>();
    private HashSet<GameObject> activeAvatars = new HashSet<GameObject>();
    
    private static AvatarPoolManager instance;
    public static AvatarPoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AvatarPoolManager>();
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
    
    private void InitializePool()
    {
        if (poolParent == null)
        {
            GameObject poolContainer = new GameObject("Avatar Pool");
            poolParent = poolContainer.transform;
            poolParent.SetParent(transform);
        }
        
        // Pre-instantiate avatars
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAvatar();
        }
        
        Debug.Log($"Avatar pool initialized with {initialPoolSize} avatars");
    }
    
    private GameObject CreateNewAvatar()
    {
        if (avatarPrefab == null)
        {
            Debug.LogError("Avatar prefab is not assigned!");
            return null;
        }
        
        GameObject avatar = Instantiate(avatarPrefab, poolParent);
        avatar.SetActive(false);
        availableAvatars.Enqueue(avatar);
        
        return avatar;
    }
    
    public GameObject GetAvatar()
    {
        GameObject avatar;
        
        if (availableAvatars.Count > 0)
        {
            avatar = availableAvatars.Dequeue();
        }
        else if (activeAvatars.Count + availableAvatars.Count < maxPoolSize)
        {
            avatar = CreateNewAvatar();
            if (avatar != null)
            {
                availableAvatars.Dequeue(); // Remove from queue since we just added it
            }
        }
        else
        {
            Debug.LogWarning("Avatar pool is at maximum capacity!");
            return null;
        }
        
        if (avatar != null)
        {
            avatar.SetActive(true);
            activeAvatars.Add(avatar);
        }
        
        return avatar;
    }
    
    public void ReturnAvatar(GameObject avatar)
    {
        if (avatar == null) return;
        
        if (activeAvatars.Contains(avatar))
        {
            activeAvatars.Remove(avatar);
            
            // Reset avatar state
            ResetAvatarState(avatar);
            
            // Deactivate and return to pool
            avatar.SetActive(false);
            avatar.transform.SetParent(poolParent);
            availableAvatars.Enqueue(avatar);
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
    
    public int GetAvailableCount()
    {
        return availableAvatars.Count;
    }
    
    public int GetTotalPoolSize()
    {
        return activeAvatars.Count + availableAvatars.Count;
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}