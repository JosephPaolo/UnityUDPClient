﻿/*
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
    [SerializeField] private Transform localClientCharacterRef;
    [SerializeField] private GameObject clientCubePrefab;

    private Dictionary<string, GameObject> playerReferences;
    private Queue<string> dataQueue; //data is queued so they are only processed once chronologically

    public Message latestMessage;
    public GameState lastestGameState;

    //TODO client dictionary

    // Start is called before the first frame update
    void Start() {
        string msgJson;
        FlagToServer flag = new FlagToServer();
        Byte[] sendBytes;

        if (bDebug){ Debug.Log("[Notice] Setting up client...");}

        udp = new UdpClient();
        playerReferences = new Dictionary<string, GameObject>();
        dataQueue = new Queue<string>();

        if (bDebug){ Debug.Log("[Notice] Client connecting to Server...");}
        udp.Connect("localhost",12345);

        //Debug.Log("udp.ToString(): " + udp.Client.LocalEndPoint);

        //Send flag 'connect' to server
        FlagToServer connectFlag = new FlagToServer();
        connectFlag.flag = NetworkMan.flag.CONNECT;
        msgJson = JsonUtility.ToJson(connectFlag);
        //Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        sendBytes = Encoding.ASCII.GetBytes(msgJson);
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);
        if (bDebug){ Debug.Log("[Notice] Client-server connection established; " + udp.Client.LocalEndPoint + "to " + udp.Client.RemoteEndPoint.ToString());}

        //if (bDebug){ Debug.Log("[Notice] Routinely sending Heartbeat.");}
        //InvokeRepeating("HeartBeat", 1, 1); // Sends 1 heartbeat message to server every 1 second.

        //if (bDebug){ Debug.Log("[Notice] Routinely sending Coordinates.");}
        //InvokeRepeating("UploadLocalClientPosition", 0.033f, 1); //Send 1 coordinate message to server every 0.033 second. Essentially 30 times per second.
    }

    void OnDestroy() {
        if (bDebug){ Debug.Log("[Notice] Cleaning up UDP Client...");}
        udp.Dispose();
    }

    public enum commands {
        NEW_CLIENT,
        UPDATE,
        DROP_CLIENT,
        CLIENT_LIST,
        PONG
    };

    public enum flag {
        NONE,
        CONNECT,
        PING,
        MESSAGE,
        COORDS,
        HEARTBEAT
    };

    [Serializable]
    public class Message {
        public commands cmd;
    }

    [Serializable]
    public class FlagToServer {
        public flag flag = flag.NONE;
    }

    [Serializable]
    public class MsgToServer {
        public flag flag = flag.MESSAGE;
        public string message;
    }

    [Serializable]
    public class Coordinates {
        public float x = 0;
        public float y = 0;
        public float z = 0;
        public flag flag = flag.COORDS;
    }

    [Serializable]
    public class Player {
        public string ip;
        public string port;
        public Coordinates position;     
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
            newObj.GetComponent<RemotePlayerData>().id = newPlayer.ip + ":" + newPlayer.port;
            playerReferences.Add(newPlayer.ip + ":" + newPlayer.port, newObj);
            dataQueue.Dequeue();
            Debug.Log("[Notice] Player " + newPlayer.ip + ":" + newPlayer.port + " has entered the game.");
        }
        //If local user joins a server with clients connected, receive list of clients, and add them to the game.
        else if (JsonUtility.FromJson<Message>(dataQueue.Peek()).cmd == commands.CLIENT_LIST) {
            GameObject otherObj;
            GameState connectedPlayers = JsonUtility.FromJson<GameState>(dataQueue.Peek());

            if (bDebug) { Debug.Log("[Notice] Client received command: CLIENT_LIST"); }
            foreach (Player targetPlayer in connectedPlayers.players) {

                if (playerReferences.ContainsKey(targetPlayer.ip + ":" + targetPlayer.port)) { //Check if player is already in the game, skip
                    if (bDebug) { Debug.Log("[Notice] Player already in game; skipping."); }
                }
                else {
                    spawnlocation = new Vector3 (UnityEngine.Random.Range(-10.0f, 10.0f),UnityEngine.Random.Range(-4.0f, 4.0f),UnityEngine.Random.Range(4.0f, 24.0f));
                    otherObj = Instantiate(clientCubePrefab, spawnlocation, Quaternion.identity);
                    otherObj.GetComponent<RemotePlayerData>().id = targetPlayer.ip + ":" + targetPlayer.port;
                    playerReferences.Add(targetPlayer.ip + ":" + targetPlayer.port, otherObj);
                    if (bDebug) { Debug.Log("[Notice] Implemented connected client to game: " + targetPlayer.ip + ":" + targetPlayer.port); }
                }
            }
            dataQueue.Dequeue();

            Debug.Log("[Notice] Connected players: ");
            foreach (Player targetPlayer in connectedPlayers.players) {
                Debug.Log("    " + targetPlayer.ip + ":" + targetPlayer.port);
            }
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
            //playerReferences[player.id].GetComponent<Renderer>().material.color = new Color (player.color.R,player.color.G,player.color.B,1);
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
        Debug.Log("[Notice] Player " + droppedPlayer.ip + ":" + droppedPlayer.port + " has left the game.");

        if (playerReferences.ContainsKey(droppedPlayer.ip + ":" + droppedPlayer.port)) {
            Destroy(playerReferences[droppedPlayer.ip + ":" + droppedPlayer.port]); // Destroy the dropped player’s game object. UNTESTED
            playerReferences.Remove(droppedPlayer.ip + ":" + droppedPlayer.port); //Remove the dropped player's entry from the dictionary of currently connected players. UNTESTED
        }
        else {
            Debug.LogError("[Error] Invalid key for player address. Skipping Operation.");
        }

        dataQueue.Dequeue();
    }

    void HeartBeat() {
        if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Sending message to server: heartbeat"); }
        string msgJson;
        FlagToServer flag = new FlagToServer();
        Byte[] sendBytes;

        flag.flag = NetworkMan.flag.HEARTBEAT;
        msgJson = JsonUtility.ToJson(flag);

        //Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        sendBytes = Encoding.ASCII.GetBytes(msgJson);
        udp.Send(sendBytes, sendBytes.Length);

    }

    //Routinely send the position of local client's character to the server
    void UploadLocalClientPosition() {

        if (!localClientCharacterRef) {
            Debug.LogError("[Error] local character ref missing! Aborting upload to server.");
            return;
        }

        if (bDebug && bVerboseDebug){ Debug.Log("[Routine] Sending message to server: coordinates " + localClientCharacterRef.position); }
        Coordinates initialPosition = new Coordinates();
        initialPosition.x = localClientCharacterRef.position.x;
        initialPosition.y = localClientCharacterRef.position.y;
        initialPosition.z = localClientCharacterRef.position.z;

        string posJson = JsonUtility.ToJson(initialPosition);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(posJson);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update() {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
