using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DebugConsole : MonoBehaviour
{
    private readonly List<string> logs = new();
    private Vector2 scrollPosition;
    private Vector2 avatarScrollPosition;
    private bool showConsole;
    
    [Header("Avatar Management")]
    [SerializeField] private int maxAvatarButtons = 20;
    [SerializeField] private float buttonWidth = 150f;
    [SerializeField] private float buttonHeight = 25f;
    
    private ChatAvatarManager avatarManager;
    
    // State for avatar options
    private string selectedAvatar = "";
    private bool showAvatarOptions = false;

    private void Start()
    {
        // Find avatar manager if not assigned
        if (avatarManager == null)
        {
            avatarManager = FindObjectOfType<ChatAvatarManager>();
        }
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            showConsole = !showConsole;
        }
        
        // Close avatar options if clicking elsewhere
        if (showAvatarOptions && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            // Convert to GUI coordinates
            mousePos.y = Screen.height - mousePos.y;
            
            Rect avatarOptionsRect = new Rect(buttonWidth + 10, 35, buttonWidth, buttonHeight * 2 + 10);
            
            if (!avatarOptionsRect.Contains(mousePos))
            {
                showAvatarOptions = false;
                selectedAvatar = "";
            }
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        logs.Add(logString);
        if (logs.Count > 1000) logs.RemoveAt(0);
    }

    private void OnGUI()
    {
        if (!showConsole) return;

        float avatarPanelWidth = buttonWidth + 20;
        float consolePanelX = avatarPanelWidth + 10;
        float consolePanelWidth = Screen.width - consolePanelX - 10;

        // Avatar Management Panel
        DrawAvatarPanel(avatarPanelWidth);
        
        // Debug Console Panel
       // DrawConsolePanel(consolePanelX, consolePanelWidth);
        
        // Avatar Options Popup
        if (showAvatarOptions)
        {
            DrawAvatarOptions();
        }
    }
    
    private void DrawAvatarPanel(float panelWidth)
    {
        GUI.Box(new Rect(10, 10, panelWidth, Screen.height / 3), "Avatar Manager");
        
        // Get active avatars
        ChatAvatar[] activeAvatars = avatarManager != null ? avatarManager.GetActiveAvatars() : new ChatAvatar[0];
        
        // Info header
        GUI.Label(new Rect(15, 35, panelWidth - 10, 20), $"Active Avatars: {activeAvatars.Length}");
        
        // Scrollable avatar list
        float scrollViewHeight = Screen.height / 3 - 70;
        float contentHeight = Mathf.Max(activeAvatars.Length * (buttonHeight + 2), scrollViewHeight);
        
        avatarScrollPosition = GUI.BeginScrollView(
            new Rect(10, 55, panelWidth, scrollViewHeight),
            avatarScrollPosition,
            new Rect(0, 0, panelWidth - 20, contentHeight)
        );

        // Draw avatar buttons (limit to maxAvatarButtons)
        int buttonCount = Mathf.Min(activeAvatars.Length, maxAvatarButtons);
        for (int i = 0; i < buttonCount; i++)
        {
            if (activeAvatars[i] != null)
            {
                string username = activeAvatars[i].Username;
                float buttonY = i * (buttonHeight + 2);
                
                // Highlight selected avatar
                Color originalColor = GUI.backgroundColor;
                if (selectedAvatar == username)
                {
                    GUI.backgroundColor = Color.yellow;
                }
                
                if (GUI.Button(new Rect(5, buttonY, buttonWidth - 10, buttonHeight), username))
                {
                    if (selectedAvatar == username)
                    {
                        // Toggle options if same avatar clicked
                        showAvatarOptions = !showAvatarOptions;
                    }
                    else
                    {
                        // Select new avatar
                        selectedAvatar = username;
                        showAvatarOptions = true;
                    }
                }
                
                GUI.backgroundColor = originalColor;
            }
        }
        
        if (activeAvatars.Length > maxAvatarButtons)
        {
            float warningY = buttonCount * (buttonHeight + 2);
            GUI.Label(new Rect(5, warningY, buttonWidth - 10, buttonHeight), 
                     $"... +{activeAvatars.Length - maxAvatarButtons} more", 
                     GUI.skin.box);
        }

        GUI.EndScrollView();
    }
    
    private void DrawConsolePanel(float panelX, float panelWidth)
    {
        GUI.Box(new Rect(panelX, 10, panelWidth, Screen.height / 3), "Debug Console");
            
        scrollPosition = GUI.BeginScrollView(
            new Rect(panelX, 35, panelWidth, Screen.height / 3 - 45),
            scrollPosition,
            new Rect(0, 0, panelWidth - 20, logs.Count * 20)
        );

        for (int i = 0; i < logs.Count; i++)
        {
            GUI.Label(new Rect(0, i * 20, panelWidth - 20, 20), logs[i]);
        }

        GUI.EndScrollView();
    }
    
    private void DrawAvatarOptions()
    {
        if (string.IsNullOrEmpty(selectedAvatar)) return;
        
        float optionsX = buttonWidth + 10;
        float optionsY = 35;
        float optionsWidth = buttonWidth;
        float optionsHeight = buttonHeight * 2 + 10;
        
        // Background box
        GUI.Box(new Rect(optionsX, optionsY, optionsWidth, optionsHeight), "");
        
        // Kill Avatar Button
        if (GUI.Button(new Rect(optionsX + 5, optionsY + 5, optionsWidth - 10, buttonHeight), "Kill Avatar"))
        {
            KillAvatar(selectedAvatar);
            showAvatarOptions = false;
            selectedAvatar = "";
        }
        
        // Reroll Avatar Button
        if (GUI.Button(new Rect(optionsX + 5, optionsY + buttonHeight + 10, optionsWidth - 10, buttonHeight), "Reroll Avatar"))
        {
            RerollAvatar(selectedAvatar);
            showAvatarOptions = false;
            selectedAvatar = "";
        }
    }
    
    private void KillAvatar(string username)
    {
        if (avatarManager == null)
        {
            Debug.LogError("AvatarManager not found!");
            return;
        }
        
        // Use the manager's RemoveAvatar method to properly despawn and return to pool
        avatarManager.RemoveAvatar(username);
        Debug.Log($"Killed avatar for {username}");
    }
    
    private void RerollAvatar(string username)
    {
        if (avatarManager == null)
        {
            Debug.LogError("AvatarManager not found!");
            return;
        }
        
        // Use the existing RerollAvatar method from ChatAvatarManager
        avatarManager.RerollAvatar(username);
        Debug.Log($"Rerolled avatar for {username}");
    }
}