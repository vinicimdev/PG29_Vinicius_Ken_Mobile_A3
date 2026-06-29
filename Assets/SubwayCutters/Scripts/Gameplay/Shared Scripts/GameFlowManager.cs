using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private Canvas _startScreen;
    [SerializeField] private Canvas _endScreen;
    [SerializeField] private Canvas _hudLayer;
    [SerializeField] private Canvas _trailSelection;

    [Header("Gameplay")]
    [SerializeField] private Timer _timer;
    [SerializeField] private Spawner _spawner;

    [Header("End Screen Display")]
    [SerializeField, Tooltip("")]
    private TextMeshProUGUI _endScoreText;

    private void Awake()
    {
        GameState.Reset();
        GameState.GameOver += HandleGameOver;
        ShowStart();
    }

    private void OnDestroy()
    {
        GameState.GameOver -= HandleGameOver;
    }

    public void StartGame()
    {
        GameState.Reset();
        SetScreens(start: false, end: false, hud: true, trail: false);
        if (_timer != null)
        {
            _timer.StartTimer();
        }

        if (_spawner != null)
        {
            _spawner.StartSpawning();
        }
    }

    public void OnTimerEnd()
    {
        GameState.TriggerGameOver();
    }

    public void ReturnToStart()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void OpenTrailMenu()
    {
        if (_trailSelection != null)
        {
            _trailSelection.gameObject.SetActive(true);
        }
    }

    public void CloseTrailMenu()
    {
        if (_trailSelection != null)
        {
            _trailSelection.gameObject.SetActive(false);
        }
    }

    private void HandleGameOver()
    {
        ShowEnd();
    }

    private void ShowStart()
    {
        SetScreens(start: true, end: false, hud: false, trail: false);
    }

    private void ShowEnd()
    {
        SetScreens(start: false, end: true, hud: false, trail: false);

        if (_endScoreText != null)
        {
            _endScoreText.text = $"Score: {GameState.Score}";
        }
    }

    private void SetScreens(bool start, bool end, bool hud, bool trail)
    {
        if (_startScreen != null)
        {
            _startScreen.gameObject.SetActive(start);
        }

        if (_endScreen != null)
        {
            _endScreen.gameObject.SetActive(end);
        }

        if (_hudLayer != null)
        {
            _hudLayer.gameObject.SetActive(hud);
        }

        if (_trailSelection != null)
        {
            _trailSelection.gameObject.SetActive(trail);
        }
    }
}
