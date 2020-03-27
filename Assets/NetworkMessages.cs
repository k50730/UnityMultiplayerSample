using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands {
        PLAYER_CONNECT,
        PLAYER_UPDATE,
        SERVER_UPDATE,
        HANDSHAKE,
        PLAYER_INPUT,
        OWNED_ID,
        PLAYER_DROPPED,
        PLAYER_LIST
    }

    [System.Serializable]
    public class NetworkHeader {
        public Commands cmd;
    }

    [System.Serializable]
    public class HandshakeMsg : NetworkHeader {
        public NetworkObjects.NetworkPlayer player;
        public HandshakeMsg() {      // Constructor
            cmd = Commands.HANDSHAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }

    [System.Serializable]
    public class PlayerUpdateMsg : NetworkHeader {
        public NetworkObjects.NetworkPlayer player;
        public PlayerUpdateMsg() {      // Constructor
            cmd = Commands.PLAYER_UPDATE;
            player = new NetworkObjects.NetworkPlayer();
        }
    };

    [System.Serializable]
    public class PlayerConnectMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer newPlayer;
        public PlayerConnectMsg()
        {
            cmd = Commands.PLAYER_CONNECT;
            newPlayer = new NetworkObjects.NetworkPlayer();
        }
    }

    [System.Serializable]
    public class PlayerListMsg: NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> players;
        public PlayerListMsg()
        {
            cmd = Commands.PLAYER_LIST;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }
    [System.Serializable]
    public class ServerUpdateMsg : NetworkHeader {
        public List<NetworkObjects.NetworkPlayer> players;
        public ServerUpdateMsg() {      // Constructor
            cmd = Commands.SERVER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }

    [System.Serializable]
    public class OwnIDMsg : NetworkHeader {
        public NetworkObjects.NetworkPlayer ownedPlayer;

        public OwnIDMsg()
        {
            cmd = Commands.OWNED_ID;
            ownedPlayer = new NetworkObjects.NetworkPlayer();
        }
    }
    [System.Serializable]
    public class PlayerDropMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer droppedPlayer;
        public PlayerDropMsg()
        {      // Constructor
            cmd = Commands.PLAYER_DROPPED;
            droppedPlayer = new NetworkObjects.NetworkPlayer();
        }
    }

} 

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject{
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject{
        public Color cubeColor;
        public Vector3 cubePos;
        public Vector3 cubeRot;

        public NetworkPlayer(){
            cubeColor = new Color();
        }
    }
}
