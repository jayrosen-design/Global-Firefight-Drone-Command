using UnityEngine;
using GlobalFirefight.Geospatial;

namespace GlobalFirefight.Debug
{
    /// <summary>
    /// Fire Position Debug Helper - helps visualize fire positions on Cesium world
    /// </summary>
    public class FirePositionDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showFireDebugInfo = true;
        [SerializeField] private bool showCoordinateConversion = true;
        [SerializeField] private float debugUpdateInterval = 2f;
        
        [Header("Test Coordinates")]
        [SerializeField] private float testLatitude = 34.0522f; // Los Angeles
        [SerializeField] private float testLongitude = -118.2437f;
        [SerializeField] private GameObject debugMarkerPrefab;
        
        private CesiumFireLoader fireLoader;
        private GeospatialManager geospatialManager;
        private float lastDebugTime;
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            fireLoader = FindFirstObjectByType<CesiumFireLoader>();
            geospatialManager = FindFirstObjectByType<GeospatialManager>();
            
            if (debugMarkerPrefab == null)
            {
                CreateDebugMarker();
            }
            
            UnityEngine.Debug.Log("🔍 Fire Position Debugger initialized");
        }
        
        private void CreateDebugMarker()
        {
            debugMarkerPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugMarkerPrefab.name = "DebugMarker";
            debugMarkerPrefab.GetComponent<Renderer>().material.color = Color.cyan;
            debugMarkerPrefab.transform.localScale = Vector3.one * 50f; // Make it visible
            debugMarkerPrefab.SetActive(false);
        }
        
        private void Update()
        {
            if (showFireDebugInfo && Time.time - lastDebugTime > debugUpdateInterval)
            {
                LogFirePositions();
                lastDebugTime = Time.time;
            }
            
            // Test coordinate conversion with keyboard input
            if (Input.GetKeyDown(KeyCode.F5))
            {
                TestCoordinateConversion();
            }
            
            if (Input.GetKeyDown(KeyCode.F6))
            {
                TestFirePlacement();
            }
        }
        
        [ContextMenu("🔍 Test Coordinate Conversion")]
        public void TestCoordinateConversion()
        {
            if (geospatialManager != null)
            {
                Vector3 unityPos = geospatialManager.ConvertLatLonToUnityPosition(testLatitude, testLongitude, 0);
                UnityEngine.Debug.Log($"🌍 Test conversion: {testLatitude}, {testLongitude} -> Unity: {unityPos}");
                
                // Place debug marker at converted position
                if (debugMarkerPrefab != null)
                {
                    var marker = Instantiate(debugMarkerPrefab, unityPos, Quaternion.identity);
                    marker.name = $"TestMarker_{testLatitude}_{testLongitude}";
                    marker.SetActive(true);
                    UnityEngine.Debug.Log($"🎯 Placed debug marker at {unityPos}");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("⚠️ GeospatialManager not available for coordinate conversion");
            }
        }
        
        [ContextMenu("🔥 Test Fire Placement")]
        public void TestFirePlacement()
        {
            if (fireLoader != null)
            {
                fireLoader.LoadFireData();
                UnityEngine.Debug.Log("🔥 Triggered fire data loading");
            }
            else
            {
                UnityEngine.Debug.LogWarning("⚠️ CesiumFireLoader not found");
            }
        }
        
        [ContextMenu("📊 Log Fire Positions")]
        public void LogFirePositions()
        {
            if (fireLoader == null) return;
            
            UnityEngine.Debug.Log("🔍 === FIRE POSITION DEBUG ===");
            
            // Find all fire objects in the scene
            var fireObjects = GameObject.FindGameObjectsWithTag("Fire");
            if (fireObjects.Length == 0)
            {
                fireObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                System.Array.FindAll(fireObjects, go => go.name.StartsWith("Fire_"));
            }
            
            if (fireObjects.Length > 0)
            {
                UnityEngine.Debug.Log($"🔥 Found {fireObjects.Length} fire objects:");
                for (int i = 0; i < Mathf.Min(5, fireObjects.Length); i++)
                {
                    var fireObj = fireObjects[i];
                    UnityEngine.Debug.Log($"   Fire {i}: {fireObj.name} at position {fireObj.transform.position}");
                }
            }
            else
            {
                UnityEngine.Debug.Log("🔥 No fire objects found in scene");
            }
            
            // Check fire container
            var fireContainer = GameObject.Find("Fire_Container");
            if (fireContainer != null)
            {
                UnityEngine.Debug.Log($"🗂️ Fire container: {fireContainer.transform.position}, children: {fireContainer.transform.childCount}");
            }
            else
            {
                UnityEngine.Debug.Log("🗂️ Fire container not found");
            }
            
            // Check Cesium world
            var cesiumWorld = GameObject.Find("Cesium World");
            if (cesiumWorld != null)
            {
                UnityEngine.Debug.Log($"🌍 Cesium World: {cesiumWorld.transform.position}, scale: {cesiumWorld.transform.lossyScale}");
            }
            
            UnityEngine.Debug.Log("🔍 === END DEBUG ===");
        }
        
        [ContextMenu("🎯 Show Camera Position")]
        public void ShowCameraPosition()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                UnityEngine.Debug.Log($"📷 Camera position: {camera.transform.position}");
                UnityEngine.Debug.Log($"📷 Camera rotation: {camera.transform.eulerAngles}");
                
                // Show distance to some fire positions for reference
                var fireContainer = GameObject.Find("Fire_Container");
                if (fireContainer != null && fireContainer.transform.childCount > 0)
                {
                    var firstFire = fireContainer.transform.GetChild(0);
                    float distance = Vector3.Distance(camera.transform.position, firstFire.position);
                    UnityEngine.Debug.Log($"🔥 Distance to first fire: {distance:F1} units");
                }
            }
        }
    }
}


