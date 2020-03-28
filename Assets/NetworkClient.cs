using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections.Generic;

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
        InvokeRepeating("HeartBeat", 1, 0.033f);

    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void HeartBeat()
    {
        HandshakeMsg m = new HandshakeMsg();
        m.player.id = playerID;
        SendToServer(JsonUtility.ToJson(m));
    }

    void OnConnect(){
        Debug.Log("We are now connected to the server");

    }

    public void SendingPosition(Vector3 pos, Vector3 rot)
    {
        PlayerUpdateMsg m = new PlayerUpdateMsg();
        m.player.id = playerID;
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
                SpawnPlayers(pcMsg.newPlayer.id, pcMsg.newPlayer.cubeColor);
                //Debug.Log("A player has connected with id:" + pcMsg.newPlayer.id);
                break;
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                Debug.Log("Handshake message received!");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                UpdatePlayers(puMsg.player.id, puMsg.player.cubePos, puMsg.player.cubeRot);
                //Debug.Log("Player update message received!");
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Server update message received!");
                break;
            case Commands.OWNED_ID:
                OwnIDMsg idMsg = JsonUtility.FromJson<OwnIDMsg>(recMsg);
                playerID = idMsg.ownedPlayer.id;
                Debug.Log("Own id received! id: " + playerID);
                break;
            case Commands.PLAYER_DROPPED:
                PlayerDropMsg dropMsg = JsonUtility.FromJson<PlayerDropMsg>(recMsg);
                
                break;
            case Commands.PLAYER_LIST:
                PlayerListMsg plMsg = JsonUtility.FromJson<PlayerListMsg>(recMsg);
                foreach (var it in plMsg.players)
                {
                    SpawnPlayers(it.id, it.cubeColor);
                    Debug.Log("Spawn player with id: " + it.id);
                }
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

    void SpawnPlayers(string id, Color color) 
    {
        if (ClientsList.ContainsKey(id))
            return;
        //Debug.Log("Spawned player id: " + id);
        GameObject temp = Instantiate(cube, new Vector3(0, 0, 0), cube.transform.rotation);
        temp.GetComponent<Renderer>().material.SetColor("_Color", color);
        if(id == playerID)
        {
            //Debug.Log("Controller Added!");
            temp.AddComponent<PlayerController>();
            temp.GetComponent<PlayerController>().client = this;
        }
        ClientsList.Add(id, temp);
        spawnCounter++;
    }

    void DestroyPlayers(string id)
    {
        if(ClientsList.ContainsKey(id))
        {
            GameObject temp = ClientsList[id];
            ClientsList.Remove(id);
            Destroy(temp);
        } 
    }

    void UpdatePlayers(string id, Vector3 pos, Vector3 rot)
    {
        if (ClientsList.ContainsKey(id))
        {
            ClientsList[id].transform.position = pos;
            ClientsList[id].transform.eulerAngles = rot;
        }
    }
}