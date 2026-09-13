using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;  // Drag your Bike here

    [Header("Settings")]
    public float height = 100f;     // How high the camera sits
    public bool rotateWithPlayer = true; // True = Map spins. False = North is always Up.

    void LateUpdate()
    {
        if (playerTarget == null) return;

        // 1. Follow Position (X and Z only)
        // We keep the Y fixed so the camera doesn't bounce up and down with hills
        Vector3 newPos = playerTarget.position;
        newPos.y = height;
        transform.position = newPos;

        // 2. Handle Rotation
        if (rotateWithPlayer)
        {
            // Rotate the camera to match the player's facing direction
            // We keep X at 90 (looking down) and apply the player's Y rotation
            transform.rotation = Quaternion.Euler(90f, playerTarget.eulerAngles.y, 0f);
        }
        else
        {
            // Lock rotation so North is always Up
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}