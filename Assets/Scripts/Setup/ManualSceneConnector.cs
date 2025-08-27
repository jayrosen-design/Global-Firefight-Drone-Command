using UnityEngine;
using UnityEditor;
using GlobalFirefight.Core;
using GlobalFirefight.Systems;
using GlobalFirefight.UI;
using GlobalFirefight.Drones;
using GlobalFirefight.Fire;
using GlobalFirefight.Geospatial;
using GlobalFirefight.API;

namespace GlobalFirefight.Setup
{
    /// <summary>
    /// Manual Scene Connection Helper - Drag and drop components to connect them
    /// This provides an easy inspector interface to wire up your scene components
    /// </summary>
    public class ManualSceneConnector : MonoBehaviour
    {
        [Header("📋 Core Management Systems")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private ScoringSystem scoringSystem;
        
        [Header("🎮 UI Systems")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private TacticalHUD tacticalHUD;
        [SerializeField] private RTSInterface rtsInterface;
        [SerializeField] private Canvas mainCanvas;
        
        [Header("🚁 Drone Systems")]
        [SerializeField] private DroneFleetManager droneFleetManager;
        [SerializeField] private DroneController[] individualDrones;
        
        [Header("🔥 Fire Systems")]
        [SerializeField] private FireSimulation fireSimulation;
        [SerializeField] private ParticleSystem[] fireEffects;
        
        [Header("🌍 Geospatial Systems")]
        [SerializeField] private GeospatialManager geospatialManager;
        [SerializeField] private NASAAPIManager nasaAPIManager;
        
        [Header("📷 Camera Systems")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera tacticalCamera;
        [SerializeField] private Camera rtsCamera;
        
        [Header("🔊 Audio Systems")]
        [SerializeField] private AudioSource backgroundAudio;
        [SerializeField] private AudioSource effectsAudio;
        
        [ContextMenu("🔗 Connect All Systems")]
        public void ConnectAllSystems()
        {
            ConnectGameManagerSystems();
            ConnectUIManagerSystems();
            ConnectCameraSystems();
            ConnectDroneSystems();
            ConnectFireSystems();
            
            UnityEngine.Debug.Log("✅ All systems connected! Your scene should now work properly.");
        }
        
        [ContextMenu("🎮 Connect GameManager Only")]
        public void ConnectGameManagerSystems()
        {
            if (gameManager == null)
            {
                UnityEngine.Debug.LogError("❌ GameManager is not assigned!");
                return;
            }
            
            // Use reflection to set the serialized private fields
            SetSerializedField(gameManager, "cameraController", cameraController);
            SetSerializedField(gameManager, "uiManager", uiManager);
            SetSerializedField(gameManager, "scoringSystem", scoringSystem);
            SetSerializedField(gameManager, "geospatialManager", geospatialManager);
            SetSerializedField(gameManager, "nasaAPIManager", nasaAPIManager);
            SetSerializedField(gameManager, "droneFleetManager", droneFleetManager);
            SetSerializedField(gameManager, "fireSimulation", fireSimulation);
            
            UnityEngine.Debug.Log("✅ GameManager systems connected!");
        }
        
        [ContextMenu("🖥️ Connect UI Systems")]
        public void ConnectUIManagerSystems()
        {
            if (uiManager == null)
            {
                UnityEngine.Debug.LogError("❌ UIManager is not assigned!");
                return;
            }
            
            SetSerializedField(uiManager, "tacticalHUD", tacticalHUD);
            SetSerializedField(uiManager, "rtsInterface", rtsInterface);
            SetSerializedField(uiManager, "mainCanvas", mainCanvas);
            
            // Connect HUD references
            if (tacticalHUD != null)
            {
                SetSerializedField(tacticalHUD, "droneFleetManager", droneFleetManager);
                SetSerializedField(tacticalHUD, "gameManager", gameManager);
            }
            
            if (rtsInterface != null)
            {
                SetSerializedField(rtsInterface, "gameManager", gameManager);
                SetSerializedField(rtsInterface, "droneFleetManager", droneFleetManager);
            }
            
            UnityEngine.Debug.Log("✅ UI systems connected!");
        }
        
        [ContextMenu("📷 Connect Camera Systems")]
        public void ConnectCameraSystems()
        {
            if (cameraController == null)
            {
                UnityEngine.Debug.LogError("❌ CameraController is not assigned!");
                return;
            }
            
            SetSerializedField(cameraController, "mainCamera", mainCamera);
            SetSerializedField(cameraController, "tacticalCamera", tacticalCamera);
            SetSerializedField(cameraController, "rtsCamera", rtsCamera);
            
            UnityEngine.Debug.Log("✅ Camera systems connected!");
        }
        
        [ContextMenu("🚁 Connect Drone Systems")]
        public void ConnectDroneSystems()
        {
            if (droneFleetManager == null)
            {
                UnityEngine.Debug.LogError("❌ DroneFleetManager is not assigned!");
                return;
            }
            
            // Register individual drones
            if (individualDrones != null && individualDrones.Length > 0)
            {
                foreach (var drone in individualDrones)
                {
                    if (drone != null)
                    {
                        // Connect drone to fleet manager
                        SetSerializedField(drone, "fleetManager", droneFleetManager);
                        SetSerializedField(drone, "gameManager", gameManager);
                    }
                }
                
                UnityEngine.Debug.Log($"✅ Connected {individualDrones.Length} drones to DroneFleetManager!");
            }
        }
        
        [ContextMenu("🔥 Connect Fire Systems")]
        public void ConnectFireSystems()
        {
            if (fireSimulation == null)
            {
                UnityEngine.Debug.LogError("❌ FireSimulation is not assigned!");
                return;
            }
            
            if (fireEffects != null && fireEffects.Length > 0)
            {
                SetSerializedField(fireSimulation, "fireParticleEffects", fireEffects);
                UnityEngine.Debug.Log($"✅ Connected {fireEffects.Length} fire effects to FireSimulation!");
            }
            
            SetSerializedField(fireSimulation, "gameManager", gameManager);
        }
        
        [ContextMenu("🔍 Auto-Find Components")]
        public void AutoFindComponents()
        {
            // Find components automatically
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (tacticalHUD == null) tacticalHUD = FindFirstObjectByType<TacticalHUD>();
            if (rtsInterface == null) rtsInterface = FindFirstObjectByType<RTSInterface>();
            if (scoringSystem == null) scoringSystem = FindFirstObjectByType<ScoringSystem>();
            if (droneFleetManager == null) droneFleetManager = FindFirstObjectByType<DroneFleetManager>();
            if (fireSimulation == null) fireSimulation = FindFirstObjectByType<FireSimulation>();
            if (geospatialManager == null) geospatialManager = FindFirstObjectByType<GeospatialManager>();
            if (nasaAPIManager == null) nasaAPIManager = FindFirstObjectByType<NASAAPIManager>();
            
            // Find cameras
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam.name.ToLower().Contains("main") && mainCamera == null)
                    mainCamera = cam;
                else if (cam.name.ToLower().Contains("tactical") && tacticalCamera == null)
                    tacticalCamera = cam;
                else if (cam.name.ToLower().Contains("rts") && rtsCamera == null)
                    rtsCamera = cam;
            }
            
            // Find canvas
            if (mainCanvas == null)
            {
                var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                if (canvases.Length > 0) mainCanvas = canvases[0];
            }
            
            // Find drones
            individualDrones = FindObjectsByType<DroneController>(FindObjectsSortMode.None);
            
            // Find fire effects
            var particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            var fireList = new System.Collections.Generic.List<ParticleSystem>();
            foreach (var ps in particles)
            {
                if (ps.name.ToLower().Contains("fire"))
                    fireList.Add(ps);
            }
            fireEffects = fireList.ToArray();
            
            // Find audio sources
            var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var audio in audioSources)
            {
                if (audio.name.ToLower().Contains("background") && backgroundAudio == null)
                    backgroundAudio = audio;
                else if (audio.name.ToLower().Contains("effect") && effectsAudio == null)
                    effectsAudio = audio;
            }
            
            UnityEngine.Debug.Log("🔍 Auto-find complete! Check the inspector to see what was found.");
        }
        
