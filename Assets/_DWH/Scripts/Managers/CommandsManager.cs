using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CommandsManager : MonoBehaviour
{
    [Header("Channel Point Reward IDs")]
    [Tooltip("The Custom Reward ID from your Twitch dashboard for the 'VIP' redemption.")]
    [SerializeField] private string vipRewardId = "your-vip-reward-id-here";
    [Tooltip("The Custom Reward ID for the 'Reroll' redemption.")]
    [SerializeField] private string rerollRewardId = "your-reroll-reward-id-here";
    [Tooltip("The Custom Reward ID for the 'Duel' redemption.")]
    [SerializeField] private string duelRewardId = "your-duel-reward-id-here";
    [Tooltip("The Custom Reward ID for the 'Colorify' redemption.")]
    [SerializeField] private string colorifyRewardId = "your-colorify-reward-id-here";
    
    [Header("VIP Rock Settings")]
    [SerializeField] private Transform vipRock;
   // [SerializeField] private float minimumScaleRequired = 2f;
   
    [Header("References")]
    private TwitchChatClient chatClient;
    private ChatAvatarManager avatarManager;
    
   // private bool someoneHeadingToRock = false;
    private string usernameHeadingToRock = "";
    private void Start()
    {
        // Find required components
        chatClient = FindObjectOfType<TwitchChatClient>();
        avatarManager = FindObjectOfType<ChatAvatarManager>();
        
        if (chatClient == null)
        {
            Debug.LogError("TwitchChatClient not found!");
            return;
        }
        
        if (avatarManager == null)
        {
            Debug.LogError("ChatAvatarManager not found!");
            return;
        }
        
        if (vipRock == null)
        {
            Debug.LogError("VIP Rock Transform not assigned!");
            return;
        }
        
        chatClient.OnMessageReceived += OnChatMessage;
    }
    
    private void OnDestroy()
    {
        if (chatClient != null)
        {
            chatClient.OnMessageReceived -= OnChatMessage;
        }
    }
    
    private void OnChatMessage(ChatMessage message)
    {
        // Route channel point redemptions to their own handler
        if (message.type == MessageType.ChannelPointRedemption && !string.IsNullOrEmpty(message.customRewardId))
        {
            ProcessRedemptionCommand(message);
            return;
        }
        
        // Check if message starts with !
        if (!message.message.StartsWith("!")) return;
        
        ProcessAvatarCommand(message);
    }
    
    private void ProcessRedemptionCommand(ChatMessage message)
    {
        string rewardId = message.customRewardId;

        if (rewardId == vipRewardId)
        {
            HandleVipRedemption(message.username);
        }
        else if (rewardId == rerollRewardId)
        {
            HandleRerollRedeem(message.username);
        }
        else if (rewardId == duelRewardId)
        {
            HandleDuelRedeem(message.username);
        }
        else if (rewardId == colorifyRewardId)
        {
            HandleColorifyRedeem(message.username);
        }
    }
    
    private void HandleVipRedemption(string username)
    {
        ChatAvatar userAvatar = FindAvatarByUsername(username);
        if (userAvatar == null)
        {
            // Avatar might not exist yet, but redemption happened. We can ignore or queue.
            // For now, we'll just log it.
            Debug.LogWarning($"VIP redemption from {username}, but their avatar isn't active.");
            return;
        }

        // If the rock is occupied, start a fight. Otherwise, claim it.
        if (vipRock.childCount > 0)
        {
            SendAutoMessage($"@{username} redeemed VIP and is challenging for the rock!");
            HandleFightCommand(username); // Reuse existing fight logic
        }
        else
        {
            SendAutoMessage($"@{username} redeemed VIP and is claiming the rock!");
            HandleVipCommand(username); // Reuse existing VIP logic
        }
    }
    
    private void HandleRerollRedeem(string username)
    {
        avatarManager.RerollAvatar(username);
        SendAutoMessage($"@{username} has rerolled their avatar!");
    }

    private void HandleDuelRedeem(string username)
    {
        ChatAvatar challenger = FindAvatarByUsername(username);
        if (challenger == null) return;

        // Find all other avatars that are not on the VIP rock
        List<ChatAvatar> potentialOpponents = avatarManager.GetActiveAvatars()
            .Where(avatar => avatar != challenger && avatar.transform.parent != vipRock)
            .ToList();

        if (potentialOpponents.Count == 0)
        {
            SendAutoMessage($"@{username} wants to duel, but there are no opponents available!");
            return;
        }

        // Pick a random opponent
        ChatAvatar opponent = potentialOpponents[Random.Range(0, potentialOpponents.Count)];

        // Compare sizes
        float challengerSize = challenger.avatarTransform.localScale.x;
        float opponentSize = opponent.avatarTransform.localScale.x;
        
        SendAutoMessage($"DUEL! @{challenger.Username} (size: {challengerSize:F1}) challenges @{opponent.Username} (size: {opponentSize:F1})!");

        ChatAvatar winner, loser;
        if (challengerSize >= opponentSize)
        {
            winner = challenger;
            loser = opponent;
        }
        else
        {
            winner = opponent;
            loser = challenger;
        }

        SendAutoMessage($"@{winner.Username} has won the duel! @{loser.Username} has been defeated. 💥");
        avatarManager.RemoveAvatar(loser.Username); // Remove the loser
    }

    private void HandleColorifyRedeem(string username)
    {
        ChatAvatar avatar = FindAvatarByUsername(username);
        if (avatar != null)
        {
            // NOTE: This assumes you have a public method on your ChatAvatar.cs script
            // that handles changing the avatar's color.
            // Example:
            // avatar.RandomizeColor();
            
            SendAutoMessage($"@{username} has colorified their avatar!");
            Debug.Log($"Colorify command called for {username}. Implement the color change logic on your ChatAvatar script.");
        }
    }
    
    private void ProcessAvatarCommand(ChatMessage message)
    {
        string command = message.message.ToLower();
        
        if (command.StartsWith("!vip"))
        {
            HandleVipCommand(message.username);
        }
        else if (command.StartsWith("!fight"))
        {
            HandleFightCommand(message.username);
        }
    }
    
    private void HandleVipCommand(string username)
    {
        // Find the user's avatar
        ChatAvatar userAvatar = FindAvatarByUsername(username);
        if (userAvatar == null)
        {
            SendAutoMessage($"@{username} Your avatar is not currently active!");
            return;
        }
    
        // Check if avatar is big enough
        // if (userAvatar.avatarTransform.localScale.x < minimumScaleRequired)
        // {
        //     SendAutoMessage($"@{username} Your avatar needs to be bigger! Current size: {userAvatar.transform.localScale.x:F1}, Required: {minimumScaleRequired}");
        //     return;
        // }
    
        // Check if rock is occupied OR someone is heading there
        if (vipRock.childCount > 0)
        {
            ChatAvatar currentVip = vipRock.GetChild(0).GetComponent<ChatAvatar>();
            SendAutoMessage($"@{username} The VIP rock is currently occupied by @{currentVip.Username}! Use !fight to challenge them!");
            return;
        }
    
        // if (someoneHeadingToRock)
        // {
        //     SendAutoMessage($"@{username} @{usernameHeadingToRock} is already heading to the VIP rock! Wait for them to arrive or use !fight!");
        //     return;
        // }
        //
        // // Set the tracking variables
        // someoneHeadingToRock = true;
        usernameHeadingToRock = username;
    
        // Move avatar to VIP rock
        MoveAvatarToVipRock(userAvatar);
    }
    
    /// <summary>
    /// Called when an avatar successfully mounts the VIP rock
    /// </summary>
    /// <param name="username">Username of avatar that mounted the rock</param>
    public void OnAvatarMountedVipRock(string username)
    {
       // someoneHeadingToRock = false;
        usernameHeadingToRock = "";
        SendAutoMessage($"@{username} has successfully claimed the VIP rock! 👑");
    }

    /// <summary>
    /// Called if an avatar fails to reach the VIP rock for any reason
    /// </summary>
    public void OnAvatarFailedToReachVipRock()
    {
       // someoneHeadingToRock = false;
        usernameHeadingToRock = "";
    }
    
    private void HandleFightCommand(string username)
    {
        // Find the user's avatar
        ChatAvatar challengerAvatar = FindAvatarByUsername(username);
        if (challengerAvatar == null)
        {
            SendAutoMessage($"@{username} Your avatar is not currently active!");
            return;
        }
        
        // Check if rock is vacant
        if (vipRock.childCount == 0)
        {
            SendAutoMessage($"@{username} The VIP rock is empty! Use !vip to claim it!");
            return;
        }
        
        // Find current VIP
        ChatAvatar currentVip = vipRock.GetChild(0).GetComponent<ChatAvatar>();
        if (currentVip == null)
        {
            Debug.LogError("VIP rock has a child but no ChatAvatar component!");
            return;
        }
        
        // Compare sizes
        float challengerSize = challengerAvatar.avatarTransform.localScale.x;
        float currentVipSize = currentVip.avatarTransform.localScale.x;
        
        if (challengerSize > currentVipSize)
        {
            // Challenger wins!
            string defeatedUsername = currentVip.Username;
            
            // Remove current VIP from rock
            RemoveAvatarFromVipRock(currentVip);
            
            // Move challenger to rock
            MoveAvatarToVipRock(challengerAvatar);
            
            SendAutoMessage($"@{username} (size: {challengerSize:F1}) has defeated @{defeatedUsername} (size: {currentVipSize:F1}) and claimed the VIP rock! 🥊👑");
        }
        else
        {
            // Challenger loses
            SendAutoMessage($"@{username} (size: {challengerSize:F1}) challenged @{currentVip.Username} (size: {currentVipSize:F1}) but was too small to win! 💪");
        }
    }
    
    private ChatAvatar FindAvatarByUsername(string username)
    {
        // Get all active avatars from the avatar manager
        var activeAvatars = avatarManager.GetActiveAvatars();
        return activeAvatars.FirstOrDefault(avatar => avatar.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
    }
    
    private void MoveAvatarToVipRock(ChatAvatar avatar)
    {
        // Tell avatar to move to VIP rock
        avatar.MoveToVipRock(vipRock);
        
        Debug.Log($"{avatar.Username} walking to VIP rock");
    }
    
    private void RemoveAvatarFromVipRock(ChatAvatar avatar)
    {
        // Unparent from rock
        avatar.transform.SetParent(null);
        
        // Move to a random position near the rock
        Vector3 randomOffset = new Vector3(
            Random.Range(-3f, 3f),
            0f,
            Random.Range(-3f, 3f)
        );
        avatar.transform.position = vipRock.position + randomOffset;
        
        // Restart walking behavior
        WalkBehavior walkBehavior = avatar.GetComponent<WalkBehavior>();
        if (walkBehavior != null)
        {
            walkBehavior.StartWalking();
        }
        
        Debug.Log($"{avatar.Username} removed from VIP rock");
    }
    
    private void SendAutoMessage(string message)
    { 
        chatClient.SendMessage(message);
    }
}