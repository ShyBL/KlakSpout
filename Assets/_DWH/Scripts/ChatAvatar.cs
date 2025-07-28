using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using System.Collections;

public class ChatAvatar : MonoBehaviour
{
    // [Header("Effects Components (Assign in Prefab)")]
    // [Tooltip("Particle system for subscription/resub events.")]
    // public ParticleSystem subEffectParticles;
    // [Tooltip("Particle system for bit cheers.")]
    // public ParticleSystem cheerEffectParticles;
    
    private string _username;
    private ChatMessage _messageData;
    private float _walkRadius;
    private float _walkSpeed;
    private Vector3 _originPosition;
    private Vector3 _targetPosition;
    private Coroutine _walkCoroutine;
    private Coroutine _timeoutCoroutine;

    private TMP_Text _nameTag;
    private ChatAvatarManager _manager;

    // Called when the object is returned to the pool
    void OnDisable()
    {
        // IMPORTANT: Stop all coroutines when the object is disabled
        if (_walkCoroutine != null) StopCoroutine(_walkCoroutine);
        if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
        _walkCoroutine = null;
        _timeoutCoroutine = null;
    }

    public void Initialize(string user, ChatMessage message, float radius, float speed, float nameHeight, ChatAvatarManager manager)
    {
        _username = user;
        _walkRadius = radius;
        _walkSpeed = speed;
        _originPosition = transform.position;
        _manager = manager;

        if (_nameTag == null) CreateNameTag(nameHeight);
        
        Refresh(message);

        SetNewTarget();
        _walkCoroutine = StartCoroutine(WalkBehavior());
    }
    
    public void Refresh(ChatMessage message)
    {
        _messageData = message;
        
        ApplyAvatarAppearance();
        ApplyMessageEffects();

        if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
        _timeoutCoroutine = StartCoroutine(AvatarTimeout());
    }

    #region Appearance and Effects
    void ApplyAvatarAppearance()
    {
        if (_nameTag == null) return;
        _nameTag.text = _username;

        if (_messageData.isBroadcaster) _nameTag.color = Color.red;
        else if (_messageData.isModerator) _nameTag.color = Color.green;
        else if (_messageData.isVip) _nameTag.color = Color.magenta;
        else if (_messageData.isSubscriber) _nameTag.color = Color.cyan;
        else _nameTag.color = Color.white;
    }

    void ApplyMessageEffects()
    {
        switch (_messageData.type)
        {
            case MessageType.BitsCheer:
                PlayCheerEffect(_messageData.bitsAmount);
                break;
                
            case MessageType.UserNotice:
                HandleUserNoticeEffects();
                break;
        }
    }

    void HandleUserNoticeEffects()
    {
        switch (_messageData.noticeType)
        {
            case UserNoticeType.Sub:
            case UserNoticeType.Resub:
                PlaySubscriptionEffect(_messageData.subMonths);
                break;
            
            case UserNoticeType.SubGift:
            case UserNoticeType.Raid:
            case UserNoticeType.BitsBadgeTier:
                Debug.Log($"Triggering effect for UserNotice: {_messageData.noticeType}");
                break;
        }
    }

    public void PlaySubscriptionEffect(int months)
    {
        Debug.Log($"Playing subscription effect for {_username} ({months} months).");
        // if (subEffectParticles != null) subEffectParticles.Play();
    }
    
    public void PlayCheerEffect(int amount)
    {
        Debug.Log($"Playing cheer effect for {_username} ({amount} bits).");
        // if (cheerEffectParticles != null) cheerEffectParticles.Play();
    }
    #endregion

    #region Movement and Lifetime
    void CreateNameTag(float height)
    {
        GameObject nameTagObject = new GameObject($"NameTag_{_username}");
        nameTagObject.transform.SetParent(transform, false);
        nameTagObject.transform.localPosition = Vector3.up * height;
        
        _nameTag = nameTagObject.AddComponent<TextMeshPro>();
        _nameTag.fontSize = 2f;
        _nameTag.alignment = TextAlignmentOptions.Center;
        
        var lookAt = nameTagObject.AddComponent<LookAtConstraint>();
        var source = new ConstraintSource { sourceTransform = Camera.main.transform, weight = 1f };
        lookAt.AddSource(source);
        lookAt.constraintActive = true;
    }

    void SetNewTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * _walkRadius;
        _targetPosition = _originPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    IEnumerator WalkBehavior()
    {
        while (true)
        {
            while (Vector3.Distance(transform.position, _targetPosition) > 0.5f)
            {
                Vector3 direction = (_targetPosition - transform.position).normalized;
                transform.position += direction * _walkSpeed * Time.deltaTime;
                
                if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
                
                yield return null;
            }
            
            yield return new WaitForSeconds(Random.Range(2f, 5f));
            SetNewTarget();
        }
    }

    IEnumerator AvatarTimeout()
    {
        yield return new WaitForSeconds(_manager.avatarTimeout);
        Debug.Log($"Avatar for {_username} timed out and is being returned to the pool.");
        _manager.RemoveAvatar(_username);
    }
    #endregion
}