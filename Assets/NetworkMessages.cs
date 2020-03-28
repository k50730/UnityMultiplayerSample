using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands
    {
        PLAYER_CONNECT,
        PLAYER_UPDATE,
        PLAYER_INPUT,
        OWNED_ID,
        PLAYER_DROPPED,
        PLAYER_LIST
    }

    [System.Serializable]
    public class NetworkHeader
    {
        public Commands cmd;
    }

    [System.Serializable]
    public class PlayerUpdateMsg : NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> players;
        public PlayerUpdateMsg()
        {
            cmd = Commands.PLAYER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    };

    [System.Serializable]
    public class PlayerInputMsg : NetworkHeader
    {
        public Vector3 position;
        public Vector3 rotation;

        public PlayerInputMsg()
        {
            cmd = Commands.PLAYER_INPUT;
        }
    }

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
    public class PlayerListMsg : NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> players;
        public PlayerListMsg()
        {
            cmd = Commands.PLAYER_LIST;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }

    [System.Serializable]
    public class OwnIDMsg : NetworkHeader
    {
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
        public List<NetworkObjects.NetworkPlayer> droppedPlayers;
        public PlayerDropMsg()
        {
            cmd = Commands.PLAYER_DROPPED;
            droppedPlayers = new List<NetworkObjects.NetworkPlayer>();
        }
        public PlayerDropMsg(List<NetworkObjects.NetworkPlayer> playerList)
        {      // Constructor
            cmd = Commands.PLAYER_DROPPED;
            droppedPlayers = playerList;
        }
    }

}

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject
    {
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject
    {
        public Color cubeColor;
        public Vector3 cubePos;
        public Vector3 cubeRot;

        public NetworkPlayer()
        {
            cubeColor = new Color();
        }
    }
}
