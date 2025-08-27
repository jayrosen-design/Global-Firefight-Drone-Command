using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalFirefight.Data;
using GlobalFirefight.API;
using GlobalFirefight.Systems;
using GlobalFirefight.UI;
using GlobalFirefight.Geospatial;
using GlobalFirefight.Fire;
using GlobalFirefight.Drones;

namespace GlobalFirefight.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Instance")]
        public static GameManager Instance { get; private set; }
        
        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private GameMode currentMode = GameMode.Scenario;
        [SerializeField] private GameSettings gameSettings;
        
        [Header("Core Systems")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private ScoringSystem scoringSystem;
        [SerializeField] private GeospatialManager geospatialManager;
        
        [Header("Managers")]
        [SerializeField] private NASAAPIManager nasaAPIManager;
        [SerializeField] private DroneFleetManager droneFleetManager;
        [SerializeField] private FireSimulation fireSimulation;
        
        [Header("Mission Data")]
        [SerializeField] private float missionStartTime;
        [SerializeField] private float missionTimeRemaining;
        [SerializeField] private bool missionActive;
        [SerializeField] private ScoreData currentScore;
        
        [Header("Performance Tracking")]
        [SerializeField] private int totalFiresLoaded;
        [SerializeField] private int activeFires;
        [SerializeField] private int extinguishedFires;
        [SerializeField] private float frameRate;
        
        // Events
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<float> OnMissionTimeUpdated;
        public static event Action<ScoreData> OnScoreUpdated;
        public static event Action OnMissionComplete;
        
        // Properties
        public GameState CurrentState => currentState;
        public GameMode CurrentMode => currentMode;
        public GameSettings Settings => gameSettings;
        public bool IsMissionActive => missionActive;
        public float MissionTimeRemaining => missionTimeRemaining;
        public ScoreData CurrentScore => currentScore;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Implement singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGameManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            StartCoroutine(InitializeGameSystems());
        }
        
        private void Update()
        {
            UpdateGameSystems(Time.deltaTime);
            UpdatePerformanceMetrics();
            
            // Handle input
            HandleGlobalInput();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && missionActive)
            {
                PauseGame();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeGameManager()
        {
            // Initialize game settings with defaults
            if (gameSettings == null)
            {
                gameSettings = new GameSettings();
            }
            
            // Initialize score data
            currentScore = new ScoreData();
            
            // Set initial game state
            ChangeGameState(GameState.Loading);
            
            UnityEngine.Debug.Log("GameManager initialized successfully");
        }
        
        private IEnumerator InitializeGameSystems()
        {
            yield return new WaitForSeconds(0.1f);
            
            // Initialize core systems in order
            UnityEngine.Debug.Log("Initializing game systems...");
            
            // Initialize geospatial system first
            if (geospatialManager != null)
            {
                geospatialManager.Initialize();
                yield return new WaitForSeconds(0.5f);
            }
            
            // Initialize API managers
            if (nasaAPIManager != null)
            {
                nasaAPIManager.Initialize(gameSettings.nasaAPIKey);
                yield return new WaitForSeconds(0.2f);
            }
            
            // Initialize gameplay systems
            if (droneFleetManager != null)
            {
                droneFleetManager.Initialize();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (fireSimulation != null)
            {
                fireSimulation.Initialize();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (scoringSystem != null)
            {
                scoringSystem.Initialize();
                yield return new WaitForSeconds(0.2f);
            }
            
            // Initialize UI last
            if (uiManager != null)
            {
                uiManager.Initialize();
                yield return new WaitForSeconds(0.2f);
            }
            
            // Initialize camera controller
            if (cameraController != null)
            {
                cameraController.Initialize();
                yield return new WaitForSeconds(0.2f);
            }
            
            UnityEngine.Debug.Log("All systems initialized successfully");
            
            // Ready to start
            ChangeGameState(GameState.MainMenu);
        }
        
        #endregion
        
        #region Game State Management
        
        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState)
                return;
                
            GameState previousState = currentState;
            currentState = newState;
            
            UnityEngine.Debug.Log($"Game state changed: {previousState} -> {newState}");
            
            // Handle state transitions
            OnGameStateExit(previousState);
            OnGameStateEnter(newState);
            
            // Notify listeners
            OnGameStateChanged?.Invoke(newState);
        }
        
        private void OnGameStateExit(GameState exitingState)
        {
            switch (exitingState)
            {
                case GameState.RTSView:
                    break;
                case GameState.DroneView:
                    break;
                case GameState.Paused:
                    Time.timeScale = 1f;
                    break;
            }
        }
        
        private void OnGameStateEnter(GameState enteringState)
        {
            switch (enteringState)
            {
                case GameState.Loading:
                    // Show loading screen
                    if (uiManager != null)
                        uiManager.SetUIState(UIState.Loading);
                    break;
                    
                case GameState.MainMenu:
                    // Show main menu
                    if (uiManager != null)
                        uiManager.SetUIState(UIState.MainMenu);
                    break;
                    
                case GameState.RTSView:
                    // Switch to RTS camera and UI
                    if (cameraController != null)
                        cameraController.SwitchToRTSView();
                    if (uiManager != null)
                        uiManager.SetUIState(UIState.RTS);
                    break;
                    
                case GameState.DroneView:
                    // Switch to drone camera and UI
                    if (uiManager != null)
                        uiManager.SetUIState(UIState.Tactical);
                    break;
                    
                case GameState.Paused:
                    Time.timeScale = 0f;
                    if (uiManager != null)
                        uiManager.SetUIState(UIState.Paused);
                    break;
                    
                case GameState.GameOver:
                    EndMission();
                    if (uiManager != null)
                        uiManager.SetUIState(UIState.GameOver);
                    break;
                    
                case GameState.PostMission:
                    ShowPostMissionResults();
                    break;
            }
        }
        
        #endregion
        
        #region Mission Management
        
        public void StartNewMission(GameMode mode, float duration = 600f)
        {
            UnityEngine.Debug.Log($"Starting new mission: {mode} for {duration} seconds");
            
            currentMode = mode;
            missionTimeRemaining = duration;
            missionStartTime = Time.time;
            missionActive = true;
            
            // Reset score
            currentScore = new ScoreData();
            
            // Initialize mission systems
            StartCoroutine(LoadMissionData());
            
            ChangeGameState(GameState.RTSView);
        }
        
        private IEnumerator LoadMissionData()
        {
            // Load fire data from NASA APIs
            if (nasaAPIManager != null && gameSettings.useRealTimeData)
            {
                UnityEngine.Debug.Log("Loading real-time fire data...");
                yield return StartCoroutine(nasaAPIManager.LoadFireData());
            }
            
            // Setup initial drone fleets
            if (droneFleetManager != null)
            {
                droneFleetManager.SetupInitialFleets();
            }
            
            UnityEngine.Debug.Log("Mission data loaded successfully");
        }
        
        public void PauseGame()
        {
            if (currentState == GameState.RTSView || currentState == GameState.DroneView)
            {
                ChangeGameState(GameState.Paused);
            }
        }
        
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeGameState(GameState.RTSView);
            }
        }
        
        public void EndMission()
        {
            UnityEngine.Debug.Log("Mission ended");
            
            missionActive = false;
            
            // Calculate final score
            if (currentScore != null)
            {
                currentScore.CalculateFinalScore();
            }
            
            // Stop all drone operations
            if (droneFleetManager != null)
            {
                droneFleetManager.RecallAllDrones();
            }
            
            OnMissionComplete?.Invoke();
            ChangeGameState(GameState.PostMission);
        }
        
        private void ShowPostMissionResults()
        {
            if (uiManager != null)
            {
                // Use the UIManager's state system instead of calling a non-existent method
                uiManager.SetUIState(UIState.GameOver);
                
                // TODO: Pass score data to the UI when the method becomes available
                if (currentScore != null)
                {
                    currentScore.CalculateFinalScore();
                    UnityEngine.Debug.Log($"Mission completed with final score calculated");
                }
            }
        }
        
        #endregion
        
        #region System Updates
        
        private void UpdateGameSystems(float deltaTime)
        {
            if (!missionActive)
                return;
                
            // Update mission timer
            UpdateMissionTimer(deltaTime);
            
            // Update core systems
            if (fireSimulation != null)
                fireSimulation.UpdateSimulation(deltaTime);
                
            if (droneFleetManager != null)
                droneFleetManager.UpdateFleets(deltaTime);
                
            if (scoringSystem != null)
                scoringSystem.UpdateScoring(deltaTime);
        }
        
        private void UpdateMissionTimer(float deltaTime)
        {
            if (gameSettings.infiniteMode)
                return;
                
            missionTimeRemaining -= deltaTime;
            OnMissionTimeUpdated?.Invoke(missionTimeRemaining);
            
            if (missionTimeRemaining <= 0f)
            {
                ChangeGameState(GameState.GameOver);
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            frameRate = 1f / Time.unscaledDeltaTime;
            
            // Update fire counts
            if (fireSimulation != null)
            {
                activeFires = fireSimulation.ActiveFireCount;
                extinguishedFires = fireSimulation.ExtinguishedFireCount;
                totalFiresLoaded = fireSimulation.TotalFireCount;
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleGlobalInput()
        {
            // ESC key handling
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                switch (currentState)
                {
                    case GameState.RTSView:
                    case GameState.DroneView:
                        PauseGame();
                        break;
                    case GameState.Paused:
                        ResumeGame();
                        break;
                }
            }
            
            // Tab key for switching views
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (currentState == GameState.RTSView)
                {
                    SwitchToDroneView();
                }
                else if (currentState == GameState.DroneView)
                {
                    SwitchToRTSView();
                }
            }
        }
        
        #endregion
        
        #region View Switching
        
        public void SwitchToRTSView()
        {
            if (cameraController != null)
            {
                cameraController.SwitchToRTSView();
                ChangeGameState(GameState.RTSView);
            }
        }
        
        public void SwitchToDroneView(DroneUnit drone = null)
        {
            if (cameraController != null)
            {
                cameraController.SwitchToDroneView(drone);
                ChangeGameState(GameState.DroneView);
            }
        }
        
        #endregion
        
        #region Public API
        
        public void UpdateScore(ScoreData newScore)
        {
            currentScore = newScore;
            OnScoreUpdated?.Invoke(currentScore);
        }
        
        public void QuitGame()
        {
            UnityEngine.Debug.Log("Quitting game...");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        public string GetPerformanceReport()
        {
            return $"FPS: {frameRate:F1} | " +
                   $"Fires: {activeFires}/{totalFiresLoaded} | " +
                   $"Extinguished: {extinguishedFires} | " +
                   $"Memory: {(GC.GetTotalMemory(false) / 1024f / 1024f):F1}MB";
        }
        
        #endregion
        
        #region Settings Management
        
        public void UpdateGameSettings(GameSettings newSettings)
        {
            gameSettings = newSettings;
            ApplySettings();
        }
        
        private void ApplySettings()
        {
            // Apply performance settings
            if (fireSimulation != null)
            {
                fireSimulation.SetMaxFireDisplay(gameSettings.maxFiresDisplayed);
            }
            
            // Apply audio settings
            AudioListener.volume = gameSettings.masterVolume;
            
            // Apply quality settings
            ApplyGraphicsSettings();
        }
        
        private void ApplyGraphicsSettings()
        {
            // Adjust particle quality
            var particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.maxParticles = gameSettings.particleQuality * 100;
            }
        }
        
        // Get current game time
        public float GetGameTime()
        {
            return Time.time;
        }
        
        // Check if connected to APIs
        public bool IsConnectedToAPIs()
        {
            return nasaAPIManager != null && nasaAPIManager.IsInitialized;
        }
        
        // Start a new game
        public void StartNewGame()
        {
            ChangeGameState(GameState.Loading);
            StartNewMission(currentMode);
        }
        
        // Restart the current game
        public void RestartGame()
        {
            ChangeGameState(GameState.Loading);
            StartNewMission(currentMode, missionTimeRemaining);
        }
        
        #endregion
    }
}

