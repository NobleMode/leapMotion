using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MapGenerator _mapGenerator;
    [SerializeField] private LeapController _leapController;
    [SerializeField] private Vector2Int _mapSize = new Vector2Int(7, 7);
    [SerializeField] private int _ballCount = 1;

    [Header("UI - Map Customization")]
    [SerializeField] private TMP_InputField inputMapX;
    [SerializeField] private TMP_InputField inputMapY;
    [SerializeField] private TMP_InputField inputBallCount;

    [Header("UI - Timer")]
    [SerializeField] private TMP_Text timerTextGame;
    [SerializeField] private TMP_Text timerTextMenuBest;
    [SerializeField] private TMP_Text timerTextVictoryCurrent;
    [SerializeField] private TMP_Text timerTextVictoryBest;

    [Header("Screen")]
    [SerializeField] private OneButtonScreen mainScreen;
    [SerializeField] private TwoButtonScreen pauseScreen;
    [SerializeField] private TwoButtonScreen winScreen;

    [Header("Sound")] 
    [SerializeField] private AudioClip start;
    [SerializeField] private AudioClip continueAudio;
    [SerializeField] private AudioClip quit;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip handLost;
    [SerializeField] private AudioClip handDetectedClip;

    internal CurrentGameState _currentGameState {get; set;} = CurrentGameState.MAIN;
    private float _gameTimer = 0f;
    private const string BestTimeKey = "BestTime_7x7";

    [SerializeField] private float requiredHoldTime = 1.5f;
    private float _currentHoldTime = 0f;
    private string _lastGesture = "";
    
    private AudioSource _audioSource;
    private bool _wasHandDetected = false;

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Initialize Inputs
        if (inputMapX)
        {
            inputMapX.text = _mapSize.x.ToString();
            inputMapX.onEndEdit.AddListener((value) => {;
                if (int.TryParse(value, out int x))
                {
                    _mapSize.x = x > 15 ? 15 : x;
                    inputMapX.text = _mapSize.x.ToString();
                }
            });
        }

        if (inputMapY)
        {
            inputMapY.text = _mapSize.y.ToString();
            inputMapY.onEndEdit.AddListener((value) => {
                if (int.TryParse(value, out int y))
                {
                    _mapSize.y = y > 15 ? 15 : y;
                    inputMapY.text = _mapSize.y.ToString();
                }
            });
        }

        if (inputBallCount)
        {
            inputBallCount.text = _ballCount.ToString();
            inputBallCount.onEndEdit.AddListener((value) => {
                if (int.TryParse(value, out int balls))
                {
                    _ballCount = balls > 4 ? 4 : balls;
                    inputBallCount.text = _ballCount.ToString();
                }
            });
        }

        // Load Best Time
        if (timerTextMenuBest)
        {
             float bestTime = PlayerPrefs.GetFloat(BestTimeKey, -1f);
             if (bestTime >= 0)
             {
                 timerTextMenuBest.text = "Best Time: " + FormatTime(bestTime) + "\n(Must be 7 by 7)";
             }
             else
             {
                 timerTextMenuBest.text = "Best Time: --.--.--\n(Must be 7 by 7)";
             }
        }

        // Initialize Screens
        if (mainScreen != null && mainScreen.screen != null) {
            mainScreen.screen.SetActive(true);
            mainScreen.mainBtn.onClick.AddListener(StartGame);
        }
        if (pauseScreen != null && pauseScreen.screen != null) {
            pauseScreen.screen.SetActive(false);
            pauseScreen.mainBtn.onClick.AddListener(ContinueGame);
            pauseScreen.altBtn.onClick.AddListener(QuitGame);
        }
        if (winScreen != null && winScreen.screen != null) {
            winScreen.screen.SetActive(false);
            winScreen.mainBtn.onClick.AddListener(ReloadLevel);
            winScreen.altBtn.onClick.AddListener(QuitGame);
        }
        
        _currentGameState = CurrentGameState.MAIN;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_leapController) return;

        string gesture = _leapController.CurrentGesture;
        bool handDetected = _leapController.IsHandDetected;

        // Handle Hand Detection Sounds
        if (handDetected && !_wasHandDetected)
        {
            PlaySound(handDetectedClip);
        }
        else if (!handDetected && _wasHandDetected)
        {
            PlaySound(handLost);
        }
        _wasHandDetected = handDetected;

        // Timer Logic
        if (_currentGameState == CurrentGameState.GAME)
        {
            _gameTimer += Time.deltaTime;
            if (timerTextGame) timerTextGame.text = FormatTime(_gameTimer);
        }
        else if (_currentGameState == CurrentGameState.MAIN)
        {
             if (timerTextGame) timerTextGame.text = "";
        }

        // Reset timer if gesture changes
        if (gesture != _lastGesture)
        {
            _currentHoldTime = 0f;
            _lastGesture = gesture;
        }

        switch (_currentGameState)
        {
            case CurrentGameState.MAIN:
                HandleMainState(gesture);
                break;
            case CurrentGameState.GAME:
                HandleGameState(handDetected);
                break;
            case CurrentGameState.PAUSE:
                HandlePauseState(gesture, handDetected);
                break;
            case CurrentGameState.FINISH:
                HandleFinishState(gesture);
                break;
        }
    }

    private void HandleMainState(string gesture)
    {
        if (gesture == "StartGame")
        {
            ProcessHold(mainScreen.positiveActionBar, StartGame);
        }
        else
        {
            ResetHold(mainScreen.positiveActionBar);
        }
    }

    private void HandleGameState(bool handDetected)
    {
        if (!handDetected)
        {
            PauseGame();
        }
    }

    private void HandlePauseState(string gesture, bool handDetected)
    {
        // If hand is detected and making V-sign (VictoryRetry) -> Continue
        if (gesture == "VictoryRetry")
        {
            ProcessHold(pauseScreen.positiveActionBar, ContinueGame);
            ResetHold(pauseScreen.negativeActionBar);
        }
        // If hand is detected and making Exit gesture -> Quit
        else if (gesture == "Exit")
        {
            ProcessHold(pauseScreen.negativeActionBar, QuitGame);
            ResetHold(pauseScreen.positiveActionBar);
        }
        else
        {
            ResetHold(pauseScreen.positiveActionBar);
            ResetHold(pauseScreen.negativeActionBar);
        }
    }

    private void HandleFinishState(string gesture)
    {
        if (gesture == "VictoryRetry")
        {
            ProcessHold(winScreen.positiveActionBar, ReloadLevel);
            ResetHold(winScreen.negativeActionBar);
        }
        else if (gesture == "Exit")
        {
            ProcessHold(winScreen.negativeActionBar, QuitGame);
            ResetHold(winScreen.positiveActionBar);
        }
        else
        {
            ResetHold(winScreen.positiveActionBar);
            ResetHold(winScreen.negativeActionBar);
        }
    }

    private void ProcessHold(SlicedFilledImage bar, Action onComplete)
    {
        if (!bar) return;

        _currentHoldTime += Time.deltaTime;
        bar.fillAmount = _currentHoldTime / requiredHoldTime;

        if (_currentHoldTime >= requiredHoldTime)
        {
            _currentHoldTime = 0f;
            bar.fillAmount = 0f;
            onComplete?.Invoke();
        }
    }

    private void ResetHold(SlicedFilledImage bar)
    {
        if (bar) bar.fillAmount = 0f;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip && _audioSource)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    private string FormatTime(float time)
    {
        int minutes = (int)time / 60;
        int seconds = (int)time % 60;
        int milliseconds = (int)(time * 100) % 100;
        return string.Format("{0:00}.{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    // --- Public Actions ---

    public void StartGame()
    {
        // Parse Map Size
        if (inputMapX && int.TryParse(inputMapX.text, out int x)) _mapSize.x = x > 15 ? 15 : x;
        if (inputMapY && int.TryParse(inputMapY.text, out int y)) _mapSize.y = y > 15 ? 15 : y;
        if (inputBallCount && int.TryParse(inputBallCount.text, out int balls)) _ballCount = balls > 4 ? 4 : balls;

        _gameTimer = 0f;
        PlaySound(start);
        _currentGameState = CurrentGameState.GAME;
        mainScreen.screen.SetActive(false);
        pauseScreen.screen.SetActive(false);
        winScreen.screen.SetActive(false);
        _mapGenerator.CreateMap(_mapSize, _ballCount);
    }

    public void ContinueGame()
    {
        PlaySound(continueAudio);
        _currentGameState = CurrentGameState.GAME;
        pauseScreen.screen.SetActive(false);
        winScreen.screen.SetActive(false);
        mainScreen.screen.SetActive(false);
    }

    public void ReloadLevel()
    {
        _gameTimer = 0f;
        PlaySound(start);
        _mapGenerator.CreateMap(_mapSize, _ballCount); // Regenerate map
        _currentGameState = CurrentGameState.GAME;
        winScreen.screen.SetActive(false);
        mainScreen.screen.SetActive(false);
        pauseScreen.screen.SetActive(false);
    }

    public void QuitGame()
    {
        PlaySound(quit);
        _currentGameState = CurrentGameState.MAIN;
        mainScreen.screen.SetActive(true);
        pauseScreen.screen.SetActive(false);
        winScreen.screen.SetActive(false);
        _mapGenerator.ClearMap(); // Clear map on exit
    }

    public void PauseGame()
    {
        _currentGameState = CurrentGameState.PAUSE;
        pauseScreen.screen.SetActive(true);
        mainScreen.screen.SetActive(false);
        winScreen.screen.SetActive(false);
    }

    public void OnLevelComplete()
    {
        if (_currentGameState != CurrentGameState.FINISH)
        {
            PlaySound(win);
            _currentGameState = CurrentGameState.FINISH;

            // Timer Logic
            if (timerTextVictoryCurrent) timerTextVictoryCurrent.text = "Time Elapsed: " + FormatTime(_gameTimer);

            if (_mapSize is { x: 7, y: 7 } && _ballCount == 1)
            {
                float bestTime = PlayerPrefs.GetFloat(BestTimeKey, float.MaxValue);
                if (_gameTimer < bestTime)
                {
                    PlayerPrefs.SetFloat(BestTimeKey, _gameTimer);
                    PlayerPrefs.Save();
                    if (timerTextVictoryBest) timerTextVictoryBest.text = "NEW BEST TIME!!!";
                    
                    // Update Menu Best Time immediately
                    if (timerTextMenuBest) timerTextMenuBest.text = "Best Time: " + FormatTime(_gameTimer) + "\n(Must be 7 x 7)";
                }
                else
                {
                    if (timerTextVictoryBest) timerTextVictoryBest.text = "Best Time: " + FormatTime(bestTime);
                }
            }
            else
            {
                if (timerTextVictoryBest) timerTextVictoryBest.text = "";
            }

            winScreen.screen.SetActive(true);
            mainScreen.screen.SetActive(false);
            pauseScreen.screen.SetActive(false);
        }
    }
}

[Serializable]
public class Screen {
    public GameObject screen;
}

[Serializable]
public class TwoButtonScreen : Screen {
    public Button mainBtn;
    public SlicedFilledImage positiveActionBar;
    public Button altBtn;
    public SlicedFilledImage negativeActionBar;
}

[Serializable]
public class OneButtonScreen : Screen {
    public Button mainBtn;
    public SlicedFilledImage positiveActionBar;
}

public enum CurrentGameState {
    MAIN,
    GAME,
    PAUSE,
    FINISH
}