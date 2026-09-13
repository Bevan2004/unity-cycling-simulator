using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;

public class DiscordAuth : MonoBehaviour
{
    [Header("Discord App Details")]
    public string clientId = "YOUR_CLIENT_ID";
    public string clientSecret = "YOUR_CLIENT_SECRET"; 
    public string guildId = "YOUR_SERVER_ID";          
    public string requiredRoleId = "YOUR_ROLE_ID";     

    private string redirectUri = "http://localhost:3000/";

    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject multiplayerPanel;

    // State flags to safely update UI on the main Unity thread
    private bool loginSuccessful = false;
    private string errorMessage = "";

    void Update()
    {
        // If PlayFab successfully logged them in, swap the UI
        if (loginSuccessful)
        {
            loginSuccessful = false;
            loginPanel.SetActive(false);
            multiplayerPanel.SetActive(true);
            Debug.Log("SUCCESS: Role verified and PlayFab account linked!");
        }
        
        // If they don't have the role, print the error
        if (errorMessage != "")
        {
            Debug.LogError(errorMessage);
            errorMessage = "";
        }
    }

    public void OpenDiscordLogin()
    {
        // Ask Discord for permission to see their profile and roles
        string authUrl = $"https://discord.com/oauth2/authorize?client_id={clientId}&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A3000%2F&scope=identify%20guilds.members.read";
        Application.OpenURL(authUrl);
        ListenForDiscordCallback();
    }

    private async void ListenForDiscordCallback()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:3000/");
        listener.Start();
        Debug.Log("Listening for Discord Login...");

        HttpListenerContext context = await listener.GetContextAsync();
        string authCode = context.Request.QueryString["code"];

        // Send a message to the browser so the player knows it worked
        HttpListenerResponse response = context.Response;
        string responseText = "<html><body style='background-color:#2c2f33; color:white; text-align:center; font-family:sans-serif; margin-top:50px;'><h2>Authenticating...</h2><p>You can close this tab and return to the game.</p></body></html>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
        response.ContentLength64 = buffer.Length;
        Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
        listener.Stop();

        // Start the secure role check
        await VerifyRoleAndLogin(authCode);
    }

    private async Task VerifyRoleAndLogin(string authCode)
    {
        // 1. Ask Discord to trade the code for an Access Token
        WWWForm tokenForm = new WWWForm();
        tokenForm.AddField("client_id", clientId);
        tokenForm.AddField("client_secret", clientSecret);
        tokenForm.AddField("grant_type", "authorization_code");
        tokenForm.AddField("code", authCode);
        tokenForm.AddField("redirect_uri", redirectUri);

        UnityWebRequest tokenReq = UnityWebRequest.Post("https://discord.com/api/oauth2/token", tokenForm);
        tokenReq.SendWebRequest();
        while (!tokenReq.isDone) await Task.Yield(); // Wait for the web request to finish

        if (tokenReq.result != UnityWebRequest.Result.Success)
        {
            errorMessage = "Failed to communicate with Discord.";
            return;
        }

        TokenResponse tokenRes = JsonUtility.FromJson<TokenResponse>(tokenReq.downloadHandler.text);

        // 2. Ask Discord to read their profile inside YOUR specific server
        UnityWebRequest memberReq = UnityWebRequest.Get($"https://discord.com/api/users/@me/guilds/{guildId}/member");
        memberReq.SetRequestHeader("Authorization", $"Bearer {tokenRes.access_token}");
        memberReq.SendWebRequest();
        while (!memberReq.isDone) await Task.Yield();

        if (memberReq.result != UnityWebRequest.Result.Success)
        {
            errorMessage = "ACCESS DENIED: You are not in the official Discord Server!";
            return;
        }

        DiscordMember memberInfo = JsonUtility.FromJson<DiscordMember>(memberReq.downloadHandler.text);

        // 3. Check if their profile contains the Patreon Role
        bool hasRole = false;
        foreach (string role in memberInfo.roles)
        {
            if (role == requiredRoleId) hasRole = true;
        }

        if (!hasRole)
        {
            errorMessage = "ACCESS DENIED: You do not have the Patreon Premium Role!";
            return;
        }

        // 4. THEY PASSED! Log them into PlayFab and print the exact error if it fails
        var request = new LoginWithCustomIDRequest { CustomId = memberInfo.user.id, CreateAccount = true };
        PlayFabClientAPI.LoginWithCustomID(request, 
            result => { loginSuccessful = true; }, 
            error => { errorMessage = "PlayFab Error: " + error.GenerateErrorReport(); }); 
    }
}

// --- JSON Helper Classes for Unity ---
[System.Serializable] public class TokenResponse { public string access_token; }
[System.Serializable] public class DiscordMember { public string[] roles; public DiscordUser user; }
[System.Serializable] public class DiscordUser { public string id; }