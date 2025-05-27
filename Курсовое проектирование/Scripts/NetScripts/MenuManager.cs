using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;

public class MenuManager : MonoBehaviourPunCallbacks
{


    public GameObject createObj;
    public GameObject playerObj;

    private TMP_InputField createInput;
    private TMP_InputField playerInput;
    public void CreateRoom()
    {
        createInput = createObj.GetComponent<TMP_InputField>();
        playerInput = playerObj.GetComponent<TMP_InputField>();
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = Int32.Parse(playerInput.text);
        PhotonNetwork.CreateRoom(createInput.text, roomOptions);
    }

    /*public void JoinRoom()
    {
        PhotonNetwork.JoinRoom("123");
    }*/

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Lobby");
    }

}
