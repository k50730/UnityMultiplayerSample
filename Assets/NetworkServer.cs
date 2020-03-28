using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_Connections;
    List<NetworkObjects.NetworkPlayer> checkDroppedPlayer;
    PlayerListMsg plMsg;
    float timer;
    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        timer = 0.5f;

        plMsg = new PlayerListMsg();
        checkDroppedPlayer = new List<NetworkObjects.NetworkPlayer>();
    }

    void SendToClient(string message, NetworkConnection c){
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void OnConnect(NetworkConnection c){
        m_Connections.Add(c);
        Debug.Log("Accepted a connection");

        // Send Own id
        OwnIDMsg idMsg = new OwnIDMsg();
        idMsg.ownedPlayer.id = c.InternalId.ToString();
        idMsg.ownedPlayer.cubeColor = new Color(Random.Range(0, 1.0f), Random.Range(0, 1.0f), Random.Range(0, 1.0f));
        Debug.Log("Sending player id: " + idMsg.ownedPlayer.id);
        SendToClient(JsonUtility.ToJson(idMsg), c);

        NetworkObjects.NetworkPlayer newPlayer = new NetworkObjects.NetworkPlayer();
        newPlayer.id = c.InternalId.ToString();
        newPlayer.cubeColor = idMsg.ownedPlayer.cubeColor;
        plMsg.players.Add(newPlayer);

        // Example to send a Connect message to the client
        for (int i = 0; i < m_Connections.Length; i++)
        {
            PlayerConnectMsg m = new PlayerConnectMsg();
            Debug.Log("Send Player Connect");
            m.newPlayer.id = c.InternalId.ToString();
            m.newPlayer.cubeColor = idMsg.ownedPlayer.cubeColor;
            SendToClient(JsonUtility.ToJson(m), m_Connections[i]);
        }

        SendToClient(JsonUtility.ToJson(plMsg), c);

    }

    void SendPostion(string id, Vector3 pos, Vector3 rot)
    {
        PlayerUpdateMsg m = new PlayerUpdateMsg();
        m.player.id = id;
        m.player.cubePos = pos;
        m.player.cubeRot = rot;
        for(int i = 0; i < m_Connections.Length; i++)
            SendToClient(JsonUtility.ToJson(m), m_Connections[i]);
    }

    void OnData(DataStreamReader stream, int i){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.PLAYER_CONNECT:
                PlayerConnectMsg pcMsg = JsonUtility.FromJson<PlayerConnectMsg>(recMsg);
                Debug.Log("A player just connected");
                break;
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                hsMsg.player.lastBeat = Time.deltaTime;
                for (int j = 0; j < checkDroppedPlayer.Count; j++)
                {
                    if (checkDroppedPlayer[j].id == hsMsg.player.id)
                        checkDroppedPlayer[j] = hsMsg.player;
                    else
                        checkDroppedPlayer.Add(hsMsg.player);
                }
                Debug.Log("Handshake message received!");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                SendPostion(puMsg.player.id, puMsg.player.cubePos, puMsg.player.cubeRot);
                //Debug.Log("Player update message received!");
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Server update message received!");
                break;
            default:
                Debug.Log("SERVER ERROR: Unrecognized message received!");
            break;
        }
    }

    void OnDisconnect(int i)
    {

        PlayerDropMsg pdMsg = new PlayerDropMsg();
        pdMsg.droppedPlayer.id = m_Connections[i].InternalId.ToString();
        for (int j = 0; j < m_Connections.Length; j++)
        {
            if (j != i)
            {
                SendToClient(JsonUtility.ToJson(pdMsg), m_Connections[i]);
            }
        }
        foreach (var it in plMsg.players)
            if (it.id == pdMsg.droppedPlayer.id)
                plMsg.players.Remove(it);

        m_Connections[i] = default(NetworkConnection);
    }

    void DropClients()
    {
        List<NetworkObjects.NetworkPlayer> droppedList = new List<NetworkObjects.NetworkPlayer>();
        for(int i = 0; i < plMsg.players.Count; i++)
        {
            plMsg.players[i].dropTimer += Time.deltaTime;
            if(plMsg.players[i].dropTimer >= 5.0f)
            {
                int index = MatchConnectionWithId(plMsg.players[i].id);
                if (index >= 0)
                    m_Connections[index] = default(NetworkConnection);

                droppedList.Add(plMsg.players[i]);
                plMsg.players.RemoveAt(i);
                i--;
            }
        }

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {

                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        if(droppedList.Count > 0)
        {
            Debug.Log("Player Dropped");
            PlayerDropMsg drop = new PlayerDropMsg(droppedList);
            for (int i = 0; i < m_Connections.Length; i++)
            {
                SendToClient(JsonUtility.ToJson(drop), m_Connections[i]);
            }
        }
    }


    void CheckHeartBeat()
    {
        foreach
    }

    int MatchConnectionWithId(string id)
    {
        for(int i = 0; i < m_Connections.Length; i++)
        {
            if (m_Connections[i].InternalId.ToString() == id)
                return i;
        }
        return -1;

    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c  != default(NetworkConnection))
        {            
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }

        //DropClients();

        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);
            
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                //else if (cmd == NetworkEvent.Type.Disconnect)
                //{
                //    OnDisconnect(i);
                //}
                // disconnect?
                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}