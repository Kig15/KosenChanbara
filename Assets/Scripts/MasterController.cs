using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Unity.VisualScripting;





public class MasterController : MonoBehaviour
{
    public const float radsX = 45f;
    public const float radsZ = 45f;

    [SerializeField] private GameObject Sword1;
    [SerializeField] private GameObject Sword2;

    [SerializeField] private GameObject PlayerObject1;
    [SerializeField] private GameObject PlayerObject2;


    private const float PlayerFront = 3f;//ëäéËÇ™ñ≥íÔçRÇÃèÍçáÇ…çUåÇÇ™í Ç¡ÇΩç€Ç…êiÇﬁãóó£
    private const float PlayerGuardSucess = 1.5f;//ëäéËÇ™ÉKÅ[Éhê¨å˜ÇµÇΩç€Ç…å„ëﬁÇ∑ÇÈãóó£

    private const float GuardSucessAngle = 70f;//Ç±ÇÃäpìxà»è„ÇXÇOìxà»â∫Ç™ÉKÅ[Éhê¨å˜äpìx

    private const float AttackBeginAngle = 45f;//Ç±ÇÃäpìxà»â∫Ç≈çUåÇèÛë‘
    private const float AttackBeginDegs = 40f;//Ç±ÇÃäpë¨ìxà»è„Ç≈çUåÇèÛë‘

    private const float DefenceBeginAngle = 80f;//Ç±ÇÃäpìxà»è„Ç≈ñhå‰èÛë‘




    private enum SwordState
    {
        Attack,
        Defence,
        Neutral,
        AfterAttack
    }

    private struct PlayerSword
    {
        public GameObject sword;//ÉIÉuÉWÉFÉNÉg
        public Quaternion previewRotation;//ëOÇÃäpìx
        public float deltaAngle;//äpë¨ìx
        public SwordState state;//åïÇÃèÛë‘

    }

    private PlayerSword player1;
    private PlayerSword player2;
    private SwordState DebugTemp;
    private SwordState DebugTemp2;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player1.sword= Sword1;
        player1.deltaAngle = 0;
        player1.previewRotation = player1.sword.transform.localRotation;
        player1.state = SwordState.Neutral;

        player2.sword = Sword2;
        player2.previewRotation = player2.sword.transform.localRotation;
        player2.deltaAngle = 0;
        player2.state = SwordState.Neutral;

