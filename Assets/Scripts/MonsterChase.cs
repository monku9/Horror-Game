using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// MonsterChase — Patrol → Chase → Investigate → Jumpscare
///
/// Jumpscare plays a monster Animator state, snaps the camera to face the monster,
/// then calls OnJumpscareComplete(). No UI image required.
///
/// Requires:
///   • Unity AI Navigation package (com.unity.ai.navigation)
///   • A baked NavMesh in the scene
///   • An Animator on the monster with a state triggered by "Jumpscare"
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterChase : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector Fields
    // ──────────────────────────────────────────────

    [Header("Target")]
    [Tooltip("Auto-found by 'Player' tag if left empty.")]
    public Transform player;

    [Header("Detection")]
    public float detectionRadius = 12f;
    [Tooltip("Half-angle of the FOV cone in degrees.")]
    public float fovAngle   = 60f;
    public LayerMask sightMask;

    [Header("Movement")]
    public float patrolSpeed   = 2f;
    public float chaseSpeed    = 6f;
    public float rotationSpeed = 8f;

    [Header("Patrol")]
    [Tooltip("Leave empty to use random wander points.")]
    public Transform[] waypoints;
    public float wanderRadius   = 10f;
    public float patrolWaitTime = 1.5f;

    [Header("Memory")]
    public float memoryDuration = 4f;

    [Header("Jumpscare")]
    [Tooltip("Distance that triggers the jumpscare.")]
    public float jumpscareRange = 1.5f;

    [Tooltip("Name of the Animator trigger parameter on the monster.")]
    public string jumpscareAnimTrigger = "Jumpscare";

    [Tooltip("Name of the Animator state that plays the jumpscare (used to read clip length).")]
    public string jumpscareStateName = "Jumpscare";

    [Tooltip("Fallback duration if the animation clip length can't be read.")]
    public float jumpscareFallbackDuration = 3f;

    [Tooltip("Screech / sting audio clip.")]
    public AudioClip jumpscareSound;
    [Range(0f, 1f)]
    public float jumpscareVolume = 1f;

    [Header("Camera Snap")]
    [Tooltip("The camera will be reparented and lerped to this local offset/angle during the jumpscare. " +
             "Leave null to skip camera snap.")]
    public Transform jumpscareCameraTarget;
    [Tooltip("How fast the camera moves into the jumpscare position (higher = snappier).")]
    public float cameraSnapSpeed = 8f;

    [Header("Audio (ambient)")]
    public AudioClip idleSound;
    public AudioClip chaseSound;

    // ──────────────────────────────────────────────
    //  Private State
    // ──────────────────────────────────────────────

    enum State { Patrol, Chase, Investigate, Jumpscare }

    State        _state = State.Patrol;
    NavMeshAgent _agent;
    Animator     _animator;
    AudioSource  _audio;

    // Patrol
    int   _waypointIndex   = 0;
    float _patrolWaitTimer = 0f;
    bool  _waitingAtPoint  = false;

    // Memory
    Vector3 _lastKnownPos;
    float   _memoryTimer = 0f;

    // Animator hashes
    static readonly int AnimSpeed     = Animator.StringToHash("Speed");
    static readonly int AnimIsChasing = Animator.StringToHash("IsChasing");

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────

    void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _audio    = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogWarning("[MonsterChase] No player found — tag your player 'Player' or assign it manually.");
        }

        SetState(State.Patrol);
    }

    void Update()
    {
        if (player == null || _state == State.Jumpscare) return;

        switch (_state)
        {
            case State.Patrol:      UpdatePatrol();      break;
            case State.Chase:       UpdateChase();       break;
            case State.Investigate: UpdateInvestigate(); break;
        }

        _animator?.SetFloat(AnimSpeed, _agent.velocity.magnitude);
    }

    // ──────────────────────────────────────────────
    //  State Updates
    // ──────────────────────────────────────────────

    void UpdatePatrol()
    {
        if (_waitingAtPoint)
        {
            _patrolWaitTimer -= Time.deltaTime;
            if (_patrolWaitTimer <= 0f)
            {
                _waitingAtPoint = false;
                MoveToNextWaypoint();
            }
        }
        else if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
        {
            _waitingAtPoint  = true;
            _patrolWaitTimer = patrolWaitTime;
        }

        CheckForPlayer();
    }

    void UpdateChase()
    {
        if (CanSeePlayer())
        {
            _lastKnownPos = player.position;
            _memoryTimer  = memoryDuration;

            if (Vector3.Distance(transform.position, player.position) <= jumpscareRange)
            {
                SetState(State.Jumpscare);
                return;
            }

            _agent.SetDestination(player.position);
        }
        else
        {
            SetState(State.Investigate);
        }
    }

    void UpdateInvestigate()
    {
        _memoryTimer -= Time.deltaTime;

        if (CanSeePlayer())   { SetState(State.Chase);  return; }

        if (_memoryTimer <= 0f ||
            (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f))
        {
            SetState(State.Patrol);
            return;
        }

        _agent.SetDestination(_lastKnownPos);
    }

    // ──────────────────────────────────────────────
    //  Jumpscare
    // ──────────────────────────────────────────────

    void TriggerJumpscare()
    {
        _agent.isStopped = true;
        _agent.ResetPath();

        // Instantly face the player
        FaceTarget(player.position);

        StartCoroutine(JumpscareRoutine());
    }

    IEnumerator JumpscareRoutine()
    {
        // ── 1. Disable player movement ──────────────────────────
        // Works with CharacterController or Rigidbody — adapt to your setup.
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // ── 2. Play monster jumpscare animation ─────────────────
        if (_animator != null)
        {
            _animator.SetFloat(AnimSpeed, 0f);
            _animator.SetBool(AnimIsChasing, false);
            _animator.SetTrigger(Animator.StringToHash(jumpscareAnimTrigger));
        }

        // ── 3. Play screech ─────────────────────────────────────
        if (jumpscareSound != null)
            AudioSource.PlayClipAtPoint(jumpscareSound,
                                        Camera.main.transform.position,
                                        jumpscareVolume);

        // ── 4. Snap camera toward monster ───────────────────────
        Camera cam = Camera.main;
        Transform originalCamParent = null;
        Vector3   originalCamLocalPos = Vector3.zero;
        Quaternion originalCamLocalRot = Quaternion.identity;

        if (cam != null && jumpscareCameraTarget != null)
        {
            // Save original parent info so we can restore later if needed
            originalCamParent   = cam.transform.parent;
            originalCamLocalPos = cam.transform.localPosition;
            originalCamLocalRot = cam.transform.localRotation;

            StartCoroutine(SnapCamera(cam));
        }

        // ── 5. Wait for animation to finish ─────────────────────
        float duration = GetAnimationClipLength(jumpscareStateName);
        if (duration <= 0f) duration = jumpscareFallbackDuration;

        yield return new WaitForSeconds(duration);

        // ── 6. Restore player + camera ──────────────────────────
        if (cc != null) cc.enabled = true;
        if (rb != null) rb.isKinematic = false;

        OnJumpscareComplete();
    }

    IEnumerator SnapCamera(Camera cam)
    {
        // Lerp the camera toward the jumpscareCameraTarget world position/rotation
        float elapsed  = 0f;
        float snapTime = 0.25f; // seconds to reach the target pose

        Vector3    startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        while (elapsed < snapTime)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / snapTime);

            cam.transform.position = Vector3.Lerp(startPos,
                                                   jumpscareCameraTarget.position,
                                                   t);
            cam.transform.rotation = Quaternion.Slerp(startRot,
                                                       jumpscareCameraTarget.rotation,
                                                       t);
            yield return null;
        }

        // Lock exactly on target for the rest of the jumpscare
        cam.transform.position = jumpscareCameraTarget.position;
        cam.transform.rotation = jumpscareCameraTarget.rotation;
    }

    /// <summary>
    /// Reads the length of a named clip from the Animator's runtime controller.
    /// Returns 0 if the clip isn't found (fallback duration is used instead).
    /// </summary>
    float GetAnimationClipLength(string stateName)
    {
        if (_animator == null) return 0f;

        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == stateName)
                return clip.length;
        }

        Debug.LogWarning($"[MonsterChase] Could not find animation clip named '{stateName}'. " +
                         $"Using fallback duration of {jumpscareFallbackDuration}s. " +
                          "Make sure the clip name matches exactly.");
        return 0f;
    }

    /// <summary>
    /// Called when the jumpscare animation finishes.
    /// Replace the body with your game-over screen, respawn, etc.
    /// </summary>
    void OnJumpscareComplete()
    {
        Debug.Log("[MonsterChase] Jumpscare complete — add your game-over / respawn logic here.");
        // Example: SceneManager.LoadScene("GameOver");
        // Example: PlayerRespawn.Respawn();
    }

    // ──────────────────────────────────────────────
    //  Sight Check
    // ──────────────────────────────────────────────

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        float   dist     = toPlayer.magnitude;

        if (dist > detectionRadius) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > fovAngle) return false;

        if (Physics.Raycast(transform.position + Vector3.up,
                            toPlayer.normalized,
                            out RaycastHit hit,
                            detectionRadius,
                            sightMask))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return false;
    }

    void CheckForPlayer()
    {
        if (CanSeePlayer())
        {
            _lastKnownPos = player.position;
            _memoryTimer  = memoryDuration;
            SetState(State.Chase);
        }
    }

    // ──────────────────────────────────────────────
    //  Patrol Helpers
    // ──────────────────────────────────────────────

    void MoveToNextWaypoint()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            _agent.SetDestination(waypoints[_waypointIndex].position);
        }
        else
        {
            _agent.SetDestination(RandomNavMeshPoint(wanderRadius));
        }
    }

    Vector3 RandomNavMeshPoint(float radius)
    {
        Vector3 dir = Random.insideUnitSphere * radius + transform.position;
        NavMesh.SamplePosition(dir, out NavMeshHit hit, radius, NavMesh.AllAreas);
        return hit.position;
    }

    // ──────────────────────────────────────────────
    //  State Machine
    // ──────────────────────────────────────────────

    void SetState(State newState)
    {
        if (_state == newState) return;
        _state = newState;

        switch (newState)
        {
            case State.Patrol:
                _agent.isStopped = false;
                _agent.speed     = patrolSpeed;
                MoveToNextWaypoint();
                PlayAmbient(idleSound);
                break;

            case State.Chase:
                _agent.isStopped = false;
                _agent.speed     = chaseSpeed;
                PlayAmbient(chaseSound);
                break;

            case State.Investigate:
                _agent.isStopped = false;
                _agent.speed     = patrolSpeed * 1.3f;
                _agent.SetDestination(_lastKnownPos);
                break;

            case State.Jumpscare:
                TriggerJumpscare();
                break;
        }

        _animator?.SetBool(AnimIsChasing, newState == State.Chase);
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    void PlayAmbient(AudioClip clip)
    {
        if (_audio == null || clip == null) return;
        if (_audio.clip == clip && _audio.isPlaying) return;
        _audio.clip = clip;
        _audio.loop = true;
        _audio.Play();
    }

    // ──────────────────────────────────────────────
    //  Editor Gizmos
    // ──────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, jumpscareRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -fovAngle, 0) * transform.forward * detectionRadius);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0,  fovAngle, 0) * transform.forward * detectionRadius);

        if (Application.isPlaying && (_state == State.Investigate || _state == State.Chase))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(_lastKnownPos, 0.3f);
        }
    }
#endif
}