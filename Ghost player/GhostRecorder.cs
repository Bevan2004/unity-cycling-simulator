using UnityEngine;
using SBPScripts;
using FluffyUnderware.Curvy.Controllers; // Needed for Spline

public class GhostRecorder : MonoBehaviour
{
    [Header("References")]
    public BicycleController bikeController;
    public SplineController splineController; // ASSIGN THIS in Inspector!

    [Header("Data")]
    public RaceData currentRaceData = new RaceData();
    private bool isRecording = false;
    private float startTime;

    void Start()
    {
        // Auto-find components if you forgot to drag them
        if (bikeController == null) bikeController = GetComponent<BicycleController>();
        if (splineController == null) splineController = GetComponent<SplineController>();
    }

    public void StartRecording()
    {
        if (splineController == null)
        {
            Debug.LogError("GhostRecorder: NO SPLINE CONTROLLER FOUND! Cannot record.");
            return;
        }

        currentRaceData = new RaceData();
        currentRaceData.frames.Clear();
        startTime = Time.time;
        isRecording = true;
        Debug.Log($"Ghost Recorder: Started at {startTime}");
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        
        isRecording = false;
        currentRaceData.totalTime = Time.time - startTime;
        Debug.Log($"Ghost Recorder: Stopped. Captured {currentRaceData.frames.Count} frames.");
    }

    void FixedUpdate()
    {
        if (isRecording)
        {
            GhostFrame frame = new GhostFrame();
            frame.timeStamp = Time.time - startTime;
            
            // KEY CHANGE: We record WHERE you are on the track
            frame.distance = (float)splineController.AbsolutePosition;
            
            // We record speed just for animation purposes
            if (bikeController != null)
                frame.speed = bikeController.rb.linearVelocity.magnitude; // Use .velocity for Unity 2022 or older
            
            currentRaceData.frames.Add(frame);
        }
    }
}