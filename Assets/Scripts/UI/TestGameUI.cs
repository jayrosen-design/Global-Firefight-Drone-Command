using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalFirefight.Core;

namespace GlobalFirefight.UI
{
    /// <summary>
    /// Simple Test UI for navigating game states and modes
    /// Uses direct screen positioning to avoid UI layout issues
    /// </summary>
    public class TestGameUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Canvas testCanvas;
        [SerializeField] private GameObject buttonPanel;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private Button switchModeButton;
        [SerializeField] private Button loadFiresButton;
        
        [Header("Current State")]
        [SerializeField] private int currentStateIndex = 0;
        [SerializeField] private int currentModeIndex = 0;
        
        private GameState[] gameStates = {
            GameState.MainMenu,
            GameState.Loading,
            GameState.RTSView,
            GameState.DroneView,
            GameState.Paused,
            GameState.GameOver,
            GameState.PostMission
        };
        
        private GameMode[] gameModes = {
            GameMode.Scenario,
            GameMode.Campaign,
            GameMode.Tutorial,
            GameMode.Sandbox
        };
        
        private void Start()
        {
            UnityEngine.Debug.Log("🎮 TestGameUI Starting...");
            CreateTestUI();
            
            // Wait a frame to ensure everything is properly initialized
            StartCoroutine(DelayedInitialization());
        }
        
        private System.Collections.IEnumerator DelayedInitialization()
        {
            yield return new WaitForEndOfFrame();
            
            // Ensure listeners are connected after a frame delay
            ConnectButtonListeners();
            UpdateUI();
            
            // Test button functionality
            UnityEngine.Debug.Log("🎮 TestGameUI initialization complete");
            UnityEngine.Debug.Log($"🎮 Button references - Previous: {previousButton != null}, Next: {nextButton != null}, Switch: {switchModeButton != null}");
        }
        
        private void CreateTestUI()
        {
            // Ensure EventSystem exists
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                UnityEngine.Debug.LogWarning("⚠️ No EventSystem found! Creating one...");
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                UnityEngine.Debug.Log("✅ EventSystem created");
            }
            else
            {
                UnityEngine.Debug.Log("✅ EventSystem found");
            }
            
            // Create main canvas if it doesn't exist
            if (testCanvas == null)
            {
                GameObject canvasGO = new GameObject("TestGameUI_Canvas");
                testCanvas = canvasGO.AddComponent<Canvas>();
                testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                testCanvas.sortingOrder = 1000; // Ensure it's on top
                
                // Add CanvasScaler for responsive design
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
                // Add GraphicRaycaster
                canvasGO.AddComponent<GraphicRaycaster>();
                UnityEngine.Debug.Log("✅ Test Canvas created");
            }
            
            // Create button panel
            // if (buttonPanel == null)
            // {
            //     GameObject panelGO = new GameObject("TestUI_Panel");
            //     panelGO.transform.SetParent(testCanvas.transform, false);
                
            //     // Add RectTransform and position at top of screen
            //     var panelRect = panelGO.AddComponent<RectTransform>();
            //     panelRect.anchorMin = new Vector2(0, 0.85f);
            //     panelRect.anchorMax = new Vector2(1, 1);
            //     panelRect.offsetMin = Vector2.zero;
            //     panelRect.offsetMax = Vector2.zero;
                
            //     // Add background
            //     var panelImage = panelGO.AddComponent<Image>();
            //     panelImage.color = new Color(0, 0, 0, 0.7f);
                
            //     buttonPanel = panelGO;
            // }
            
            // Create Previous Button
            if (previousButton == null)
            {
                previousButton = CreateButton("Previous State", new Vector2(50, -50), new Vector2(150, 40));
                UnityEngine.Debug.Log("🎮 Previous button created");
            }
            
            // Create Next Button
            if (nextButton == null)
            {
                nextButton = CreateButton("Next State", new Vector2(220, -50), new Vector2(150, 40));
                UnityEngine.Debug.Log("🎮 Next button created");
            }
            
            // Create Mode Switch Button
            if (switchModeButton == null)
            {
                switchModeButton = CreateButton("Switch Mode", new Vector2(390, -50), new Vector2(150, 40));
                UnityEngine.Debug.Log("🎮 Switch Mode button created");
            }
            
