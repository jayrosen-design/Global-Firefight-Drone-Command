using UnityEngine;
using GlobalFirefight.Geospatial;

namespace GlobalFirefight.Testing
{
    /// <summary>
    /// Quick Fire Testing Component - Add this to any GameObject to test fire loading
    /// </summary>
    public class QuickFireTester : MonoBehaviour
    {
        [Header("Quick Test Controls")]
        [SerializeField] private KeyCode loadFiresKey = KeyCode.F7;
        [SerializeField] private KeyCode clearFiresKey = KeyCode.F8;
        [SerializeField] private KeyCode showStatsKey = KeyCode.F9;
        [SerializeField] private KeyCode teleportToCalifornia = KeyCode.F10;
        
        [Header("Test Results")]
        [SerializeField] private bool fireLoaderFound = false;
        [SerializeField] private bool geospatialManagerFound = false;
        [SerializeField] private int fireCount = 0;
        
        private CesiumFireLoader fireLoader;
        private GeospatialManager geospatialManager;
        private Camera mainCamera;
        
        private void Start()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            fireLoader = FindFirstObjectByType<CesiumFireLoader>();
            geospatialManager = FindFirstObjectByType<GeospatialManager>();
            mainCamera = Camera.main;
            
            fireLoaderFound = fireLoader != null;
            geospatialManagerFound = geospatialManager != null;
            
            UnityEngine.Debug.Log("🧪 Quick Fire Tester initialized");
            UnityEngine.Debug.Log($"   Fire Loader: {(fireLoaderFound ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"   Geospatial Manager: {(geospatialManagerFound ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"   Controls: {loadFiresKey}=Load, {clearFiresKey}=Clear, {showStatsKey}=Stats, {teleportToCalifornia}=Teleport");
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(loadFiresKey))
            {
                LoadAndTestFires();
            }
            
            if (Input.GetKeyDown(clearFiresKey))
            {
                ClearFires();
            }
            
            if (Input.GetKeyDown(showStatsKey))
            {
                ShowFireStats();
            }
            
            if (Input.GetKeyDown(teleportToCalifornia))
            {
                TeleportToCaliforniaFires();
            }
        }
        
        [ContextMenu("🔥 Load and Test Fires")]
        public void LoadAndTestFires()
        {
            UnityEngine.Debug.Log("🧪 === FIRE LOADING TEST ===");
            
            if (fireLoader != null)
            {
                fireLoader.LoadFireData();
                
                // Wait a moment then check results
                Invoke(nameof(CheckFireResults), 1f);
            }
            else
            {
                UnityEngine.Debug.LogError("❌ CesiumFireLoader not found! Make sure it's in the scene.");
            }
        }
        
        private void CheckFireResults()
        {
            UnityEngine.Debug.Log("🧪 === FIRE TEST RESULTS ===");
            
            // Count fire objects
            var fireObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int foundFires = 0;
            
            foreach (var obj in fireObjects)
            {
                if (obj.name.StartsWith("Fire_"))
                {
                    foundFires++;
                    if (foundFires <= 3) // Show first 3 for debugging
                    {
                        UnityEngine.Debug.Log($"🔥 Found fire: {obj.name} at {obj.transform.position}");
                    }
                }
            }
            
            fireCount = foundFires;
            UnityEngine.Debug.Log($"🔥 Total fires found: {foundFires}");
            
            // Check fire container
            var container = GameObject.Find("Fire_Container");
            if (container != null)
            {
                UnityEngine.Debug.Log($"📦 Fire container: {container.transform.childCount} children at {container.transform.position}");
            }
            
            // Check camera distance to fires
            if (mainCamera != null && foundFires > 0)
            {
                var firstFire = GameObject.Find("Fire_0_34.05_-118.24");
                if (firstFire != null)
                {
                    float distance = Vector3.Distance(mainCamera.transform.position, firstFire.transform.position);
                    UnityEngine.Debug.Log($"📷 Camera distance to LA fire: {distance:F1} units");
                    
                    if (distance > 10000f)
                    {
                        UnityEngine.Debug.LogWarning("⚠️ Camera is very far from fires - they may not be visible!");
                    }
                }
            }
        }
        
        [ContextMenu("🗑️ Clear Fires")]
        public void ClearFires()
        {
            if (fireLoader != null)
            {
                fireLoader.ClearAllFires();
                fireCount = 0;
                UnityEngine.Debug.Log("🗑️ Fires cleared");
            }
        }
        
        [ContextMenu("📊 Show Fire Stats")]
        public void ShowFireStats()
        {
            if (fireLoader != null)
            {
                fireLoader.ShowFireStats();
            }
            
            UnityEngine.Debug.Log("🧪 === CURRENT SCENE STATE ===");
            UnityEngine.Debug.Log($"   Camera position: {(mainCamera != null ? mainCamera.transform.position.ToString("F1") : "No camera")}");
            UnityEngine.Debug.Log($"   Active fires: {fireCount}");
            
            var cesiumWorld = GameObject.Find("Cesium World");
            if (cesiumWorld != null)
            {
                UnityEngine.Debug.Log($"   Cesium World: Position {cesiumWorld.transform.position}, Scale {cesiumWorld.transform.lossyScale}");
            }
        }
        
        [ContextMenu("✈️ Teleport to California Fires")]
        public void TeleportToCaliforniaFires()
        {
            if (mainCamera == null)
            {
                UnityEngine.Debug.LogWarning("⚠️ No main camera found");
                return;
            }
            
            // California coordinates: 34.0522, -118.2437 (Los Angeles)
            Vector3 targetPosition;
            
            if (geospatialManager != null && geospatialManager.IsInitialized)
            {
                // Use proper coordinate conversion
                targetPosition = geospatialManager.ConvertLatLonToUnityPosition(34.0522, -118.2437, 1000); // 1km altitude
                UnityEngine.Debug.Log($"🌍 Using GeospatialManager conversion: {targetPosition}");
            }
            else
            {
                // Use fallback calculation matching CesiumFireLoader
                float x = (-118.2437f + 180f) * 10f - 1800f;
                float z = (34.0522f + 90f) * 10f - 900f;
                float y = 500f; // High altitude for overview
                targetPosition = new Vector3(x, y, z);
                UnityEngine.Debug.Log($"🔧 Using fallback conversion: {targetPosition}");
            }
            
            // Move camera to fire location
            mainCamera.transform.position = targetPosition;
            mainCamera.transform.LookAt(targetPosition + Vector3.down * 200f); // Look down at fires
            
            UnityEngine.Debug.Log($"✈️ Camera teleported to California fire area: {targetPosition}");
        }
        
        private void OnGUI()
        {
            // Simple on-screen display
            GUI.Label(new Rect(10, 10, 300, 20), $"Fire Tester - {fireCount} fires loaded");
            GUI.Label(new Rect(10, 30, 300, 20), $"Press {loadFiresKey} to load fires, {teleportToCalifornia} to teleport");
        }
    }
}

