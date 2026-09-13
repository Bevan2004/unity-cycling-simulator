using UnityEngine;
using SBPScripts;
using FluffyUnderware.Curvy.Controllers;

public class GhostReplayer : MonoBehaviour
{
    [Header("References")]
    public BicycleController bikeController;
    public SplineController splineController; // ASSIGN THIS in Inspector!
    
    [Header("State")]
    public RaceData raceData;
    private bool isPlaying = false;
    private float startTime;
    private int playIndex = 0;

    public void StartReplay(RaceData data)
    {
        // Basic error checking
        if (data == null || data.frames.Count == 0)
        {
            Debug.LogWarning("GhostReplayer: No data to play!");
            return;
        }
        
        // Auto-find if missing
        if (splineController == null) splineController = GetComponent<SplineController>();
        
        // Ensure Spline Controller is ON so we can move it
        if (splineController != null) splineController.enabled = true;

        raceData = data;
        startTime = Time.time;
        playIndex = 0;
        isPlaying = true;
        
        // Disable physics on the ghost so it doesn't fight us
        if (bikeController != null)
        {
            bikeController.enabled = false; // Turn off physics script
            bikeController.rb.isKinematic = true; // Turn off gravity/collisions
        }
        
        Debug.Log("Ghost Replay Started!");
    }

    void FixedUpdate()
    {
        if (!isPlaying || raceData == null) return;

        float currentTime = Time.time - startTime;

        // 1. End of tape check
        if (playIndex >= raceData.frames.Count - 1)
        {
            // Optional: Stop or Loop
            return;
        }

        // 2. Fast forward to the correct frame for right now
        while (playIndex < raceData.frames.Count - 2 && raceData.frames[playIndex + 1].timeStamp < currentTime)
        {
            playIndex++;
        }

        // 3. Get the frame data
        GhostFrame currentFrame = raceData.frames[playIndex];
        GhostFrame nextFrame = raceData.frames[playIndex + 1];

        // 4. Smoothly interpolate between frames (makes it look smooth even at low FPS)
        float timeGap = nextFrame.timeStamp - currentFrame.timeStamp;
        float progress = (currentTime - currentFrame.timeStamp) / timeGap;
        
        float targetDistance = Mathf.Lerp(currentFrame.distance, nextFrame.distance, progress);

        // 5. Apply Position to Spline
        if (splineController != null)
        {
            splineController.AbsolutePosition = targetDistance;
        }
    }
}