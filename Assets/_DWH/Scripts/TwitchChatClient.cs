using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class TwitchChatClient : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string channel = "your_channel";
    
    private TcpClient tcpClient;
    private StreamReader reader;
    private StreamWriter writer;
    private bool isConnected;
    
    public static event Action<ChatMessage> OnMessageReceived;
    
    void Start()
    {
        ConnectToTwitch();
    }
    
    void OnDestroy()
    {
        Disconnect();
    }
    
    void ConnectToTwitch()
    {
        try
        {
            // Connect to Twitch IRC server
            tcpClient = new TcpClient("irc.chat.twitch.tv", 6667);
            reader = new StreamReader(tcpClient.GetStream());
            writer = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true };
            
            // Anonymous connection with capabilities request
            string anonymousNick = "justinfan" + UnityEngine.Random.Range(10000, 99999);
            writer.WriteLine("CAP REQ :twitch.tv/tags twitch.tv/commands");
            writer.WriteLine($"NICK {anonymousNick}");
            writer.WriteLine($"JOIN #{channel.ToLower()}");
            
            isConnected = true;
            StartCoroutine(ReadChatMessages());
            
            Debug.Log($"Connected anonymously to Twitch chat: #{channel}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to Twitch: {e.Message}");
            Disconnect();
        }
    }
    
    IEnumerator ReadChatMessages()
    {
        while (isConnected && tcpClient?.Connected == true)
        {
            try
            {
                if (tcpClient.Available > 0 || reader.Peek() >= 0)
                {
                    string line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        ProcessIRCMessage(line);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading from Twitch: {e.Message}");
                break;
            }
            
            yield return null;
        }
        
        Disconnect();
    }
    
    void ProcessIRCMessage(string rawMessage)
    {
        // Handle PING to keep connection alive
        if (rawMessage.StartsWith("PING"))
        {
            string pongResponse = rawMessage.Replace("PING", "PONG");
            writer.WriteLine(pongResponse);
            return;
        }
        
        // Handle chat messages (PRIVMSG)
        if (rawMessage.Contains("PRIVMSG"))
        {
            var chatMessage = ParsePrivMsg(rawMessage);
            if (!string.IsNullOrEmpty(chatMessage.username))
            {
                OnMessageReceived?.Invoke(chatMessage);
            }
        }
        
        // Handle User Notices (subs, raids, etc)
        if (rawMessage.Contains("USERNOTICE"))
        {
            var noticeMessage = ParseUserNotice(rawMessage);
            if (!string.IsNullOrEmpty(noticeMessage.username))
            {
                OnMessageReceived?.Invoke(noticeMessage);
            }
        }
        
        // Handle successful connection confirmation
        if (rawMessage.Contains("366")) // End of /NAMES list - successful join
        {
            Debug.Log("Successfully joined Twitch chat channel");
        }
    }
    
    ChatMessage ParsePrivMsg(string rawMessage)
    {
        try
        {
            var message = new ChatMessage
            {
                timestamp = DateTime.Now,
                type = MessageType.RegularChat
            };
            
            // Parse tags if present
            Dictionary<string, string> tags = new Dictionary<string, string>();
            if (rawMessage.StartsWith("@"))
            {
                int tagEnd = rawMessage.IndexOf(' ');
                string tagString = rawMessage.Substring(1, tagEnd - 1);
                tags = ParseTags(tagString);
                rawMessage = rawMessage.Substring(tagEnd + 1);
            }
            
            // Extract username
            int userStart = rawMessage.IndexOf(':') + 1;
            int userEnd = rawMessage.IndexOf('!');
            message.username = rawMessage.Substring(userStart, userEnd - userStart);
            
            // Extract message content
            int messageStart = rawMessage.LastIndexOf(':') + 1;
            message.message = rawMessage.Substring(messageStart);
            
            // Parse tags data
            if (tags.Count > 0)
            {
                ParseMessageTags(ref message, tags);
            }
            
            return message;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse PRIVMSG: {rawMessage} - {e.Message}");
            return new ChatMessage();
        }
    }
    
    ChatMessage ParseUserNotice(string rawMessage)
    {
        try
        {
            var message = new ChatMessage
            {
                timestamp = DateTime.Now,
                type = MessageType.UserNotice,
                noticeType = UserNoticeType.Other
            };
            
            // Parse tags
            Dictionary<string, string> tags = new Dictionary<string, string>();
            if (rawMessage.StartsWith("@"))
            {
                int tagEnd = rawMessage.IndexOf(' ');
                string tagString = rawMessage.Substring(1, tagEnd - 1);
                tags = ParseTags(tagString);
                rawMessage = rawMessage.Substring(tagEnd + 1);
            }
            
            // Extract optional message content
            int messageStart = rawMessage.LastIndexOf(':');
            if (messageStart > 0 && messageStart < rawMessage.Length - 1)
            {
                message.message = rawMessage.Substring(messageStart + 1);
            }
            
            // Parse user notice specific data
            if (tags.Count > 0)
            {
                ParseUserNoticeTags(ref message, tags);
            }
            
            return message;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse USERNOTICE: {rawMessage} - {e.Message}");
            return new ChatMessage();
        }
    }
    
    Dictionary<string, string> ParseTags(string tagString)
    {
        var tags = new Dictionary<string, string>();
        string[] tagPairs = tagString.Split(';');
        
        foreach (string tagPair in tagPairs)
        {
            string[] keyValue = tagPair.Split('=');
            if (keyValue.Length == 2)
            {
                tags[keyValue[0]] = keyValue[1];
            }
            else if (keyValue.Length == 1)
            {
                tags[keyValue[0]] = "";
            }
        }
        
        return tags;
    }
    
    void ParseMessageTags(ref ChatMessage message, Dictionary<string, string> tags)
    {
        // Parse badges
        if (tags.ContainsKey("badges") && !string.IsNullOrEmpty(tags["badges"]))
        {
            string[] badgeList = tags["badges"].Split(',');
            message.badges = badgeList;
            
            foreach (string badge in badgeList)
            {
                if (badge.StartsWith("subscriber/")) message.isSubscriber = true;
                else if (badge.StartsWith("moderator/")) message.isModerator = true;
                else if (badge.StartsWith("vip/")) message.isVip = true;
                else if (badge.StartsWith("broadcaster/")) message.isBroadcaster = true;
            }
        }
        
        // Parse bits
        if (tags.ContainsKey("bits") && !string.IsNullOrEmpty(tags["bits"]))
        {
            if (int.TryParse(tags["bits"], out int bitsAmount))
            {
                message.hasBits = true;
                message.bitsAmount = bitsAmount;
                message.type = MessageType.BitsCheer;
            }
        }
        
        // Parse emotes
        if (tags.ContainsKey("emotes") && !string.IsNullOrEmpty(tags["emotes"]))
        {
            message.hasEmotes = true;
            message.emotes = ParseEmotes(tags["emotes"], message.message);
            
            // Check if message is emote-only
            if (IsEmoteOnlyMessage(message.message, message.emotes))
            {
                message.type = MessageType.EmoteOnly;
            }
        }
    }
    
    void ParseUserNoticeTags(ref ChatMessage message, Dictionary<string, string> tags)
    {
        // Get username from login tag
        if (tags.ContainsKey("login"))
        {
            message.username = tags["login"];
        }
        
        // Parse system message
        if (tags.ContainsKey("system-msg"))
        {
            message.systemMessage = tags["system-msg"].Replace("\\s", " ");
        }
        
        // Parse notice type
        if (tags.ContainsKey("msg-id"))
        {
            switch (tags["msg-id"])
            {
                case "sub":
                    message.noticeType = UserNoticeType.Sub;
                    break;
                case "resub":
                    message.noticeType = UserNoticeType.Resub;
                    if (tags.TryGetValue("msg-param-cumulative-months", out var tag1))
                        int.TryParse(tag1, out message.subMonths);
                    break;
                case "subgift":
                    message.noticeType = UserNoticeType.SubGift;
                    break;
                case "raid":
                    message.noticeType = UserNoticeType.Raid;
                    if (tags.TryGetValue("msg-param-displayName", out var tag2))
                        message.raidFrom = tag2;
                    if (tags.TryGetValue("msg-param-viewerCount", out var tag3))
                        int.TryParse(tag3, out message.raidViewers);
                    break;
                case "bitsbadgetier":
                    message.noticeType = UserNoticeType.BitsBadgeTier;
                    break;
            }
        }
        
        // Parse badges for user notices too
        if (tags.ContainsKey("badges") && !string.IsNullOrEmpty(tags["badges"]))
        {
            string[] badgeList = tags["badges"].Split(',');
            message.badges = badgeList;
            
            foreach (string badge in badgeList)
            {
                if (badge.StartsWith("subscriber/")) message.isSubscriber = true;
                else if (badge.StartsWith("moderator/")) message.isModerator = true;
                else if (badge.StartsWith("vip/")) message.isVip = true;
                else if (badge.StartsWith("broadcaster/")) message.isBroadcaster = true;
            }
        }
    }
    
    EmoteInfo[] ParseEmotes(string emoteString, string messageText)
    {
        try
        {
            var emoteList = new System.Collections.Generic.List<EmoteInfo>();
            string[] emoteGroups = emoteString.Split('/');
            
            foreach (string emoteGroup in emoteGroups)
            {
                string[] parts = emoteGroup.Split(':');
                if (parts.Length == 2)
                {
                    string emoteId = parts[0];
                    string[] positions = parts[1].Split(',');
                    
                    foreach (string position in positions)
                    {
                        string[] range = position.Split('-');
                        if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                        {
                            string emoteName = "";
                            if (start < messageText.Length && end < messageText.Length)
                            {
                                emoteName = messageText.Substring(start, end - start + 1);
                            }
                            
                            emoteList.Add(new EmoteInfo
                            {
                                emoteId = emoteId,
                                emoteName = emoteName,
                                startIndex = start,
                                endIndex = end
                            });
                        }
                    }
                }
            }
            
            return emoteList.ToArray();
        }
        catch
        {
            return new EmoteInfo[0];
        }
    }
    
    bool IsEmoteOnlyMessage(string messageText, EmoteInfo[] emotes)
    {
        if (emotes == null || emotes.Length == 0) return false;
        
        // Calculate total character coverage by emotes
        int emoteCharCount = 0;
        foreach (var emote in emotes)
        {
            emoteCharCount += (emote.endIndex - emote.startIndex + 1);
        }
        
        // Count non-whitespace characters
        int nonWhitespaceCount = 0;
        foreach (char c in messageText)
        {
            if (!char.IsWhiteSpace(c)) nonWhitespaceCount++;
        }
        
        // If emotes cover most/all non-whitespace characters, it's emote-only
        return emoteCharCount >= nonWhitespaceCount * 0.8f;
    }
    
    public void SendMessage(string message)
    {
        Debug.LogWarning("Cannot send messages with anonymous connection. Create authenticated bot for sending messages.");
    }
    
    void Disconnect()
    {
        isConnected = false;
        
        try
        {
            writer?.Close();
            reader?.Close();
            tcpClient?.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error during disconnect: {e.Message}");
        }
        
        writer = null;
        reader = null;
        tcpClient = null;
        
        Debug.Log("Disconnected from Twitch IRC");
    }
}