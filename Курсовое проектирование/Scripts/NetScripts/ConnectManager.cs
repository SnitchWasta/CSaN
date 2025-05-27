using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;

public class ConnectManager : MonoBehaviourPunCallbacks
{


    public GameObject joineObj;

    private TMP_InputField joinInput;


    public void JoinRoom()
    {
        joinInput = joineObj.GetComponent<TMP_InputField>();
        PhotonNetwork.JoinRoom(joinInput.text);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Lobby");
    }

}
