using UnityEngine;
using Unity.Netcode;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using Unity.Cinemachine; // Keeps your Unity 6 Camera code
using SBPScripts;
using System.Collections;

public class BikeNetworkSetup : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // 1. SETUP PHYSICS & SPLINE (Local Player)
            SetupBikePhysics();

            // 2. DELAY CAMERA (Prevents the "0,0,0" camera bug)
            StartCoroutine(ConnectCameraDelayed());
        }
        else
        {
            // --- REMOTE PLAYER SETUP ---
            
            // CRITICAL FIX: We leave this ON so the legs can animate!
            // GetComponent<BicycleController>().enabled = false; <--- REMOVED THIS
            
            // We DO disable the SplineController to prevent physics fighting
            var remoteSpline = GetComponent<SplineController>();
            if (remoteSpline != null) remoteSpline.enabled = false; 

            // Disable HUD for other players
            var hud = GetComponent<ZwiftHUD>();
            if (hud != null) hud.enabled = false;
        }
    }

    void SetupBikePhysics()
    {
        var mySplineController = GetComponent<SplineController>();
        var myBikeController = GetComponent<BicycleController>();
        GameObject trackObj = GameObject.Find("RaceTrack");

        if (trackObj != null && mySplineController != null)
        {
            CurvySpline trackData = trackObj.GetComponentInChildren<CurvySpline>();
            if (trackData != null)
            {
                mySplineController.Spline = trackData;
                myBikeController.splineController = mySplineController;

                float startPos = (float)OwnerClientId * 10f; 
                mySplineController.RelativePosition = 0; 
                mySplineController.AbsolutePosition = startPos; 
                
                Vector3 startPoint = trackData.InterpolateByDistance(startPos);
                transform.position = startPoint;
                
                mySplineController.enabled = true;
            }
        }
    }

    // THIS IS YOUR ORIGINAL CAMERA CODE (KEPT EXACTLY AS IS)
    IEnumerator ConnectCameraDelayed()
    {
        yield return new WaitForSeconds(0.1f);

        var vCam = FindFirstObjectByType<CinemachineCamera>();
        if (vCam != null)
        {
            vCam.Follow = transform;
            vCam.LookAt = transform;
            vCam.PreviousStateIsValid = false; 
            vCam.enabled = false;
            vCam.enabled = true;
        }
    }
}