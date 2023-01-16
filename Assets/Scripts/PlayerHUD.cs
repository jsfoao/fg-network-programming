using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField]
    public PlayerData PlayerData;
    private TextMeshProUGUI _scoreText;

    [SerializeField]
    void Start()
    {
        _scoreText = transform.Find("Score").gameObject.GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        _scoreText.text = PlayerData.Health.Value.ToString();
    }
}
