using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Netcode;

public class InSceneServerBrowser : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject multiplayerUIPanel; 
    public GameObject hudCanvas; // Your HUD Canvas

    [Header("Hosting UI")]
    public TMP_InputField serverNameInput;
    public TextMeshProUGUI statusText;
    
    [Header("Browser UI")]
    public Transform serverListContainer; 
    public GameObject serverButtonPrefab; 

    private async void Start()
    {
        // 1. Automatically hide the HUD visually, but leave it AWAKE so the bike can find it!
        if (hudCanvas != null)
        {
            CanvasGroup cg = hudCanvas.GetComponent<CanvasGroup>();
            if (cg == null) cg = hudCanvas.AddComponent<CanvasGroup>();
            cg.alpha = 0f; // Make it invisible
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        
        UpdateStatus("Online Services Ready.");
        RefreshServerList(); 
    }

    private void Update()
    {
        // BRUTE FORCE FIX: If the menu is open, fight back against the camera/player script 
        // and aggressively force the mouse to stay visible and unlocked!
        if (multiplayerUIPanel != null && multiplayerUIPanel.activeSelf)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // --- 1. HOST A SERVER ---
    public async void HostServer()
    {
        string sessionName = string.IsNullOrEmpty(serverNameInput.text) ? "Zwift Freeroam" : serverNameInput.text;
        UpdateStatus("Creating " + sessionName + "...");

        try
        {
            var sessionOptions = new SessionOptions
            {
                MaxPlayers = 100,
                IsPrivate = false,
                Name = sessionName 
            }.WithRelayNetwork(); 

            await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
            
            UpdateStatus("Hosting Server!");
            HideUI();
        }
        catch (SessionException e)
        {
            Debug.LogError(e);
            UpdateStatus("Failed to host.");
        }
    }

    // --- 2. BROWSE SERVERS ---
    public async void RefreshServerList()
    {
        UpdateStatus("Searching for active rides...");
        
        foreach (Transform child in serverListContainer)
        {
            Destroy(child.gameObject);
        }

        try
        {
            var queryOptions = new QuerySessionsOptions { Count = 20 };
            var results = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);

            if (results.Sessions.Count == 0)
            {
                UpdateStatus("No active rides found.");
                return;
            }

            UpdateStatus($"Found {results.Sessions.Count} ride(s).");

            foreach (var sessionInfo in results.Sessions)
            {
                GameObject buttonObj = Instantiate(serverButtonPrefab, serverListContainer);
                
                TextMeshProUGUI btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                btnText.text = $"{sessionInfo.Name} (Max: {sessionInfo.MaxPlayers})";

                Button btn = buttonObj.GetComponent<Button>();
                string sessionId = sessionInfo.Id; 
                btn.onClick.AddListener(() => JoinServerById(sessionId));
            }
        }
        catch (SessionException e)
        {
            Debug.LogError(e);
            UpdateStatus("Search failed.");
        }
    }

    // --- 3. JOIN A SERVER ---
    private async void JoinServerById(string sessionId)
    {
        UpdateStatus("Joining Ride...");
        try
        {
            var joinOptions = new JoinSessionOptions();
            await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, joinOptions);
            
            UpdateStatus("Joined!");
            HideUI();
        }
        catch (SessionException e)
        {
            Debug.LogError(e);
            UpdateStatus("Failed to join.");
        }
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void HideUI()
    {
        if (multiplayerUIPanel != null) multiplayerUIPanel.SetActive(false);
        
        // Turn the HUD back up to full visibility!
        if (hudCanvas != null) 
        {
            CanvasGroup cg = hudCanvas.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }
        
        // Once the UI closes, lock the mouse again so you can play the game
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --- 4. RETURN TO LOBBY (Added for the Settings Menu) ---
    public void ShowUI()
    {
        // 1. Turn the Lobby UI back on
        if (multiplayerUIPanel != null) multiplayerUIPanel.SetActive(true);
        
        // 2. Hide the Game HUD again
        if (hudCanvas != null) 
        {
            CanvasGroup cg = hudCanvas.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }
        
        // 3. Unlock the mouse so you can click the server list
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 4. Refresh the server list so it's up to date
        RefreshServerList();
        UpdateStatus("Disconnected. Ready to join.");
    }
}