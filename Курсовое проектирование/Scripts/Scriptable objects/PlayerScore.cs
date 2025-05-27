using TMPro;
using UnityEngine;

public class PlayerScoreEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text scoreText;

    public void Initialize(string name, string kills, string deaths, string score)
    {
        nameText.text = name;
        killsText.text = kills;
        deathsText.text = deaths;
        scoreText.text = score;
    }
}