            // Create Load Fires Button
            if (loadFiresButton == null)
            {
                loadFiresButton = CreateButton("Load Fires", new Vector2(560, -50), new Vector2(120, 40));
                UnityEngine.Debug.Log("🔥 Load Fires button created");
            }
            
            // Create Status Text
            if (statusText == null)
            {
                statusText = CreateText("Status: MainMenu", new Vector2(50, -100), new Vector2(400, 30));
                statusText.fontSize = 16;
                statusText.fontStyle = FontStyles.Bold;
            }
            
            // Create Mode Text
            if (modeText == null)
            {
                modeText = CreateText("Mode: Scenario", new Vector2(50, -130), new Vector2(400, 30));
                modeText.fontSize = 14;
            }
            
            // Connect button listeners AFTER all UI elements are created
            ConnectButtonListeners();
        }
        
        private void ConnectButtonListeners()
        {
            UnityEngine.Debug.Log("🔗 Connecting button listeners...");
            
            if (previousButton != null)
            {
                previousButton.onClick.RemoveAllListeners(); // Clear any existing listeners
                previousButton.onClick.AddListener(PreviousState);
                UnityEngine.Debug.Log("✅ Previous button listener connected");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ Previous button is null when trying to connect listener");
            }
            
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners(); // Clear any existing listeners
                nextButton.onClick.AddListener(NextState);
                UnityEngine.Debug.Log("✅ Next button listener connected");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ Next button is null when trying to connect listener");
            }
            
            if (switchModeButton != null)
            {
                switchModeButton.onClick.RemoveAllListeners(); // Clear any existing listeners
                switchModeButton.onClick.AddListener(SwitchMode);
                UnityEngine.Debug.Log("✅ Switch Mode button listener connected");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ Switch Mode button is null when trying to connect listener");
            }
            
            if (loadFiresButton != null)
            {
                loadFiresButton.onClick.RemoveAllListeners(); // Clear any existing listeners
                loadFiresButton.onClick.AddListener(LoadFires);
                UnityEngine.Debug.Log("✅ Load Fires button listener connected");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ Load Fires button is null when trying to connect listener");
            }
        }
        
        private Button CreateButton(string text, Vector2 position, Vector2 size)
        {
            GameObject buttonGO = new GameObject($"TestUI_{text.Replace(" ", "")}");
            buttonGO.transform.SetParent(buttonPanel.transform, false);
            
            // Setup RectTransform
            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            
            // Add Button component
            var button = buttonGO.AddComponent<Button>();
            button.interactable = true; // Ensure button is interactable
            
            // Add background image
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.8f, 0.8f);
            image.raycastTarget = true; // Ensure image can receive raycasts
            
            // Create button text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            // Setup button colors
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.3f, 0.8f, 0.8f);
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.1f, 0.2f, 0.7f, 1f);
            button.colors = colors;
            
            return button;
        }
        
        private TextMeshProUGUI CreateText(string text, Vector2 position, Vector2 size)
        {
            GameObject textGO = new GameObject($"TestUI_Text");
            textGO.transform.SetParent(buttonPanel.transform, false);
            
            // Setup RectTransform
            var rectTransform = textGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            
            // Add TextMeshProUGUI
            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            
            return textComponent;
        }
        
        private void PreviousState()
        {
            UnityEngine.Debug.Log("🎮 Previous State button clicked");
            
            currentStateIndex--;
            if (currentStateIndex < 0)
                currentStateIndex = gameStates.Length - 1;
            
            UnityEngine.Debug.Log($"🎮 Changed to state index: {currentStateIndex} ({gameStates[currentStateIndex]})");
            ChangeToCurrentState();
        }
        
        private void NextState()
        {
            UnityEngine.Debug.Log("🎮 Next State button clicked");
            
            currentStateIndex++;
            if (currentStateIndex >= gameStates.Length)
                currentStateIndex = 0;
            
            UnityEngine.Debug.Log($"🎮 Changed to state index: {currentStateIndex} ({gameStates[currentStateIndex]})");
            ChangeToCurrentState();
        }
        
        private void SwitchMode()
        {
            UnityEngine.Debug.Log("🎮 Switch Mode button clicked");
            
            currentModeIndex++;
            if (currentModeIndex >= gameModes.Length)
                currentModeIndex = 0;
            
            UnityEngine.Debug.Log($"🎮 Changed to mode index: {currentModeIndex} ({gameModes[currentModeIndex]})");
            UpdateUI();
        }
        
        private void LoadFires()
        {
            UnityEngine.Debug.Log("🔥 Load Fires button clicked");
            
            // Find CesiumFireLoader and trigger fire loading
            var fireLoader = FindFirstObjectByType<GlobalFirefight.Geospatial.CesiumFireLoader>();
            if (fireLoader != null)
            {
                fireLoader.LoadFireData();
                UnityEngine.Debug.Log("✅ Fire loading triggered");
            }
            else
            {
                UnityEngine.Debug.LogWarning("⚠️ CesiumFireLoader not found in scene");
            }
            
            // Also trigger fire position debugging if available
            var fireDebuggers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var debugger in fireDebuggers)
            {
                if (debugger.GetType().Name == "FirePositionDebugger")
                {
                    var method = debugger.GetType().GetMethod("LogFirePositions");
                    if (method != null)
                    {
                        method.Invoke(debugger, null);
                        break;
                    }
                }
            }
        }
        
        private void ChangeToCurrentState()
        {
            var targetState = gameStates[currentStateIndex];
            var targetMode = gameModes[currentModeIndex];
            
            UnityEngine.Debug.Log($"🎮 Switching to State: {targetState}, Mode: {targetMode}");
            
            // Get GameManager and change state
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                UnityEngine.Debug.LogError("❌ GameManager.Instance is null! Make sure GameManager exists in the scene.");
                
                // Try to find it manually
                gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager == null)
                {
                    UnityEngine.Debug.LogError("❌ No GameManager found in scene! Creating a temporary one...");
                    
                    // Create a temporary GameManager for testing
                    var go = new GameObject("GameManager (Test)");
                    gameManager = go.AddComponent<GameManager>();
                }
                else
                {
                    UnityEngine.Debug.Log("✅ Found GameManager via FindFirstObjectByType");
                }
            }
            else
            {
                UnityEngine.Debug.Log($"✅ GameManager found! Current state: {gameManager.CurrentState}");
            }
            
            if (gameManager != null)
            {
                // Handle special state transitions
                switch (targetState)
                {
                    case GameState.RTSView:
                        UnityEngine.Debug.Log("🎮 Transitioning to RTS View");
                        if (gameManager.CurrentState == GameState.MainMenu)
                        {
                            // Start a new mission first
                            UnityEngine.Debug.Log("🎮 Starting new mission from main menu");
                            gameManager.StartNewMission(targetMode);
                        }
                        else
                        {
                            UnityEngine.Debug.Log("🎮 Switching to RTS view");
                            gameManager.SwitchToRTSView();
                        }
                        break;
                        
                    case GameState.DroneView:
                        UnityEngine.Debug.Log("🎮 Transitioning to Drone View");
                        if (gameManager.CurrentState == GameState.MainMenu)
                        {
                            // Start mission first, then switch to drone view
                            UnityEngine.Debug.Log("🎮 Starting new mission then switching to drone view");
                            gameManager.StartNewMission(targetMode);
                            gameManager.SwitchToDroneView();
                        }
                        else
                        {
                            UnityEngine.Debug.Log("🎮 Switching to drone view");
                            gameManager.SwitchToDroneView();
                        }
                        break;
                        
                    case GameState.Paused:
                        UnityEngine.Debug.Log("🎮 Pausing game");
                        gameManager.PauseGame();
                        break;
                        
                    case GameState.MainMenu:
                        UnityEngine.Debug.Log("🎮 Going to main menu");
                        gameManager.ChangeGameState(GameState.MainMenu);
                        break;
                        
                    default:
                        UnityEngine.Debug.Log($"🎮 Changing to state: {targetState}");
                        gameManager.ChangeGameState(targetState);
                        break;
                }
            }
            
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            var currentState = gameStates[currentStateIndex];
            var currentMode = gameModes[currentModeIndex];
            
            if (statusText != null)
            {
                statusText.text = $"State: {currentState} ({currentStateIndex + 1}/{gameStates.Length})";
            }
            
            if (modeText != null)
            {
                modeText.text = $"Mode: {currentMode} ({currentModeIndex + 1}/{gameModes.Length})";
            }
            
            UnityEngine.Debug.Log($"🎮 UI Updated - State: {currentState}, Mode: {currentMode}");
        }
        
        private void Update()
        {
            // Update UI every frame to reflect actual game state
            var gameManager = GameManager.Instance;
            if (gameManager != null && statusText != null)
            {
                var actualState = gameManager.CurrentState;
                var actualMode = gameManager.CurrentMode;
                
                // Find the index of the actual state
                for (int i = 0; i < gameStates.Length; i++)
                {
                    if (gameStates[i] == actualState)
                    {
                        currentStateIndex = i;
                        break;
                    }
                }
                
                // Update status text with actual game state
                statusText.text = $"State: {actualState} ({currentStateIndex + 1}/{gameStates.Length})";
                
                // Update mode text with our UI selection (not GameManager's mode)
                var uiMode = gameModes[currentModeIndex];
                if (modeText != null)
                {
                    modeText.text = $"Mode: {uiMode} ({currentModeIndex + 1}/{gameModes.Length})";
                }
            }
        }
        
        private void OnDestroy()
        {
            // Clean up button listeners
            if (previousButton != null)
                previousButton.onClick.RemoveAllListeners();
            if (nextButton != null)
                nextButton.onClick.RemoveAllListeners();
            if (switchModeButton != null)
                switchModeButton.onClick.RemoveAllListeners();
        }
        
        [ContextMenu("🧪 Test Previous Button")]
        public void TestPreviousButton()
        {
            UnityEngine.Debug.Log("🧪 Manual test - Previous button");
            PreviousState();
        }
        
        [ContextMenu("🧪 Test Next Button")]
        public void TestNextButton()
        {
            UnityEngine.Debug.Log("🧪 Manual test - Next button");
            NextState();
        }
        
        [ContextMenu("🧪 Test Switch Mode Button")]
        public void TestSwitchModeButton()
        {
            UnityEngine.Debug.Log("🧪 Manual test - Switch mode button");
            SwitchMode();
        }
        
        [ContextMenu("🔍 Debug Button Status")]
        public void DebugButtonStatus()
        {
            UnityEngine.Debug.Log("=== BUTTON DEBUG STATUS ===");
            UnityEngine.Debug.Log($"Previous Button: {(previousButton != null ? "✅ Exists" : "❌ Null")}");
            UnityEngine.Debug.Log($"Next Button: {(nextButton != null ? "✅ Exists" : "❌ Null")}");
            UnityEngine.Debug.Log($"Switch Mode Button: {(switchModeButton != null ? "✅ Exists" : "❌ Null")}");
            UnityEngine.Debug.Log($"Status Text: {(statusText != null ? "✅ Exists" : "❌ Null")}");
            UnityEngine.Debug.Log($"Mode Text: {(modeText != null ? "✅ Exists" : "❌ Null")}");
            UnityEngine.Debug.Log($"Canvas: {(testCanvas != null ? "✅ Exists" : "❌ Null")}");
            UnityEngine.Debug.Log($"Button Panel: {(buttonPanel != null ? "✅ Exists" : "❌ Null")}");
            
            if (previousButton != null)
            {
                UnityEngine.Debug.Log($"Previous Button Listeners: {previousButton.onClick.GetPersistentEventCount()}");
                UnityEngine.Debug.Log($"Previous Button Interactable: {previousButton.interactable}");
            }
            if (nextButton != null)
            {
                UnityEngine.Debug.Log($"Next Button Listeners: {nextButton.onClick.GetPersistentEventCount()}");
                UnityEngine.Debug.Log($"Next Button Interactable: {nextButton.interactable}");
            }
            if (switchModeButton != null)
            {
                UnityEngine.Debug.Log($"Switch Mode Button Listeners: {switchModeButton.onClick.GetPersistentEventCount()}");
                UnityEngine.Debug.Log($"Switch Mode Button Interactable: {switchModeButton.interactable}");
            }
        }
        
        [ContextMenu("🔧 Reconnect Button Listeners")]
        public void ReconnectButtonListeners()
        {
            UnityEngine.Debug.Log("🔧 Reconnecting button listeners...");
            ConnectButtonListeners();
        }
        
        [ContextMenu("🎯 Test Button Click Simulation")]
        public void TestButtonClickSimulation()
        {
            UnityEngine.Debug.Log("🎯 Simulating button clicks...");
            
            if (previousButton != null)
            {
                UnityEngine.Debug.Log("🎯 Simulating Previous button click");
                previousButton.onClick.Invoke();
            }
            
            if (nextButton != null)
            {
                UnityEngine.Debug.Log("🎯 Simulating Next button click");
                nextButton.onClick.Invoke();
            }
        }
    }
}

