using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour {
    public UdpClient udp;

    [SerializeField] private bool bDebug = true;
    [SerializeField] private bool bVerboseDebug = false;

    [SerializeField] private Material cubeColorMatRef;
    [SerializeField] private GameObject clientCubeRef;

    private Dictionary<string, GameObject> playerReferences;

    public Message latestMessage;
    public GameState lastestGameState;

    //TODO client dictionary

    // Start is called before the first frame update
    void Start() {
        if (bDebug){ Debug.Log("[Notice] Setting up client...");}

        udp = new UdpClient();
        playerReferences = new Dictionary<string, GameObject>();

        if (bDebug){ Debug.Log("[Notice] Client connecting to Server...");}
        udp.Connect("localhost",12345);
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);
        if (bDebug){ Debug.Log("[Notice] Client server connection established.");}

        if (bDebug){ Debug.Log("[Notice] Routinely sending Heartbeat.");}
        InvokeRepeating("HeartBeat", 1, 1); // Sends 1 heartbeat message to server every 1 second.

    }

    void OnDestroy() {
        if (bDebug){ Debug.Log("[Notice] Cleaning up UDP Client...");}
        udp.Dispose();
    }


    public enum commands {
        NEW_CLIENT,
        UPDATE
    };
    
    [Serializable]
    public class Message {
        public commands cmd;
    }
    
    [Serializable]
    public class Player {
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        public string id;
        public receivedColor color;        
    }

    [Serializable]
    public class NewPlayer {
        
    }

    [Serializable]
    public class GameState {
        public Player[] players;
    }

    void OnReceived(IAsyncResult result) {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Received Data: " + returnData); }
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:
                    if (bDebug){ Debug.Log("[Notice] Client received command: NEW_CLIENT");}

                    break;
                case commands.UPDATE:
                    if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Client received command: UPDATE"); }
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);

                    //Debug.Log("lastestGameState.players: " + lastestGameState.players);
                    //Debug.Log("lastestGameState.players[0].color: " + lastestGameState.players[0].color);
                    //Color myColor = new Color (lastestGameState.players[0].color.R,lastestGameState.players[0].color.G,lastestGameState.players[0].color.B,1);
                    //string mystring = latestMessage['color']
                    //UpdateClientCubeColor(new Color (lastestGameState.players[0].color.R,lastestGameState.players[0].color.G,lastestGameState.players[0].color.B,1)); 
                    //UpdateClientCubeColor(lastestGameState.players[0].color.R,lastestGameState.players[0].color.G,lastestGameState.players[0].color.B);
                    //UpdateClientCubeColor();
                    //clientCubeRef.GetComponent<Renderer>().material.color = new Color (lastestGameState.players[0].color.R,lastestGameState.players[0].color.G,lastestGameState.players[0].color.B,1);
                    //cubeColorMatRef.SetColor("_Color", new Color (lastestGameState.players[0].color.R,lastestGameState.players[0].color.G,lastestGameState.players[0].color.B,1));
                    break;
                default:
                    Debug.LogError("[Error] Client received invalid command enum from message.");
                    break;
            }
        }
        catch (Exception e){
            Debug.LogError("[Exception] Failed to receive message from server. " + e.ToString());
            //Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    //When a new player is connected, the client adds the details of this player into a list of currently connected players. (Implementation Missing)

    //When a new player is connected, the client spawns a cube to represent the newly connected player. 
    //The cube should contain a script that holds the network id of this player. (Implementation Missing)
    void SpawnPlayers() {
        //if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Spawning Players."); }

    }

    //The client loops through all the currently connected players and updates the player game object properties (color, network id). (Implementation Missing) 
    void UpdatePlayers() {
        if (lastestGameState.players.Length <= 0) {
            //No players yet, client must be set up first
            return;
        }
        if (!clientCubeRef) {
            Debug.LogError("[Error] clientCubeRef reference not found! Aborting player update.");
            return;
        }

        //if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Updating Player Colors."); }
        clientCubeRef.GetComponent<Renderer>().material.color = new Color (lastestGameState.players[0].color.R,lastestGameState.players[0].color.G,lastestGameState.players[0].color.B,1);
    }

    //When a player is dropped, the client destroys the player’s game object. (Implementation Missing)
    //When a player is dropped, the client removes the player’s entry from the list of currently connected players. (Implementation Missing)
    void DestroyPlayers() {
        //if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Destroying Players."); }

    }

    void HeartBeat() {
        if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Sending message to server: hearbeat"); }
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update() {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
