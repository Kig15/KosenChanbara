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

    private enum SwordState
    {
        Attack,
        Defence,
        Neutral,
        AfterAttack
    }

    private struct PlayerSword
    {
        public GameObject sword;//オブジェクト
        public Vector3 previewRotation;//前の角速度
        public Vector3 degs;//角速度
        public SwordState state;//剣の状態

    }

    private PlayerSword player1;
    private PlayerSword player2;

   
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player1.sword= Sword1;
        player1.degs = new Vector3(0, 0, 0);
        player1.previewRotation = player1.sword.transform.localRotation.eulerAngles;
        player1.state = SwordState.Neutral;

        player2.sword = Sword2;
        player2.previewRotation = player2.sword.transform.localRotation.eulerAngles;
        player2.degs = new Vector3(0, 0, 0);
        player2.state = SwordState.Neutral;

        this.gameObject.transform.position = new Vector3(0, 0, 0);
        this.gameObject.transform.rotation = new Quaternion(0,0,0,0);
    }

    // Update is called once per frame
    void Update()
    {
        DebugSwordControl(player1.sword,player2.sword);

        float deltaAngleX_1 = Mathf.DeltaAngle(player1.previewRotation.x, player1.sword.transform.localEulerAngles.x);
        float deltaAngleX_2 = Mathf.DeltaAngle(player2.previewRotation.x, player2.sword.transform.localEulerAngles.x);

        player1.degs.x = deltaAngleX_1/Time.deltaTime;
        player2.degs.x = deltaAngleX_2/Time.deltaTime;

        

        switch ((Mathf.DeltaAngle(0f,player1.sword.transform.localEulerAngles.x), player1.degs.x)) 
        { 
            case(>= -70,>=5):
                if(player1.state != SwordState.AfterAttack)
                     player1.state = SwordState.Attack;
                break;
            case ( <= -70, <5):
                player1.state = SwordState.Defence; 
                break;
            default:
                player1.state = SwordState.Neutral;
                break;
        }

        switch (Mathf.DeltaAngle(0f,player2.sword.transform.localEulerAngles.x))
        {
            case ( >= -40):
                if (player2.state != SwordState.AfterAttack)
                    player2.state = SwordState.Attack;
                break;
            case (<= -70):
                player2.state = SwordState.Defence;
                break;
            default:
                player2.state = SwordState.Neutral;
                break;
        }

        Debug.Log("Player1:"+ player1.state + " Rotaion:" + Mathf.DeltaAngle(0f, player1.sword.transform.localEulerAngles.x));
       // Debug.Log("Player2:" + player2.state);

        //switch ((player1.state, player2.state)) {
        //    case (SwordState.Attack, SwordState.Neutral):
        //        this.gameObject.transform.position += new Vector3(0,0,5f);
        //        player1.state = SwordState.AfterAttack;
        //        break;
        //    case (SwordState.Neutral, SwordState.Attack):
        //        this.gameObject.transform.position += new Vector3(0, 0, -5f);
        //        player2.state = SwordState.AfterAttack;
        //        break;
        //    case (SwordState.Attack, SwordState.Defence):
        //        this.gameObject.transform.position += new Vector3(0, 0, -2.5f);
        //        player1.state = SwordState.AfterAttack;
        //        break;
        //    case (SwordState.Defence,SwordState.Attack):
        //        this.gameObject.transform.position += new Vector3(0, 0, 2.5f);
        //        player2.state = SwordState.AfterAttack;
        //        break;
        //    default:

        //        break;

        //}

        player1.previewRotation = player1.sword.transform.localEulerAngles;
        player2.previewRotation = player2.sword.transform.localEulerAngles;

    }

    private void DebugSwordControl(GameObject sword1,GameObject sword2)
    {
        if (Keyboard.current == null)
            return;

        float deltaTime = Time.deltaTime;


        // ==============================
        // 剣1
        // 矢印キー
        // ==============================

        Vector3 sword1Euler =
            sword1.transform.localEulerAngles;

        float sword1X = sword1Euler.x;
        float sword1Z = sword1Euler.z;


        // ↑
        // X軸に +11 deg/s
        // 最大90度
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


        // ↓
        // X軸に -11 deg/s
        // 最小0度
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


        // ←
        // Z軸に +10 deg/s
        if (Keyboard.current.leftArrowKey.isPressed)
        {
            sword1Z += radsZ * deltaTime;
        }


        // →
        // Z軸に -10 deg/s
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
        // 剣2
        // WASD
        // ==============================

        Vector3 sword2Euler =
            sword2.transform.localEulerAngles;

        float sword2X = sword2Euler.x;
        float sword2Z = sword2Euler.z;


        // W
        // X軸に +11 deg/s
        // 最大90度
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
        // X軸に -11 deg/s
        // 最小0度
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
        // Z軸に +10 deg/s
        if (Keyboard.current.aKey.isPressed)
        {
            sword2Z += radsZ * deltaTime;
        }


        // D
        // Z軸に -10 deg/s
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

    



