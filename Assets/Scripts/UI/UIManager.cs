using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalFirefight.Core;
using GlobalFirefight.Data;
using GlobalFirefight.Drones;
using GlobalFirefight.Fire;
using GlobalFirefight.API;
using GlobalFirefight.Systems;

namespace GlobalFirefight.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private RTSInterface rtsInterface;
        [SerializeField] private TacticalHUD tacticalHUD;
        
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadingPanel;
        
        [Header("Loading Screen")]
        [SerializeField] private Slider loadingProgressBar;
        [SerializeField] private TextMeshProUGUI loadingStatusText;
        [SerializeField] private Image loadingBackground;
        [SerializeField] private GameObject[] loadingTips;
        [SerializeField] private float tipRotationInterval = 3f;
        
        [Header("Settings Configuration")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Dropdown graphicsQualityDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Slider uiScaleSlider;
        
        [Header("Performance Monitoring")]
        [SerializeField] private GameObject performancePanel;
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI memoryText;
        [SerializeField] private TextMeshProUGUI droneCountText;
        [SerializeField] private bool showPerformancePanel = false;
        [SerializeField] private float performanceUpdateInterval = 1f;
        
        [Header("Notification System")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private Transform notificationParent;
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private int maxNotifications = 5;
        [SerializeField] private float notificationDuration = 5f;
        
        [Header("Confirmation Dialogs")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI confirmationTitleText;
        [SerializeField] private TextMeshProUGUI confirmationMessageText;
        [SerializeField] private Button confirmationYesButton;
        [SerializeField] private Button confirmationNoButton;
        
        [Header("Game State UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI gameStatsText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        
        // Private fields
        private GameManager gameManager;
        private CameraController cameraController;
        private AudioSource audioSource;
        private UIState currentUIState = UIState.MainMenu;
        private List<GameObject> activeNotifications = new List<GameObject>();
        private bool isInitialized = false;
        private float lastPerformanceUpdate = 0f;
        private int currentTipIndex = 0;
        private Coroutine tipRotationCoroutine;
        private System.Action currentConfirmationCallback;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public UIState CurrentUIState => currentUIState;
        public RTSInterface RTSInterface => rtsInterface;
        public TacticalHUD TacticalHUD => tacticalHUD;
        
        #region Initialization
        
        public void Initialize()
        {
            UnityEngine.Debug.Log("Initializing UI Manager...");
            
            // Get required components
            gameManager = FindFirstObjectByType<GameManager>();
            cameraController = FindFirstObjectByType<CameraController>();
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            // Find UI components if not assigned
            FindUIComponents();
            
            // Setup event listeners
            SetupEventListeners();
            
            // Initialize sub-systems
            InitializeRTSInterface();
            InitializeTacticalHUD();
            InitializeSettings();
            
            // Setup initial UI state
            SetUIState(UIState.MainMenu);
            
            // Subscribe to game events
            SubscribeToGameEvents();
            
            isInitialized = true;
            UnityEngine.Debug.Log("UI Manager initialized successfully");
        }
        
        private void FindUIComponents()
        {
            // Find RTS Interface
            if (rtsInterface == null)
                rtsInterface = FindFirstObjectByType<RTSInterface>();
            
            // Find Tactical HUD
            if (tacticalHUD == null)
                tacticalHUD = FindFirstObjectByType<TacticalHUD>();
            
            // Performance panel toggle based on build
            if (performancePanel != null)
            {
                #if DEVELOPMENT_BUILD || UNITY_EDITOR
                showPerformancePanel = true;
                #else
                showPerformancePanel = false;
                #endif
                performancePanel.SetActive(showPerformancePanel);
            }
        }
        
        private void SetupEventListeners()
        {
            // Main menu buttons
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            
            // Settings controls
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            
            if (graphicsQualityDropdown != null)
                graphicsQualityDropdown.onValueChanged.AddListener(SetGraphicsQuality);
            
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(SetResolution);
            
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            
            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(SetVSync);
            
            if (uiScaleSlider != null)
                uiScaleSlider.onValueChanged.AddListener(SetUIScale);
            
            // Confirmation dialog buttons
            if (confirmationYesButton != null)
                confirmationYesButton.onClick.AddListener(ConfirmAction);
            
            if (confirmationNoButton != null)
                confirmationNoButton.onClick.AddListener(CancelConfirmation);
        }
        
        private void InitializeRTSInterface()
        {
            if (rtsInterface != null)
            {
                rtsInterface.Initialize();
            }
        }
        
        private void InitializeTacticalHUD()
        {
            if (tacticalHUD != null)
            {
                tacticalHUD.Initialize();
            }
        }
        
        private void InitializeSettings()
        {
            // Load saved settings
            LoadSettings();
            
            // Populate resolution dropdown
            PopulateResolutionDropdown();
            
            // Setup graphics quality dropdown
            if (graphicsQualityDropdown != null)
            {
                graphicsQualityDropdown.ClearOptions();
                var qualityOptions = new List<string>();
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    qualityOptions.Add(QualitySettings.names[i]);
                }
                graphicsQualityDropdown.AddOptions(qualityOptions);
                graphicsQualityDropdown.value = QualitySettings.GetQualityLevel();
            }
        }
        
        private void SubscribeToGameEvents()
        {
            if (gameManager != null)
            {
                // Subscribe to game state changes
                GameManager.OnGameStateChanged += OnGameStateChanged;
                // TODO: Fix OnScoreUpdated event signature
                // GameManager.OnScoreUpdated += OnScoreUpdated;
            }
        }
        
        #endregion
        
        #region UI State Management
        
        public void SetUIState(UIState newState)
        {
            if (currentUIState == newState)
                return;
            
            UnityEngine.Debug.Log($"UI State changing: {currentUIState} -> {newState}");
            
            // Hide all panels first
            HideAllPanels();
            
            // Show appropriate panels for new state
            switch (newState)
            {
                case UIState.MainMenu:
                    ShowMainMenu();
                    break;
                    
                case UIState.Loading:
                    ShowLoadingScreen();
                    break;
                    
                case UIState.RTS:
                    ShowRTSInterface();
                    break;
                    
                case UIState.Tactical:
                    ShowTacticalHUD();
                    break;
                    
                case UIState.Paused:
                    ShowPauseMenu();
                    break;
                    
                case UIState.Settings:
                    ShowSettings();
                    break;
                    
                case UIState.GameOver:
                    ShowGameOver();
                    break;
                    
                case UIState.Victory:
                    ShowVictory();
                    break;
            }
            
            currentUIState = newState;
        }
        
        private void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            
            if (rtsInterface != null) rtsInterface.gameObject.SetActive(false);
            if (tacticalHUD != null) tacticalHUD.gameObject.SetActive(false);
        }
        
        public void ShowMainMenu()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
            
            // Pause the game
            Time.timeScale = 0f;
        }
        
        public void ShowLoadingScreen()
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(true);
            
            // Start tip rotation
            if (tipRotationCoroutine != null)
                StopCoroutine(tipRotationCoroutine);
            tipRotationCoroutine = StartCoroutine(RotateLoadingTips());
        }
        
        public void ShowRTSInterface()
        {
            if (rtsInterface != null)
            {
                rtsInterface.gameObject.SetActive(true);
                rtsInterface.ShowRTSInterface();
            }
            
            // Resume game time
            Time.timeScale = 1f;
        }
        
        public void ShowTacticalHUD()
        {
            if (tacticalHUD != null)
            {
                tacticalHUD.gameObject.SetActive(true);
                tacticalHUD.ShowHUD();
            }
            
            if (rtsInterface != null)
            {
                rtsInterface.ShowTacticalInterface();
            }
            
            // Resume game time
            Time.timeScale = 1f;
        }
        
        public void ShowPauseMenu()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
            
            // Pause the game
            Time.timeScale = 0f;
        }
        
        private void ShowSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }
        
        private void ShowGameOver()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
            
            UpdateFinalGameStats();
            
            // Pause the game
            Time.timeScale = 0f;
        }
        
        private void ShowVictory()
        {
            if (victoryPanel != null)
                victoryPanel.SetActive(true);
            
            UpdateFinalGameStats();
            
            // Pause the game
            Time.timeScale = 0f;
        }
        
        public void ShowPostMissionScreen()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
            
            UpdateFinalGameStats();
            
            // Pause the game
            Time.timeScale = 0f;
        }
        
        #endregion
        
        #region Loading Screen
        
        public void UpdateLoadingProgress(float progress, string status)
        {
            if (loadingProgressBar != null)
                loadingProgressBar.value = progress;
            
            if (loadingStatusText != null)
                loadingStatusText.text = status;
        }
        
        private System.Collections.IEnumerator RotateLoadingTips()
        {
            while (currentUIState == UIState.Loading)
            {
                if (loadingTips != null && loadingTips.Length > 0)
                {
                    // Hide all tips
                    foreach (var tip in loadingTips)
                    {
                        if (tip != null)
                            tip.SetActive(false);
                    }
                    
                    // Show current tip
                    if (loadingTips[currentTipIndex] != null)
                        loadingTips[currentTipIndex].SetActive(true);
                    
                    currentTipIndex = (currentTipIndex + 1) % loadingTips.Length;
                }
                
                yield return new WaitForSecondsRealtime(tipRotationInterval);
            }
        }
        
        #endregion
        
        #region Settings Management
        
        private void LoadSettings()
        {
            // Audio settings
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            
            if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
            
            // Graphics settings
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            bool vsync = PlayerPrefs.GetInt("VSync", 1) == 1;
            float uiScale = PlayerPrefs.GetFloat("UIScale", 1f);
            
            if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
            if (vsyncToggle != null) vsyncToggle.isOn = vsync;
            if (uiScaleSlider != null) uiScaleSlider.value = uiScale;
            
            // Apply settings
            SetMasterVolume(masterVolume);
            SetSFXVolume(sfxVolume);
            SetMusicVolume(musicVolume);
            SetFullscreen(fullscreen);
            SetVSync(vsync);
            SetUIScale(uiScale);
        }
        
        private void SaveSettings()
        {
            if (masterVolumeSlider != null)
                PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            
            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
            
            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
            
            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            
            if (vsyncToggle != null)
                PlayerPrefs.SetInt("VSync", vsyncToggle.isOn ? 1 : 0);
            
            if (uiScaleSlider != null)
                PlayerPrefs.SetFloat("UIScale", uiScaleSlider.value);
            
            PlayerPrefs.Save();
        }
        
        private void SetMasterVolume(float volume)
        {
            AudioListener.volume = volume;
        }
        
        private void SetSFXVolume(float volume)
        {
            // Set SFX volume for all SFX audio sources
            // This would require a proper audio management system
        }
        
        private void SetMusicVolume(float volume)
        {
            // Set music volume for background music
            // This would require a proper audio management system
        }
        
        private void SetGraphicsQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
        }
        
        private void SetResolution(int resolutionIndex)
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutionIndex < resolutions.Length)
            {
                Resolution resolution = resolutions[resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            }
        }
        
        private void SetFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }
        
        private void SetVSync(bool vsync)
        {
            QualitySettings.vSyncCount = vsync ? 1 : 0;
        }
        
        private void SetUIScale(float scale)
        {
            // Apply UI scaling to all canvases
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scaler.scaleFactor = scale;
                }
            }
        }
        
        private void PopulateResolutionDropdown()
        {
            if (resolutionDropdown == null)
                return;
            
            resolutionDropdown.ClearOptions();
            var resolutionOptions = new List<string>();
            
            Resolution[] resolutions = Screen.resolutions;
            int currentResolutionIndex = 0;
            
            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = $"{resolutions[i].width} x {resolutions[i].height}";
                resolutionOptions.Add(option);
                
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }
            
            resolutionDropdown.AddOptions(resolutionOptions);
            resolutionDropdown.value = currentResolutionIndex;
        }
        
        #endregion
        
        #region Notification System
        
        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            if (notificationParent == null || notificationPrefab == null)
                return;
            
            // Remove oldest notification if at max
            while (activeNotifications.Count >= maxNotifications)
            {
                var oldNotification = activeNotifications[0];
                activeNotifications.RemoveAt(0);
                if (oldNotification != null)
                    Destroy(oldNotification);
            }
            
            // Create new notification
            GameObject notification = Instantiate(notificationPrefab, notificationParent);
            activeNotifications.Add(notification);
            
            // Setup notification content
            var notificationText = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (notificationText != null)
            {
                notificationText.text = message;
                notificationText.color = GetNotificationColor(type);
            }
            
            // Auto-remove notification
            StartCoroutine(RemoveNotificationAfterDelay(notification, notificationDuration));
        }
        
        private Color GetNotificationColor(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Info:
                    return Color.white;
                case NotificationType.Success:
                    return Color.green;
                case NotificationType.Warning:
                    return Color.yellow;
                case NotificationType.Error:
                    return Color.red;
                default:
                    return Color.white;
            }
        }
        
        private System.Collections.IEnumerator RemoveNotificationAfterDelay(GameObject notification, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            
            if (notification != null)
            {
                activeNotifications.Remove(notification);
                Destroy(notification);
            }
        }
        
        #endregion
        
        #region Confirmation Dialogs
        
        public void ShowConfirmationDialog(string title, string message, System.Action onConfirm)
        {
            if (confirmationDialog == null)
                return;
            
            confirmationDialog.SetActive(true);
            
            if (confirmationTitleText != null)
                confirmationTitleText.text = title;
            
            if (confirmationMessageText != null)
                confirmationMessageText.text = message;
            
            currentConfirmationCallback = onConfirm;
        }
        
        private void ConfirmAction()
        {
            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);
            
            currentConfirmationCallback?.Invoke();
            currentConfirmationCallback = null;
        }
        
        private void CancelConfirmation()
        {
            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);
            
            currentConfirmationCallback = null;
        }
        
        #endregion
        
        #region Game Events
        
        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Loading:
                    SetUIState(UIState.Loading);
                    break;
                    
                case GameState.RTSView:
                    SetUIState(UIState.RTS);
                    break;
                    
                case GameState.Paused:
                    SetUIState(UIState.Paused);
                    break;
                    
                case GameState.GameOver:
                    SetUIState(UIState.GameOver);
                    break;
                    
                case GameState.PostMission:
                    SetUIState(UIState.Victory);
                    break;
            }
        }
        
        private void OnScoreUpdated(float newScore)
        {
            // Update score displays in UI
            if (rtsInterface != null)
            {
                // RTS interface will handle its own score display
            }
        }
        
        #endregion
        
        #region Game Control
        
        public void StartNewGame()
        {
            if (gameManager != null)
            {
                // TODO: Implement StartNewGame method in GameManager
                gameManager.ChangeGameState(GameState.Loading);
            }
        }
        
        public void RestartGame()
        {
            ShowConfirmationDialog("Restart Game", "Are you sure you want to restart the current game?", () =>
            {
                if (gameManager != null)
                {
                    // TODO: Implement RestartGame method in GameManager
                    gameManager.ChangeGameState(GameState.Loading);
                }
            });
        }
        
        public void ReturnToMainMenu()
        {
            ShowConfirmationDialog("Return to Main Menu", "Are you sure you want to return to the main menu?", () =>
            {
                SetUIState(UIState.MainMenu);
                if (gameManager != null)
                {
                    gameManager.ChangeGameState(GameState.MainMenu);
                }
            });
        }
        
        public void PauseGame()
        {
            if (gameManager != null)
            {
                gameManager.ChangeGameState(GameState.Paused);
            }
        }
        
        public void ResumeGame()
        {
            if (gameManager != null)
            {
                gameManager.ChangeGameState(GameState.RTSView);
            }
        }
        
        public void QuitGame()
        {
            ShowConfirmationDialog("Quit Game", "Are you sure you want to quit?", () =>
            {
                SaveSettings();
                Application.Quit();
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            });
        }
        
        #endregion
        
        #region Performance Monitoring
        
        private void UpdatePerformanceDisplay()
        {
            if (!showPerformancePanel || performancePanel == null)
                return;
            
            // Update FPS
            if (fpsText != null)
            {
                float fps = 1f / Time.unscaledDeltaTime;
                fpsText.text = $"FPS: {fps:F0}";
                
                // Color based on performance
                if (fps >= 50)
                    fpsText.color = Color.green;
                else if (fps >= 30)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.red;
            }
            
            // Update memory usage
            if (memoryText != null)
            {
                long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
                memoryText.text = $"Memory: {memoryUsage} MB";
            }
            
            // Update drone count
            if (droneCountText != null)
            {
                var fleetManager = FindFirstObjectByType<DroneFleetManager>();
                if (fleetManager != null)
                {
                    droneCountText.text = $"Drones: {fleetManager.TotalActiveDrones}";
                }
            }
        }
        
        #endregion
        
        #region Final Game Stats
        
        private void UpdateFinalGameStats()
        {
            if (gameManager == null)
                return;
            
            var scoringSystem = FindFirstObjectByType<ScoringSystem>();
            if (scoringSystem == null)
                return;
            
            // Update final score
            if (finalScoreText != null)
            {
                // TODO: Implement GetTotalScore method in ScoringSystem
                finalScoreText.text = $"Final Score: {scoringSystem.TotalScore:N0}";
            }
            
            // Update game statistics
            if (gameStatsText != null)
            {
                // TODO: Implement GetGameStatistics method in ScoringSystem
                gameStatsText.text = $"Game Statistics:\n" +
                                   $"Mission Complete\n" +
                                   $"Time: {Time.time:F0}s";
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Update()
        {
            if (!isInitialized)
                return;
            
            // Handle input
            HandleGlobalInput();
            
            // Update performance display
            if (Time.time - lastPerformanceUpdate >= performanceUpdateInterval)
            {
                UpdatePerformanceDisplay();
                lastPerformanceUpdate = Time.time;
            }
        }
        
        private void HandleGlobalInput()
        {
            // ESC key handling
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                switch (currentUIState)
                {
                    case UIState.RTS:
                    case UIState.Tactical:
                        PauseGame();
                        break;
                        
                    case UIState.Paused:
                        ResumeGame();
                        break;
                        
                    case UIState.Settings:
                        SetUIState(UIState.Paused);
                        break;
                }
            }
            
            // F11 for fullscreen toggle
            if (Input.GetKeyDown(KeyCode.F11))
            {
                SetFullscreen(!Screen.fullScreen);
                if (fullscreenToggle != null)
                    fullscreenToggle.isOn = Screen.fullScreen;
            }
            
            // F1 for performance panel toggle (debug builds only)
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showPerformancePanel = !showPerformancePanel;
                if (performancePanel != null)
                    performancePanel.SetActive(showPerformancePanel);
            }
            #endif
        }
        
        private void OnDestroy()
        {
            // Save settings before destruction
            SaveSettings();
            
            // Unsubscribe from events
            if (gameManager != null)
            {
                GameManager.OnGameStateChanged -= OnGameStateChanged;
                // TODO: Fix OnScoreUpdated event signature
                // GameManager.OnScoreUpdated -= OnScoreUpdated;
            }
        }
        
        #endregion
    }
    
    #region Supporting Enums and Classes
    
    public enum UIState
    {
        MainMenu,
        Loading,
        RTS,
        Tactical,
        Paused,
        Settings,
        GameOver,
        Victory
    }
    
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
    
    #endregion
}

