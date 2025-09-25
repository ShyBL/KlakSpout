using System;

[System.Serializable]
public struct ChatMessage
{
    public MessageType type;
    public string username;
    public string message;
    public DateTime timestamp;
    
    // Channel Points Redeem Info
    public string customRewardId;
    public string customRewardTitle;
    public long customRewardCost;
        
    // Badge info
    public bool isSubscriber;
    public bool isModerator;
    public bool isVip;
    public bool isBroadcaster;
    public string[] badges;
        
    // Emote info
    public bool hasEmotes;
    public EmoteInfo[] emotes;
        
    // Bits info
    public bool hasBits;
    public int bitsAmount;
        
    // Subscription data
    public string subTier;
    public bool isGift;
    
    // Follow data
    public string followedAt;
    
    // Hype train data
    public int hypeTrainLevel;
    public long hypeTrainProgress;
    public long hypeTrainGoal;
    
    // Raid data
    public long raidViewerCount;
    
    // User Notice info
    public UserNoticeType noticeType;
    public string systemMessage;
    public int subMonths;
    public string raidFrom;
    public int raidViewers;
}