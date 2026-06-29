using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField, Tooltip("")]
    private TextMeshProUGUI _scoreText;

    [SerializeField, Tooltip("")]
    private string _format = "Score: {0}";

    private void Awake()
    {
        if (_scoreText == null)
        {
            _scoreText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        GameState.ScoreChanged += HandleScoreChanged;

        HandleScoreChanged(GameState.Score);
    }

    private void OnDisable()
    {
        GameState.ScoreChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged(int newScore)
    {
        if (_scoreText != null)
        {
            _scoreText.text = string.Format(_format, newScore);
        }
    }
}