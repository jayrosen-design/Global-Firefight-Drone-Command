using UnityEngine;

namespace GlobalFirefight.Setup
{
    /// <summary>
    /// Scene Setup Guide - Provides step-by-step instructions for setting up the scene
    /// </summary>
    public class SceneSetupGuide : MonoBehaviour
    {
        [Header("📖 Setup Instructions")]
        [TextArea(10, 20)]
        public string setupInstructions = @"
🎮 GLOBAL FIREFIGHT SCENE SETUP GUIDE

STEP 1: ADD CORE MANAGERS
▫️ Create empty GameObject named 'GameManager'
▫️ Add GameManager.cs script
▫️ Create empty GameObject named 'Systems'
▫️ Add CameraController.cs, ScoringSystem.cs scripts

STEP 2: ADD UI SYSTEMS  
▫️ Create Canvas named 'MainUI'
▫️ Add UIManager.cs script to Canvas
▫️ Create child objects for TacticalHUD and RTSInterface
▫️ Add respective scripts

STEP 3: ADD DRONE SYSTEMS
▫️ Create empty GameObject named 'DroneFleet'
▫️ Add DroneFleetManager.cs script
▫️ Create individual drone GameObjects with DroneController.cs

STEP 4: ADD FIRE SYSTEMS
▫️ Create empty GameObject named 'FireSimulation'
▫️ Add FireSimulation.cs script
▫️ Add ParticleSystem objects for fire effects

STEP 5: ADD GEOSPATIAL SYSTEMS
▫️ Create empty GameObject named 'GeospatialManager'
▫️ Add GeospatialManager.cs and NASAAPIManager.cs

STEP 6: CONNECT EVERYTHING
▫️ Add ManualSceneConnector.cs to any GameObject
▫️ Click 'Auto-Find Components'
▫️ Manually assign any missing references
▫️ Click 'Connect All Systems'

✅ Your scene should now be fully functional!
";
        
        [ContextMenu("🏗️ Create Core Hierarchy")]
        public void CreateCoreHierarchy()
        {
            CreateGameManagerHierarchy();
            CreateUIHierarchy();
            CreateDroneHierarchy();
            CreateFireHierarchy();
            CreateGeospatialHierarchy();
            
            UnityEngine.Debug.Log("✅ Core hierarchy created! Now add the ManualSceneConnector to connect everything.");
        }
        
        [ContextMenu("🎮 Create GameManager")]
        public void CreateGameManagerHierarchy()
        {
            // Create GameManager
            var gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GlobalFirefight.Core.GameManager>();
            
            // Create Systems parent
            var systemsObj = new GameObject("Systems");
            systemsObj.AddComponent<GlobalFirefight.Systems.CameraController>();
            systemsObj.AddComponent<GlobalFirefight.Systems.ScoringSystem>();
            
            UnityEngine.Debug.Log("✅ GameManager hierarchy created");
        }
        
        [ContextMenu("🖥️ Create UI Hierarchy")]
        public void CreateUIHierarchy()
        {
            // Create main canvas
            var canvasObj = new GameObject("MainUI");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.AddComponent<GlobalFirefight.UI.UIManager>();
            
            // Create TacticalHUD
            var tacticalObj = new GameObject("TacticalHUD");
            tacticalObj.transform.SetParent(canvasObj.transform);
            tacticalObj.AddComponent<RectTransform>();
            tacticalObj.AddComponent<GlobalFirefight.UI.TacticalHUD>();
            
            // Create RTSInterface
            var rtsObj = new GameObject("RTSInterface");
            rtsObj.transform.SetParent(canvasObj.transform);
            rtsObj.AddComponent<RectTransform>();
            rtsObj.AddComponent<GlobalFirefight.UI.RTSInterface>();
            
            UnityEngine.Debug.Log("✅ UI hierarchy created");
        }
        
        [ContextMenu("🚁 Create Drone Hierarchy")]
        public void CreateDroneHierarchy()
        {
            // Create drone fleet manager
            var fleetObj = new GameObject("DroneFleet");
            fleetObj.AddComponent<GlobalFirefight.Drones.DroneFleetManager>();
            
            // Create sample drones
            for (int i = 0; i < 3; i++)
            {
                var droneObj = new GameObject($"Drone_{i + 1}");
                droneObj.transform.SetParent(fleetObj.transform);
                droneObj.AddComponent<GlobalFirefight.Drones.DroneController>();
                
                // Add basic mesh and collider
                droneObj.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                droneObj.AddComponent<MeshRenderer>();
                droneObj.AddComponent<CapsuleCollider>();
                droneObj.AddComponent<Rigidbody>();
                
                // Position them
                droneObj.transform.position = new Vector3(i * 5, 10, 0);
            }
            
            UnityEngine.Debug.Log("✅ Drone hierarchy created with 3 sample drones");
        }
        
