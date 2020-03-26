using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public GameObject cube;
    public string serverIP;
    public ushort serverPort;
    int spawnCounter;
    string playerID;
    Dictionary<string, GameObject> ClientsList;
    
    NetworkObjects.NetworkPlayer myPlayer;

    
    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        m_Connection = m_Driver.Connect(endpoint);
        myPlayer = new NetworkObjects.NetworkPlayer();
        spawnCounter = 0;
        ClientsList = new Dictionary<string, GameObject>();
    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("We are now connected to the server");

        // Example to send a handshake message:
        //HandshakeMsg m = new HandshakeMsg();
        //m.player.id = m_Connection.InternalId.ToString();
        //playerID = m_Connection.InternalId.ToString();
        //Debug.Log(m.player.id + " is connected");
        //SendToServer(JsonUtility.ToJson(m));
        //SpawnPlayers(m.player.id);
    }

    public void SendingPosition(Vector3 pos, Vector3 rot)
    {
        PlayerUpdateMsg m = new PlayerUpdateMsg();
        m.player.cubePos = pos;
        m.player.cubeRot = rot;
        SendToServer(JsonUtility.ToJson(m));
    }

    void OnData(DataStreamReader stream){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.PLAYER_CONNECT:
                PlayerConnectMsg pcMsg = JsonUtility.FromJson<PlayerConnectMsg>(recMsg);
                SpawnPlayers(playerID);
                break;
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                Debug.Log("Handshake message received!");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                Debug.Log("Player update message received!");
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Server update message received!");
                break;
            case Commands.OWNED_ID:
                OwnIDMsg idMsg = JsonUtility.FromJson<OwnIDMsg>(recMsg);
                idMsg.ownedPlayer.id = playerID;
                Debug.Log("Own id received! id: " + playerID);
                break;
            default:
                Debug.Log("Unrecognized message received!");
            break;
        }
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);

    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }   
    void Update()
    {
        //if (Input.GetKey(KeyCode.W))
        //{
        //    myPlayer.cubePos += transform.TransformVector(Vector3.forward) * Time.deltaTime;
        //}
        //if (Input.GetKey(KeyCode.S))
        //{
        //    myPlayer.cubePos -= transform.TransformVector(Vector3.forward) * Time.deltaTime;
        //}
        //if (Input.GetKey(KeyCode.A))
        //{
        //    myPlayer.cubeRot += new Vector3(0, 1, 0) * Time.deltaTime * 90f;
        //}
        //if (Input.GetKey(KeyCode.D))
        //{
        //    myPlayer.cubeRot -= new Vector3(0, 1, 0) * Time.deltaTime * 90f;
        //}

        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }

    }

    void SpawnPlayers(string id) 
    {
        
        GameObject temp = Instantiate(cube, new Vector3(-5 + spawnCounter, 0, 0), cube.transform.rotation);
        if(id == playerID)
        {
            temp.AddComponent<PlayerController>();
            temp.GetComponent<PlayerController>().client = this;
        }
        spawnCounter++;
    }

    void DestroyPlayers(string id)
    {

    }
}