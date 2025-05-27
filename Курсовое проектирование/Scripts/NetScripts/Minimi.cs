using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Minimi : MonoBehaviour
{
    [SerializeField] private GameObject field;
    private TMP_InputField playerNameInput;
    void Start()
    {
        playerNameInput = field.GetComponent<TMP_InputField>();

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);
        }
        playerNameInput.text = PhotonNetwork.NickName;
    }
    public void OnPlayerNameChanged()
    {
        PhotonNetwork.NickName = playerNameInput.text;
    }
}
