using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalFirefight.UI;
using GlobalFirefight.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GlobalFirefight.Setup
{
    /// <summary>
    /// UI Connection Helper - Automatically finds and connects all UI panels and elements
    /// This script discovers all UI components in the scene and properly wires them together
    /// </summary>
    public class UIConnector : MonoBehaviour
    {
        [Header("UI Connection Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private string connectionStatus = "Ready to connect UI components";
        
        [Header("Connection Options")]
        [SerializeField] private bool connectOnStart = false;
        [SerializeField] private bool fixUIPositioning = true;
        [SerializeField] private bool showDebugLogs = true;
        
        [Header("Found UI Managers")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private TacticalHUD tacticalHUD;
        [SerializeField] private RTSInterface rtsInterface;
        [SerializeField] private Canvas mainCanvas;
        
        [Header("UI Discovery Results")]
        [SerializeField] private int panelsFound = 0;
        [SerializeField] private int buttonsFound = 0;
        [SerializeField] private int slidersFound = 0;
        [SerializeField] private int textComponentsFound = 0;
        [SerializeField] private int imagesFound = 0;
        
        private Dictionary<string, GameObject> foundPanels = new Dictionary<string, GameObject>();
        private Dictionary<string, Button> foundButtons = new Dictionary<string, Button>();
        private Dictionary<string, Slider> foundSliders = new Dictionary<string, Slider>();
        private Dictionary<string, TextMeshProUGUI> foundTexts = new Dictionary<string, TextMeshProUGUI>();
        private Dictionary<string, Image> foundImages = new Dictionary<string, Image>();
        private Dictionary<string, Toggle> foundToggles = new Dictionary<string, Toggle>();
        private Dictionary<string, Dropdown> foundDropdowns = new Dictionary<string, Dropdown>();
        
        private void Start()
        {
            if (connectOnStart && !isConnected)
            {
                ConnectAllUI();
            }
        }
        
        [ContextMenu("🔗 Connect All UI Components")]
        public void ConnectAllUI()
        {
            connectionStatus = "Discovering UI components...";
            
            DiscoverUIComponents();
            FixUIPositioning();
            ConnectUIManagerComponents();
            ConnectTacticalHUDComponents();
            ConnectRTSInterfaceComponents();
            
            isConnected = true;
            connectionStatus = "UI connection complete!";
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("✅ UI Connection Complete! All UI components are now properly connected.");
                UnityEngine.Debug.Log($"📊 Connected: {panelsFound} panels, {buttonsFound} buttons, {slidersFound} sliders, {textComponentsFound} texts");
            }
        }
        
        [ContextMenu("🔍 Discover UI Components")]
        public void DiscoverUIComponents()
        {
            // Clear previous results
            foundPanels.Clear();
            foundButtons.Clear();
            foundSliders.Clear();
            foundTexts.Clear();
            foundImages.Clear();
            foundToggles.Clear();
            foundDropdowns.Clear();
            
            // Find UI managers
            uiManager = FindFirstObjectByType<UIManager>();
            tacticalHUD = FindFirstObjectByType<TacticalHUD>();
            rtsInterface = FindFirstObjectByType<RTSInterface>();
            
            // Find main canvas
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.name.ToLower().Contains("main") || 
                    canvas.gameObject.name.ToLower().Contains("ui") ||
                    canvas.transform.childCount > 0)
                {
                    mainCanvas = canvas;
                    break;
                }
            }
            
            if (mainCanvas == null && canvases.Length > 0)
                mainCanvas = canvases[0];
            
            if (mainCanvas != null)
            {
                // Discover all UI components in the canvas hierarchy
                DiscoverComponentsInHierarchy(mainCanvas.transform);
                
                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log($"🔍 UI Discovery complete in canvas: {mainCanvas.name}");
                    UnityEngine.Debug.Log($"Found: {foundPanels.Count} panels, {foundButtons.Count} buttons, {foundSliders.Count} sliders");
                }
            }
            
            // Update counters
            panelsFound = foundPanels.Count;
            buttonsFound = foundButtons.Count;
            slidersFound = foundSliders.Count;
            textComponentsFound = foundTexts.Count;
            imagesFound = foundImages.Count;
        }
        
        private void DiscoverComponentsInHierarchy(Transform parent)
        {
            // Check current object
            string objName = parent.name.ToLower();
            
            // Find panels (GameObjects with specific names)
            if (objName.Contains("panel") || objName.Contains("menu") || objName.Contains("hud") || 
                objName.Contains("interface") || objName.Contains("dialog") || objName.Contains("popup"))
            {
                if (!foundPanels.ContainsKey(objName))
                {
                    foundPanels[objName] = parent.gameObject;
                }
            }
            
            // Find UI components
            var button = parent.GetComponent<Button>();
            if (button != null && !foundButtons.ContainsKey(objName))
            {
                foundButtons[objName] = button;
            }
            
            var slider = parent.GetComponent<Slider>();
            if (slider != null && !foundSliders.ContainsKey(objName))
            {
                foundSliders[objName] = slider;
            }
            
            var text = parent.GetComponent<TextMeshProUGUI>();
            if (text != null && !foundTexts.ContainsKey(objName))
            {
                foundTexts[objName] = text;
            }
            
            var image = parent.GetComponent<Image>();
            if (image != null && !foundImages.ContainsKey(objName))
            {
                foundImages[objName] = image;
            }
            
            var toggle = parent.GetComponent<Toggle>();
            if (toggle != null && !foundToggles.ContainsKey(objName))
            {
                foundToggles[objName] = toggle;
            }
            
            var dropdown = parent.GetComponent<Dropdown>();
            if (dropdown != null && !foundDropdowns.ContainsKey(objName))
            {
                foundDropdowns[objName] = dropdown;
            }
            
            // Recursively check children
            for (int i = 0; i < parent.childCount; i++)
            {
                DiscoverComponentsInHierarchy(parent.GetChild(i));
            }
        }
        
        [ContextMenu("📐 Fix UI Positioning")]
        public void FixUIPositioning()
        {
            if (!fixUIPositioning || mainCanvas == null) return;
            
            connectionStatus = "Fixing UI positioning...";
            
            // Ensure canvas is properly configured
            var canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            // Configure canvas scaler for proper UI scaling
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // Ensure GraphicRaycaster exists
            if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Fix positioning of major panels
            PositionUIPanel("mainmenupanel", new Vector2(0, 0), new Vector2(1, 1));
            PositionUIPanel("pausemenupanel", new Vector2(0, 0), new Vector2(1, 1));
            PositionUIPanel("settingspanel", new Vector2(0, 0), new Vector2(1, 1));
            PositionUIPanel("loadingpanel", new Vector2(0, 0), new Vector2(1, 1));
            
            // Position HUD elements
            PositionUIPanel("tacticalhud", new Vector2(0, 0), new Vector2(1, 1));
            PositionUIPanel("rtsinterface", new Vector2(0, 0), new Vector2(1, 1));
            PositionUIPanel("hudpanel", new Vector2(0, 0), new Vector2(1, 1));
            
            // Position specific HUD components
            PositionUIPanel("droneinfopanel", new Vector2(0, 1), new Vector2(0.3f, 0.8f), new Vector2(0, 1));
            PositionUIPanel("speedometerpanel", new Vector2(0, 0), new Vector2(0.2f, 0.2f), new Vector2(0, 0));
            PositionUIPanel("altimeterpanel", new Vector2(0.8f, 0), new Vector2(1, 0.2f), new Vector2(1, 0));
            PositionUIPanel("compasspanel", new Vector2(0.8f, 0.8f), new Vector2(1, 1), new Vector2(1, 1));
            PositionUIPanel("missionpanel", new Vector2(0.3f, 0.8f), new Vector2(0.7f, 1), new Vector2(0.5f, 1));
            PositionUIPanel("targetpanel", new Vector2(0.7f, 0.6f), new Vector2(1, 0.8f), new Vector2(1, 0.7f));
            
            // Position RTS panels
            PositionUIPanel("fireinfopanel", new Vector2(0, 0.6f), new Vector2(0.3f, 1), new Vector2(0, 1));
            PositionUIPanel("fleetpanel", new Vector2(0.7f, 0), new Vector2(1, 0.6f), new Vector2(1, 0));
            PositionUIPanel("globecontrolpanel", new Vector2(0, 0), new Vector2(0.3f, 0.4f), new Vector2(0, 0));
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("✅ UI positioning fixed! All panels should now be properly positioned.");
            }
        }
        
        private void PositionUIPanel(string panelName, Vector2 anchorMin, Vector2 anchorMax, Vector2? pivot = null)
        {
            if (foundPanels.TryGetValue(panelName, out GameObject panel))
            {
                var rectTransform = panel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    
                    if (pivot.HasValue)
                    {
                        rectTransform.pivot = pivot.Value;
                    }
                    
                    if (showDebugLogs)
                    {
                        UnityEngine.Debug.Log($"📐 Positioned panel: {panelName}");
                    }
                }
            }
        }
        
        private void ConnectUIManagerComponents()
        {
            if (uiManager == null) return;
            
            connectionStatus = "Connecting UIManager components...";
            
            // Connect main interfaces
            SetPrivateField(uiManager, "rtsInterface", rtsInterface);
            SetPrivateField(uiManager, "tacticalHUD", tacticalHUD);
            
            // Connect menu panels
            ConnectUIComponent(uiManager, "mainMenuPanel", "mainmenupanel");
            ConnectUIComponent(uiManager, "pauseMenuPanel", "pausemenupanel");
            ConnectUIComponent(uiManager, "settingsPanel", "settingspanel");
            ConnectUIComponent(uiManager, "loadingPanel", "loadingpanel");
            
            // Connect loading screen components
            ConnectUIComponent(uiManager, "loadingProgressBar", "loadingprogressbar", typeof(Slider));
            ConnectUIComponent(uiManager, "loadingStatusText", "loadingstatustext", typeof(TextMeshProUGUI));
            ConnectUIComponent(uiManager, "loadingBackground", "loadingbackground", typeof(Image));
            
            // Connect settings components
            ConnectUIComponent(uiManager, "masterVolumeSlider", "mastervolumeSlider", typeof(Slider));
            ConnectUIComponent(uiManager, "sfxVolumeSlider", "sfxvolumeSlider", typeof(Slider));
            ConnectUIComponent(uiManager, "musicVolumeSlider", "musicvolumeSlider", typeof(Slider));
            ConnectUIComponent(uiManager, "graphicsQualityDropdown", "graphicsqualitydropdown", typeof(Dropdown));
            ConnectUIComponent(uiManager, "resolutionDropdown", "resolutiondropdown", typeof(Dropdown));
            ConnectUIComponent(uiManager, "fullscreenToggle", "fullscreentoggle", typeof(Toggle));
            ConnectUIComponent(uiManager, "vsyncToggle", "vsynctoggle", typeof(Toggle));
            ConnectUIComponent(uiManager, "uiScaleSlider", "uiscaleslider", typeof(Slider));
            
            // Connect performance monitoring
            ConnectUIComponent(uiManager, "performancePanel", "performancepanel");
            ConnectUIComponent(uiManager, "fpsText", "fpstext", typeof(TextMeshProUGUI));
            ConnectUIComponent(uiManager, "memoryText", "memorytext", typeof(TextMeshProUGUI));
            ConnectUIComponent(uiManager, "droneCountText", "dronecounttext", typeof(TextMeshProUGUI));
            
            // Connect notification system
            ConnectUIComponent(uiManager, "notificationPanel", "notificationpanel");
            
            // Connect game state UI
            ConnectUIComponent(uiManager, "gameOverPanel", "gameoverpanel");
            ConnectUIComponent(uiManager, "victoryPanel", "victorypanel");
            ConnectUIComponent(uiManager, "finalScoreText", "finalscoretext", typeof(TextMeshProUGUI));
            ConnectUIComponent(uiManager, "restartButton", "restartbutton", typeof(Button));
            ConnectUIComponent(uiManager, "mainMenuButton", "mainmenubutton", typeof(Button));
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("✅ UIManager components connected!");
            }
        }
        
        private void ConnectTacticalHUDComponents()
        {
            if (tacticalHUD == null) return;
            
            connectionStatus = "Connecting TacticalHUD components...";
            
            // Connect main HUD elements
            SetPrivateField(tacticalHUD, "tacticalCanvas", mainCanvas);
            ConnectUIComponent(tacticalHUD, "hudPanel", "hudpanel");
            ConnectUIComponent(tacticalHUD, "crosshairUI", "crosshairui");
            ConnectUIComponent(tacticalHUD, "speedometerPanel", "speedometerpanel");
            ConnectUIComponent(tacticalHUD, "altimeterPanel", "altimeterpanel");
            ConnectUIComponent(tacticalHUD, "compassPanel", "compasspanel");
            
            // Connect drone status
            ConnectUIComponent(tacticalHUD, "droneInfoPanel", "droneinfopanel");
            ConnectUIComponent(tacticalHUD, "droneNameText", "dronenametext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "droneTypeText", "dronetypetext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "droneAvatarImage", "droneavatarimage", typeof(Image));
            ConnectUIComponent(tacticalHUD, "batterySlider", "batteryslider", typeof(Slider));
            ConnectUIComponent(tacticalHUD, "payloadSlider", "payloadslider", typeof(Slider));
            ConnectUIComponent(tacticalHUD, "batteryPercentText", "batterypercenttext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "payloadAmountText", "payloadamounttext", typeof(TextMeshProUGUI));
            
            // Connect flight instruments
            ConnectUIComponent(tacticalHUD, "speedText", "speedtext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "altitudeText", "altitudetext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "headingText", "headingtext", typeof(TextMeshProUGUI));
            
            // Connect target information
            ConnectUIComponent(tacticalHUD, "targetPanel", "targetpanel");
            ConnectUIComponent(tacticalHUD, "targetNameText", "targetnametext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "distanceToTargetText", "distancetotargettext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "etaText", "etatext", typeof(TextMeshProUGUI));
            
            // Connect mission status
            ConnectUIComponent(tacticalHUD, "missionPanel", "missionpanel");
            ConnectUIComponent(tacticalHUD, "missionTitleText", "missiontitletext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "missionObjectiveText", "missionobjectivetext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "missionProgressSlider", "missionprogressslider", typeof(Slider));
            ConnectUIComponent(tacticalHUD, "missionTimeText", "missiontimetext", typeof(TextMeshProUGUI));
            ConnectUIComponent(tacticalHUD, "abortMissionButton", "abortmissionbutton", typeof(Button));
            
            // Connect suppression controls
            ConnectUIComponent(tacticalHUD, "suppressionPanel", "suppressionpanel");
            ConnectUIComponent(tacticalHUD, "suppressionToggleButton", "suppressiontogglebutton", typeof(Button));
            ConnectUIComponent(tacticalHUD, "suppressionIntensitySlider", "suppressionintensityslider", typeof(Slider));
            ConnectUIComponent(tacticalHUD, "suppressionModeText", "suppressionmodetext", typeof(TextMeshProUGUI));
            
            // Connect camera controls
            ConnectUIComponent(tacticalHUD, "cameraControlPanel", "cameracontrolpanel");
            ConnectUIComponent(tacticalHUD, "switchToRTSButton", "switchtortsbutton", typeof(Button));
            ConnectUIComponent(tacticalHUD, "thermalVisionButton", "thermalvisionbutton", typeof(Button));
            ConnectUIComponent(tacticalHUD, "nightVisionButton", "nightvisionbutton", typeof(Button));
            ConnectUIComponent(tacticalHUD, "zoomInButton", "zoominbutton", typeof(Button));
            ConnectUIComponent(tacticalHUD, "zoomOutButton", "zoomoutbutton", typeof(Button));
            ConnectUIComponent(tacticalHUD, "cameraZoomSlider", "camerazoomslider", typeof(Slider));
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("✅ TacticalHUD components connected!");
            }
        }
        
        private void ConnectRTSInterfaceComponents()
        {
            if (rtsInterface == null) return;
            
            connectionStatus = "Connecting RTSInterface components...";
            
            // Connect main panels
            SetPrivateField(rtsInterface, "mainCanvas", mainCanvas);
            ConnectUIComponent(rtsInterface, "rtsPanel", "rtspanel");
            ConnectUIComponent(rtsInterface, "tacticalPanel", "tacticalpanel");
            ConnectUIComponent(rtsInterface, "menuPanel", "menupanel");
            ConnectUIComponent(rtsInterface, "settingsPanel", "settingspanel");
            
            // Connect global map controls
            ConnectUIComponent(rtsInterface, "globeControlPanel", "globecontrolpanel");
            ConnectUIComponent(rtsInterface, "zoomSlider", "zoomslider", typeof(Slider));
            ConnectUIComponent(rtsInterface, "resetViewButton", "resetviewbutton", typeof(Button));
            ConnectUIComponent(rtsInterface, "satelliteViewButton", "satelliteviewbutton", typeof(Button));
            ConnectUIComponent(rtsInterface, "terrainViewButton", "terrainviewbutton", typeof(Button));
            ConnectUIComponent(rtsInterface, "showFiresToggle", "showfirestoggle", typeof(Toggle));
            ConnectUIComponent(rtsInterface, "showDronesToggle", "showdronestoggle", typeof(Toggle));
            ConnectUIComponent(rtsInterface, "showWeatherToggle", "showweathertoggle", typeof(Toggle));
            
            // Connect fire information
            ConnectUIComponent(rtsInterface, "fireInfoPanel", "fireinfopanel");
            ConnectUIComponent(rtsInterface, "fireNameText", "firenametext", typeof(TextMeshProUGUI));
            ConnectUIComponent(rtsInterface, "fireLocationText", "firelocationtext", typeof(TextMeshProUGUI));
            ConnectUIComponent(rtsInterface, "fireIntensityText", "fireintensitytext", typeof(TextMeshProUGUI));
            ConnectUIComponent(rtsInterface, "fireAreaText", "fireareatext", typeof(TextMeshProUGUI));
            ConnectUIComponent(rtsInterface, "economicImpactText", "economicimpacttext", typeof(TextMeshProUGUI));
            ConnectUIComponent(rtsInterface, "deployDronesButton", "deploydronesbutton", typeof(Button));
            ConnectUIComponent(rtsInterface, "closeFireInfoButton", "closefireinfobutton", typeof(Button));
            
            // Connect fleet management
            ConnectUIComponent(rtsInterface, "fleetPanel", "fleetpanel");
            ConnectUIComponent(rtsInterface, "refreshFleetButton", "refreshfleetbutton", typeof(Button));
            ConnectUIComponent(rtsInterface, "countryFilterDropdown", "countryfilterdropdown", typeof(Dropdown));
            ConnectUIComponent(rtsInterface, "showIdleOnlyToggle", "showidleonlytoggle", typeof(Toggle));
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("✅ RTSInterface components connected!");
            }
        }
        
        private void ConnectUIComponent(object target, string fieldName, string searchName, System.Type componentType = null)
        {
            object component = null;
            
            if (componentType == typeof(Button) && foundButtons.TryGetValue(searchName, out Button button))
                component = button;
            else if (componentType == typeof(Slider) && foundSliders.TryGetValue(searchName, out Slider slider))
                component = slider;
            else if (componentType == typeof(TextMeshProUGUI) && foundTexts.TryGetValue(searchName, out TextMeshProUGUI text))
                component = text;
            else if (componentType == typeof(Image) && foundImages.TryGetValue(searchName, out Image image))
                component = image;
            else if (componentType == typeof(Toggle) && foundToggles.TryGetValue(searchName, out Toggle toggle))
                component = toggle;
            else if (componentType == typeof(Dropdown) && foundDropdowns.TryGetValue(searchName, out Dropdown dropdown))
                component = dropdown;
            else if (componentType == null && foundPanels.TryGetValue(searchName, out GameObject panel))
                component = panel;
            
            if (component != null)
            {
                SetPrivateField(target, fieldName, component);
            }
        }
        
        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;
            
            var field = target.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(target, value);
                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log($"🔗 Connected {fieldName} to {target.GetType().Name}");
                }
            }
        }
        
        [ContextMenu("❓ Debug UI Status")]
        public void DebugUIStatus()
        {
            UnityEngine.Debug.Log("=== UI CONNECTION STATUS ===");
            UnityEngine.Debug.Log($"UIManager: {(uiManager != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"TacticalHUD: {(tacticalHUD != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"RTSInterface: {(rtsInterface != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"Main Canvas: {(mainCanvas != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"Panels found: {panelsFound}");
            UnityEngine.Debug.Log($"Buttons found: {buttonsFound}");
            UnityEngine.Debug.Log($"Sliders found: {slidersFound}");
            UnityEngine.Debug.Log($"Text components found: {textComponentsFound}");
            UnityEngine.Debug.Log($"Connection Status: {connectionStatus}");
            UnityEngine.Debug.Log($"Is Connected: {isConnected}");
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(UIConnector))]
    public class UIConnectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            UIConnector connector = (UIConnector)target;
            
            EditorGUILayout.HelpBox(
                "🖥️ UI Connection Helper\n\n" +
                "This script will automatically discover all UI components in your scene " +
                "and properly connect them to the UIManager, TacticalHUD, and RTSInterface.\n\n" +
                "It will also fix UI positioning so panels appear correctly in the viewport.", 
                MessageType.Info
            );
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("🔍 Discover UI Components", GUILayout.Height(30)))
            {
                connector.DiscoverUIComponents();
            }
            
            if (GUILayout.Button("📐 Fix UI Positioning", GUILayout.Height(30)))
            {
                connector.FixUIPositioning();
            }
            
            if (GUILayout.Button("🔗 Connect All UI", GUILayout.Height(35)))
            {
                connector.ConnectAllUI();
            }
            
            if (GUILayout.Button("❓ Debug UI Status", GUILayout.Height(25)))
            {
                connector.DebugUIStatus();
            }
            
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
#endif
}

