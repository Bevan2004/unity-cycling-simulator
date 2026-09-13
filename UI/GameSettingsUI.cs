using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GameSettingsUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;   
    public Button openSettingsButton;  
    public Button closeSettingsButton; 

    [Header("Audio Controls")]
    public Slider volumeSlider;        
    public Toggle muteToggle;          

    [Header("Game Controls")]
    public Button exitGameButton;      

    [Header("References")]
    // ADDED: We need a reference to the browser to tell it to turn back on!
    public InSceneServerBrowser serverBrowser; 

    private bool isMenuOpen = false;

    void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0;
            volumeSlider.maxValue = 100;
            volumeSlider.value = 50; 
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (muteToggle != null)
        {
            muteToggle.isOn = false;
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
        }

        if (openSettingsButton != null) openSettingsButton.onClick.AddListener(ToggleMenu);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(ToggleMenu);
        if (exitGameButton != null) exitGameButton.onClick.AddListener(OnExitClicked);

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (settingsPanel != null) settingsPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnVolumeChanged(float value)
    {
        // Add your Vivox volume logic back here if you have it!
    }

    void OnMuteToggled(bool isMuted)
    {
        // Add your Vivox mute logic back here if you have it!
    }

    // --- UPDATED EXIT LOGIC ---
    void OnExitClicked()
    {
        // 1. Shut down Multiplayer (Disconnect from server)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 2. Hide the Settings Menu
        if (isMenuOpen) ToggleMenu(); 

        // 3. Tell the Server Browser to turn back on!
        if (serverBrowser != null)
        {
            serverBrowser.ShowUI();
        }
        else
        {
            Debug.LogError("Server Browser reference is missing in GameSettingsUI! Please drag it in the Inspector.");
        }
    }
}