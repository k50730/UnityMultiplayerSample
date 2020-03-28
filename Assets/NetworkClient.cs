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
    string playerID;
    Dictionary<string, GameObject> ClientsList;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
        ClientsList = new Dictionary<string, GameObject>();
    }

    void SendToServer(string message)
    {
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect()
    {
        Debug.Log("We are now connected to the server");
    }

    public void SendingPosition(Vector3 pos, Vector3 rot)
    {
        PlayerInputMsg m = new PlayerInputMsg();
        m.position = pos;
        m.rotation = rot;
        SendToServer(JsonUtility.ToJson(m));
    }

    void OnData(DataStreamReader stream)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.cmd)
        {
            case Commands.PLAYER_CONNECT:
                PlayerConnectMsg pcMsg = JsonUtility.FromJson<PlayerConnectMsg>(recMsg);
                SpawnPlayers(pcMsg.newPlayer.id, pcMsg.newPlayer.cubeColor);
                //Debug.Log("A player has connected with id:" + pcMsg.newPlayer.id);
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                UpdatePlayers(puMsg.players);
                //Debug.Log("Player update message received!");
                break;
            case Commands.OWNED_ID:
                OwnIDMsg idMsg = JsonUtility.FromJson<OwnIDMsg>(recMsg);
                playerID = idMsg.ownedPlayer.id;
                Debug.Log("Own id received! id: " + playerID);
                break;
            case Commands.PLAYER_DROPPED:
                PlayerDropMsg dropMsg = JsonUtility.FromJson<PlayerDropMsg>(recMsg);
                foreach (NetworkObjects.NetworkPlayer p in dropMsg.droppedPlayers)
                {
                    DestroyPlayers(p.id);
                }
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

    void Disconnect()
    {
        m_Connection.Disconnect(m_Driver);
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
                Disconnect();
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
        if (id == playerID)
        {
            //Debug.Log("Controller Added!");
            temp.AddComponent<PlayerController>().client = this;
        }
        ClientsList.Add(id, temp);
    }

    void DestroyPlayers(string id)
    {
        if (ClientsList.ContainsKey(id))
        {
            GameObject temp = ClientsList[id];
            ClientsList.Remove(id);
            Destroy(temp);
        }
    }

    void UpdatePlayers(List<NetworkObjects.NetworkPlayer> players)
    {
        foreach (NetworkObjects.NetworkPlayer p in players)
        {
            if (ClientsList.ContainsKey(p.id))
            {
                ClientsList[p.id].transform.position = p.cubePos;
                ClientsList[p.id].transform.eulerAngles = p.cubeRot;
            }
        }

    }
}