        [ContextMenu("🔥 Create Fire Hierarchy")]
        public void CreateFireHierarchy()
        {
            // Create fire simulation manager
            var fireObj = new GameObject("FireSimulation");
            fireObj.AddComponent<GlobalFirefight.Fire.FireSimulation>();
            
            // Create sample fire effects
            for (int i = 0; i < 2; i++)
            {
                var effectObj = new GameObject($"FireEffect_{i + 1}");
                effectObj.transform.SetParent(fireObj.transform);
                effectObj.AddComponent<ParticleSystem>();
                effectObj.transform.position = new Vector3(i * 10, 0, 10);
            }
            
            UnityEngine.Debug.Log("✅ Fire simulation hierarchy created");
        }
        
        [ContextMenu("🌍 Create Geospatial Hierarchy")]
        public void CreateGeospatialHierarchy()
        {
            // Create geospatial manager
            var geoObj = new GameObject("GeospatialSystems");
            geoObj.AddComponent<GlobalFirefight.Geospatial.GeospatialManager>();
            geoObj.AddComponent<GlobalFirefight.API.NASAAPIManager>();
            
            UnityEngine.Debug.Log("✅ Geospatial hierarchy created");
        }
        
        [ContextMenu("📷 Setup Cameras")]
        public void SetupCameras()
        {
            // Find or create main camera
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }
            
            // Position main camera
            mainCam.transform.position = new Vector3(0, 20, -30);
            mainCam.transform.LookAt(Vector3.zero);
            
            UnityEngine.Debug.Log("✅ Camera setup complete");
        }
        
        [ContextMenu("❓ Show Current Scene Status")]
        public void ShowSceneStatus()
        {
            UnityEngine.Debug.Log("=== SCENE SETUP STATUS ===");
            
            // Check for core components
            var gameManager = FindFirstObjectByType<GlobalFirefight.Core.GameManager>();
            var cameraController = FindFirstObjectByType<GlobalFirefight.Systems.CameraController>();
            var uiManager = FindFirstObjectByType<GlobalFirefight.UI.UIManager>();
            var droneFleetManager = FindFirstObjectByType<GlobalFirefight.Drones.DroneFleetManager>();
            var fireSimulation = FindFirstObjectByType<GlobalFirefight.Fire.FireSimulation>();
            var geospatialManager = FindFirstObjectByType<GlobalFirefight.Geospatial.GeospatialManager>();
            
            UnityEngine.Debug.Log($"GameManager: {(gameManager != null ? "✅" : "❌ Missing")}");
            UnityEngine.Debug.Log($"CameraController: {(cameraController != null ? "✅" : "❌ Missing")}");
            UnityEngine.Debug.Log($"UIManager: {(uiManager != null ? "✅" : "❌ Missing")}");
            UnityEngine.Debug.Log($"DroneFleetManager: {(droneFleetManager != null ? "✅" : "❌ Missing")}");
            UnityEngine.Debug.Log($"FireSimulation: {(fireSimulation != null ? "✅" : "❌ Missing")}");
            UnityEngine.Debug.Log($"GeospatialManager: {(geospatialManager != null ? "✅" : "❌ Missing")}");
            
            var drones = FindObjectsByType<GlobalFirefight.Drones.DroneController>(FindObjectsSortMode.None);
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            
            UnityEngine.Debug.Log($"Drones in scene: {drones.Length}");
            UnityEngine.Debug.Log($"Cameras in scene: {cameras.Length}");
            UnityEngine.Debug.Log($"Canvases in scene: {canvases.Length}");
            
            if (gameManager != null && cameraController != null && uiManager != null && 
                droneFleetManager != null && fireSimulation != null && geospatialManager != null)
            {
                UnityEngine.Debug.Log("🎉 All core systems are present! Now use ManualSceneConnector to connect them.");
            }
            else
            {
                UnityEngine.Debug.Log("⚠️ Some core systems are missing. Use 'Create Core Hierarchy' to create them.");
            }
        }
    }
}

