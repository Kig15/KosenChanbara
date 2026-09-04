using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MasterController : MonoBehaviour
{
    public const float radsX = 45f;
    public const float radsZ = 45f;

    [SerializeField] private GameObject Sword1;
    [SerializeField] private GameObject Sword2;
    [SerializeField] private GameObject PlayerObject1;
    [SerializeField] private GameObject PlayerObject2;

    private const float PlayerFront = 3f;
    private const float PlayerGuardSucess = 1.5f;
    private const float GuardSucessAngle = 70f;
    private const float AttackBeginAngle = 45f;
    private const float AttackBeginDegs = 40f;
    private const float AttackResetAngle = 80f;

    private enum SwordState
    {
        Attack,
        Defence,
        Neutral,
        AfterAttack
    }

    private struct PlayerSword
    {
        public GameObject Sword;
        public MotionController Motion;
        public Quaternion PreviousRotation;
        public float AngularSpeed;
        public SwordState State;
    }

    private PlayerSword player1;
    private PlayerSword player2;
    private SwordState previousDebugState1;
    private SwordState previousDebugState2;
    private uint impactSequence;

    public event Action<CombatImpact> CombatImpactResolved;

    private void Start()
    {
        if (Sword1 == null || Sword2 == null || PlayerObject1 == null || PlayerObject2 == null)
        {
            Debug.LogError("MasterController requires both swords and both player objects.", this);
            enabled = false;
            return;
        }

        player1 = CreatePlayerSword(Sword1);
        player2 = CreatePlayerSword(Sword2);

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    private static PlayerSword CreatePlayerSword(GameObject sword)
    {
        return new PlayerSword
        {
            Sword = sword,
            Motion = sword.GetComponent<MotionController>(),
            PreviousRotation = sword.transform.localRotation,
            AngularSpeed = 0f,
            State = SwordState.Neutral
        };
    }

    private void Update()
    {
        if (CombatHitStopController.IsGameplayStopped)
        {
            // Keep the keyboard fallback's rotation baseline synchronized so
            // resuming cannot manufacture a large one-frame angular speed.
            player1.PreviousRotation = player1.Sword.transform.localRotation;
            player2.PreviousRotation = player2.Sword.transform.localRotation;
            return;
        }

        DebugSwordControl(ref player1, ref player2);

        player1.AngularSpeed = GetAngularSpeed(player1);
        player2.AngularSpeed = GetAngularSpeed(player2);

        player1.State = GetSwordState(
            PlayerObject1,
            player1.Sword,
            player1.AngularSpeed,
            player1.State,
            player1.Motion != null && player1.Motion.GuardHeld);
        player2.State = GetSwordState(
            PlayerObject2,
            player2.Sword,
            player2.AngularSpeed,
            player2.State,
            player2.Motion != null && player2.Motion.GuardHeld);

        float swordAngle = GetSwordAngle(player1.Sword, player2.Sword);
        if (player1.State != previousDebugState1 || player2.State != previousDebugState2)
        {
            Debug.Log($"Player1:{player1.State}  Player2:{player2.State} {swordAngle}");
        }

        previousDebugState1 = player1.State;
        previousDebugState2 = player2.State;

        switch ((player1.State, player2.State))
        {
            case (SwordState.Attack, SwordState.Neutral):
                ResolveImpact(
                    ref player1,
                    CombatPlayer.Player1,
                    PlayerObject1,
                    CombatPlayer.Player2,
                    PlayerObject2,
                    player2.Sword,
                    CombatImpactOutcome.NormalHit,
                    new Vector3(0, 0, PlayerFront));
                break;
            case (SwordState.Neutral, SwordState.Attack):
                ResolveImpact(
                    ref player2,
                    CombatPlayer.Player2,
                    PlayerObject2,
                    CombatPlayer.Player1,
                    PlayerObject1,
                    player1.Sword,
                    CombatImpactOutcome.NormalHit,
                    new Vector3(0, 0, -PlayerFront));
                break;
            case (SwordState.Attack, SwordState.AfterAttack):
                ResolveImpact(
                    ref player1,
                    CombatPlayer.Player1,
                    PlayerObject1,
                    CombatPlayer.Player2,
                    PlayerObject2,
                    player2.Sword,
                    CombatImpactOutcome.NormalHit,
                    new Vector3(0, 0, PlayerFront));
                break;
            case (SwordState.AfterAttack, SwordState.Attack):
                ResolveImpact(
                    ref player2,
                    CombatPlayer.Player2,
                    PlayerObject2,
                    CombatPlayer.Player1,
                    PlayerObject1,
                    player1.Sword,
                    CombatImpactOutcome.NormalHit,
                    new Vector3(0, 0, -PlayerFront));
                break;
            case (SwordState.Attack, SwordState.Defence):
                if (swordAngle > GuardSucessAngle)
                {
                    Debug.Log("GuardSucess");
                    ResolveImpact(
                        ref player1,
                        CombatPlayer.Player1,
                        PlayerObject1,
                        CombatPlayer.Player2,
                        PlayerObject2,
                        player2.Sword,
                        CombatImpactOutcome.GuardSuccess,
                        new Vector3(0, 0, -PlayerGuardSucess));
                }
                else
                {
                    Debug.Log("GuardFaild" + swordAngle);
                    ResolveImpact(
                        ref player1,
                        CombatPlayer.Player1,
                        PlayerObject1,
                        CombatPlayer.Player2,
                        PlayerObject2,
                        player2.Sword,
                        CombatImpactOutcome.GuardFailure,
                        new Vector3(0, 0, PlayerGuardSucess));
                }
                break;
            case (SwordState.Defence, SwordState.Attack):
                if (swordAngle > GuardSucessAngle)
                {
                    Debug.Log("GuardSucess");
                    ResolveImpact(
                        ref player2,
                        CombatPlayer.Player2,
                        PlayerObject2,
                        CombatPlayer.Player1,
                        PlayerObject1,
                        player1.Sword,
                        CombatImpactOutcome.GuardSuccess,
                        new Vector3(0, 0, PlayerGuardSucess));
                }
                else
                {
                    Debug.Log("GuardFaild" + swordAngle);
                    ResolveImpact(
                        ref player2,
                        CombatPlayer.Player2,
                        PlayerObject2,
                        CombatPlayer.Player1,
                        PlayerObject1,
                        player1.Sword,
                        CombatImpactOutcome.GuardFailure,
                        new Vector3(0, 0, -PlayerGuardSucess));
                }
                break;
        }

        player1.PreviousRotation = player1.Sword.transform.localRotation;
        player2.PreviousRotation = player2.Sword.transform.localRotation;
    }

    private void ResolveImpact(
        ref PlayerSword attacker,
        CombatPlayer attackerId,
        GameObject attackerObject,
        CombatPlayer defenderId,
        GameObject defenderObject,
        GameObject defenderSword,
        CombatImpactOutcome outcome,
        Vector3 arenaDisplacement)
    {
        transform.position += arenaDisplacement;
        attacker.State = SwordState.AfterAttack;

        Vector3 worldPosition = outcome == CombatImpactOutcome.NormalHit
            ? defenderObject.transform.position
            : Vector3.Lerp(attacker.Sword.transform.position, defenderSword.transform.position, 0.5f);
        Vector3 direction = defenderObject.transform.position - attackerObject.transform.position;
        CombatImpact impact = new CombatImpact(
            ++impactSequence,
            attackerId,
            defenderId,
            outcome,
            worldPosition,
            direction,
            arenaDisplacement.magnitude);

        CombatImpactResolved?.Invoke(impact);
    }

    private static float GetAngularSpeed(PlayerSword player)
    {
        if (player.Motion != null && player.Motion.IsConnected)
        {
            return player.Motion.AngularSpeedDegrees;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.00001f);
        return Quaternion.Angle(player.PreviousRotation, player.Sword.transform.localRotation) / deltaTime;
    }

    private static SwordState GetSwordState(
        GameObject player,
        GameObject sword,
        float angularSpeed,
        SwordState currentState,
        bool guardHeld)
    {
        // Guard is explicit. The old "sword angle >= 80 degrees" defence trigger
        // has been removed so an accidental pose cannot enter Defence.
        if (guardHeld)
        {
            return SwordState.Defence;
        }

        Vector3 swordDirection = sword.transform.forward;
        float angle = Vector3.Angle(swordDirection, player.transform.forward);

        if (currentState == SwordState.AfterAttack)
        {
            return angle >= AttackResetAngle ? SwordState.Neutral : SwordState.AfterAttack;
        }

        if (angle <= AttackBeginAngle && angularSpeed >= AttackBeginDegs)
        {
            return SwordState.Attack;
        }

        return SwordState.Neutral;
    }

    private static float GetSwordAngle(GameObject sword1, GameObject sword2)
    {
        float angle = Vector3.Angle(sword1.transform.forward, sword2.transform.forward);
        return Mathf.Min(angle, 180f - angle);
    }

    private static void DebugSwordControl(ref PlayerSword first, ref PlayerSword second)
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (first.Motion == null || !first.Motion.IsConnected)
        {
            ApplyKeyboardRotation(
                first.Sword,
                Keyboard.current.upArrowKey.isPressed,
                Keyboard.current.downArrowKey.isPressed,
                Keyboard.current.leftArrowKey.isPressed,
                Keyboard.current.rightArrowKey.isPressed);
        }

        if (second.Motion == null || !second.Motion.IsConnected)
        {
            ApplyKeyboardRotation(
                second.Sword,
                Keyboard.current.wKey.isPressed,
                Keyboard.current.sKey.isPressed,
                Keyboard.current.aKey.isPressed,
                Keyboard.current.dKey.isPressed);
        }
    }

    private static void ApplyKeyboardRotation(
        GameObject sword,
        bool increaseX,
        bool decreaseX,
        bool increaseZ,
        bool decreaseZ)
    {
        if (!increaseX && !decreaseX && !increaseZ && !decreaseZ)
        {
            return;
        }

        Vector3 euler = sword.transform.localEulerAngles;
        float x = euler.x;
        float z = euler.z;
        if (increaseX)
        {
            x = Mathf.Clamp(x + radsX * Time.deltaTime, 0f, 90f);
        }
        if (decreaseX)
        {
            x = Mathf.Clamp(x - radsX * Time.deltaTime, 0f, 90f);
        }
        if (increaseZ)
        {
            z += radsZ * Time.deltaTime;
        }
        if (decreaseZ)
        {
            z -= radsZ * Time.deltaTime;
        }

        sword.transform.localRotation = Quaternion.Euler(x, 0f, z);
    }
}
