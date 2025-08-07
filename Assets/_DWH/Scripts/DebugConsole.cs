using System.Collections.Generic;
using UnityEngine;

public class DebugConsole : MonoBehaviour
{
    private readonly List<string> logs = new();
    private Vector2 scrollPosition;
    private bool showConsole;

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
        if (Debug.isDebugBuild && Input.GetKeyDown(KeyCode.F1))
        {
            showConsole = !showConsole;
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

        GUI.Box(new Rect(10, 10, Screen.width - 20, Screen.height / 3), "Debug Console");
            
        scrollPosition = GUI.BeginScrollView(
            new Rect(10, 35, Screen.width - 20, Screen.height / 3 - 45),
            scrollPosition,
            new Rect(0, 0, Screen.width - 40, logs.Count * 20)
        );

        for (int i = 0; i < logs.Count; i++)
        {
            GUI.Label(new Rect(0, i * 20, Screen.width - 40, 20), logs[i]);
        }

        GUI.EndScrollView();
    }
}