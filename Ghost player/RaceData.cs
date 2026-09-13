using UnityEngine;
using System.Collections.Generic;

namespace SBPScripts
{
    [System.Serializable]
    public struct GhostFrame
    {
        public float timeStamp; // Time since start
        public float distance;  // Meters along the track (Spline Position)
        public float speed;     // How fast were we going? (For animation)
    }

    [System.Serializable]
    public class RaceData
    {
        public float totalTime;
        public List<GhostFrame> frames = new List<GhostFrame>();
    }
}