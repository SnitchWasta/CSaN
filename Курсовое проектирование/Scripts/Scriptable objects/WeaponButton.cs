using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponButton : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image background;
    [SerializeField] private Button button;

    private void Awake()
    {
        if (background == null)
            background = GetComponent<Image>();
    }

    public void Initialize(string name, Sprite icon, System.Action onClick)
    {
        if (nameText != null)
            nameText.text = name;

        if (iconImage != null)
            iconImage.sprite = icon;

        if (button != null)
            button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
    }
}