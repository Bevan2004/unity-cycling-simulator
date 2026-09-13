using UnityEngine;

public class RiderAudio : MonoBehaviour
{
    [Header("Drag your two Audio Sources here from the Inspector")]
    public AudioSource pedalingSound;
    public AudioSource freewheelSound;

    void Start()
    {
        if (pedalingSound != null && !pedalingSound.isPlaying) pedalingSound.Play();
        if (freewheelSound != null && !freewheelSound.isPlaying) freewheelSound.Play();

        if (pedalingSound != null) pedalingSound.volume = 0f;
        if (freewheelSound != null) freewheelSound.volume = 0f;
    }

    void Update()
    {
        if (FreeWindowsBike.Instance != null)
        {
            // STATE 1: PEDALING
            if (FreeWindowsBike.Instance.LiveWatts > 0)
            {
                if (pedalingSound != null)
                {
                    pedalingSound.volume = Mathf.Lerp(pedalingSound.volume, 1f, Time.deltaTime * 5f);
                    pedalingSound.pitch = 1f + (FreeWindowsBike.Instance.LiveWatts / 800f);
                }
                if (freewheelSound != null)
                {
                    freewheelSound.volume = Mathf.Lerp(freewheelSound.volume, 0f, Time.deltaTime * 10f);
                }
            }
            // STATE 2: COASTING
            else
            {
                if (pedalingSound != null)
                {
                    pedalingSound.volume = Mathf.Lerp(pedalingSound.volume, 0f, Time.deltaTime * 5f);
                    pedalingSound.pitch = 1f;
                }
                if (freewheelSound != null)
                {
                    freewheelSound.volume = Mathf.Lerp(freewheelSound.volume, 1f, Time.deltaTime * 5f);
                }
            }
        }
    }
}