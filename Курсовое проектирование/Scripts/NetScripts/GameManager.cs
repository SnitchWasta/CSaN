using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Game Settings")]
    public float gameDuration = 300f; // 5 minutes in seconds
    public int killPoints = 10;

    [Header("UI References")]
    public TMP_Text timerText;
    public TMP_Text killFeedText;
    public GameObject scoreboardPanel;
    public Transform scoreboardContent;
    public GameObject playerScorePrefab;
    public GameObject endGamePanel;
    public Transform endGameContent;

    [Header("Scoreboard Settings")]
    public float scoreEntryHeight = 50f; // Height of each PlayerScoreEntry
    public float scoreEntrySpacing = 5f; // Spacing between entries
    public Vector2 scoreEntryStartPos = new Vector2(0, 0); // Starting position (top-center)

    private Dictionary<int, PlayerStats> playerStats = new Dictionary<int, PlayerStats>();
    private float gameTimeRemaining;
    private bool isGameRunning;

    void Awake()
    {
        // Prevent UI destruction across scenes
        DontDestroyOnLoad(gameObject);
        if (scoreboardPanel != null) DontDestroyOnLoad(scoreboardPanel);
        if (endGamePanel != null) DontDestroyOnLoad(endGamePanel);
    }

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        gameTimeRemaining = gameDuration;
        isGameRunning = true;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerStats[player.ActorNumber] = new PlayerStats(player);
            Debug.Log($"Initialized stats for player {player.NickName} (Actor {player.ActorNumber})");
        }

        if (timerText != null) UpdateTimerDisplay();
        else Debug.LogWarning("timerText is not assigned!");

        if (scoreboardPanel != null) scoreboardPanel.SetActive(true);
        else Debug.LogWarning("scoreboardPanel is not assigned!");

        if (endGamePanel != null) endGamePanel.SetActive(false);
        else Debug.LogWarning("endGamePanel is not assigned!");

        if (scoreboardContent == null || !scoreboardContent) Debug.LogError("scoreboardContent is not assigned or destroyed!");
        if (playerScorePrefab == null) Debug.LogError("playerScorePrefab is not assigned!");
        if (endGameContent == null || !endGameContent) Debug.LogError("endGameContent is not assigned or destroyed!");

        UpdateScoreboard();
    }

    void Update()
    {
        if (isGameRunning)
        {
            gameTimeRemaining -= Time.deltaTime;
            if (timerText != null) UpdateTimerDisplay();
            else Debug.Log($"gameTimeRemaining: {gameTimeRemaining}");

            if (gameTimeRemaining <= 0)
            {
                Debug.Log("Game timer ended, calling EndGame");
                EndGame();
            }
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(gameTimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(gameTimeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void RegisterKill(int killerActorNumber, int victimActorNumber)
    {
        if (!isGameRunning) return;

        Debug.Log($"RegisterKill called: killer={killerActorNumber}, victim={victimActorNumber}");
        photonView.RPC("RPC_RegisterKill", RpcTarget.All, killerActorNumber, victimActorNumber);
    }

    [PunRPC]
    void RPC_RegisterKill(int killerActorNumber, int victimActorNumber)
    {
        if (killerActorNumber != victimActorNumber && playerStats.ContainsKey(killerActorNumber))
        {
            playerStats[killerActorNumber].kills++;
            playerStats[killerActorNumber].score += killPoints;
        }

        if (playerStats.ContainsKey(victimActorNumber))
        {
            playerStats[victimActorNumber].deaths++;
        }

        Player killer = PhotonNetwork.CurrentRoom?.GetPlayer(killerActorNumber);
        Player victim = PhotonNetwork.CurrentRoom?.GetPlayer(victimActorNumber);
        if (killer != null && victim != null)
        {
            AddKillFeedMessage($"{killer.NickName} убил {victim.NickName}");
            Debug.Log($"Kill feed: {killer.NickName} убил {victim.NickName}");
        }
        else
        {
            Debug.LogWarning($"Failed to get player names for killer={killerActorNumber}, victim={victimActorNumber}");
        }

        UpdateScoreboard();
    }

    void AddKillFeedMessage(string message)
    {
        if (killFeedText == null)
        {
            Debug.LogWarning("killFeedText is not assigned!");
            return;
        }

        killFeedText.text = message + "\n" + killFeedText.text;

        string[] lines = killFeedText.text.Split('\n');
        if (lines.Length > 5)
        {
            killFeedText.text = string.Join("\n", lines, 0, 5);
        }
    }

    void UpdateScoreboard()
    {
        if (scoreboardContent == null || !scoreboardContent)
        {
            Debug.LogError("Cannot update scoreboard: scoreboardContent is null or destroyed!");
            return;
        }

        if (playerScorePrefab == null)
        {
            Debug.LogError("Cannot update scoreboard: playerScorePrefab is null!");
            return;
        }

        // Clear existing entries
        foreach (Transform child in scoreboardContent)
        {
            if (child != null) Destroy(child.gameObject);
        }

        List<PlayerStats> sortedStats = new List<PlayerStats>(playerStats.Values);
        sortedStats.Sort((a, b) => b.score.CompareTo(a.score));

        Debug.Log($"Updating scoreboard with {sortedStats.Count} entries");

        // Position entries manually
        for (int i = 0; i < sortedStats.Count; i++)
        {
            PlayerStats stats = sortedStats[i];
            GameObject entry = Instantiate(playerScorePrefab, scoreboardContent);
            PlayerScoreEntry scoreEntry = entry.GetComponent<PlayerScoreEntry>();
            if (scoreEntry != null)
            {
                scoreEntry.Initialize(
                    stats.player.NickName,
                    stats.kills.ToString(),
                    stats.deaths.ToString(),
                    stats.score.ToString()
                );

                // Set position
                RectTransform rect = entry.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    float yPos = scoreEntryStartPos.y - (i * (scoreEntryHeight + scoreEntrySpacing));
                    rect.anchoredPosition = new Vector2(scoreEntryStartPos.x, yPos);
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, scoreEntryHeight);
                    Debug.Log($"Positioned entry for {stats.player.NickName} at y={yPos}");
                }
                else
                {
                    Debug.LogWarning($"RectTransform missing on PlayerScoreEntry for {stats.player.NickName}");
                }
            }
            else
            {
                Debug.LogWarning($"PlayerScoreEntry component missing on instantiated prefab for {stats.player.NickName}");
            }
        }
    }

    void EndGame()
    {
        isGameRunning = false;
        Debug.Log("Ending game via RPC");
        photonView.RPC("RPC_EndGame", RpcTarget.All);
    }

    [PunRPC]
    void RPC_EndGame()
    {
        Time.timeScale = 0;
        timerText.text = string.Empty;
        killFeedText.text = string.Empty;
        if (scoreboardPanel != null && scoreboardPanel) scoreboardPanel.SetActive(false);
        else Debug.LogWarning("scoreboardPanel is not assigned or destroyed!");

        if (endGamePanel != null && endGamePanel)
        {
            endGamePanel.SetActive(true);
            Debug.Log("endGamePanel activated");
        }
        else
        {
            Debug.LogError("endGamePanel is not assigned or destroyed!");
        }

        if (endGameContent == null || !endGameContent || playerScorePrefab == null)
        {
            Debug.LogError("Cannot populate endGameContent: endGameContent or playerScorePrefab is null or destroyed!");
            return;
        }

        // Clear existing entries
        foreach (Transform child in endGameContent)
        {
            if (child != null) Destroy(child.gameObject);
        }

        List<PlayerStats> sortedStats = new List<PlayerStats>(playerStats.Values);
        sortedStats.Sort((a, b) => b.score.CompareTo(a.score));

        Debug.Log($"Populating endGameContent with {sortedStats.Count} entries");

        // Position entries manually
        for (int i = 0; i < sortedStats.Count; i++)
        {
            PlayerStats stats = sortedStats[i];
            GameObject entry = Instantiate(playerScorePrefab, endGameContent);
            PlayerScoreEntry scoreEntry = entry.GetComponent<PlayerScoreEntry>();
            if (scoreEntry != null)
            {
                scoreEntry.Initialize(
                    stats.player.NickName,
                    stats.kills.ToString(),
                    stats.deaths.ToString(),
                    stats.score.ToString()
                );

                // Set position
                RectTransform rect = entry.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    float yPos = scoreEntryStartPos.y - (i * (scoreEntryHeight + scoreEntrySpacing));
                    rect.anchoredPosition = new Vector2(scoreEntryStartPos.x, yPos);
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, scoreEntryHeight);
                    Debug.Log($"Positioned endGame entry for {stats.player.NickName} at y={yPos}");
                }
                else
                {
                    Debug.LogWarning($"RectTransform missing on PlayerScoreEntry for {stats.player.NickName}");
                }
            }
            else
            {
                Debug.LogWarning($"PlayerScoreEntry component missing on instantiated prefab for {stats.player.NickName}");
            }
        }
        StartCoroutine(EndGameTimer());
    }

    private IEnumerator EndGameTimer()
    {
        Debug.Log("Starting EndGameTimer coroutine");
        yield return new WaitForSecondsRealtime(10f);
        Debug.Log("EndGameTimer finished, loading Lobby scene");
        if (PhotonNetwork.IsMasterClient)
        {
            Cursor.lockState = CursorLockMode.None;
            PhotonNetwork.LoadLevel("Lobby");
        }
        else
        {
            Debug.Log("Not master client, waiting for scene sync");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!playerStats.ContainsKey(newPlayer.ActorNumber))
        {
            playerStats[newPlayer.ActorNumber] = new PlayerStats(newPlayer);
            Debug.Log($"Player {newPlayer.NickName} joined, updating scoreboard");
            UpdateScoreboard();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (playerStats.ContainsKey(otherPlayer.ActorNumber))
        {
            playerStats.Remove(otherPlayer.ActorNumber);
            Debug.Log($"Player {otherPlayer.NickName} left, updating scoreboard");
            UpdateScoreboard();
        }
    }

    private class PlayerStats
    {
        public Player player;
        public int kills;
        public int deaths;
        public int score;

        public PlayerStats(Player player)
        {
            this.player = player;
            kills = 0;
            deaths = 0;
            score = 0;
        }
    }
}