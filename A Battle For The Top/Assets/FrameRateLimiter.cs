using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    [SerializeField]
    private int targetFrameRate = 60; // Default target frame rate

    void Start()
    {
        // Set the target frame rate
        Application.targetFrameRate = targetFrameRate;
    }

    void Update()
    {
        // Optionally, you can update the frame rate dynamically if needed
        if (Application.targetFrameRate != targetFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }

    // Allow changing the target frame rate via script
    public void SetTargetFrameRate(int newTargetFrameRate)
    {
        targetFrameRate = newTargetFrameRate;
        Application.targetFrameRate = newTargetFrameRate;
    }
}