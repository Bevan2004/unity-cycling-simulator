using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Services.Vivox;
using Unity.Collections;

public class PlayerNameplate : NetworkBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI usernameText;
    public GameObject speakerIcon;
    public Canvas nameplateCanvas;
    
    // DRAG YOUR UI BUTTON HERE IN THE INSPECTOR
    public Button muteButton; 
    public Image muteButtonImage; // Drag the button's image component here

    private NetworkVariable<FixedString64Bytes> vivoxUsername = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes(""), 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );

    private Camera mainCamera;
    private bool isMuted = false;

    public override void OnNetworkSpawn()
    {
        vivoxUsername.OnValueChanged += OnNameChanged;

        if (IsOwner)
        {
            // Hide my own nameplate so I don't block my view
            if (nameplateCanvas != null) nameplateCanvas.gameObject.SetActive(false);
            
            // Send my name to the network
            if (VivoxManager.Instance != null && !string.IsNullOrEmpty(VivoxManager.Instance.MyDisplayName))
            {
                vivoxUsername.Value = new FixedString64Bytes(VivoxManager.Instance.MyDisplayName);
            }
        }
        else
        {
            SetName(vivoxUsername.Value.ToString());
        }
    }

    private void OnNameChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
    {
        SetName(newName.ToString());
    }

    void Start()
    {
        mainCamera = Camera.main;
        if(speakerIcon != null) speakerIcon.SetActive(false);

        // SETUP THE MUTE BUTTON CLICK LISTENER
        if (muteButton != null)
        {
            muteButton.onClick.AddListener(ToggleMute);
        }
    }

    // This function runs when you click the button
    void ToggleMute()
    {
        isMuted = !isMuted;

        // 1. Tell VivoxManager to actually mute the audio
        VivoxManager.Instance.ToggleMutePlayer(vivoxUsername.Value.ToString(), isMuted);

        // 2. Change the button color so you know they are muted
        if (muteButtonImage != null)
        {
            muteButtonImage.color = isMuted ? Color.red : Color.white;
        }
    }

    void LateUpdate()
    {
        // Make the nameplate always face the camera
        if (nameplateCanvas != null && nameplateCanvas.gameObject.activeInHierarchy && mainCamera != null)
        {
            nameplateCanvas.transform.rotation = mainCamera.transform.rotation;
        }
        UpdateSpeakerIcon();
    }

    void UpdateSpeakerIcon()
    {
        if (VivoxService.Instance == null || string.IsNullOrEmpty(vivoxUsername.Value.ToString())) return;

        bool isTalking = false;
        foreach (var channel in VivoxService.Instance.ActiveChannels)
        {
            foreach (var participant in channel.Value)
            {
                if (participant.DisplayName == vivoxUsername.Value.ToString())
                {
                    if (participant.SpeechDetected) isTalking = true;
                }
            }
        }

        if (speakerIcon != null)
        {
            if (speakerIcon.activeSelf != isTalking) speakerIcon.SetActive(isTalking);
        }
    }

    public void SetName(string newName)
    {
        if (usernameText != null) usernameText.text = newName;
    }
}