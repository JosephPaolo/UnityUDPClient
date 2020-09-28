/*
 * Authors: Started by Galal Hassan, modified by Joseph Malibiran
 * Last Updated: September 28, 2020
 */

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

    //[SerializeField] private Material cubeColorMatRef;
    //[SerializeField] private GameObject localClientCubeRef;

    [SerializeField] private GameObject clientCubePrefab;

    private Dictionary<string, GameObject> playerReferences;
    private Queue<string> dataQueue; //data is queued so they are only processed once chronologically

    public Message latestMessage;
    public GameState lastestGameState;

    //TODO client dictionary

    // Start is called before the first frame update
    void Start() {
        if (bDebug){ Debug.Log("[Notice] Setting up client...");}

        udp = new UdpClient();
        playerReferences = new Dictionary<string, GameObject>();
        dataQueue = new Queue<string>();

        if (bDebug){ Debug.Log("[Notice] Client connecting to Server...");}
        udp.Connect("18.224.179.199",12345);
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
        UPDATE,
        DROP_CLIENT,
        CLIENT_LIST
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
        public Player player;
    }

    [Serializable]
    public class DroppedPlayer {
        public Player player;
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
        dataQueue.Enqueue(returnData);
        if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Received Data: " + returnData); }
        
        //latestMessage = JsonUtility.FromJson<Message>(returnData);
        //try{
        //    switch(latestMessage.cmd){
        //        case commands.NEW_CLIENT:
        //            if (bDebug){ Debug.Log("[Notice] Client received command: NEW_CLIENT!");}
        //            break;
        //        case commands.CLIENT_LIST:
        //            if (bDebug){ Debug.Log("[Notice] Client received command: CLIENT_LIST!");}
        //            break;
        //        case commands.DROP_CLIENT:
        //            if (bDebug){ Debug.Log("[Notice] Client received command: DROP_CLIENT!");}
        //            break;
        //        case commands.UPDATE:
        //            if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Client received command: UPDATE"); }
        //            lastestGameState = JsonUtility.FromJson<GameState>(returnData);
        //            break;
        //        default:
        //            Debug.LogError("[Error] Client received invalid command enum from message.");
        //            break;
        //    }
        //}
        //catch (Exception e){
        //    Debug.LogError("[Exception] Failed to receive message from server. " + e.ToString());
        //    //Debug.Log(e.ToString());
        //}

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    //When a new player is connected, the client adds the details of this player into a list of currently connected players. (Implementation Missing)

    //When a new player is connected, the client spawns a cube to represent the newly connected player. 
    //The cube should contain a script that holds the network id of this player. (Implementation Missing)
    void SpawnPlayers() {
        //if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Spawning Players."); }

        //Cube spawn locations are randomized per client. The positions will not be the same in each client because the positions aren't tracked by the server.

        Vector3 spawnlocation = Vector3.zero; 

        //Only process this function if there is data in queue
        if (dataQueue.Count <= 0) {
            return;
        }
        //When a new player is connected, the client adds the details of this player into a list of currently connected players.
        if (JsonUtility.FromJson<Message>(dataQueue.Peek()).cmd == commands.NEW_CLIENT) {
            GameObject newObj;
            Player newPlayer;

            if (bDebug){ Debug.Log("[Notice] Client received command: NEW_CLIENT");}
            newPlayer = JsonUtility.FromJson<NewPlayer>(dataQueue.Peek()).player;
            spawnlocation = new Vector3 (UnityEngine.Random.Range(-10.0f, 10.0f),UnityEngine.Random.Range(-4.0f, 4.0f),UnityEngine.Random.Range(4.0f, 24.0f));
            newObj = Instantiate(clientCubePrefab, spawnlocation, Quaternion.identity);
            newObj.GetComponent<RemotePlayerData>().id = newPlayer.id;
            playerReferences.Add(newPlayer.id, newObj);
            dataQueue.Dequeue();
            Debug.Log("[Notice] Player " + newPlayer.id + " has entered the game.");
        }
        //If local user joins a server with clients connected, receive list of clients, and add them to the game.
        else if (JsonUtility.FromJson<Message>(dataQueue.Peek()).cmd == commands.CLIENT_LIST) {
            GameObject otherObj;
            GameState connectedPlayers = JsonUtility.FromJson<GameState>(dataQueue.Peek());

            if (bDebug) { Debug.Log("[Notice] Client received command: CLIENT_LIST"); }
            foreach (Player targetPlayer in connectedPlayers.players) {
                if (playerReferences.ContainsKey(targetPlayer.id)) { //Check if player is already in the game, skip
                    continue;
                }

                spawnlocation = new Vector3 (UnityEngine.Random.Range(-10.0f, 10.0f),UnityEngine.Random.Range(-4.0f, 4.0f),UnityEngine.Random.Range(4.0f, 24.0f));
                otherObj = Instantiate(clientCubePrefab, spawnlocation, Quaternion.identity);
                otherObj.GetComponent<RemotePlayerData>().id = targetPlayer.id;
                playerReferences.Add(targetPlayer.id, otherObj);
                if (bDebug) { Debug.Log("[Notice] Implemented connected client to game: " + targetPlayer.id); }
            }
            dataQueue.Dequeue();
        }

    }

    //The client loops through all the currently connected players and updates the player game object properties (color, network id). (Implementation Missing) 
    void UpdatePlayers() {
        //Only process this function if there is data in queue
        if (dataQueue.Count <= 0) {
            return;
        }
        //This function only processes update commands
        if (JsonUtility.FromJson<Message>(dataQueue.Peek()).cmd != commands.UPDATE) {
            return;
        }

        lastestGameState = JsonUtility.FromJson<GameState>(dataQueue.Peek());

        foreach (Player player in lastestGameState.players) {
            playerReferences[player.id].GetComponent<Renderer>().material.color = new Color (player.color.R,player.color.G,player.color.B,1);
        }
        dataQueue.Dequeue();
    }

    //When a player is dropped, the client destroys the player’s game object. 
    //When a player is dropped, the client removes the player’s entry from the list of currently connected players. 
    void DestroyPlayers() {
        //Only process this function if there is data in queue
        if (dataQueue.Count <= 0) {
            return;
        }
        //This function only processes drop client commands
        if (JsonUtility.FromJson<Message>(dataQueue.Peek()).cmd != commands.DROP_CLIENT) {
            return;
        }

        Player droppedPlayer;
        droppedPlayer = JsonUtility.FromJson<DroppedPlayer>(dataQueue.Peek()).player;
        Debug.Log("[Notice] Player " + droppedPlayer.id + " has left the game.");

        if (playerReferences.ContainsKey(droppedPlayer.id)) {
            Destroy(playerReferences[droppedPlayer.id]); // Destroy the dropped player’s game object. UNTESTED
            playerReferences.Remove(droppedPlayer.id); //Remove the dropped player's entry from the dictionary of currently connected players. UNTESTED
        }
        else {
            Debug.LogError("[Error] Invalid key for player address. Skipping Operation.");
        }

        dataQueue.Dequeue();
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
