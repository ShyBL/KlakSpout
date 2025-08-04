using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class FallingEmote : MonoBehaviour
{
    [Header("Emote Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayerMask = 1; // Default layer
    
    private EmoteData emoteData;
    private Rigidbody rb;
    private Collider col;
    private bool hasLanded = false;
    private bool isBeingCollected = false;
    private Coroutine imageLoadCoroutine;
    
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
        
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // Configure rigidbody for falling
       // rb.useGravity = true;
        //rb.drag = 1f;
        //rb.angularDrag = 0.5f;
        
        // Configure collider as trigger for avatar collection
        //col.isTrigger = true;
    }
    
    public void Initialize(EmoteData data, Vector3 spawnPosition, Transform cameraToLook)
    {
        emoteData = data;
        transform.position = spawnPosition;
        hasLanded = false;
        isBeingCollected = false;
        
        // // Reset physics
        // rb.velocity = Vector3.zero;
        // rb.angularVelocity = Vector3.zero;
        //
        // // Apply initial downward velocity
        // rb.velocity = Vector3.down * fallSpeed;
        
        // Load emote image
        LoadEmoteImage();
        var lookAt = GetComponentInChildren<LookAtConstraint>();
        lookAt.AddSource(new ConstraintSource { sourceTransform = cameraToLook.transform, weight = 1f });
        lookAt.constraintActive = true;
        // Start ground checking
        StartCoroutine(GroundCheckCoroutine());
    }
    
    public void ResetEmote()
    {
        // Reset state for pooling
        emoteData = new EmoteData();
        hasLanded = false;
        isBeingCollected = false;
        
        // Stop any running coroutines
        if (imageLoadCoroutine != null)
        {
            StopCoroutine(imageLoadCoroutine);
            imageLoadCoroutine = null;
        }
        
        // Clear sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
        }
        
        // Reset physics
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    private void LoadEmoteImage()
    {
        if (imageLoadCoroutine != null)
        {
            StopCoroutine(imageLoadCoroutine);
        }
        
        imageLoadCoroutine = StartCoroutine(LoadEmoteImageCoroutine());
    }
    
    private IEnumerator LoadEmoteImageCoroutine()
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(emoteData.imageUrl))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                
                if (texture != null && spriteRenderer != null)
                {
                    // Create sprite from texture
                    Sprite emoteSprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), // Pivot at center
                        100f // Pixels per unit
                    );
                    
                    spriteRenderer.sprite = emoteSprite;
                }
            }
            else
            {
                Debug.LogWarning($"Failed to load emote image: {emoteData.emoteName} - {www.error}");
                
                // Create a fallback colored square
                CreateFallbackSprite();
            }
        }
        
        imageLoadCoroutine = null;
    }
    
    private void CreateFallbackSprite()
    {
        if (spriteRenderer == null) return;
        
        // Create a simple colored square as fallback
        Texture2D fallbackTexture = new Texture2D(64, 64);
        Color fallbackColor = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                fallbackTexture.SetPixel(x, y, fallbackColor);
            }
        }
        
        fallbackTexture.Apply();
        
        Sprite fallbackSprite = Sprite.Create(
            fallbackTexture,
            new Rect(0, 0, 64, 64),
            new Vector2(0.5f, 0.5f),
            100f
        );
        
        spriteRenderer.sprite = fallbackSprite;
    }
    
    private IEnumerator GroundCheckCoroutine()
    {
        while (!hasLanded)
        {
            // Check if we're close to the ground
            if (Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayerMask))
            {
                hasLanded = true;
                OnLanded();
            }
            
            yield return new WaitForFixedUpdate();
        }
    }
    
    private void OnLanded()
    {
        // Stop physics movement
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
        
        // Notify the emote manager that we've landed
        emoteManager.OnEmoteLanded(this);
        
        Debug.Log($"Emote {emoteData.emoteName} landed at {transform.position}");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if an avatar is collecting this emote
        ChatAvatar avatar = other.GetComponent<ChatAvatar>();
        if (avatar != null)// && hasLanded && !isBeingCollected)
        {
            isBeingCollected = true;
            avatar.CollectEmote(this);
        }
    }
    
    public void OnCollected()
    {
        // Called when avatar successfully collects this emote
        emoteManager.ReturnEmoteToPool(this);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw ground check ray
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
        
        // Draw collection trigger
        if (col != null)
        {
            Gizmos.color = hasLanded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
}