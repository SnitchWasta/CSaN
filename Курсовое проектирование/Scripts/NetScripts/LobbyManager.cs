using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public Button weaponSelectionButton;
    public TMP_Text statusText;
    public TMP_Text playersListText;
    public Button backButton;
    public GameObject confirmationPanel;
    public TMP_Text confirmationText;
    public Button confirmButton;
    public Button cancelButton;
    public string weaponSelectionScene = "WeaponSelectionScene";

    [Header("Map Selection")]
    public MapData[] availableMaps;
    public GameObject mapTilePrefab;
    public RectTransform mapsContent;
    public TMP_Text selectedMapText;

    private int selectedMapIndex = -1;
    private List<MapTile> mapTiles = new List<MapTile>();

    void Start()
    {
        backButton.onClick.AddListener(ShowExitConfirmation);
        confirmButton.onClick.AddListener(OnConfirmExit);
        cancelButton.onClick.AddListener(HideExitConfirmation);
        confirmationPanel.SetActive(false);

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("PhotonNetwork не подключен!");
            SceneManager.LoadScene("Menu");
            return;
        }

        // Reset room state
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedMapScene"))
            {
                PhotonNetwork.CurrentRoom.CustomProperties.Remove("SelectedMapScene");
                PhotonNetwork.CurrentRoom.SetCustomProperties(PhotonNetwork.CurrentRoom.CustomProperties);
                Debug.Log("Cleared SelectedMapScene custom property");
            }
        }

        weaponSelectionButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        weaponSelectionButton.onClick.AddListener(StartWeaponSelection);
        weaponSelectionButton.interactable = true; // Ensure button is interactable

        UpdateUI();
        InitializeMapSelection();
        UpdatePlayersList();
    }

    void UpdateUI()
    {
        statusText.text = PhotonNetwork.IsMasterClient
            ? "Вы хост комнаты"
            : "Ожидаем выбора карты...";
    }

    public void StartWeaponSelection()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only master client can start weapon selection");
            return;
        }

        if (selectedMapIndex < 0 || selectedMapIndex >= availableMaps.Length)
        {
            Debug.LogWarning("No map selected, cannot start game");
            statusText.text = "Пожалуйста, выберите карту!";
            return;
        }

        weaponSelectionButton.interactable = false;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        photonView.RPC("RPC_LoadWeaponSelection", RpcTarget.All);
        Debug.Log("StartWeaponSelection initiated");
    }

    [PunRPC]
    void RPC_LoadWeaponSelection()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (selectedMapIndex >= 0 && selectedMapIndex < availableMaps.Length)
            {
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["SelectedMapScene"] = availableMaps[selectedMapIndex].sceneName;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
                Debug.Log($"Master Client set SelectedMapScene to {availableMaps[selectedMapIndex].mapName}");
            }
            else
            {
                Debug.LogWarning("Invalid selectedMapIndex, cannot set SelectedMapScene");
            }
        }
        StartCoroutine(LoadWeaponSelectionScene());
    }

    private IEnumerator LoadWeaponSelectionScene()
    {
        Debug.Log("Starting LoadWeaponSelectionScene coroutine");
        yield return new WaitForSecondsRealtime(0.5f); // Use real-time for reliability
        Debug.Log($"Attempting to load {weaponSelectionScene}");
        try
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(weaponSelectionScene);
                Debug.Log($"Master client loading {weaponSelectionScene}");
            }
            else
            {
                Debug.Log("Non-master client waiting for scene sync");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load {weaponSelectionScene}: {ex.Message}");
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdateUI();
        weaponSelectionButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        weaponSelectionButton.interactable = true;
    }

    void ShowExitConfirmation()
    {
        confirmationText.text = PhotonNetwork.IsMasterClient
            ? "Вы уверены, что хотите закрыть комнату и выйти?"
            : "Вы уверены, что хотите покинуть комнату?";
        confirmationPanel.SetActive(true);
    }

    void HideExitConfirmation()
    {
        confirmationPanel.SetActive(false);
    }

    void OnConfirmExit()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("Not in a room, loading Menu scene");
            SceneManager.LoadScene("Menu");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            photonView.RPC("RPC_RoomClosed", RpcTarget.All);
            StartCoroutine(DelayedLeaveRoom()); // Delay to ensure RPC is sent
        }
        else
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    [PunRPC]
    void RPC_RoomClosed()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            Debug.LogWarning("Already left room or not in room");
            SceneManager.LoadScene("Menu");
        }
    }

    private IEnumerator DelayedLeaveRoom()
    {
        Debug.Log("Delaying LeaveRoom to ensure RPC_RoomClosed is processed");
        yield return new WaitForSecondsRealtime(0.5f);
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room, loading Menu scene");
        SceneManager.LoadScene("Menu");
    }

    void UpdatePlayersList()
    {
        if (PhotonNetwork.InRoom)
        {
            playersListText.text = string.Join("\n", GetPlayerNames());
        }
        else
        {
            playersListText.text = "Не в комнате";
        }
    }

    private List<string> GetPlayerNames()
    {
        List<string> names = new List<string>();
        foreach (var player in PhotonNetwork.PlayerList)
        {
            names.Add(player.NickName);
        }
        return names;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayersList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayersList();
    }

    #region Map Selection
    void InitializeMapSelection()
    {
        foreach (Transform child in mapsContent)
        {
            Destroy(child.gameObject);
        }
        mapTiles.Clear();

        float tileWidth = 415.323f;
        float spacing = 20f;
        float totalWidth = (availableMaps.Length * tileWidth) + ((availableMaps.Length - 1) * spacing);
        mapsContent.sizeDelta = new Vector2(totalWidth, mapsContent.sizeDelta.y);

        for (int i = 0; i < availableMaps.Length; i++)
        {
            CreateMapTile(i, tileWidth, spacing);
        }
    }

    void CreateMapTile(int mapIndex, float tileWidth, float spacing)
    {
        GameObject tileObj = Instantiate(mapTilePrefab, mapsContent);
        MapTile tile = tileObj.GetComponent<MapTile>();

        if (tile != null)
        {
            RectTransform tileRect = tileObj.GetComponent<RectTransform>();
            float xPos = (tileWidth + spacing) * mapIndex;
            tileRect.anchoredPosition = new Vector2(xPos, 0);

            tile.MapImage.sprite = availableMaps[mapIndex].mapPreview;
            tile.MapName.text = availableMaps[mapIndex].mapName;
            tile.SelectButton.onClick.AddListener(() => OnMapTileClicked(mapIndex));

            mapTiles.Add(tile);
        }
        else
        {
            Debug.LogError("Компонент MapTile не найден на префабе!");
            Destroy(tileObj);
        }
    }

    void OnMapTileClicked(int mapIndex)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Только хост может выбирать карту");
            return;
        }

        selectedMapIndex = mapIndex;
        selectedMapText.text = availableMaps[mapIndex].mapName;
        UpdateMapTilesSelection();

        if (photonView != null)
        {
            photonView.RPC("RPC_SyncSelectedMap", RpcTarget.All, mapIndex);
        }
        else
        {
            Debug.LogError("PhotonView не найден!");
        }
    }

    void UpdateMapTilesSelection()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            bool isSelected = (i == selectedMapIndex);
            mapTiles[i].Background.color = isSelected ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
        }
    }

    [PunRPC]
    void RPC_SyncSelectedMap(int mapIndex)
    {
        selectedMapIndex = mapIndex;
        selectedMapText.text = availableMaps[mapIndex].mapName;
        UpdateMapTilesSelection();
    }
    #endregion
}

[System.Serializable]
public class MapData
{
    public string mapName;
    public Sprite mapPreview;
    public string sceneName;
}