using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central state machine for SHIFT.
/// Tracks game states and exposes events for UI/audio to react to.
/// Singleton — access via GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ──────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─── State ───────────────────────────────────────────────────────────────────
    public enum GameState { Idle, Playing, Paused, Won }

    [Header("Current State (read-only in Inspector)")]
    [SerializeField] private GameState _currentState = GameState.Idle;
    public GameState CurrentState => _currentState;

    // ─── Events ──────────────────────────────────────────────────────────────────
    [Header("Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnGameWon;
    public UnityEvent OnGamePaused;
    public UnityEvent OnGameResumed;
    public UnityEvent OnGameReset;

    // ─── Timer ───────────────────────────────────────────────────────────────────
    private float _elapsedTime;
    public float ElapsedTime => _elapsedTime;
    private bool _timerRunning;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

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

    private void Update()
    {
        if (_timerRunning)
            _elapsedTime += Time.deltaTime;
    }

    // ─── State Transitions ───────────────────────────────────────────────────────

    public void StartGame()
    {
        if (_currentState == GameState.Playing) return;
        SetState(GameState.Playing);
        _elapsedTime = 0f;
        _timerRunning = true;
        OnGameStart?.Invoke();
        Debug.Log("[GameManager] Game Started");
    }

    public void WinGame()
    {
        if (_currentState != GameState.Playing) return;
        SetState(GameState.Won);
        _timerRunning = false;
        OnGameWon?.Invoke();
        Debug.Log($"[GameManager] Win! Time: {ElapsedTime:F2}s");
    }

    public void PauseGame()
    {
        if (_currentState != GameState.Playing) return;
        SetState(GameState.Paused);
        _timerRunning = false;
        Time.timeScale = 0f;
        OnGamePaused?.Invoke();
    }

    public void ResumeGame()
    {
        if (_currentState != GameState.Paused) return;
        SetState(GameState.Playing);
        _timerRunning = true;
        Time.timeScale = 1f;
        OnGameResumed?.Invoke();
    }

    public void ResetGame()
    {
        SetState(GameState.Idle);
        _elapsedTime = 0f;
        _timerRunning = false;
        Time.timeScale = 1f;
        OnGameReset?.Invoke();
        Debug.Log("[GameManager] Game Reset");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private void SetState(GameState newState)
    {
        _currentState = newState;
    }

    /// <summary>Returns the elapsed time formatted as MM:SS</summary>
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(ElapsedTime / 60f);
        int seconds = Mathf.FloorToInt(ElapsedTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
