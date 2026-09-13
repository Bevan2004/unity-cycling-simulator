using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class BluetoothManager : MonoBehaviour
{
    public static BluetoothManager Instance;

    [Header("Bluetooth Status")]
    public string bikeName = "Scanning...";
    public bool isConnected = false;
    public float LiveWatts = 0f;

    // The universal Bluetooth codes for Smart Bikes
    private string targetService = "{00001818-0000-1000-8000-00805f9b34fb}"; 
    private string targetCharacteristic = "{00002a63-0000-1000-8000-00805f9b34fb}";

    // --- IMPORTING THE EXTERNAL DLL ---
    [DllImport("BleWinrtDll.dll", EntryPoint = "StartDeviceScan")]
    public static extern void StartDeviceScan();

    [DllImport("BleWinrtDll.dll", EntryPoint = "StopDeviceScan")]
    public static extern void StopDeviceScan();

    [DllImport("BleWinrtDll.dll", EntryPoint = "ScanDevices")]
    public static extern void ScanDevices(IntPtr deviceData, int bufferSize);

    // FIXED: Using the correct entry point name for the C++ DLL
    [DllImport("BleWinrtDll.dll", EntryPoint = "SubscribeCharacteristic")]
    public static extern void SubscribeCharacteristic(string deviceId, string serviceUuid, string characteristicUuid);

    [DllImport("BleWinrtDll.dll", EntryPoint = "PollData")]
    public static extern bool PollData(out BleData data);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BleData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string deviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string serviceUuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string characteristicUuid;
        public int size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] buf;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(ScanAndConnect());
    }

    IEnumerator ScanAndConnect()
    {
        Debug.Log("Starting Bluetooth Scan...");
        StartDeviceScan();

        // Let it scan for 3 seconds
        yield return new WaitForSeconds(3f);
        
        // Connect to the first device broadcasting the Cycling Power Service
        // FIXED: Calling the updated method name
        SubscribeCharacteristic("", targetService, targetCharacteristic);
        StopDeviceScan();
        
        isConnected = true;
        Debug.Log("Listening for Bike Data...");
    }

    void Update()
    {
        if (!isConnected) return;

        // Poll the DLL for new Bluetooth data
        BleData incomingData;
        while (PollData(out incomingData))
        {
            // If the data is from our Cycling Power characteristic, decode it!
            if (incomingData.characteristicUuid.Contains("2a63"))
            {
                DecodePowerData(incomingData.buf);
            }
        }
    }

    void DecodePowerData(byte[] data)
    {
        // The Cycling Power profile specifies that bytes 0 and 1 are configuration flags.
        // Bytes 2 and 3 contain the instantaneous power (Watts) as a 16-bit integer (Little Endian).
        if (data != null && data.Length >= 4)
        {
            // Combine byte 2 and 3 into a readable number
            int watts = BitConverter.ToInt16(data, 2);
            LiveWatts = watts;
            
            // Note: I left out the Debug.Log here so it doesn't spam your console. 
            // Your BluetoothDebug.cs script will handle printing the clean logs!
        }
    }

    private void OnApplicationQuit()
    {
        StopDeviceScan();
    }
}