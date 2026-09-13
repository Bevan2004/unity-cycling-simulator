using UnityEngine;

// Add this to your Curvy Spline Control Points (GameObjects)
public class ZwiftSegment : MonoBehaviour
{
    [Tooltip("The incline in percentage (e.g., 5 = 5% slope, -3 = 3% downhill).")]
    public float gradePercentage = 0f;

    [Tooltip("Visual helper to see this in the scene view")]
    private void OnDrawGizmos()
    {
        // Draw a label above the point showing the grade
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Zwift Grade: {gradePercentage}%");
        #endif
    }
}