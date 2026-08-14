using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class MotionController : MonoBehaviour
{
    [Header("Server")]
    [SerializeField]
    private string serverUrl = "wss://koreisai.ryutolab.com/ws";

    [Header("Controller")]
    [SerializeField]
    private string controllerId = "controller_001";//サーバーに存在する空きコントローラーの枠を使うようにする（後で）

    private ClientWebSocket socket;//鯖との通信経路のオブジェクト

    private CancellationTokenSource cancellation;//非同期通信を中断するオブジェクト

    private readonly object rotationLock = new();

    private Quaternion latestRotation = Quaternion.identity;

    private bool hasRotation = false;


    async void Start()
    {
        cancellation =  new CancellationTokenSource();

        socket = new ClientWebSocket();

        try
        {
            Debug.Log("Connecting to controller server...");

            await socket.ConnectAsync(
                new Uri(serverUrl),
                cancellation.Token
            );

            Debug.Log("Controller server connected");

            _ = ReceiveLoop();
        }
        //catch (Exception e)
        //{
        //    Debug.LogError(
        //        $"WebSocket connection failed: {e}"
        //    );
        //}
        catch (WebSocketException e)
        {
            Debug.LogError(
                "===== WebSocket Connection Error =====\n" +
                $"URL: {serverUrl}\n" +
                $"WebSocketErrorCode: {e.WebSocketErrorCode}\n" +
                $"Message: {e.Message}\n" +
                $"InnerException: {e.InnerException}\n" +
                $"Socket State: {socket?.State}\n" +
                $"Full Exception:\n{e}"
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "===== Unexpected Error =====\n" +
                $"Type: {e.GetType().FullName}\n" +
                $"Message: {e.Message}\n" +
                $"InnerException: {e.InnerException}\n" +
                $"Full Exception:\n{e}"
            );
        }
    }
    //あああ

    async Task ReceiveLoop()//進行中、または将来完了する動作
    {
        byte[] buffer = new byte[8192];//Json一時置き場

        while (
            socket != null &&
            socket.State == WebSocketState.Open &&
            !cancellation.IsCancellationRequested
        )
        {
            try
            {
                using MemoryStream stream = new MemoryStream();

                WebSocketReceiveResult result;

                do
                {
                    result =
                        await socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            cancellation.Token
                        );

                    if (result.MessageType ==WebSocketMessageType.Close)
                    {
                        return;
                    }

                    stream.Write(buffer,0,result.Count);

                }
                while (!result.EndOfMessage);


                string json = Encoding.UTF8.GetString(stream.ToArray());//byte[]にしてからエンコード

                HandleMessage(json);
            }
            catch (Exception e)
            {
                if (!cancellation.IsCancellationRequested)
                {
                    Debug.LogError(
                        $"Receive error: {e}"
                    );
                }

                break;
            }
        }
    }


    void HandleMessage(string json)
    {
        StatePacket packet;

        try
        {
            packet =
                JsonConvert.DeserializeObject<StatePacket>(
                    json
                );
        }
        catch
        {
            return;
        }

        if (packet == null)
            return;

        if (packet.type != "state")
            return;

        if (packet.controllers == null)
            return;

        if (!packet.controllers.TryGetValue(controllerId,out ControllerData controller))
        {
            return;
        }

        QuaternionData q = controller.quat;

        if (q == null)
            return;


        // リポジトリ内のThree.jsサンプルと同じ軸順を
        // とりあえず初期値として使用
        Quaternion rotation =
            new Quaternion(
                q.y,
                q.z,
                q.x,
                q.w
            );//ここがキモいのはUnityとウェブの３D軸が同じではないから


        lock (rotationLock)
        {
            latestRotation = rotation;
            hasRotation = true;
        }
    }


    void Update()
    {
        if (!hasRotation)
            return;

        Quaternion rotation;

        lock (rotationLock)
        {
            rotation = latestRotation;
        }

        transform.rotation = rotation;
    }


    public async void Calibrate()
    {
        if (socket == null || socket.State != WebSocketState.Open)
        {
            return;
        }

        var command = new
        {
            type = "calibrate",
            target = controllerId
        };

        string json =
            JsonConvert.SerializeObject(command);

        byte[] bytes =
            Encoding.UTF8.GetBytes(json);

        try
        {
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellation.Token
            );

            Debug.Log(
                $"Calibration requested: {controllerId}"
            );
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }


    void OnDestroy()
    {
        cancellation?.Cancel();

        socket?.Dispose();

        cancellation?.Dispose();
    }
}


public class StatePacket
{
    public string type;

    public long timestamp;

    public int controllerCount;

    public Dictionary<string, ControllerData>
        controllers;
}


public class ControllerData
{
    public string id;

    public QuaternionData quat;

    public float accuracy;

    public long lastUpdate;
}


public class QuaternionData
{
    public float w;

    public float x;

    public float y;

    public float z;
}