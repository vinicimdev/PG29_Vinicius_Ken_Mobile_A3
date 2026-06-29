using UnityEngine;
using Unity.Services.LevelPlay;
// using com.unity.services.levelplay;

public class AdManager : MonoBehaviour
{
    [Header("LevelPlay Credentials")]
    [SerializeField, Tooltip("")]
    private string _appKey = "26ed35d65";

    [SerializeField, Tooltip("")]
    private string _interstitialAdUnitId = "f3ber0ugyepgtjih";

    [Header("Behavior")]
    [SerializeField, Tooltip("")]
    private bool _autoShowOnGameOver = true;

    [SerializeField, Tooltip("")]
    private bool _editorLogOnly = true;

    [SerializeField, Tooltip("")]
    private float _retryLoadDelay = 5f;

    public static AdManager Instance { get; private set; }

    private LevelPlayInterstitialAd _interstitial;
    private bool _isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_autoShowOnGameOver == true)
        {
            GameState.GameOver += HandleGameOver;
        }

        if (Application.isEditor == true && _editorLogOnly == true)
        {
            Debug.Log("[AdManager] Editor log-only mode. LevelPlay calls skipped.");
            _isInitialized = true;
            return;
        }

        InitializeLevelPlay();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            GameState.GameOver -= HandleGameOver;
            Instance = null;
        }
    }

    private void InitializeLevelPlay()
    {
        LevelPlay.OnInitSuccess += OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed += OnLevelPlayInitFailed;
        LevelPlay.Init(_appKey);
    }

    private void OnLevelPlayInitSuccess(LevelPlayConfiguration configuration)
    {
        Debug.Log("[AdManager] LevelPlay initialized.");
        _isInitialized = true;
        CreateAndLoadInterstitial();
    }

    private void OnLevelPlayInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdManager] LevelPlay init failed: {error.ErrorMessage}");
    }

    private void CreateAndLoadInterstitial()
    {
        _interstitial = new LevelPlayInterstitialAd(_interstitialAdUnitId);

        _interstitial.OnAdLoaded += info =>
        {
            Debug.Log("[AdManager] Interstitial ready.");
        };

        _interstitial.OnAdLoadFailed += error =>
        {
            Debug.LogWarning($"[AdManager] Interstitial load failed: {error.ErrorMessage}. Retrying in {_retryLoadDelay}s.");
            Invoke(nameof(RetryLoad), _retryLoadDelay);
        };

        _interstitial.OnAdDisplayed += info =>
        {
            Debug.Log("[AdManager] Interstitial displayed.");
        };

        _interstitial.OnAdDisplayFailed += (error, info) =>
        {
            Debug.LogWarning($"[AdManager] Interstitial display failed: {error}");
        };

        _interstitial.OnAdClosed += info =>
        {
            Debug.Log("[AdManager] Interstitial closed. Preloading next.");
            _interstitial.LoadAd();
        };

        _interstitial.LoadAd();
    }

    private void RetryLoad()
    {
        if (_interstitial != null)
        {
            _interstitial.LoadAd();
        }
    }

    public void ShowInterstitial()
    {
        if (Application.isEditor == true && _editorLogOnly == true)
        {
            Debug.Log("[AdManager] [EDITOR MOCK] Would show interstitial now. Build to Android to verify the real ad.");
            return;
        }

        if (_isInitialized == false)
        {
            Debug.LogWarning("[AdManager] Show requested before LevelPlay finished initializing.");
            return;
        }

        if (_interstitial != null && _interstitial.IsAdReady() == true)
        {
            _interstitial.ShowAd();
        }
        else
        {
            Debug.LogWarning("[AdManager] Interstitial not ready yet, skipping show.");
        }
    }

    private void HandleGameOver()
    {
        ShowInterstitial();
    }
}