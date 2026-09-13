using UnityEngine;
using Unity.Netcode;

public class SimpleNetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 250));

        // SAFETY CHECK: If NetworkManager is missing, stop here.
        if (NetworkManager.Singleton == null)
        {
            GUILayout.Label("NetworkManager MISSING!");
            GUILayout.EndArea();
            return;
        }

        // Only show buttons if we aren't connected yet
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host (P1)"))
            {
                NetworkManager.Singleton.StartHost();
            }

            if (GUILayout.Button("Start Client (P2)"))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
        else
        {
            string status = NetworkManager.Singleton.IsHost ? "Host" : "Client";
            GUILayout.Label("Status: " + status);
            
            // Optional: Disconnect button
            if (GUILayout.Button("Disconnect"))
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        GUILayout.EndArea();
    }
}