using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { NotStarted, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] GameObject startUI;     // assign in Inspector
    [SerializeField] GameObject gameOverUI;  // assign in Inspector

    [SerializeField] PlayerRunner playerRunner;         // auto-found if left empty
    [SerializeField] ObstacleSpawner obstacleSpawner;   // auto-found if left empty

    public GameState State { get; private set; } = GameState.NotStarted;
    public bool IsPlaying => State == GameState.Playing;
    public bool IsGameOver => State == GameState.GameOver;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Auto-wire if not set
        playerRunner    ??= FindAnyObjectByType<PlayerRunner>(FindObjectsInactive.Exclude);
        obstacleSpawner ??= FindAnyObjectByType<ObstacleSpawner>(FindObjectsInactive.Exclude);

        Time.timeScale = 1f;
        if (startUI) startUI.SetActive(true);
        if (gameOverUI) gameOverUI.SetActive(false);
    }

    public void StartGame()
    {
        if (State != GameState.NotStarted) return;
        ResetScores();
        State = GameState.Playing;
        if (startUI) startUI.SetActive(false);
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        State = GameState.GameOver;

        if (playerRunner)    playerRunner.enabled = false;
        if (obstacleSpawner) obstacleSpawner.enabled = false;

        if (gameOverUI) gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (State == GameState.NotStarted &&
            (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            StartGame();

        if (IsGameOver && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    
    public int CoinScore { get; private set; }
    public void AddCoins(int amount)
    {
        if (!IsPlaying) return;
        CoinScore += amount;
    }
    public void ResetScores()
    {
        CoinScore = 0;
    }
}