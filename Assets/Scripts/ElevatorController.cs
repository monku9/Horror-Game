using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Holds mid-ride state between scenes so the elevator can resume seamlessly.
/// This is static, so it survives scene loads automatically.
/// </summary>
public static class ElevatorState
{
    public static bool IsResuming   = false;
    public static float ResumeProgress = 0f; // 0..1 how far through the ride we were
}

/// <summary>
/// ElevatorController - Unity 6
///
/// HOW TO USE:
///   1. Attach this script to your elevator GameObject in BOTH scenes.
///   2. In Scene A: fill in "Scene To Load", "Ride Duration", "Move Distance" (negative = down).
///   3. In Scene B: the elevator will auto-resume mid-ride if ElevatorState.IsResuming is true.
///      Set the same "Ride Duration" and "Move Distance" values so movement feels continuous.
///   4. Make sure your target scene is added to File > Build Settings.
/// </summary>
public class ElevatorController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The exact name of the scene to load (must be in Build Settings).")]
    [SerializeField] private string sceneToLoad = "NextFloor";

    [Header("Elevator Settings")]
    [Tooltip("Total duration of the elevator ride in seconds.")]
    [SerializeField] private float rideDuration = 4f;

    [Tooltip("Total distance the elevator travels. Negative = down.")]
    [SerializeField] private float moveDistance = -8f;

    [Tooltip("0..1 — how far through the ride to trigger the scene switch. 0.5 = halfway.")]
    [Range(0f, 0.99f)]
    [SerializeField] private float sceneSwapAtProgress = 0.4f;

    [Tooltip("Key the player presses to start the elevator (ignored if resuming).")]
    [SerializeField] private KeyCode activateKey = KeyCode.E;

    // ---------------------------------------------------------------

    private bool  isRunning  = false;
    private float elapsed    = 0f;

    // ---------------------------------------------------------------

    private void Start()
    {
        // If Scene B is loading in, resume mid-ride automatically
        if (ElevatorState.IsResuming)
        {
            ElevatorState.IsResuming = false;
            isRunning = true;
            elapsed   = ElevatorState.ResumeProgress * rideDuration;

            // Immediately snap elevator to where it should be at this point
            float t = Mathf.SmoothStep(0f, 1f, ElevatorState.ResumeProgress);
            transform.position += Vector3.up * (moveDistance * t);

            StartCoroutine(ElevatorRoutine(beginAsync: false));
        }
    }

    private void Update()
    {
        if (!isRunning && Input.GetKeyDown(activateKey))
        {
            isRunning = true;
            StartCoroutine(ElevatorRoutine(beginAsync: true));
        }
    }

    // ---------------------------------------------------------------

    private IEnumerator ElevatorRoutine(bool beginAsync)
    {
        Debug.Log("[Elevator] Running. Scene swap at " + (sceneSwapAtProgress * 100f) + "% progress.");

        AsyncOperation asyncLoad = null;
        bool sceneSwapTriggered  = false;

        Vector3 startPos = transform.position;
        Vector3 endPos   = startPos + Vector3.up * moveDistance;

        // If resuming, recompute start/end so Lerp stays consistent
        if (!beginAsync)
        {
            float resumeT = Mathf.SmoothStep(0f, 1f, ElevatorState.ResumeProgress);
            startPos = transform.position - Vector3.up * (moveDistance * resumeT);
            endPos   = startPos + Vector3.up * moveDistance;
        }

        if (beginAsync)
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncLoad.allowSceneActivation = false;
        }

        while (elapsed < rideDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / rideDuration);
            float t        = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(startPos, endPos, t);

            // Trigger scene swap at the configured progress point
            if (beginAsync && !sceneSwapTriggered && progress >= sceneSwapAtProgress)
            {
                sceneSwapTriggered = true;
                Debug.Log("[Elevator] Swap point reached — waiting for scene to be ready...");

                // Save state so Scene B can resume
                ElevatorState.IsResuming    = true;
                ElevatorState.ResumeProgress = progress;

                // Wait until loaded, then activate
                while (asyncLoad.progress < 0.9f)
                    yield return null;

                asyncLoad.allowSceneActivation = true;
                yield break; // Scene A is done
            }

            yield return null;
        }

        Debug.Log("[Elevator] Ride complete.");
        isRunning = false;
    }
}