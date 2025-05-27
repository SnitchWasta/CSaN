using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class WeaponSelectionManager : MonoBehaviourPunCallbacks
{
    [Header("Settings")]
    public string defaultGameScene = "de_map1";
    public float selectionTime = 60f;
    public float startDelay = 5f;
    public WeaponData[] availableWeapons;

    [Header("UI References")]
    public TMP_Text timerText;
    public TMP_Text statusText;
    public RectTransform weaponGrid;
    public GameObject weaponButtonPrefab;
    public Button startGameButton;
    public Button backToLobbyButton;
    public TMP_Text selectedWeaponText;

    private float remainingTime;
    private bool countingDown;
    private int localSelectedWeapon = -1;
    private List<WeaponButton> weaponButtons = new List<WeaponButton>();
    private string selectedMapScene;

    void Start()
    {
        // Получаем выбранную карту
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedMapScene", out object sceneName))
        {
            selectedMapScene = (string)sceneName;
            Debug.Log($"Будет загружена карта: {selectedMapScene}");
        }
        else
        {
            selectedMapScene = defaultGameScene;
            Debug.LogWarning("Карта не выбрана, будет использована сцена по умолчанию");
        }

        remainingTime = selectionTime;
        countingDown = true;

        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        startGameButton.onClick.AddListener(StartGame);
        backToLobbyButton.onClick.AddListener(ReturnToLobby);

        InitializeWeaponGrid();
        UpdateUI();
    }

    void InitializeWeaponGrid()
    {
        // Очистка предыдущих кнопок
        foreach (Transform child in weaponGrid)
        {
            if (child != null && child.gameObject != null)
                Destroy(child.gameObject);
        }
        weaponButtons.Clear();

        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            Debug.LogError("No weapons available in availableWeapons array!");
            return;
        }

        float tileWidth = 333f;
        float spacing = 20f;
        float totalWidth = (availableWeapons.Length * tileWidth) +
                         ((availableWeapons.Length - 1) * spacing);

        weaponGrid.sizeDelta = new Vector2(totalWidth, weaponGrid.sizeDelta.y);

        for (int i = 0; i < availableWeapons.Length; i++)
        {
            CreateWeaponButton(i, tileWidth, spacing);
        }
    }

    void CreateWeaponButton(int weaponIndex, float tileWidth, float spacing)
    {
        if (weaponButtonPrefab == null)
        {
            Debug.LogError("Weapon button prefab is not assigned!");
            return;
        }

        GameObject buttonObj = Instantiate(weaponButtonPrefab, weaponGrid);
        if (buttonObj == null)
        {
            Debug.LogError("Failed to instantiate weapon button!");
            return;
        }

        WeaponButton button = buttonObj.GetComponent<WeaponButton>();
        if (button == null)
        {
            Debug.LogError("WeaponButton component not found on prefab!");
            Destroy(buttonObj);
            return;
        }

        // Устанавливаем позицию кнопки
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            float xPos = (tileWidth + spacing) * weaponIndex - tileWidth;
            buttonRect.anchoredPosition = new Vector2(xPos, 0);
        }

        // Инициализируем кнопку
        if (weaponIndex >= 0 && weaponIndex < availableWeapons.Length)
        {
            button.Initialize(
                availableWeapons[weaponIndex].weaponName,
                availableWeapons[weaponIndex].weaponIcon,
                () => SelectWeapon(weaponIndex)
            );
        }

        weaponButtons.Add(button);
    }

    void SelectWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponButtons.Count || weaponIndex >= availableWeapons.Length)
        {
            Debug.LogError($"Invalid weapon index: {weaponIndex}");
            return;
        }

        if (localSelectedWeapon == weaponIndex)
            return;

        // Снимаем выделение с предыдущей кнопки
        if (localSelectedWeapon >= 0 && localSelectedWeapon < weaponButtons.Count)
        {
            weaponButtons[localSelectedWeapon]?.SetSelected(false);
        }

        localSelectedWeapon = weaponIndex;

        if (weaponButtons[weaponIndex] != null)
        {
            weaponButtons[weaponIndex].SetSelected(true);
            selectedWeaponText.text = $"Выбрано: {availableWeapons[weaponIndex].weaponName}";
        }

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["selectedWeapon"] = availableWeapons[weaponIndex].weaponName;
        Debug.Log(props["selectedWeapon"]);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    void Update()
    {
        if (countingDown)
        {
            remainingTime -= Time.deltaTime;
            UpdateUI();

            if (remainingTime <= 0)
            {
                countingDown = false;
                AutoSelectWeapons();
            }
        }
    }

    void UpdateUI()
    {
        if (timerText != null)
            timerText.text = $"Осталось: {Mathf.FloorToInt(remainingTime)} сек";

        if (statusText != null)
            statusText.text = countingDown ? "Выберите оружие" : "Завершаем выбор...";
    }

    void AutoSelectWeapons()
    {
        if (PhotonNetwork.PlayerList == null || availableWeapons == null || availableWeapons.Length == 0)
            return;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player != null && !player.CustomProperties.ContainsKey("selectedWeapon"))
            {
                int randomWeapon = Random.Range(0, availableWeapons.Length);
                if (photonView != null)
                {
                    photonView.RPC("RPC_ForceSelectWeapon", player, randomWeapon);
                }
            }
        }
    }

    [PunRPC]
    void RPC_ForceSelectWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < availableWeapons.Length)
        {
            SelectWeapon(weaponIndex);
        }
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        bool allSelected = true;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == null || !player.CustomProperties.ContainsKey("selectedWeapon"))
            {
                allSelected = false;
                break;
            }
        }

        if (allSelected)
        {
            if (photonView != null)
            {
                photonView.RPC("RPC_StartGameCountdown", RpcTarget.All);
            }
        }
        else if (statusText != null)
        {
            statusText.text = "Не все игроки выбрали оружие!";
        }
    }

    [PunRPC]
    void RPC_StartGameCountdown()
    {
        StartCoroutine(GameStartCountdown());
    }

    IEnumerator GameStartCountdown()
    {
        countingDown = false;

        // Синхронизируем изменение текста для всех игроков
        photonView.RPC("RPC_UpdateStatusText", RpcTarget.All, "Игра начинается...");

        yield return new WaitForSeconds(startDelay);

        if (PhotonNetwork.IsMasterClient)
        {
            if (!string.IsNullOrEmpty(selectedMapScene))
            {
                // Сохраняем выбранную сцену в свойствах комнаты
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props["SelectedMapScene"] = selectedMapScene;
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                // Загружаем сцену для всех игроков
                PhotonNetwork.LoadLevel(selectedMapScene);
            }
            else
            {
                Debug.LogError("Selected map scene is null or empty!");
                photonView.RPC("RPC_UpdateStatusText", RpcTarget.All, "Ошибка: карта не выбрана");
            }
        }
    }

    [PunRPC]
    void RPC_UpdateStatusText(string newText)
    {
        if (statusText != null)
        {
            statusText.text = newText;
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("SelectedMapScene"))
        {
            string sceneName = (string)propertiesThatChanged["SelectedMapScene"];
            if (!string.IsNullOrEmpty(sceneName))
            {
                PhotonNetwork.LoadLevel(sceneName);
            }
        }
    }

    public void ReturnToLobby()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            photonView.RPC("RPC_ReturnToLobby", RpcTarget.All);
        }
        else
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    [PunRPC]
    private void RPC_ReturnToLobby()
    {
        PhotonNetwork.LoadLevel("Lobby");
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Menu");
    }
}

[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public Sprite weaponIcon;
    public GameObject weaponPrefab;
}
