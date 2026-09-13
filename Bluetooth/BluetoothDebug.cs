using UnityEngine;

public class BluetoothDebug : MonoBehaviour
{
    private bool wasConnected = false;
    private float lastWatts = -1f;

    void Update()
    {
        // Safety check to make sure the manager is in the scene
        if (BluetoothManager.Instance == null) return;

        // 1. Log Connection State Changes
        if (BluetoothManager.Instance.isConnected != wasConnected)
        {
            wasConnected = BluetoothManager.Instance.isConnected;
            
            if (wasConnected)
            {
                Debug.Log("<color=green>✅ BIKE CONNECTED!</color>");
            }
            else
            {
                Debug.Log("<color=yellow>⚠️ SCANNING FOR BIKE...</color>");
            }
        }

        // 2. Log Wattage Changes (Only when the number actually changes!)
        if (wasConnected && BluetoothManager.Instance.LiveWatts != lastWatts)
        {
            lastWatts = BluetoothManager.Instance.LiveWatts;
            Debug.Log($"🚴 Live Power: {lastWatts} W");
        }
    }
}