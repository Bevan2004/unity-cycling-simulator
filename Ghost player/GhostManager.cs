using UnityEngine;
using System.IO;
using SBPScripts;

public class GhostManager : MonoBehaviour
{
    [Header("References")]
    public GhostRecorder recorder;
    public GhostReplayer ghost;
    
    private string savePath;

    void Start()
    {
        savePath = Application.persistentDataPath + "/ghost_data.json";
        
        // 1. Prepare the Ghost (Load data, but DO NOT move yet)
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            RaceData data = JsonUtility.FromJson<RaceData>(json);
            
            if (data != null && data.frames.Count > 0)
            {
                ghost.gameObject.SetActive(true);
                ghost.raceData = data; // Give it the map
                // ghost.StartReplay() is REMOVED from here. We wait for StartRace().
                Debug.Log($"Ghost Loaded! Ready to race.");
            }
        }
    }

    // This function runs when the Countdown says "GO!"
    public void StartRace()
    {
        Debug.Log("GO! Race Started.");
        
        // 1. Start the Player Recording
        if (recorder != null) recorder.StartRecording();

        // 2. Start the Ghost Replay (if it exists)
        if (ghost != null && ghost.gameObject.activeSelf)
        {
            ghost.StartReplay(ghost.raceData);
        }
    }

    public void FinishLineCrossed()
    {
        if (recorder == null) return;
        recorder.StopRecording();
        
        float newTime = recorder.currentRaceData.totalTime;
        Debug.Log($"Lap Finished! Time: {newTime:F2}s");

        RaceData bestRun = null;
        if (File.Exists(savePath))
        {
            bestRun = JsonUtility.FromJson<RaceData>(File.ReadAllText(savePath));
        }

        if (bestRun == null || newTime < bestRun.totalTime)
        {
            string json = JsonUtility.ToJson(recorder.currentRaceData);
            File.WriteAllText(savePath, json);
            Debug.Log($"<color=green>NEW RECORD! Saved: {newTime:F2}s</color>");
        }
    }
}