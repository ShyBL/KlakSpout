using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class TwitchChatClient : MonoBehaviour
{
    [Header("Twitch Credentials")]
    [Tooltip("The OAuth token for the bot account. Get from twitchapps.com/tmi/. MUST start with 'oauth:'.")]
    [SerializeField] private string _oauthToken = "oauth:your_oauth_token";
    [Tooltip("The username of the bot account.")]
    [SerializeField] private string _botUsername = "your_bot_username";
    [Tooltip("The Twitch channel to connect to.")]
    [SerializeField] private string _channelName = "your_channel_name";

    [Header("Connection Settings")]
    [SerializeField] private float _reconnectDelay = 5f;
    [SerializeField] private float _maxReconnectDelay = 60f;

    private TcpClient _tcpClient;
    private StreamReader _reader;
    private StreamWriter _writer;
    private Coroutine _reconnectCoroutine;

    private readonly Queue<string> _messageQueue = new Queue<string>();
    private float _messageSendTimer = 0f;
    private const float MESSAGE_SEND_RATE = 1.5f;

    public static event Action<ChatMessage> OnMessageReceived;

    #region Unity Lifecycle
    void Start()
    {
        ConnectToTwitch();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void Update()
    {
        if (_writer != null && _messageQueue.Count > 0)
        {
            _messageSendTimer -= Time.deltaTime;
            if (_messageSendTimer <= 0)
            {
                SendMessageImmediate(_messageQueue.Dequeue());
                _messageSendTimer = MESSAGE_SEND_RATE;
            }
        }
    }
    #endregion

    #region Connection Management
    private void ConnectToTwitch()
    {
        if (_tcpClient?.Connected == true) return;
        if (_reconnectCoroutine != null) StopCoroutine(_reconnectCoroutine);

        try
        {
            _tcpClient = new TcpClient("irc.chat.twitch.tv", 6667);
            _reader = new StreamReader(_tcpClient.GetStream());
            _writer = new StreamWriter(_tcpClient.GetStream()) { AutoFlush = true };

            _writer.WriteLine("CAP REQ :twitch.tv/tags twitch.tv/commands");
            _writer.WriteLine($"PASS {_oauthToken}");
            _writer.WriteLine($"NICK {_botUsername.ToLower()}");
            _writer.WriteLine($"JOIN #{_channelName.ToLower()}");

            StartCoroutine(ReadChatMessages());

            Debug.Log($"Successfully connected to Twitch chat: #{_channelName}");
            _reconnectDelay = 5f;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to Twitch: {e.Message}");
            HandleDisconnect();
        }
    }

    private void Disconnect()
    {
        if (_reconnectCoroutine != null) StopCoroutine(_reconnectCoroutine);
        
        _tcpClient?.Close();
        _reader?.Close();
        _writer?.Close();

        _writer = null;
        _reader = null;
        _tcpClient = null;
        Debug.Log("Disconnected from Twitch IRC");
    }

    private void HandleDisconnect()
    {
        Disconnect();
        if (gameObject.activeInHierarchy)
        {
             _reconnectCoroutine = StartCoroutine(Reconnect());
        }
    }
    
    private IEnumerator Reconnect()
    {
        Debug.LogWarning($"Disconnected. Attempting to reconnect in {_reconnectDelay} seconds...");
        yield return new WaitForSeconds(_reconnectDelay);
        _reconnectDelay = Mathf.Min(_reconnectDelay * 2, _maxReconnectDelay);
        ConnectToTwitch();
    }

    private IEnumerator ReadChatMessages()
    {
        while (_tcpClient?.Connected == true)
        {
            try
            {
                if (_tcpClient.Available > 0 || _reader.Peek() >= 0)
                {
                    string line = _reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        ProcessIRCMessage(line);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading from Twitch: {e.Message}");
                HandleDisconnect();
                yield break;
            }
            yield return null;
        }
        HandleDisconnect();
    }
    #endregion

    #region Message Processing
    private void ProcessIRCMessage(string rawMessage)
    {
        if (rawMessage.StartsWith("PING"))
        {
            _writer.WriteLine("PONG :tmi.twitch.tv");
            return;
        }

        if (rawMessage.Contains("PRIVMSG"))
        {
            var chatMessage = ParsePrivMsg(rawMessage);
            if (!string.IsNullOrEmpty(chatMessage.username))
            {
                OnMessageReceived?.Invoke(chatMessage);
            }
        }

        if (rawMessage.Contains("USERNOTICE"))
        {
            var noticeMessage = ParseUserNotice(rawMessage);
            if (!string.IsNullOrEmpty(noticeMessage.username))
            {
                OnMessageReceived?.Invoke(noticeMessage);
            }
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
            
            Dictionary<string, string> tags = new Dictionary<string, string>();
            if (rawMessage.StartsWith("@"))
            {
                int tagEnd = rawMessage.IndexOf(' ');
                string tagString = rawMessage.Substring(1, tagEnd - 1);
                tags = ParseTags(tagString);
                rawMessage = rawMessage.Substring(tagEnd + 1);
            }
            
            int userStart = rawMessage.IndexOf(':') + 1;
            int userEnd = rawMessage.IndexOf('!');
            message.username = rawMessage.Substring(userStart, userEnd - userStart);
            
            int messageStart = rawMessage.LastIndexOf(':') + 1;
            message.message = rawMessage.Substring(messageStart);
            
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
            
            Dictionary<string, string> tags = new Dictionary<string, string>();
            if (rawMessage.StartsWith("@"))
            {
                int tagEnd = rawMessage.IndexOf(' ');
                string tagString = rawMessage.Substring(1, tagEnd - 1);
                tags = ParseTags(tagString);
                rawMessage = rawMessage.Substring(tagEnd + 1);
            }
            
            int messageStart = rawMessage.LastIndexOf(':');
            if (messageStart > 0 && messageStart < rawMessage.Length - 1)
            {
                message.message = rawMessage.Substring(messageStart + 1);
            }
            
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
        }
        return tags;
    }
    
    void ParseMessageTags(ref ChatMessage message, Dictionary<string, string> tags)
    {
        if (tags.TryGetValue("badges", out var badgeValue) && !string.IsNullOrEmpty(badgeValue))
        {
            string[] badgeList = badgeValue.Split(',');
            message.badges = badgeList;
            
            foreach (string badge in badgeList)
            {
                if (badge.StartsWith("subscriber/")) message.isSubscriber = true;
                else if (badge.StartsWith("moderator/")) message.isModerator = true;
                else if (badge.StartsWith("vip/")) message.isVip = true;
                else if (badge.StartsWith("broadcaster/")) message.isBroadcaster = true;
            }
        }
        
        if (tags.TryGetValue("bits", out var bitsValue) && int.TryParse(bitsValue, out int bitsAmount))
        {
            message.hasBits = true;
            message.bitsAmount = bitsAmount;
            message.type = MessageType.BitsCheer;
        }
        
        if (tags.TryGetValue("emotes", out var emoteValue) && !string.IsNullOrEmpty(emoteValue))
        {
            message.hasEmotes = true;
            message.emotes = ParseEmotes(emoteValue, message.message);
            
            if (IsEmoteOnlyMessage(message.message, message.emotes))
            {
                message.type = MessageType.EmoteOnly;
            }
        }
    }
    
    void ParseUserNoticeTags(ref ChatMessage message, Dictionary<string, string> tags)
    {
        if (tags.TryGetValue("login", out var loginValue))
        {
            message.username = loginValue;
        }
        
        if (tags.TryGetValue("system-msg", out var sysMsgValue))
        {
            message.systemMessage = sysMsgValue.Replace("\\s", " ");
        }
        
        if (tags.TryGetValue("msg-id", out var msgIdValue))
        {
            switch (msgIdValue)
            {
                case "sub":
                    message.noticeType = UserNoticeType.Sub;
                    break;
                case "resub":
                    message.noticeType = UserNoticeType.Resub;
                    if (tags.TryGetValue("msg-param-cumulative-months", out var months))
                        int.TryParse(months, out message.subMonths);
                    break;
                case "subgift":
                    message.noticeType = UserNoticeType.SubGift;
                    break;
                case "raid":
                    message.noticeType = UserNoticeType.Raid;
                    if (tags.TryGetValue("msg-param-displayName", out var raider))
                        message.raidFrom = raider;
                    if (tags.TryGetValue("msg-param-viewerCount", out var viewers))
                        int.TryParse(viewers, out message.raidViewers);
                    break;
                case "bitsbadgetier":
                    message.noticeType = UserNoticeType.BitsBadgeTier;
                    break;
            }
        }
    }
    
    EmoteInfo[] ParseEmotes(string emoteString, string messageText)
    {
        var emoteList = new List<EmoteInfo>();
        string[] emoteGroups = emoteString.Split('/');
        
        foreach (string emoteGroup in emoteGroups)
        {
            string[] parts = emoteGroup.Split(':');
            if (parts.Length != 2) continue;

            string emoteId = parts[0];
            string[] positions = parts[1].Split(',');
            
            foreach (string position in positions)
            {
                string[] range = position.Split('-');
                if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                {
                    emoteList.Add(new EmoteInfo
                    {
                        emoteId = emoteId,
                        emoteName = messageText.Substring(start, end - start + 1),
                        startIndex = start,
                        endIndex = end
                    });
                }
            }
        }
        return emoteList.ToArray();
    }
    
    bool IsEmoteOnlyMessage(string messageText, EmoteInfo[] emotes)
    {
        if (emotes == null || emotes.Length == 0) return false;
        
        int emoteCharCount = 0;
        foreach (var emote in emotes)
        {
            emoteCharCount += (emote.endIndex - emote.startIndex + 1);
        }
        
        int nonWhitespaceCount = 0;
        foreach (char c in messageText)
        {
            if (!char.IsWhiteSpace(c)) nonWhitespaceCount++;
        }
        
        return emoteCharCount >= nonWhitespaceCount;
    }
    #endregion

    #region Sending Messages
    public void SendChatMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        _messageQueue.Enqueue(message);
    }

    private void SendMessageImmediate(string message)
    {
        if (_writer == null || _tcpClient?.Connected != true)
        {
            Debug.LogWarning("Cannot send message, not connected.");
            return;
        }

        _writer.WriteLine($"PRIVMSG #{_channelName.ToLower()} :{message}");
    }
    #endregion
}