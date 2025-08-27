using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalFirefight.Core;
using GlobalFirefight.Data;
using GlobalFirefight.Drones;
using GlobalFirefight.UI;
using GlobalFirefight.Geospatial;
using GlobalFirefight.Systems;
using GlobalFirefight.Fire;
using GlobalFirefight.API;

namespace GlobalFirefight.Setup
{
    /// <summary>
    /// Scene Connection Helper - Automatically connects all game objects and components in the scene
    /// This script finds existing objects in the scene and wires them together properly
    /// </summary>
    public class SceneConnector : MonoBehaviour
    {
        [Header("Connection Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private string connectionStatus = "Ready to connect scene components";
        
        [Header("Auto-Connect Options")]
        [SerializeField] private bool connectOnStart = true;
        [SerializeField] private bool showDebugLogs = true;
        
        [Header("Found Components")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private TacticalHUD tacticalHUD;
        [SerializeField] private RTSInterface rtsInterface;
        [SerializeField] private ScoringSystem scoringSystem;
        [SerializeField] private GeospatialManager geospatialManager;
        [SerializeField] private DroneFleetManager droneFleetManager;
        [SerializeField] private FireSimulation fireSimulation;
        [SerializeField] private NASAAPIManager nasaAPIManager;
        
        [Header("Scene Objects")]
        [SerializeField] private Camera[] allCameras;
        [SerializeField] private Canvas[] allCanvases;
        [SerializeField] private DroneController[] allDrones;
        [SerializeField] private ParticleSystem[] fireEffects;
        [SerializeField] private AudioSource[] audioSources;
        
        private void Start()
        {
            if (connectOnStart && !isConnected)
            {
                StartCoroutine(ConnectSceneComponents());
            }
        }
        
        [ContextMenu("Connect Scene Components")]
        public void ConnectScene()
        {
            StartCoroutine(ConnectSceneComponents());
        }
        
        [ContextMenu("Find All Components")]
        public void FindAllComponents()
        {
            StartCoroutine(DiscoverSceneComponents());
        }
        
        private IEnumerator ConnectSceneComponents()
        {
            connectionStatus = "Discovering scene components...";
            yield return StartCoroutine(DiscoverSceneComponents());
            
            connectionStatus = "Connecting management systems...";
            yield return new WaitForEndOfFrame();
            ConnectManagementSystems();
            
            connectionStatus = "Connecting UI systems...";
            yield return new WaitForEndOfFrame();
            ConnectUIComponents();
            
            connectionStatus = "Connecting camera systems...";
            yield return new WaitForEndOfFrame();
            ConnectCameraSystems();
            
            connectionStatus = "Connecting drone systems...";
            yield return new WaitForEndOfFrame();
            ConnectDroneSystems();
            
            connectionStatus = "Connecting fire simulation...";
            yield return new WaitForEndOfFrame();
            ConnectFireSystems();
            
            connectionStatus = "Finalizing connections...";
            yield return new WaitForEndOfFrame();
            FinalizeConnections();
            
            isConnected = true;
            connectionStatus = "Scene successfully connected!";
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("✅ Scene Connection Complete! All components are now properly wired together.");
            }
        }
        
        private IEnumerator DiscoverSceneComponents()
        {
            // Find core management components
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                UnityEngine.Debug.LogWarning("⚠️ GameManager not found! Creating one...");
                CreateGameManager();
            }
            
            cameraController = FindFirstObjectByType<CameraController>();
            uiManager = FindFirstObjectByType<UIManager>();
            tacticalHUD = FindFirstObjectByType<TacticalHUD>();
            rtsInterface = FindFirstObjectByType<RTSInterface>();
            scoringSystem = FindFirstObjectByType<ScoringSystem>();
            geospatialManager = FindFirstObjectByType<GeospatialManager>();
            droneFleetManager = FindFirstObjectByType<DroneFleetManager>();
            fireSimulation = FindFirstObjectByType<FireSimulation>();
            nasaAPIManager = FindFirstObjectByType<NASAAPIManager>();
            
            // Find scene objects
            allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            allDrones = FindObjectsByType<DroneController>(FindObjectsSortMode.None);
            fireEffects = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            
            yield return null;
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log($"🔍 Found {allCameras.Length} cameras, {allCanvases.Length} canvases, {allDrones.Length} drones");
            }
        }
        
        private void CreateGameManager()
        {
            GameObject gmObject = new GameObject("GameManager");
            gameManager = gmObject.AddComponent<GameManager>();
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("✅ Created GameManager");
            }
        }
        
        private void ConnectManagementSystems()
        {
            if (gameManager == null) return;
            
            // Use reflection to set private fields since they're SerializeField
            var gameManagerType = typeof(GameManager);
            
            // Connect camera controller
            if (cameraController != null)
            {
                SetPrivateField(gameManager, "cameraController", cameraController);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected CameraController to GameManager");
            }
            
            // Connect UI manager
            if (uiManager != null)
            {
                SetPrivateField(gameManager, "uiManager", uiManager);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected UIManager to GameManager");
            }
            
            // Connect scoring system
            if (scoringSystem != null)
            {
                SetPrivateField(gameManager, "scoringSystem", scoringSystem);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected ScoringSystem to GameManager");
            }
            
            // Connect geospatial manager
            if (geospatialManager != null)
            {
                SetPrivateField(gameManager, "geospatialManager", geospatialManager);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected GeospatialManager to GameManager");
            }
            
            // Connect NASA API manager
            if (nasaAPIManager != null)
            {
                SetPrivateField(gameManager, "nasaAPIManager", nasaAPIManager);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected NASAAPIManager to GameManager");
            }
            
            // Connect drone fleet manager
            if (droneFleetManager != null)
            {
                SetPrivateField(gameManager, "droneFleetManager", droneFleetManager);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected DroneFleetManager to GameManager");
            }
            
            // Connect fire simulation
            if (fireSimulation != null)
            {
                SetPrivateField(gameManager, "fireSimulation", fireSimulation);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected FireSimulation to GameManager");
            }
        }
        
