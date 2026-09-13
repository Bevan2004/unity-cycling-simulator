using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Threading.Tasks;

public class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance;
    public string MyDisplayName;
    private const string ChannelName = "FreeRoamGlobal";

    async void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        await InitializeVivox();
    }

    async Task InitializeVivox()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        await VivoxService.Instance.InitializeAsync();
        LoginToVivox();
    }

    async void LoginToVivox()
    {
        MyDisplayName = "Rider " + Random.Range(100, 999);
        LoginOptions options = new LoginOptions();
        options.DisplayName = MyDisplayName;

        try
        {
            await VivoxService.Instance.LoginAsync(options);
            Debug.Log("Logged in as: " + MyDisplayName);
            Join3DChannel();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Login Failed: " + e.Message);
        }
    }

    async void Join3DChannel()
    {
        try
        {
            Channel3DProperties properties = new Channel3DProperties(30, 2, 1.0f, AudioFadeModel.InverseByDistance);
            await VivoxService.Instance.JoinPositionalChannelAsync(ChannelName, ChatCapability.AudioOnly, properties);
            Debug.Log("Joined 3D Channel: " + ChannelName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Join 3D Failed: " + e.Message);
        }
    }

    public void ToggleMutePlayer(string usernameToMute, bool shouldMute)
    {
        if (VivoxService.Instance.ActiveChannels.ContainsKey(ChannelName))
        {
            var channel = VivoxService.Instance.ActiveChannels[ChannelName];
            foreach (var participant in channel)
            {
                if (participant.DisplayName == usernameToMute)
                {
                    if (shouldMute) participant.MutePlayerLocally();
                    else participant.UnmutePlayerLocally();
                }
            }
        }
    }

    public void SetVoiceVolume(float volumePercent)
    {
        int volume = Mathf.Clamp((int)volumePercent, 0, 100);
        VivoxService.Instance.SetOutputDeviceVolume(volume);
    }

    public void MuteVoiceChat(bool shouldMute)
    {
        if (shouldMute) VivoxService.Instance.SetOutputDeviceVolume(0);
        else VivoxService.Instance.SetOutputDeviceVolume(50);
    }

    // --- NEW: Leave Channel Command ---
    public void LeaveAllChannels()
    {
        if (VivoxService.Instance.ActiveChannels.ContainsKey(ChannelName))
        {
            VivoxService.Instance.LeaveChannelAsync(ChannelName);
        }
    }
}