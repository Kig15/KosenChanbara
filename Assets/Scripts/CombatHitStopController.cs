using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pauses combat presentation for a short, outcome-specific duration while
/// realtime systems such as phone input and WebRTC continue to run.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(MasterController))]
public sealed class CombatHitStopController : MonoBehaviour
{
    [Header("Hit Stop Durations (seconds)")]
    [SerializeField, Min(0f)] private float normalHitDuration = 0.06f;
    [SerializeField, Min(0f)] private float guardSuccessDuration = 0.10f;
    [SerializeField, Min(0f)] private float guardFailureDuration = 0.06f;

    [Header("Animation")]
    [Tooltip("Animators under this root are paused. Uses this transform when omitted.")]
    [SerializeField] private Transform animationRoot;

    private static int activeControllerCount;

    private readonly List<AnimatorState> animatorStates = new List<AnimatorState>();
    private MasterController masterController;
    private double stopUntilRealtime;
    private bool isHitStopActive;

    public static bool IsGameplayStopped => activeControllerCount > 0;
    public bool IsHitStopActive => isHitStopActive;
    public float RemainingSeconds => isHitStopActive
        ? Mathf.Max(0f, (float)(stopUntilRealtime - Time.realtimeSinceStartupAsDouble))
        : 0f;

    private readonly struct AnimatorState
    {
        public AnimatorState(Animator animator, float speed)
        {
            Animator = animator;
            Speed = speed;
        }

        public Animator Animator { get; }
        public float Speed { get; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        activeControllerCount = 0;
    }

    private void Awake()
    {
        masterController = GetComponent<MasterController>();
    }

    private void OnEnable()
    {
        if (masterController == null)
        {
            masterController = GetComponent<MasterController>();
        }

        masterController.CombatImpactResolved += HandleCombatImpact;
    }

    private void Update()
    {
        if (isHitStopActive && Time.realtimeSinceStartupAsDouble >= stopUntilRealtime)
        {
            EndHitStop();
        }
    }

    private void OnDisable()
    {
        if (masterController != null)
        {
            masterController.CombatImpactResolved -= HandleCombatImpact;
        }

        EndHitStop();
    }

    private void HandleCombatImpact(CombatImpact impact)
    {
        RequestHitStop(GetDuration(impact.Outcome));
    }

    /// <summary>
    /// Starts or extends hit stop using realtime seconds. A later request never
    /// shortens a stop that is already active.
    /// </summary>
    public void RequestHitStop(float durationSeconds)
    {
        float duration = Mathf.Max(0f, durationSeconds);
        if (duration <= 0f)
        {
            return;
        }

        double requestedEnd = Time.realtimeSinceStartupAsDouble + duration;
        stopUntilRealtime = Math.Max(stopUntilRealtime, requestedEnd);

        if (isHitStopActive)
        {
            return;
        }

        isHitStopActive = true;
        activeControllerCount++;
        PauseAnimators();
    }

    private float GetDuration(CombatImpactOutcome outcome)
    {
        switch (outcome)
        {
            case CombatImpactOutcome.NormalHit:
                return normalHitDuration;
            case CombatImpactOutcome.GuardSuccess:
                return guardSuccessDuration;
            case CombatImpactOutcome.GuardFailure:
                return guardFailureDuration;
            default:
                return 0f;
        }
    }

    private void PauseAnimators()
    {
        animatorStates.Clear();
        Transform root = animationRoot != null ? animationRoot : transform;
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            animatorStates.Add(new AnimatorState(animator, animator.speed));
            animator.speed = 0f;
        }
    }

    private void EndHitStop()
    {
        if (!isHitStopActive)
        {
            return;
        }

        foreach (AnimatorState state in animatorStates)
        {
            if (state.Animator != null)
            {
                state.Animator.speed = state.Speed;
            }
        }
        animatorStates.Clear();

        isHitStopActive = false;
        stopUntilRealtime = 0d;
        activeControllerCount = Mathf.Max(0, activeControllerCount - 1);
    }
}
