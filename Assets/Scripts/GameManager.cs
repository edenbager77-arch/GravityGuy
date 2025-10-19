using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState { Menu, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }



    [Header("UI (MainMenu)")]
    public TMP_Text highScoreText;
public GameObject helpPanel; 

    [Header("UI (Game)")]
    public TMP_Text scoreText;
    public TMP_Text topRightHelpText;   // “Press SPACE to Flip”
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;

AudioSource musicSrc;[Header("Audio")]
public AudioClip menuMusic;
public AudioClip gameOverMusic;

[Range(0f, 1f)] public float musicVolume = 0.4f;

bool gameOverShown = false;


    [Header("Run & Score")]
    public float scorePerSecond = 10f;
    public float startSpeed = 4.5f;
    public float maxSpeed = 14f;
    public float accelPerSecond = 0.25f;

    public GameState State { get; private set; } = GameState.Menu;
    public int Score { get; private set; }
    public int HighScore { get; private set; }

    float _scoreAccu;
    float _currentSpeed;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
musicSrc = gameObject.AddComponent<AudioSource>();
musicSrc.loop = true;
musicSrc.playOnAwake = false;
musicSrc.volume = musicVolume;
musicSrc.ignoreListenerPause = true;

        HighScore = PlayerPrefs.GetInt("HIGH_SCORE", 0);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene s, LoadSceneMode m)
{
    Time.timeScale = 1f;         // always unpause
    RebindSceneUI();             // (re)grab scene UI refs

    if (s.name == "MainMenu")
    {
        State = GameState.Menu;
        gameOverShown = false;                             // reset here

        if (helpPanel) helpPanel.SetActive(false);          // start hidden
        if (highScoreText) highScoreText.text = $"High Score: {HighScore}";
musicSrc.loop = true;

 PlayMusic(menuMusic);
    }
    else if (s.name == "Game")
    {
        State = GameState.Playing;
        gameOverShown = false;                             // reset here

        Score = 0; _scoreAccu = 0f; _currentSpeed = startSpeed;

        if (gameOverPanel) gameOverPanel.SetActive(false);  // hidden until death
        if (topRightHelpText) topRightHelpText.gameObject.SetActive(true);

        UpdateScoreUI();  // shows "Score: 0" and keeps high score if you display it in-game
    }


}


    void Update()
    {
        if (State != GameState.Playing) return;

        // speed ramp
        _currentSpeed = Mathf.Min(_currentSpeed + accelPerSecond * Time.deltaTime, maxSpeed);

        // time-based scoring
        _scoreAccu += scorePerSecond * Time.deltaTime;
        if (_scoreAccu >= 1f)
        {
            int add = Mathf.FloorToInt(_scoreAccu);
            _scoreAccu -= add;
            Score += add;
            UpdateScoreUI();
        }


    }

    public float CurrentSpeed => _currentSpeed;

    public void AddScore(int amount = 1)
    {
        Score += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText)     scoreText.text     = $"Score: {Score}";
        if (highScoreText) highScoreText.text = $"High Score: {HighScore}";
    }



  public void GameOver()
{
    if (State != GameState.Playing || gameOverShown) return;
    gameOverShown = true;

    State = GameState.GameOver;

    if (Score > HighScore) {
        HighScore = Score;
        PlayerPrefs.SetInt("HIGH_SCORE", HighScore);
        PlayerPrefs.Save();
    }

    if (finalScoreText) finalScoreText.text = $"Score: {Score}";
    if (gameOverPanel)  gameOverPanel.SetActive(true);

    Time.timeScale = 0f; // pause gameplay
if (gameOverMusic)
{
    musicSrc.Stop();                     // stop the menu music
    musicSrc.loop = false;               // make sure it doesn’t loop
    musicSrc.clip = gameOverMusic;
    musicSrc.Play();                     // play it once
}
}

    /// UI buttons
public void BTN_Play()  { Time.timeScale = 1f; SceneManager.LoadScene("Game"); }
public void BTN_Retry()
{
    Time.timeScale = 1f;
    gameOverShown = false;
    SceneManager.LoadScene("Game");
}
public void BTN_Menu()
{
    Time.timeScale = 1f;
    gameOverShown = false;
    SceneManager.LoadScene("MainMenu");
}	
public void BTN_Help()        { if (helpPanel) helpPanel.SetActive(true); }   // open
public void BTN_HelpClose()   { if (helpPanel) helpPanel.SetActive(false); }  // close

public void BTN_Exit()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false; // quits Play Mode in Editor
#else
    Application.Quit();
#endif
}


    public void HideHelp()
    {
        if (topRightHelpText) topRightHelpText.gameObject.SetActive(false);
    }

    void SaveHighScore()
    {
        PlayerPrefs.SetInt("HIGH_SCORE", HighScore);
        PlayerPrefs.Save();
    }

    void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt("HIGH_SCORE", 0);
    }

  void RebindSceneUI()
{
    string scene = SceneManager.GetActiveScene().name;

    // --- MAIN MENU ---
    if (scene == "MainMenu")
    {
        // Find MainMenu elements
        if (!highScoreText)
            highScoreText = GameObject.Find("HighScoreTXT")?.GetComponent<TMP_Text>();

        if (!helpPanel)
        {
            // HelpPanel is inside MenuCanvas
            var menuCanvas = GameObject.Find("MenuCanvas");
            if (menuCanvas)
                helpPanel = menuCanvas.transform.Find("HelpPanel")?.gameObject;
        }

        return;
    }

    // --- GAME SCENE ---
    if (scene == "Game")
    {
        // HUD texts
        if (!scoreText)
            scoreText = GameObject.Find("ScoreText")?.GetComponent<TMP_Text>();

        if (!topRightHelpText)
            topRightHelpText = GameObject.Find("HelpText")?.GetComponent<TMP_Text>();

        // Look under GameCanvas
        var canvasGO = GameObject.Find("GameCanvas");
        if (canvasGO)
        {
            if (!gameOverPanel)
                gameOverPanel = canvasGO.transform.Find("GameOverPanel")?.gameObject;

            if (!finalScoreText && gameOverPanel)
                finalScoreText = gameOverPanel.transform.Find("FinalScoreText")?.GetComponent<TMP_Text>();
        }
    }
}
void PlayMusic(AudioClip clip)
{
    if (!clip) return;
    if (musicSrc.clip == clip && musicSrc.isPlaying) return; // already playing
    musicSrc.clip = clip;
    musicSrc.Play();
}





}