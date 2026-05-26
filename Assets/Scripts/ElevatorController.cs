using System.Collections;
using UnityEngine;
using Mirror;

public static class ElevatorState
{
    public static bool   IsResuming        = false;
    public static float  ResumeProgress    = 0f;
    public static string ResumeElevatorID  = "";
}

public class ElevatorController : NetworkBehaviour
{
    [Header("Elevator ID")]
    [SerializeField] private string elevatorID = "Enter";

    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad = "NextFloor";

    [Header("Elevator Settings")]
    [SerializeField] private float rideDuration        = 4f;
    [SerializeField] private float moveDistance        = -8f;
    [Range(0f, 0.99f)]
    [SerializeField] private float sceneSwapAtProgress = 0.5f;

    [SyncVar] private bool  isRunning     = false;
    [SyncVar] private float syncedElapsed = 0f;

    private Vector3 startPos;
    private Vector3 endPos;

    private void Awake()
    {
        startPos = transform.position;
        endPos   = startPos + Vector3.up * moveDistance;
    }

    public override void OnStartServer()
    {
        if (!ElevatorState.IsResuming) return;
        if (ElevatorState.ResumeElevatorID != elevatorID) return;

        ElevatorState.IsResuming = false;
        isRunning     = true;
        syncedElapsed = ElevatorState.ResumeProgress * rideDuration;

        float t = Mathf.SmoothStep(0f, 1f, ElevatorState.ResumeProgress);
        transform.position = startPos + Vector3.up * (moveDistance * t);

        StartCoroutine(ElevatorRoutine(beginSceneLoad: false));
    }

    [Server]
    public void Activate()
    {
        if (isRunning) return;
        isRunning = true;
        StartCoroutine(ElevatorRoutine(beginSceneLoad: true));
    }

    private IEnumerator ElevatorRoutine(bool beginSceneLoad)
    {
        Debug.Log($"[Elevator: {elevatorID}] Running.");

        bool  sceneSwapTriggered = false;
        float elapsed = syncedElapsed;

        if (!beginSceneLoad)
        {
            float resumeT = Mathf.SmoothStep(0f, 1f, ElevatorState.ResumeProgress);
            startPos = transform.position - Vector3.up * (moveDistance * resumeT);
            endPos   = startPos + Vector3.up * moveDistance;
        }

        while (elapsed < rideDuration)
        {
            elapsed       += Time.deltaTime;
            syncedElapsed  = elapsed;

            float progress = Mathf.Clamp01(elapsed / rideDuration);
            float t        = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(startPos, endPos, t);

            if (beginSceneLoad && !sceneSwapTriggered && progress >= sceneSwapAtProgress)
            {
                sceneSwapTriggered           = true;
                ElevatorState.IsResuming     = true;
                ElevatorState.ResumeProgress  = progress;
                ElevatorState.ResumeElevatorID = elevatorID;

                Debug.Log($"[Elevator: {elevatorID}] Changing scene to {sceneToLoad}...");
                NetworkManager.singleton.ServerChangeScene(sceneToLoad);
                yield break;
            }

            yield return null;
        }

        Debug.Log($"[Elevator: {elevatorID}] Ride complete.");
        isRunning = false;
    }
}