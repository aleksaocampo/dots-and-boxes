using UnityEngine;
using TMPro;
using Unity.Netcode;

public class UIManager : NetworkBehaviour
{
    public TextMeshProUGUI currentTurnText;
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("UIManager: GameManager.Instance is null!");
            return;
        }

        // Subscribe to NetworkVariable changes
        GameManager.Instance.CurrentPlayer.OnValueChanged += UpdateTurnUI;
        GameManager.Instance.Player1Score.OnValueChanged += UpdateScoresUI;
        GameManager.Instance.Player2Score.OnValueChanged += UpdateScoresUI;

        // Initial UI update
        UpdateTurnUI(0, GameManager.Instance.CurrentPlayer.Value);
        UpdateScoresUI(0, GameManager.Instance.Player1Score.Value);
        UpdateScoresUI(0, GameManager.Instance.Player2Score.Value);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentPlayer.OnValueChanged -= UpdateTurnUI;
            GameManager.Instance.Player1Score.OnValueChanged -= UpdateScoresUI;
            GameManager.Instance.Player2Score.OnValueChanged -= UpdateScoresUI;
        }
    }

    private void UpdateTurnUI(int prev, int current)
    {
        if (currentTurnText == null) return;

        currentTurnText.text = $"Current Turn: Player {current}";

        // Debug.Log($"[UI] Turn updated: Player {current}");
    }

    private void UpdateScoresUI(int prev, int current)
    {
        if (player1ScoreText == null || player2ScoreText == null) return;

        player1ScoreText.text = $"Player 1: {GameManager.Instance.Player1Score.Value}";
        player2ScoreText.text = $"Player 2: {GameManager.Instance.Player2Score.Value}";

        // Debug.Log($"[UI] Scores updated: P1={GameManager.Instance.Player1Score.Value}, P2={GameManager.Instance.Player2Score.Value}");
    }
}