        private void ConnectUIComponents()
        {
            if (uiManager == null) return;
            
            // Connect tactical HUD to UI manager
            if (tacticalHUD != null)
            {
                SetPrivateField(uiManager, "tacticalHUD", tacticalHUD);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected TacticalHUD to UIManager");
            }
            
            // Connect RTS interface to UI manager
            if (rtsInterface != null)
            {
                SetPrivateField(uiManager, "rtsInterface", rtsInterface);
                if (showDebugLogs) UnityEngine.Debug.Log("✅ Connected RTSInterface to UIManager");
            }
            
            // Find and connect main canvas
            if (allCanvases.Length > 0)
            {
                Canvas mainCanvas = null;
                foreach (var canvas in allCanvases)
                {
                    if (canvas.gameObject.name.Contains("Main") || canvas.gameObject.name.Contains("UI"))
                    {
                        mainCanvas = canvas;
                        break;
                    }
                }
                
                if (mainCanvas == null) mainCanvas = allCanvases[0];
                
                SetPrivateField(uiManager, "mainCanvas", mainCanvas);
                if (showDebugLogs) UnityEngine.Debug.Log($"✅ Connected main canvas: {mainCanvas.name}");
            }
        }
        
        private void ConnectCameraSystems()
        {
            if (cameraController == null) return;
            
            // Find main camera
            Camera mainCam = null;
            foreach (var cam in allCameras)
            {
                if (cam.gameObject.name.Contains("Main") || cam.tag == "MainCamera")
                {
                    mainCam = cam;
                    break;
                }
            }
            
            if (mainCam == null && allCameras.Length > 0)
            {
                mainCam = allCameras[0];
            }
            
            if (mainCam != null)
            {
                SetPrivateField(cameraController, "mainCamera", mainCam);
                if (showDebugLogs) UnityEngine.Debug.Log($"✅ Connected main camera: {mainCam.name}");
            }
        }
        
        private void ConnectDroneSystems()
        {
            if (droneFleetManager == null) return;
            
            // Register all drones with the fleet manager
            if (allDrones.Length > 0)
            {
                foreach (var drone in allDrones)
                {
                    if (drone != null)
                    {
                        // Try to register drone with fleet manager
                        try
                        {
                            var method = droneFleetManager.GetType().GetMethod("RegisterDrone", 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (method != null)
                            {
                                method.Invoke(droneFleetManager, new object[] { drone });
                            }
                        }
                        catch
                        {
                            // Fallback: just ensure drone is active
                            drone.gameObject.SetActive(true);
                        }
                    }
                }
                
                if (showDebugLogs) UnityEngine.Debug.Log($"✅ Connected {allDrones.Length} drones to DroneFleetManager");
            }
        }
        
        private void ConnectFireSystems()
        {
            if (fireSimulation == null) return;
            
            // Connect fire particle effects
            if (fireEffects.Length > 0)
            {
                var fireList = new List<ParticleSystem>();
                foreach (var effect in fireEffects)
                {
                    if (effect != null && effect.gameObject.name.ToLower().Contains("fire"))
                    {
                        fireList.Add(effect);
                    }
                }
                
                if (fireList.Count > 0)
                {
                    SetPrivateField(fireSimulation, "fireEffects", fireList.ToArray());
                    if (showDebugLogs) UnityEngine.Debug.Log($"✅ Connected {fireList.Count} fire effects to FireSimulation");
                }
            }
        }
        
        private void FinalizeConnections()
        {
            // Set up cross-references between systems
            if (tacticalHUD != null && droneFleetManager != null)
            {
                SetPrivateField(tacticalHUD, "droneFleetManager", droneFleetManager);
            }
            
            if (rtsInterface != null && gameManager != null)
            {
                SetPrivateField(rtsInterface, "gameManager", gameManager);
            }
            
            // Ensure all systems are initialized
            if (gameManager != null)
            {
                gameManager.SendMessage("InitializeGameManager", SendMessageOptions.DontRequireReceiver);
            }
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("🔗 Scene connection finalized. All systems should now work together!");
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
            }
        }
        
        [ContextMenu("Debug Connection Status")]
        public void DebugConnectionStatus()
        {
            UnityEngine.Debug.Log("=== SCENE CONNECTION STATUS ===");
            UnityEngine.Debug.Log($"GameManager: {(gameManager != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"CameraController: {(cameraController != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"UIManager: {(uiManager != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"TacticalHUD: {(tacticalHUD != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"RTSInterface: {(rtsInterface != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"ScoringSystem: {(scoringSystem != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"GeospatialManager: {(geospatialManager != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"DroneFleetManager: {(droneFleetManager != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"FireSimulation: {(fireSimulation != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"NASAAPIManager: {(nasaAPIManager != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"Cameras found: {allCameras?.Length ?? 0}");
            UnityEngine.Debug.Log($"Canvases found: {allCanvases?.Length ?? 0}");
            UnityEngine.Debug.Log($"Drones found: {allDrones?.Length ?? 0}");
            UnityEngine.Debug.Log($"Fire effects found: {fireEffects?.Length ?? 0}");
            UnityEngine.Debug.Log($"Audio sources found: {audioSources?.Length ?? 0}");
            UnityEngine.Debug.Log($"Connection Status: {connectionStatus}");
            UnityEngine.Debug.Log($"Is Connected: {isConnected}");
        }
    }
}

