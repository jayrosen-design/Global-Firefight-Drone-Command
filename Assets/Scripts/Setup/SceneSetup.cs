using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using GlobalFirefight.Core;
using GlobalFirefight.Data;
using GlobalFirefight.Drones;
using GlobalFirefight.UI;
using GlobalFirefight.Geospatial;
using GlobalFirefight.Systems;
using GlobalFirefight.Fire;
using GlobalFirefight.API;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace GlobalFirefight.Setup
{
    /// <summary>
    /// Comprehensive scene setup script for Global Firefight: Drone Command
    /// This script automatically creates the complete game scene hierarchy with all required components
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Scene Setup Configuration")]
        [SerializeField] private bool autoSetupOnStart = false;
        [SerializeField] private bool createPrefabs = true;
        [SerializeField] private bool setupLighting = true;
        [SerializeField] private bool setupAudio = true;
        [SerializeField] private bool setupInput = true;
        
        [Header("Asset References")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Material fireMaterial;
        [SerializeField] private Material droneMaterial;
        
        [Header("Prefab Assets")]
        [SerializeField] private GameObject droneBasePrefab;
        [SerializeField] private GameObject commandVehiclePrefab;
        [SerializeField] private GameObject fireEffectPrefab;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private GameObject smokeEffectPrefab;
        
        [Header("Audio Assets")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip fireAmbientSound;
        [SerializeField] private AudioClip droneSound;
        [SerializeField] private AudioClip uiClickSound;
        
        [Header("Progress Tracking")]
        [SerializeField] private bool showProgressBar = true;
        [SerializeField] private string progressStatus = "Ready to setup scene...";
        [Range(0f, 1f)]
        [SerializeField] private float setupProgress = 0f;
        
        // Private fields
        private GameObject gameManagerObject;
        private GameObject cameraSystemObject;
        private GameObject uiSystemObject;
        private GameObject droneSystemObject;
        private GameObject audioSystemObject;
        private GameObject effectsSystemObject;
        private Camera mainCamera;
        private Canvas mainCanvas;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                StartCoroutine(SetupCompleteScene());
            }
        }
        
        #endregion
        
        #region Public Interface
        
        [ContextMenu("Setup Complete Scene")]
        public void SetupScene()
        {
            StartCoroutine(SetupCompleteScene());
        }
        
        [ContextMenu("Create Essential GameObjects")]
        public void CreateEssentialGameObjects()
        {
            StartCoroutine(CreateCoreHierarchy());
        }
        
        [ContextMenu("Setup Lighting & Post-Processing")]
        public void SetupLightingAndPostProcessing()
        {
            StartCoroutine(ConfigureLightingSystem());
        }
        
        [ContextMenu("Create UI System")]
        public void SetupUISystem()
        {
            StartCoroutine(CreateUIHierarchy());
        }
        
        [ContextMenu("Validate Scene Setup")]
        public void ValidateSetup()
        {
            ValidateSceneComponents();
        }
        
        #endregion
        
        #region Main Setup Coroutine
        
        private IEnumerator SetupCompleteScene()
        {
            UnityEngine.Debug.Log("=== Starting Global Firefight Scene Setup ===");
            setupProgress = 0f;
            
            // Step 1: Create core hierarchy
            progressStatus = "Creating core game hierarchy...";
            yield return StartCoroutine(CreateCoreHierarchy());
            setupProgress = 0.15f;
            
            // Step 2: Setup camera system
            progressStatus = "Setting up camera system...";
            yield return StartCoroutine(SetupCameraSystem());
            setupProgress = 0.25f;
            
            // Step 3: Create UI system
            progressStatus = "Creating UI system...";
            yield return StartCoroutine(CreateUIHierarchy());
            setupProgress = 0.40f;
            
            // Step 4: Setup drone system
            progressStatus = "Setting up drone fleet system...";
            yield return StartCoroutine(SetupDroneSystem());
            setupProgress = 0.55f;
            
            // Step 5: Configure lighting
            if (setupLighting)
            {
                progressStatus = "Configuring lighting and post-processing...";
                yield return StartCoroutine(ConfigureLightingSystem());
                setupProgress = 0.70f;
            }
            
            // Step 6: Setup audio system
            if (setupAudio)
            {
                progressStatus = "Setting up audio system...";
                yield return StartCoroutine(SetupAudioSystem());
                setupProgress = 0.80f;
            }
            
            // Step 7: Create effects system
            progressStatus = "Creating effects system...";
            yield return StartCoroutine(SetupEffectsSystem());
            setupProgress = 0.90f;
            
            // Step 8: Final configuration
            progressStatus = "Finalizing scene configuration...";
            yield return StartCoroutine(FinalizeScene());
            setupProgress = 1.0f;
            
            progressStatus = "Scene setup complete!";
            UnityEngine.Debug.Log("=== Scene Setup Complete! ===");
            
            // Validate the setup
            ValidateSceneComponents();
        }
        
        #endregion
        
        #region Core Hierarchy Creation
        
        private IEnumerator CreateCoreHierarchy()
        {
            UnityEngine.Debug.Log("Creating core game hierarchy...");
            
            // 1. Game Manager
            gameManagerObject = new GameObject("=== GAME MANAGER ===");
            gameManagerObject.AddComponent<GameManager>();
            gameManagerObject.AddComponent<FireSimulation>();
            gameManagerObject.AddComponent<ScoringSystem>();
            
            yield return null;
            
            // 2. API Systems
            GameObject apiSystemObject = new GameObject("=== API SYSTEMS ===");
            apiSystemObject.transform.SetParent(gameManagerObject.transform);
            
            GameObject nasaAPIObject = new GameObject("NASA API Manager");
            nasaAPIObject.transform.SetParent(apiSystemObject.transform);
            nasaAPIObject.AddComponent<NASAAPIManager>();
            
            GameObject googleAPIObject = new GameObject("Google Maps API Manager");
            googleAPIObject.transform.SetParent(apiSystemObject.transform);
            googleAPIObject.AddComponent<GoogleMapsAPIManager>();
            
            yield return null;
            
            // 3. Geospatial System
            GameObject geospatialObject = new GameObject("=== GEOSPATIAL SYSTEM ===");
            geospatialObject.AddComponent<GeospatialManager>();
            
            // Earth representation
            GameObject earthObject = new GameObject("Earth");
            earthObject.transform.SetParent(geospatialObject.transform);
            
            // Create basic sphere for Earth visualization
            GameObject earthSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earthSphere.name = "Earth Sphere";
            earthSphere.transform.SetParent(earthObject.transform);
            earthSphere.transform.localScale = Vector3.one * 1000f; // Large Earth representation
            
            // Remove collider from earth sphere
            DestroyImmediate(earthSphere.GetComponent<Collider>());
            
            yield return null;
            
            // 4. Create terrain layers
            GameObject terrainLayer = new GameObject("Terrain Layer");
            terrainLayer.transform.SetParent(earthObject.transform);
            
            GameObject waterLayer = new GameObject("Water Layer");
            waterLayer.transform.SetParent(earthObject.transform);
            
            GameObject atmosphereLayer = new GameObject("Atmosphere Layer");
            atmosphereLayer.transform.SetParent(earthObject.transform);
            
            yield return null;
            
            UnityEngine.Debug.Log("Core hierarchy created successfully");
        }
        
        #endregion
        
        #region Camera System Setup
        
        private IEnumerator SetupCameraSystem()
        {
            UnityEngine.Debug.Log("Setting up camera system...");
            
            // Create camera system root
            cameraSystemObject = new GameObject("=== CAMERA SYSTEM ===");
            
            // Main Camera
            GameObject mainCameraObject = new GameObject("Main Camera");
            mainCameraObject.transform.SetParent(cameraSystemObject.transform);
            mainCameraObject.tag = "MainCamera";
            
            mainCamera = mainCameraObject.AddComponent<Camera>();
            mainCamera.fieldOfView = 60f;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 10000f;
            mainCamera.transform.position = new Vector3(0, 500, -1000);
            mainCamera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            
            // Add camera controller
            mainCameraObject.AddComponent<CameraController>();
            
            // Add audio listener
            mainCameraObject.AddComponent<AudioListener>();
            
            yield return null;
            
            // Add Universal Render Pipeline components
            var cameraData = mainCameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = CameraRenderType.Base;
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            
            yield return null;
            
            // Drone Camera (for tactical view)
            GameObject droneCameraObject = new GameObject("Drone Camera");
            droneCameraObject.transform.SetParent(cameraSystemObject.transform);
            
            Camera droneCamera = droneCameraObject.AddComponent<Camera>();
            droneCamera.fieldOfView = 75f;
            droneCamera.nearClipPlane = 0.1f;
            droneCamera.farClipPlane = 5000f;
            droneCamera.enabled = false; // Disabled by default
            
            var droneCameraData = droneCameraObject.AddComponent<UniversalAdditionalCameraData>();
            droneCameraData.renderType = CameraRenderType.Base;
            
            yield return null;
            
            // Mini-map Camera
            GameObject miniMapCameraObject = new GameObject("MiniMap Camera");
            miniMapCameraObject.transform.SetParent(cameraSystemObject.transform);
            
            Camera miniMapCamera = miniMapCameraObject.AddComponent<Camera>();
            miniMapCamera.orthographic = true;
            miniMapCamera.orthographicSize = 2000f;
            miniMapCamera.transform.position = Vector3.up * 5000f;
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            miniMapCamera.cullingMask = LayerMask.GetMask("Default", "Terrain", "Drones");
            
            // Create render texture for mini-map
            RenderTexture miniMapTexture = new RenderTexture(256, 256, 16);
            miniMapTexture.name = "MiniMapTexture";
            miniMapCamera.targetTexture = miniMapTexture;
            
            yield return null;
            
            UnityEngine.Debug.Log("Camera system setup complete");
        }
        
        #endregion
        
        #region UI System Creation
        
        private IEnumerator CreateUIHierarchy()
        {
            UnityEngine.Debug.Log("Creating UI system...");
            
            // Create UI system root
            uiSystemObject = new GameObject("=== UI SYSTEM ===");
            
            // Add UI Manager
            uiSystemObject.AddComponent<UIManager>();
            
            yield return null;
            
            // Main Canvas
            GameObject mainCanvasObject = new GameObject("Main Canvas");
            mainCanvasObject.transform.SetParent(uiSystemObject.transform);
            
            mainCanvas = mainCanvasObject.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 0;
            
            CanvasScaler canvasScaler = mainCanvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            mainCanvasObject.AddComponent<GraphicRaycaster>();
            
            yield return null;
            
            // RTS Interface
            GameObject rtsInterfaceObject = new GameObject("RTS Interface");
            rtsInterfaceObject.transform.SetParent(mainCanvasObject.transform);
            rtsInterfaceObject.AddComponent<RTSInterface>();
            
            yield return StartCoroutine(CreateRTSUIElements(rtsInterfaceObject));
            
            // Tactical HUD
            GameObject tacticalHUDObject = new GameObject("Tactical HUD");
            tacticalHUDObject.transform.SetParent(mainCanvasObject.transform);
            tacticalHUDObject.AddComponent<TacticalHUD>();
            
            yield return StartCoroutine(CreateTacticalUIElements(tacticalHUDObject));
            
            // Event System
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(uiSystemObject.transform);
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            yield return null;
            
            UnityEngine.Debug.Log("UI system created successfully");
        }
        
        private IEnumerator CreateRTSUIElements(GameObject parent)
        {
            // RTS Panel
            GameObject rtsPanel = CreateUIPanel("RTS Panel", parent);
            
            // Globe Controls
            GameObject globeControls = CreateUIPanel("Globe Controls", rtsPanel);
            SetAnchoredPosition(globeControls, AnchorPresets.TopLeft, new Vector2(10, -10), new Vector2(300, 120));
            
            CreateUIButton("Reset View", globeControls);
            CreateUIButton("Satellite View", globeControls);
            CreateUIToggle("Show Fires", globeControls);
            
            yield return null;
            
            // Fleet Panel
            GameObject fleetPanel = CreateUIPanel("Fleet Panel", rtsPanel);
            SetAnchoredPosition(fleetPanel, AnchorPresets.MiddleLeft, new Vector2(10, 0), new Vector2(350, 400));
            
            CreateUIText("Fleet Management", fleetPanel, 18, FontStyles.Bold);
            GameObject fleetList = CreateUIScrollView("Fleet List", fleetPanel);
            
            yield return null;
            
            // Fire Info Panel
            GameObject fireInfoPanel = CreateUIPanel("Fire Info Panel", rtsPanel);
            SetAnchoredPosition(fireInfoPanel, AnchorPresets.TopRight, new Vector2(-10, -10), new Vector2(300, 200));
            fireInfoPanel.SetActive(false);
            
            CreateUIText("Fire Information", fireInfoPanel, 16, FontStyles.Bold);
            CreateUIButton("Deploy Drones", fireInfoPanel);
            
            yield return null;
            
            // Status Bar
            GameObject statusBar = CreateUIPanel("Status Bar", rtsPanel);
            SetAnchoredPosition(statusBar, AnchorPresets.BottomStretch, Vector2.zero, new Vector2(0, 60));
            
            CreateUIText("Game Time: 00:00", statusBar, 14);
            CreateUIText("Score: 0", statusBar, 14);
            CreateUIText("Budget: $1,000,000", statusBar, 14);
            
            yield return null;
        }
        
        private IEnumerator CreateTacticalUIElements(GameObject parent)
        {
            // Tactical Panel
            GameObject tacticalPanel = CreateUIPanel("Tactical Panel", parent);
            tacticalPanel.SetActive(false);
            
            // HUD Elements
            GameObject hudPanel = CreateUIPanel("HUD Panel", tacticalPanel);
            
            // Flight Instruments
            GameObject instruments = CreateUIPanel("Flight Instruments", hudPanel);
            SetAnchoredPosition(instruments, AnchorPresets.BottomLeft, new Vector2(10, 10), new Vector2(300, 150));
            
            CreateUIText("Speed: 0 m/s", instruments, 12);
            CreateUIText("Altitude: 0 m", instruments, 12);
            CreateUIText("Heading: 0°", instruments, 12);
            
            yield return null;
            
            // Drone Status
            GameObject droneStatus = CreateUIPanel("Drone Status", hudPanel);
            SetAnchoredPosition(droneStatus, AnchorPresets.TopLeft, new Vector2(10, -10), new Vector2(250, 120));
            
            CreateUIText("Drone: None", droneStatus, 14, FontStyles.Bold);
            CreateUISlider("Battery", droneStatus);
            CreateUISlider("Payload", droneStatus);
            
            yield return null;
            
            // Crosshair
            GameObject crosshair = CreateUICrosshair("Crosshair", hudPanel);
            SetAnchoredPosition(crosshair, AnchorPresets.MiddleCenter, Vector2.zero, new Vector2(50, 50));
            
            // Mission Panel
            GameObject missionPanel = CreateUIPanel("Mission Panel", hudPanel);
            SetAnchoredPosition(missionPanel, AnchorPresets.TopRight, new Vector2(-10, -10), new Vector2(300, 150));
            missionPanel.SetActive(false);
            
            CreateUIText("Mission: Fire Suppression", missionPanel, 14, FontStyles.Bold);
            CreateUISlider("Progress", missionPanel);
            
            yield return null;
        }
        
        #endregion
        
        #region Drone System Setup
        
        private IEnumerator SetupDroneSystem()
        {
            UnityEngine.Debug.Log("Setting up drone system...");
            
            // Create drone system root
            droneSystemObject = new GameObject("=== DRONE SYSTEM ===");
            
            // Add fleet manager
            droneSystemObject.AddComponent<DroneFleetManager>();
            
            yield return null;
            
            // Drone fleets container
            GameObject droneFleets = new GameObject("Drone Fleets");
            droneFleets.transform.SetParent(droneSystemObject.transform);
            
            // Create example drones for each country
            yield return StartCoroutine(CreateExampleDrones(droneFleets));
            
            // Command vehicles container
            GameObject commandVehicles = new GameObject("Command Vehicles");
            commandVehicles.transform.SetParent(droneSystemObject.transform);
            
            yield return StartCoroutine(CreateExampleCommandVehicles(commandVehicles));
            
            UnityEngine.Debug.Log("Drone system setup complete");
        }
        
        private IEnumerator CreateExampleDrones(GameObject parent)
        {
            string[] countries = { "USA", "Canada", "Australia", "Brazil", "China" };
            
            for (int i = 0; i < countries.Length; i++)
            {
                GameObject countryFleet = new GameObject($"{countries[i]} Fleet");
                countryFleet.transform.SetParent(parent.transform);
                
                // Create 3 example drones per country
                for (int j = 0; j < 3; j++)
                {
                    GameObject drone = CreateDroneGameObject($"{countries[i]}_Drone_{j + 1}", countryFleet);
                    
                    // Position drones in formation
                    float angle = (360f / 3f) * j * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(
                        Mathf.Cos(angle) * 100f,
                        200f + i * 50f,
                        Mathf.Sin(angle) * 100f
                    );
                    drone.transform.position = position;
                }
                
                yield return null;
            }
        }
        
        private IEnumerator CreateExampleCommandVehicles(GameObject parent)
        {
            string[] countries = { "USA", "Canada", "Australia", "Brazil", "China" };
            
            for (int i = 0; i < countries.Length; i++)
            {
                GameObject commandVehicle = CreateCommandVehicleGameObject($"{countries[i]}_CommandVehicle", parent);
                
                // Position command vehicles globally
                Vector3 position = new Vector3(i * 500f - 1000f, 150f, 0f);
                commandVehicle.transform.position = position;
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Lighting System Setup
        
        private IEnumerator ConfigureLightingSystem()
        {
            UnityEngine.Debug.Log("Configuring lighting system...");
            
            // Create lighting system root
            GameObject lightingSystem = new GameObject("=== LIGHTING SYSTEM ===");
            
            // Sun (Directional Light)
            GameObject sunObject = new GameObject("Sun");
            sunObject.transform.SetParent(lightingSystem.transform);
            
            Light sunLight = sunObject.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.95f, 0.8f);
            sunLight.intensity = 1.5f;
            sunLight.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            
            yield return null;
            
            // Ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.3f;
            
            // Fog settings for atmosphere
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0001f;
            
            yield return null;
            
            // Setup skybox
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
            }
            
            // Post-processing volume
            GameObject postProcessVolume = new GameObject("Global Post Process Volume");
            postProcessVolume.transform.SetParent(lightingSystem.transform);
            
            var volume = postProcessVolume.AddComponent<Volume>();
            volume.isGlobal = true;
            
            yield return null;
            
            UnityEngine.Debug.Log("Lighting system configured");
        }
        
        #endregion
        
        #region Audio System Setup
        
        private IEnumerator SetupAudioSystem()
        {
            UnityEngine.Debug.Log("Setting up audio system...");
            
            // Create audio system root
            audioSystemObject = new GameObject("=== AUDIO SYSTEM ===");
            
            // Background Music
            GameObject musicObject = new GameObject("Background Music");
            musicObject.transform.SetParent(audioSystemObject.transform);
            
            AudioSource musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0.3f;
            musicSource.playOnAwake = true;
            
            yield return null;
            
            // Ambient Fire Sounds
            GameObject fireAmbientObject = new GameObject("Fire Ambient");
            fireAmbientObject.transform.SetParent(audioSystemObject.transform);
            
            AudioSource fireAmbientSource = fireAmbientObject.AddComponent<AudioSource>();
            fireAmbientSource.clip = fireAmbientSound;
            fireAmbientSource.loop = true;
            fireAmbientSource.volume = 0.2f;
            fireAmbientSource.spatialBlend = 1f; // 3D audio
            fireAmbientSource.rolloffMode = AudioRolloffMode.Logarithmic;
            
            yield return null;
            
            // UI Audio Source
            GameObject uiAudioObject = new GameObject("UI Audio");
            uiAudioObject.transform.SetParent(audioSystemObject.transform);
            
            AudioSource uiAudioSource = uiAudioObject.AddComponent<AudioSource>();
            uiAudioSource.clip = uiClickSound;
            uiAudioSource.playOnAwake = false;
            
            yield return null;
            
            UnityEngine.Debug.Log("Audio system setup complete");
        }
        
        #endregion
        
        #region Effects System Setup
        
        private IEnumerator SetupEffectsSystem()
        {
            UnityEngine.Debug.Log("Setting up effects system...");
            
            // Create effects system root
            effectsSystemObject = new GameObject("=== EFFECTS SYSTEM ===");
            
            // Fire Effects Pool
            GameObject fireEffectsPool = new GameObject("Fire Effects Pool");
            fireEffectsPool.transform.SetParent(effectsSystemObject.transform);
            
            // Create pooled fire effects
            for (int i = 0; i < 20; i++)
            {
                if (fireEffectPrefab != null)
                {
                    GameObject fireEffect = Instantiate(fireEffectPrefab, fireEffectsPool.transform);
                    fireEffect.name = $"Fire Effect {i + 1}";
                    fireEffect.SetActive(false);
                }
                
                if (i % 5 == 0) yield return null; // Yield every 5 objects
            }
            
            // Explosion Effects Pool
            GameObject explosionEffectsPool = new GameObject("Explosion Effects Pool");
            explosionEffectsPool.transform.SetParent(effectsSystemObject.transform);
            
            for (int i = 0; i < 10; i++)
            {
                if (explosionEffectPrefab != null)
                {
                    GameObject explosionEffect = Instantiate(explosionEffectPrefab, explosionEffectsPool.transform);
                    explosionEffect.name = $"Explosion Effect {i + 1}";
                    explosionEffect.SetActive(false);
                }
            }
            
            yield return null;
            
            // Smoke Effects Pool
            GameObject smokeEffectsPool = new GameObject("Smoke Effects Pool");
            smokeEffectsPool.transform.SetParent(effectsSystemObject.transform);
            
            for (int i = 0; i < 30; i++)
            {
                if (smokeEffectPrefab != null)
                {
                    GameObject smokeEffect = Instantiate(smokeEffectPrefab, smokeEffectsPool.transform);
                    smokeEffect.name = $"Smoke Effect {i + 1}";
                    smokeEffect.SetActive(false);
                }
                
                if (i % 10 == 0) yield return null;
            }
            
            UnityEngine.Debug.Log("Effects system setup complete");
        }
        
        #endregion
        
        #region Scene Finalization
        
        private IEnumerator FinalizeScene()
        {
            UnityEngine.Debug.Log("Finalizing scene configuration...");
            
            // Setup layers
            SetupLayers();
            yield return null;
            
            // Configure physics settings
            ConfigurePhysics();
            yield return null;
            
            // Setup input system
            if (setupInput)
            {
                SetupInputSystem();
                yield return null;
            }
            
            // Initialize systems
            InitializeSystems();
            yield return null;
            
            // Create example fires
            yield return StartCoroutine(CreateExampleFires());
            
            // Save scene if in editor
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                UnityEngine.Debug.Log("Scene marked as dirty for saving");
            }
            #endif
            
            UnityEngine.Debug.Log("Scene finalization complete");
        }
        
        private void SetupLayers()
        {
            // Note: Layer setup requires manual configuration in Project Settings
            UnityEngine.Debug.Log("Layer setup reminder: Configure these layers in Project Settings:");
            UnityEngine.Debug.Log("- Layer 8: Terrain");
            UnityEngine.Debug.Log("- Layer 9: Water");
            UnityEngine.Debug.Log("- Layer 10: Drones");
            UnityEngine.Debug.Log("- Layer 11: Fires");
            UnityEngine.Debug.Log("- Layer 12: UI");
        }
        
        private void ConfigurePhysics()
        {
            Physics.gravity = new Vector3(0, -9.81f, 0);
            Physics.defaultContactOffset = 0.01f;
            Physics.sleepThreshold = 0.005f;
        }
        
        private void SetupInputSystem()
        {
            UnityEngine.Debug.Log("Input System setup reminder: Configure Input Actions Asset");
            // Input system configuration would be done through the Input Actions asset
        }
        
        private void InitializeSystems()
        {
            // Initialize game manager
            if (gameManagerObject != null)
            {
                var gameManager = gameManagerObject.GetComponent<GameManager>();
                if (gameManager != null)
                {
                    // GameManager will initialize itself
                    UnityEngine.Debug.Log("Game Manager ready for initialization");
                }
            }
            
            // Initialize UI Manager
            if (uiSystemObject != null)
            {
                var uiManager = uiSystemObject.GetComponent<UIManager>();
                if (uiManager != null)
                {
                    // UI Manager will initialize itself
                    UnityEngine.Debug.Log("UI Manager ready for initialization");
                }
            }
        }
        
        private IEnumerator CreateExampleFires()
        {
            UnityEngine.Debug.Log("Creating example fires for testing...");
            
            // Create example fire locations around the world
            Vector3[] fireLocations = {
                new Vector3(1000, 0, 1000),   // California
                new Vector3(-500, 0, 800),    // Australia
                new Vector3(200, 0, -600),    // Brazil
                new Vector3(-800, 0, 1200),   // Canada
                new Vector3(1500, 0, 300)     // China
            };
            
            for (int i = 0; i < fireLocations.Length; i++)
            {
                GameObject fireObject = new GameObject($"Example Fire {i + 1}");
                fireObject.transform.position = fireLocations[i];
                
                // Add fire visualization (particle system)
                ParticleSystem fireParticles = fireObject.AddComponent<ParticleSystem>();
                var main = fireParticles.main;
                main.startColor = Color.red;
                main.startLifetime = 2f;
                main.startSpeed = 5f;
                main.maxParticles = 100;
                
                var emission = fireParticles.emission;
                emission.rateOverTime = 50f;
                
                yield return null;
            }
        }
        
        #endregion
        
        #region UI Creation Helpers
        
        private GameObject CreateUIPanel(string name, GameObject parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);
            
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.5f);
            
            return panel;
        }
        
        private GameObject CreateUIButton(string text, GameObject parent)
        {
            GameObject button = new GameObject($"Button - {text}");
            button.transform.SetParent(parent.transform, false);
            
            RectTransform rectTransform = button.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 30);
            
            Image image = button.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            Button buttonComponent = button.AddComponent<Button>();
            
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(button.transform, false);
            
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return button;
        }
        
        private GameObject CreateUIText(string text, GameObject parent, int fontSize = 14, FontStyles fontStyle = FontStyles.Normal)
        {
            GameObject textObject = new GameObject($"Text - {text}");
            textObject.transform.SetParent(parent.transform, false);
            
            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 30);
            
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.color = Color.white;
            
            return textObject;
        }
        
        private GameObject CreateUISlider(string name, GameObject parent)
        {
            GameObject slider = new GameObject($"Slider - {name}");
            slider.transform.SetParent(parent.transform, false);
            
            RectTransform rectTransform = slider.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(150, 20);
            
            Slider sliderComponent = slider.AddComponent<Slider>();
            sliderComponent.minValue = 0f;
            sliderComponent.maxValue = 1f;
            sliderComponent.value = 1f;
            
            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(slider.transform, false);
            background.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform, false);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            fill.AddComponent<Image>().color = Color.green;
            
            sliderComponent.fillRect = fill.GetComponent<RectTransform>();
            
            return slider;
        }
        
        private GameObject CreateUIToggle(string text, GameObject parent)
        {
            GameObject toggle = new GameObject($"Toggle - {text}");
            toggle.transform.SetParent(parent.transform, false);
            
            RectTransform rectTransform = toggle.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 20);
            
            Toggle toggleComponent = toggle.AddComponent<Toggle>();
            
            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toggle.transform, false);
            background.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            checkmark.AddComponent<Image>().color = Color.green;
            
            toggleComponent.graphic = checkmark.GetComponent<Image>();
            
            // Label
            CreateUIText(text, toggle, 12);
            
            return toggle;
        }
        
        private GameObject CreateUIScrollView(string name, GameObject parent)
        {
            GameObject scrollView = new GameObject(name);
            scrollView.transform.SetParent(parent.transform, false);
            
            RectTransform rectTransform = scrollView.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 200);
            
            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            viewport.AddComponent<RectTransform>();
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>();
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            
            scrollRect.content = contentRect;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            
            return scrollView;
        }
        
        private GameObject CreateUICrosshair(string name, GameObject parent)
        {
            GameObject crosshair = new GameObject(name);
            crosshair.transform.SetParent(parent.transform, false);
            
            RectTransform rectTransform = crosshair.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(50, 50);
            
            Image image = crosshair.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.8f);
            
            return crosshair;
        }
        
        private void SetAnchoredPosition(GameObject uiObject, AnchorPresets preset, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
            
            switch (preset)
            {
                case AnchorPresets.TopLeft:
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 1);
                    break;
                case AnchorPresets.TopRight:
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 1);
                    break;
                case AnchorPresets.BottomLeft:
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    rectTransform.pivot = new Vector2(0, 0);
                    break;
                case AnchorPresets.BottomStretch:
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPresets.MiddleLeft:
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    rectTransform.pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPresets.MiddleCenter:
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
            
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }
        
        #endregion
        
        #region GameObject Creation Helpers
        
        private GameObject CreateDroneGameObject(string name, GameObject parent)
        {
            GameObject drone = new GameObject(name);
            drone.transform.SetParent(parent.transform);
            drone.layer = LayerMask.NameToLayer("Drones");
            
            // Add basic components
            drone.AddComponent<DroneController>();
            drone.AddComponent<DroneAI>();
            
            // Add rigidbody for physics
            Rigidbody rb = drone.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 1f;
            
            // Add collider
            BoxCollider collider = drone.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 1f, 2f);
            
            // Add visual representation (basic cube for now)
            GameObject droneModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            droneModel.name = "Drone Model";
            droneModel.transform.SetParent(drone.transform);
            droneModel.transform.localScale = new Vector3(2f, 0.5f, 2f);
            
            // Remove collider from model (parent has the collider)
            DestroyImmediate(droneModel.GetComponent<Collider>());
            
            return drone;
        }
        
        private GameObject CreateCommandVehicleGameObject(string name, GameObject parent)
        {
            GameObject commandVehicle = new GameObject(name);
            commandVehicle.transform.SetParent(parent.transform);
            
            // Add visual representation
            GameObject cvModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cvModel.name = "Command Vehicle Model";
            cvModel.transform.SetParent(commandVehicle.transform);
            cvModel.transform.localScale = new Vector3(5f, 2f, 5f);
            
            return commandVehicle;
        }
        
        #endregion
        
        #region Validation
        
        private void ValidateSceneComponents()
        {
            UnityEngine.Debug.Log("=== Scene Validation Report ===");
            
            // Check for required managers
            ValidateComponent<GameManager>("Game Manager");
            ValidateComponent<UIManager>("UI Manager");
            ValidateComponent<DroneFleetManager>("Drone Fleet Manager");
            ValidateComponent<CameraController>("Camera Controller");
            ValidateComponent<FireSimulation>("Fire Simulation");
            ValidateComponent<ScoringSystem>("Scoring System");
            ValidateComponent<GeospatialManager>("Geospatial Manager");
            
            // Check for camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
                UnityEngine.Debug.Log("✓ Main Camera found");
            else
                UnityEngine.Debug.LogWarning("✗ Main Camera not found");
            
            // Check for Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
                UnityEngine.Debug.Log("✓ UI Canvas found");
            else
                UnityEngine.Debug.LogWarning("✗ UI Canvas not found");
            
            // Check for Event System
            UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
                UnityEngine.Debug.Log("✓ Event System found");
            else
                UnityEngine.Debug.LogWarning("✗ Event System not found");
            
            UnityEngine.Debug.Log("=== Validation Complete ===");
        }
        
        private void ValidateComponent<T>(string componentName) where T : Component
        {
            T component = FindFirstObjectByType<T>();
            if (component != null)
                UnityEngine.Debug.Log($"✓ {componentName} found");
            else
                UnityEngine.Debug.LogWarning($"✗ {componentName} not found");
        }
        
        #endregion
        
        #region Editor GUI
        
        #if UNITY_EDITOR
        [CustomEditor(typeof(SceneSetup))]
        public class SceneSetupEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                
                SceneSetup sceneSetup = (SceneSetup)target;
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Setup Complete Scene", GUILayout.Height(30)))
                {
                    sceneSetup.SetupScene();
                }
                
                GUILayout.Space(5);
                
                if (GUILayout.Button("Create Essential GameObjects"))
                {
                    sceneSetup.CreateEssentialGameObjects();
                }
                
                if (GUILayout.Button("Setup Lighting & Post-Processing"))
                {
                    sceneSetup.SetupLightingAndPostProcessing();
                }
                
                if (GUILayout.Button("Create UI System"))
                {
                    sceneSetup.SetupUISystem();
                }
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Validate Scene Setup"))
                {
                    sceneSetup.ValidateSetup();
                }
                
                // Progress bar
                if (sceneSetup.showProgressBar)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Setup Progress:");
                    EditorGUILayout.Slider(sceneSetup.setupProgress, 0f, 1f);
                    EditorGUILayout.LabelField(sceneSetup.progressStatus);
                }
            }
        }
        #endif
        
        #endregion
    }
    
    #region Supporting Enums
    
    public enum AnchorPresets
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomStretch,
        MiddleLeft,
        MiddleCenter
    }
    
    #endregion
}

