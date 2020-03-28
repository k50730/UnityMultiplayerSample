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
    PlayerListMsg plMsg;
    List<float> msgInterval;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        plMsg = new PlayerListMsg();
        msgInterval = new List<float>();

        InvokeRepeating("SendPosition", 1, 0.033f);
    }

    void SendToClient(string message, NetworkConnection c)
    {
        if (!c.IsCreated)
        {
            Debug.LogError("Connection not created");
            return;
        }
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void OnConnect(NetworkConnection c)
    {
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
        msgInterval.Add(0.0f);

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
        m_Connections.Add(c);
    }

    void SendPosition()
    {
        PlayerUpdateMsg m = new PlayerUpdateMsg();
        foreach (NetworkObjects.NetworkPlayer p in plMsg.players)
        {
            NetworkObjects.NetworkPlayer temp = new NetworkObjects.NetworkPlayer();
            temp.id = p.id;
            temp.cubePos = p.cubePos;
            temp.cubeRot = p.cubeRot;
            m.players.Add(temp);
        }
        foreach (NetworkConnection c in m_Connections)
        {
            SendToClient(JsonUtility.ToJson(m), c);
        }
    }

    void OnData(DataStreamReader stream, int i)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        int index = MatchPlayerWithId(m_Connections[i].InternalId.ToString());

        switch (header.cmd)
        {
            case Commands.PLAYER_INPUT:
                PlayerInputMsg input = JsonUtility.FromJson<PlayerInputMsg>(recMsg);
                plMsg.players[index].cubePos = input.position;
                plMsg.players[index].cubeRot = input.rotation;
                msgInterval[index] = Time.deltaTime;
                break;
            default:
                Debug.Log("SERVER ERROR: Unrecognized message received!");
                break;
        }
    }

    void DropClients()
    {
        PlayerDropMsg droppedList = new PlayerDropMsg();
        for (int i = 0; i < plMsg.players.Count; i++)
        {
            msgInterval[i] += Time.deltaTime;
            if (msgInterval[i] >= 5.0f)
            {
                int index = MatchConnectionWithId(plMsg.players[i].id);
                if (index >= 0)
                    m_Connections[index] = default(NetworkConnection);

                droppedList.droppedPlayers.Add(plMsg.players[i]);
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

        if (droppedList.droppedPlayers.Count > 0)
        {
            Debug.Log("Player Dropped");
            for (int i = 0; i < m_Connections.Length; i++)
            {
                SendToClient(JsonUtility.ToJson(droppedList), m_Connections[i]);
            }
        }
    }

    int MatchConnectionWithId(string id)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (m_Connections[i].InternalId.ToString() == id)
                return i;
        }
        return -1;
    }
    int MatchPlayerWithId(string id)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (plMsg.players[i].id == id)
                return i;
        }
        return -1;
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c != default(NetworkConnection))
        {
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }

        DropClients();

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
                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}