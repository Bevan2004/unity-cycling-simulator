using UnityEngine;
using TMPro; 
using System.Collections;
using SBPScripts; 

public class GhostCountdown : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI countdownText; 
    
    [Header("Manager Reference")]
    public GhostManager manager;

    [Header("Player Control")]
    public BicycleController bikeController; 

    void Start()
    {
        Debug.Log("Countdown Script: Started!"); // DEBUG 1
        StartCoroutine(RunCountdown());
    }

    IEnumerator RunCountdown()
    {
        Debug.Log("Countdown: Locking Bike..."); // DEBUG 2
        
        // Safety Check 1: Is the bike assigned?
        if (bikeController != null)
        {
            // Try to find Rigidbody safely
            Rigidbody rb = bikeController.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; 
        }
        else
        {
            Debug.LogError("Countdown Error: Player Bike is missing!");
        }

        // Safety Check 2: Is text assigned?
        if (countdownText != null)
        {
            countdownText.text = "READY";
            Debug.Log("Countdown: READY shown");
        }
        else
        {
            Debug.LogError("Countdown Error: Text object is missing!");
            yield break; // Stop here if no text
        }

        yield return new WaitForSeconds(1f);

        if(countdownText != null) countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        if(countdownText != null) countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        if(countdownText != null) countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        if(countdownText != null) countdownText.text = "GO!";
        Debug.Log("Countdown: GO!");

        // Unlock bike
        if (bikeController != null) 
        {
            Rigidbody rb = bikeController.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }
        
        // Start Manager
        if (manager != null)
        {
            manager.StartRace();
        }
        else
        {
            Debug.LogError("Countdown Error: Manager is missing!");
        }

        yield return new WaitForSeconds(1f);
        if(countdownText != null) countdownText.text = ""; 
    }
}