        this.gameObject.transform.position = new Vector3(0, 0, 0);
        this.gameObject.transform.rotation = new Quaternion(0,0,0,0);
    }

    // Update is called once per frame
    void Update()
    {
        DebugSwordControl(player1.sword,player2.sword);


        player1.deltaAngle = Quaternion.Angle(player1.previewRotation, player1.sword.transform.localRotation) / Time.deltaTime;
        player2.deltaAngle = Quaternion.Angle(player2.previewRotation, player2.sword.transform.localRotation) / Time.deltaTime;

        player1.state = GetSwordState(PlayerObject1, player1.sword, player1.deltaAngle, player1.state);
        player2.state = GetSwordState(PlayerObject2, player2.sword, player2.deltaAngle, player2.state);

        float SwordAngle = GetSwordAngle(player1.sword, player2.sword);

        if (player1.state != DebugTemp|| player2.state != DebugTemp2)
        {
            Debug.Log("Player1:" + player1.state + "  Player2:" + player2.state + SwordAngle);

        }

      



        DebugTemp = player1.state;
        DebugTemp2 = player2.state;

       

        switch ((player1.state, player2.state))
        {//ÉvÉåÉCÉÑÅ[ÇPÇÃê≥ñ Ç™Zé≤ê≥ñ 
            case (SwordState.Attack, SwordState.Neutral):
                this.gameObject.transform.position += new Vector3(0, 0, PlayerFront);
                player1.state = SwordState.AfterAttack;
              
                break;
            case (SwordState.Neutral, SwordState.Attack):
                this.gameObject.transform.position += new Vector3(0, 0, -PlayerFront);
                player2.state = SwordState.AfterAttack;
                break;

            case (SwordState.Attack, SwordState.AfterAttack):
                this.gameObject.transform.position += new Vector3(0, 0, PlayerFront);
                player2.state = SwordState.AfterAttack;
                break;
            case (SwordState.AfterAttack, SwordState.Attack):
                this.gameObject.transform.position += new Vector3(0, 0, -PlayerFront);
                player2.state = SwordState.AfterAttack;
                break;

            case (SwordState.Attack, SwordState.Defence):
               
                if (SwordAngle > GuardSucessAngle)
                {
                    this.gameObject.transform.position += new Vector3(0, 0, -PlayerGuardSucess);//ÉvÉåÉCÉÑÅ[ÇQÉKÅ[Éhê¨å˜
                    Debug.Log("GuardSucess");
                }
                else 
                {
                    this.gameObject.transform.position += new Vector3(0, 0, PlayerGuardSucess);//ÉvÉåÉCÉÑÅ[ÇQÉKÅ[Éhé∏îs
                    Debug.Log("GuardFaild"+ SwordAngle);
                }
                    player1.state = SwordState.AfterAttack;
                break;
            case (SwordState.Defence, SwordState.Attack):
                if (SwordAngle > GuardSucessAngle)
                {
                    this.gameObject.transform.position += new Vector3(0, 0, PlayerGuardSucess);//ÉvÉåÉCÉÑÅ[ÇPÉKÅ[Éhê¨å˜
                    Debug.Log("GuardSucess");
                }
                else
                {
                    this.gameObject.transform.position += new Vector3(0, 0, -PlayerGuardSucess);//ÉvÉåÉCÉÑÅ[ÇPÉKÅ[Éhé∏îs
                    Debug.Log("GuardFaild" + SwordAngle);
                }
                player2.state = SwordState.AfterAttack;
                break;


            default:

                break;

        }

        player1.previewRotation = player1.sword.transform.localRotation;
        player2.previewRotation = player2.sword.transform.localRotation;

    }

    private SwordState GetSwordState(GameObject player,GameObject sword,float angularSpeed,SwordState currentState)
    {
        Vector3 swordDirection =sword.transform.forward;

        Vector3 playerForward =player.transform.forward;

        Vector3 playerUp =player.transform.up;

        Vector3 playerRight =player.transform.right;


        // =====================================
        // çUåÇépê®
        //
        // åïÇ™ÉvÉåÉCÉÑÅ[ÇÃëOï˚å¸Ç…
        // Ç«ÇÍÇ≠ÇÁÇ¢ãﬂÇ¢Ç©
        // =====================================

        float Angle = Vector3.Angle(swordDirection,playerForward);
       


        // =====================================
        // åïÇêUÇËè„Ç∞ÇΩépê®
        //
        // AfterAttackÇ©ÇÁçƒçUåÇâ¬î\Ç…Ç∑ÇÈÇΩÇﬂÇ…
        // åïÇ™è„ï˚å¸Ç‹Ç≈ñﬂÇ¡ÇΩÇ©Çå©ÇÈ
        // =====================================

        float readyAngle =Vector3.Angle(swordDirection,playerUp);


   

        // =====================================
        // çUåÇå„
        // =====================================

        if (currentState == SwordState.AfterAttack)
        {
            // åïÇè„Ç‹Ç≈ñﬂÇ≥Ç»Ç¢å¿ÇË
            // çƒçUåÇÇ≈Ç´Ç»Ç¢
            if (Angle >= DefenceBeginAngle)
            {
                return SwordState.Neutral;
            }

            return SwordState.AfterAttack;
        }


        // =====================================
        // ñhå‰
        // =====================================

        if (Angle >= DefenceBeginAngle)
        {
            return SwordState.Defence;
        }


        // =====================================
        // çUåÇ
        // =====================================

        if (Angle <= AttackBeginAngle &&angularSpeed >= AttackBeginDegs)
        {
            return SwordState.Attack;
        }


        // =====================================
        // ÇªÇÍà»äO
        // =====================================

        return SwordState.Neutral;
    }

    private float GetSwordAngle(GameObject sword1,GameObject sword2)
    {
        Vector3 direction1 =sword1.transform.forward;

        Vector3 direction2 =sword2.transform.forward;

        float angle =Vector3.Angle(direction1,direction2);

        // åïêÊÇÃå¸Ç´ÇÕñ≥éãÇµÇƒ
        // Åu2ñ{ÇÃíºê¸Ç∆ÇµÇƒÇÃäpìxÅvÇ…Ç∑ÇÈ
        angle =Mathf.Min(angle,180f - angle);

        return angle;
    }

    private void DebugSwordControl(GameObject sword1,GameObject sword2)
    {
        if (Keyboard.current == null)
            return;

        float deltaTime = Time.deltaTime;


        // ==============================
        // åï1
        // ñÓàÛÉLÅ[
        // ==============================

        Vector3 sword1Euler =
            sword1.transform.localEulerAngles;

        float sword1X = sword1Euler.x;
        float sword1Z = sword1Euler.z;


        // Å™
        // Xé≤Ç… +11 deg/s
        // ç≈ëÂ90ìx
        if (Keyboard.current.upArrowKey.isPressed)
        {
            sword1X += radsX * deltaTime;

            sword1X =
                Mathf.Clamp(
                    sword1X,
                    0f,
                    90f
                );
        }


        // Å´
        // Xé≤Ç… -11 deg/s
        // ç≈è¨0ìx
        if (Keyboard.current.downArrowKey.isPressed)
        {
            sword1X -= radsX * deltaTime;

            sword1X =
                Mathf.Clamp(
                    sword1X,
                    0f,
                    90f
                );
        }


        // Å©
        // Zé≤Ç… +10 deg/s
        if (Keyboard.current.leftArrowKey.isPressed)
        {
            sword1Z += radsZ * deltaTime;
        }


        // Å®
        // Zé≤Ç… -10 deg/s
        if (Keyboard.current.rightArrowKey.isPressed)
        {
            sword1Z -= radsZ * deltaTime;
        }


        sword1.transform.localRotation =
            Quaternion.Euler(
                sword1X,
                0f,
                sword1Z
            );


        // ==============================
        // åï2
        // WASD
        // ==============================

        Vector3 sword2Euler =
            sword2.transform.localEulerAngles;

        float sword2X = sword2Euler.x;
        float sword2Z = sword2Euler.z;


        // W
        // Xé≤Ç… +11 deg/s
        // ç≈ëÂ90ìx
        if (Keyboard.current.wKey.isPressed)
        {
            sword2X += radsX * deltaTime;

            sword2X =
                Mathf.Clamp(
                    sword2X,
                    0f,
                    90f
                );
        }


        // S
        // Xé≤Ç… -11 deg/s
        // ç≈è¨0ìx
        if (Keyboard.current.sKey.isPressed)
        {
            sword2X -= radsX * deltaTime;

            sword2X =
                Mathf.Clamp(
                    sword2X,
                    0f,
                    90f
                );
        }


        // A
        // Zé≤Ç… +10 deg/s
        if (Keyboard.current.aKey.isPressed)
        {
            sword2Z += radsZ * deltaTime;
        }


        // D
        // Zé≤Ç… -10 deg/s
        if (Keyboard.current.dKey.isPressed)
        {
            sword2Z -= radsZ * deltaTime;
        }


        sword2.transform.localRotation =
            Quaternion.Euler(
                sword2X,
                0f,
                sword2Z
            );
    }
}

    



