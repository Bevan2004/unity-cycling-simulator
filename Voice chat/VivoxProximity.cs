using UnityEngine;
using Unity.Netcode;
using Unity.Services.Vivox;

public class VivoxProximity : NetworkBehaviour
{
    public Transform headPosition; // Optional: Drag the rider's head here

    private float updateTimer = 0f;
    private float updateRate = 0.3f; 

    void Update()
    {
        // Only run for the LOCAL player (Me)
        if (!IsOwner) return;

        // Ensure we are connected
        if (VivoxService.Instance == null || VivoxService.Instance.ActiveChannels.Count == 0) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateRate)
        {
            UpdateVoicePosition();
            updateTimer = 0f;
        }
    }

    void UpdateVoicePosition()
    {
        Vector3 pos = headPosition != null ? headPosition.position : transform.position;
        Vector3 forward = headPosition != null ? headPosition.forward : transform.forward;
        Vector3 up = headPosition != null ? headPosition.up : transform.up;

        // Loop through active channels
        foreach (var channel in VivoxService.Instance.ActiveChannels)
        {
            // FIX: Swapped arguments. Position comes FIRST, Channel Name comes LAST.
            VivoxService.Instance.Set3DPosition(pos, pos, forward, up, channel.Key);
        }
    }
}