        [ContextMenu("❓ Debug Current Connections")]
        public void DebugConnections()
        {
            UnityEngine.Debug.Log("=== MANUAL SCENE CONNECTOR STATUS ===");
            UnityEngine.Debug.Log($"GameManager: {(gameManager != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"CameraController: {(cameraController != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"UIManager: {(uiManager != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"TacticalHUD: {(tacticalHUD != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"RTSInterface: {(rtsInterface != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"ScoringSystem: {(scoringSystem != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"DroneFleetManager: {(droneFleetManager != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"FireSimulation: {(fireSimulation != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"GeospatialManager: {(geospatialManager != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"NASAAPIManager: {(nasaAPIManager != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"Main Camera: {(mainCamera != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"Main Canvas: {(mainCanvas != null ? "✅ Connected" : "❌ Missing")}");
            UnityEngine.Debug.Log($"Individual Drones: {(individualDrones?.Length ?? 0)} found");
            UnityEngine.Debug.Log($"Fire Effects: {(fireEffects?.Length ?? 0)} found");
        }
        
        private void SetSerializedField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;
            
            var field = target.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(target, value);
                UnityEngine.Debug.Log($"✅ Connected {fieldName} to {target.GetType().Name}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"⚠️ Field '{fieldName}' not found in {target.GetType().Name}");
            }
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(ManualSceneConnector))]
    public class ManualSceneConnectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ManualSceneConnector connector = (ManualSceneConnector)target;
            
            EditorGUILayout.HelpBox(
                "🔗 Scene Connection Helper\n\n" +
                "1. Click 'Auto-Find Components' to automatically discover scene objects\n" +
                "2. Manually assign any missing components in the inspector\n" +
                "3. Click 'Connect All Systems' to wire everything together\n" +
                "4. Use 'Debug Current Connections' to check status", 
                MessageType.Info
            );
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("🔍 Auto-Find Components", GUILayout.Height(30)))
            {
                connector.AutoFindComponents();
            }
            
            if (GUILayout.Button("🔗 Connect All Systems", GUILayout.Height(30)))
            {
                connector.ConnectAllSystems();
            }
            
            if (GUILayout.Button("❓ Debug Current Connections", GUILayout.Height(25)))
            {
                connector.DebugConnections();
            }
            
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
#endif
}

