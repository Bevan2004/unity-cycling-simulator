using UnityEngine;
using TMPro; 
using SBPScripts; 
using FluffyUnderware.Curvy.Controllers; 
using Unity.Netcode; // REQUIRED for Multiplayer features

public class ZwiftHUD : NetworkBehaviour
{
    [Header("Target Bike")]
    // We don't drag this in anymore; the script finds it on the player.
    public BicycleController targetBike;

    // Internal UI references
    private TextMeshProUGUI speedText;
    private TextMeshProUGUI powerText;
    private TextMeshProUGUI gradeText;     
    private TextMeshProUGUI distanceText;  
    private TextMeshProUGUI elevationText; 
    private TextMeshProUGUI timeText; 

    [Header("Settings")]
    public bool showImperial = false; 

    private float _elapsedTime = 0f;
    private bool isReady = false;

    // ------------------------------------------------------------------
    // HYBRID SETUP: Works for both Offline (Ghost) and Online (Free Roam)
    // ------------------------------------------------------------------

    void Start()
    {
        // Check: Are we in the Offline scene (Desert)?
        // If there is no NetworkManager, or it's not active, we are single player.
        bool isOffline = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

        if (isOffline)
        {
            InitializeHUD();
        }
    }

    public override void OnNetworkSpawn()
    {
        // Check: Are we in the Online scene?
        // Only initialize if this is OUR bike.
        if (IsOwner)
        {
            InitializeHUD();
        }
        else
        {
            // If this script is on someone else's bike, turn it off.
            // We don't want their stats on our screen.
            enabled = false;
        }
    }

    void InitializeHUD()
    {
        // 1. Get the bike controller attached to THIS GameObject
        if (targetBike == null)
            targetBike = GetComponent<BicycleController>();

        // 2. FIND THE UI OBJECTS BY NAME
        // (Make sure your Scene UI objects have these EXACT names!)
        GameObject obj;

        obj = GameObject.Find("SpeedText");
        if (obj) speedText = obj.GetComponent<TextMeshProUGUI>();

        obj = GameObject.Find("PowerText");
        if (obj) powerText = obj.GetComponent<TextMeshProUGUI>();

        obj = GameObject.Find("GradeText");
        if (obj) gradeText = obj.GetComponent<TextMeshProUGUI>();

        obj = GameObject.Find("DistanceText");
        if (obj) distanceText = obj.GetComponent<TextMeshProUGUI>();

        obj = GameObject.Find("ElevationText");
        if (obj) elevationText = obj.GetComponent<TextMeshProUGUI>();

        obj = GameObject.Find("TimeText");
        if (obj) timeText = obj.GetComponent<TextMeshProUGUI>();

        isReady = true;
    }

    // ------------------------------------------------------------------
    // UPDATE LOOP
    // ------------------------------------------------------------------

    void Update()
    {
        // Only run if we are fully initialized and have a bike
        if (!isReady || targetBike == null) return;

        // --- 0. TIME ---
        _elapsedTime += Time.deltaTime;
        if (timeText != null)
        {
            System.TimeSpan t = System.TimeSpan.FromSeconds(_elapsedTime);
            timeText.text = (t.Hours > 0) 
                ? string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds)
                : string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }

        // --- 1. POWER (Watts) ---
        if (powerText != null)
        {
            powerText.text = targetBike.inputWatts.ToString("F0");
        }

        // --- 2. SPEED ---
        if (speedText != null)
        {
            float speed = targetBike.currentSpeedKph;
            if (showImperial) speed *= 0.621371f;
            
            speedText.text = speed.ToString("F0"); 
        }

        // --- 3. GRADE (Gradient) ---
        if (gradeText != null)
        {
            float grade = targetBike.currentGradePercent;
            gradeText.text = grade.ToString("F1") + "%"; // Added % sign
            
            if (grade > 5f) gradeText.color = new Color(1f, 0.5f, 0f); // Orange
            else if (grade > 2f) gradeText.color = Color.yellow;
            else gradeText.color = Color.white;
        }

        // --- 4. DISTANCE ---
        if (distanceText != null && targetBike.splineController != null)
        {
            float distMeters = targetBike.splineController.AbsolutePosition;
            float distKm = distMeters / 1000f;
            
            if (showImperial)
            {
                distanceText.text = (distKm * 0.621371f).ToString("F1");
            }
            else
            {
                distanceText.text = distKm.ToString("F1");
            }
        }

        // --- 5. ELEVATION ---
        if (elevationText != null)
        {
            float altMeters = targetBike.transform.position.y;
            elevationText.text = altMeters.ToString("F0");
        }
    }
}