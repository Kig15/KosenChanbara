using UnityEngine;

public enum CombatPlayer
{
    Player1,
    Player2
}

public enum CombatImpactOutcome
{
    NormalHit,
    GuardSuccess,
    GuardFailure
}

/// <summary>
/// Immutable result of one resolved attack. Presentation systems can subscribe
/// to MasterController.CombatImpactResolved without depending on sword states.
/// </summary>
public readonly struct CombatImpact
{
    public CombatImpact(
        uint sequence,
        CombatPlayer attacker,
        CombatPlayer defender,
        CombatImpactOutcome outcome,
        Vector3 worldPosition,
        Vector3 direction,
        float strength)
    {
        Sequence = sequence;
        Attacker = attacker;
        Defender = defender;
        Outcome = outcome;
        WorldPosition = worldPosition;
        Direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.forward;
        Strength = Mathf.Max(0f, strength);
    }

    public uint Sequence { get; }
    public CombatPlayer Attacker { get; }
    public CombatPlayer Defender { get; }
    public CombatImpactOutcome Outcome { get; }
    public Vector3 WorldPosition { get; }
    public Vector3 Direction { get; }
    public float Strength { get; }
}
