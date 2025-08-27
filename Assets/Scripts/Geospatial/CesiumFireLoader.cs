using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalFirefight.Core;
using GlobalFirefight.API;

namespace GlobalFirefight.Geospatial
{
    /// <summary>
    /// Cesium Fire Data Loader - Integrates NASA FIRMS fire data with Cesium 3D world
    /// </summary>
    public class CesiumFireLoader : MonoBehaviour
    {
        [Header("Fire Data Settings")]
        [SerializeField] private string nasaAPIKey = "";
        [SerializeField] private string mapKey = "";
        [SerializeField] private bool loadFiresOnStart = true;
        [SerializeField] private float updateInterval = 300f; // 5 minutes
        
        [Header("Fire Display")]
        [SerializeField] private GameObject fireParticlePrefab;
        [SerializeField] private int maxFiresToDisplay = 100;
        [SerializeField] private float fireScale = 1.0f;
        [SerializeField] private bool showFireMarkers = true;
        
        [Header("Cesium Integration")]
        [SerializeField] private Transform cesiumWorldParent;
        [SerializeField] private Transform fireContainer;
        [SerializeField] private GeospatialManager geospatialManager;
        
        [Header("Fire Data Status")]
        [SerializeField] private int loadedFireCount = 0;
        [SerializeField] private int displayedFireCount = 0;
        [SerializeField] private bool isLoading = false;
        [SerializeField] private string lastUpdateTime = "";
        
        private List<FireData> currentFires = new List<FireData>();
        private List<GameObject> fireGameObjects = new List<GameObject>();
        private Coroutine updateCoroutine;
        
        [System.Serializable]
        public class FireData
        {
            public float latitude;
            public float longitude;
            public float brightness;
            public float confidence;
            public string acquisitionDate;
            public string acquisitionTime;
            public float frp; // Fire Radiative Power
        }
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            // Get API keys from GameManager if available
            var gameManager = GameManager.Instance;
            if (gameManager != null && gameManager.Settings != null)
            {
                if (string.IsNullOrEmpty(nasaAPIKey))
                    nasaAPIKey = gameManager.Settings.nasaAPIKey;
            }
            
            // Find GeospatialManager if not assigned
            if (geospatialManager == null)
            {
                geospatialManager = FindFirstObjectByType<GeospatialManager>();
                if (geospatialManager == null)
                {
                    UnityEngine.Debug.LogWarning("⚠️ GeospatialManager not found - fires may not appear at correct coordinates");
                }
            }
            
            // Find Cesium world if not assigned
            if (cesiumWorldParent == null)
            {
                var cesiumObj = GameObject.Find("Cesium World");
                if (cesiumObj != null)
                    cesiumWorldParent = cesiumObj.transform;
            }
            
            // Create fire container
            if (fireContainer == null)
            {
                GameObject containerGO = new GameObject("Fire_Container");
                if (cesiumWorldParent != null)
                    containerGO.transform.SetParent(cesiumWorldParent);
                fireContainer = containerGO.transform;
            }
            
            // Create simple fire particle prefab if none provided
            if (fireParticlePrefab == null)
            {
                CreateDefaultFirePrefab();
            }
            
            if (loadFiresOnStart)
            {
                LoadFireData();
            }
            
            // Start auto-update coroutine
            if (updateCoroutine == null)
            {
                updateCoroutine = StartCoroutine(AutoUpdateFires());
            }
            
            UnityEngine.Debug.Log("🔥 Cesium Fire Loader initialized");
        }
        
        private void CreateDefaultFirePrefab()
        {
            GameObject prefab = new GameObject("DefaultFire");
            
            // Add a simple particle system
            var particles = prefab.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 5f;
            main.startSize = 0.5f;
            main.startColor = Color.red;
            main.maxParticles = 50;
            
            var emission = particles.emission;
            emission.rateOverTime = 25f;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            
            // Add a sphere collider for interaction
            var collider = prefab.AddComponent<SphereCollider>();
            collider.radius = 1f;
            collider.isTrigger = true;
            
            // Add fire marker script
            prefab.AddComponent<FireMarker>();
            
            fireParticlePrefab = prefab;
            
            UnityEngine.Debug.Log("✅ Created default fire prefab");
        }
        
        [ContextMenu("🔥 Load Fire Data")]
        public void LoadFireData()
        {
            if (isLoading)
            {
                UnityEngine.Debug.LogWarning("Fire data is already loading...");
                return;
            }
            
            StartCoroutine(LoadFireDataCoroutine());
        }
        
        private IEnumerator LoadFireDataCoroutine()
        {
            isLoading = true;
            UnityEngine.Debug.Log("🔥 Loading NASA FIRMS fire data...");
            
            // Clear existing fires
            ClearDisplayedFires();
            
            // For testing, create some sample fire data
            // In a real implementation, you would fetch from NASA FIRMS API
            yield return StartCoroutine(LoadSampleFireData());
            
            // Display the fires on the Cesium world
            DisplayFires();
            
            lastUpdateTime = System.DateTime.Now.ToString("HH:mm:ss");
            isLoading = false;
            
            UnityEngine.Debug.Log($"🔥 Fire data loaded: {loadedFireCount} fires found, {displayedFireCount} displayed");
        }
        
        private IEnumerator LoadSampleFireData()
        {
            // Sample fire data for testing (major fire-prone areas)
            currentFires.Clear();
            
            // California fires
            AddSampleFire(34.0522f, -118.2437f, 320.5f, 85f); // Los Angeles area
            AddSampleFire(37.7749f, -122.4194f, 315.2f, 78f); // San Francisco area
            AddSampleFire(38.5816f, -121.4944f, 298.7f, 92f); // Sacramento area
            
            // Australia fires
            AddSampleFire(-33.8688f, 151.2093f, 305.1f, 88f); // Sydney area
            AddSampleFire(-37.8136f, 144.9631f, 312.4f, 76f); // Melbourne area
            
            // Amazon fires
            AddSampleFire(-3.4653f, -62.2159f, 295.8f, 83f); // Manaus area
            AddSampleFire(-15.8267f, -47.9218f, 287.3f, 91f); // Brasília area
            
            // European fires
            AddSampleFire(41.9028f, 12.4964f, 278.9f, 74f); // Rome area
            AddSampleFire(40.4168f, -3.7038f, 289.6f, 82f); // Madrid area
            
            // African fires
            AddSampleFire(-26.2041f, 28.0473f, 301.2f, 86f); // Johannesburg area
            AddSampleFire(-1.2921f, 36.8219f, 294.7f, 79f); // Nairobi area
            
            loadedFireCount = currentFires.Count;
            
            yield return new WaitForSeconds(0.1f); // Simulate API delay
        }
        
        private void AddSampleFire(float lat, float lon, float brightness, float confidence)
        {
            var fireData = new FireData
            {
                latitude = lat,
                longitude = lon,
                brightness = brightness,
                confidence = confidence,
                acquisitionDate = System.DateTime.Now.ToString("yyyy-MM-dd"),
                acquisitionTime = System.DateTime.Now.ToString("HHmm"),
                frp = Random.Range(10f, 500f)
            };
            currentFires.Add(fireData);
        }
        
        private void DisplayFires()
        {
            if (fireParticlePrefab == null || fireContainer == null)
            {
                UnityEngine.Debug.LogError("❌ Fire prefab or container not set up!");
                return;
            }
            
            UnityEngine.Debug.Log($"🔥 Starting to display fires. Prefab: {fireParticlePrefab.name}, Container: {fireContainer.name}");
            UnityEngine.Debug.Log($"🔥 GeospatialManager available: {geospatialManager != null && geospatialManager.IsInitialized}");
            
            // Log fire container details
            if (fireContainer != null)
            {
                UnityEngine.Debug.Log($"🔥 Fire container details:");
                UnityEngine.Debug.Log($"  - Position: {fireContainer.position}");
                UnityEngine.Debug.Log($"  - Local Scale: {fireContainer.localScale}");
                UnityEngine.Debug.Log($"  - Lossy Scale: {fireContainer.lossyScale}");
                UnityEngine.Debug.Log($"  - GameObject: {fireContainer.gameObject.name}");
            }
            
            displayedFireCount = 0;
            int firesToDisplay = Mathf.Min(maxFiresToDisplay, currentFires.Count);
            
            for (int i = 0; i < firesToDisplay; i++)
            {
                var fireData = currentFires[i];
                
                // Convert lat/lon to world position
                Vector3 worldPos = ConvertLatLonToWorldPosition(fireData.latitude, fireData.longitude);
                
                // Create fire object WITHOUT parent first to preserve world position
                GameObject fireObj = Instantiate(fireParticlePrefab, worldPos, Quaternion.identity);
                fireObj.name = $"Fire_{i}_{fireData.latitude:F2}_{fireData.longitude:F2}";
                
                // Add fire interaction component for clicking/selection
                FireInteraction fireInteraction = fireObj.GetComponent<FireInteraction>();
                if (fireInteraction == null)
                {
                    fireInteraction = fireObj.AddComponent<FireInteraction>();
                }
                fireInteraction.SetFireData(fireData);
                
                // Check if container scale is problematic (too large)
                bool useContainer = fireContainer.lossyScale.x < 10f; // Don't use container if it's too large
                
                if (useContainer)
                {
                    // Set parent while preserving world position
                    fireObj.transform.SetParent(fireContainer, true);
                    UnityEngine.Debug.Log($"  - Using fire container as parent");
                }
                else
                {
                    // Don't use container if it's too large - parent to scene root instead
                    UnityEngine.Debug.Log($"  - Container too large ({fireContainer.lossyScale.x:F2}), parenting to scene root");
                }
                
                // Ensure the world position is exactly what we calculated
                fireObj.transform.position = worldPos;
                
                // Log detailed fire placement info
                UnityEngine.Debug.Log($"🔥 Fire {i} details:");
                UnityEngine.Debug.Log($"  - Lat/Lon: {fireData.latitude:F6}, {fireData.longitude:F6}");
                UnityEngine.Debug.Log($"  - Calculated World Position: {worldPos}");
                UnityEngine.Debug.Log($"  - Local Position (in container): {fireObj.transform.localPosition}");
                UnityEngine.Debug.Log($"  - Final World Position: {fireObj.transform.position}");
                
                // Use the prefab's original scale as the base, but limit the user's fireScale
                Vector3 prefabScale = fireParticlePrefab.transform.localScale;
                float containerScale = fireContainer.lossyScale.x;
                
                // Limit fireScale to reasonable values (max 100x the prefab size)
                float limitedFireScale = Mathf.Clamp(fireScale, 0.1f, 100f);
                if (fireScale != limitedFireScale)
                {
                    UnityEngine.Debug.LogWarning($"⚠️ Fire scale {fireScale} is too extreme! Using {limitedFireScale} instead.");
                }
                
                // Apply intensity scaling based on fire brightness
                float intensityScale = Mathf.Lerp(0.5f, 2.0f, fireData.brightness / 400f);
                
                // Calculate final scale: prefab scale × limited user scale × intensity
                Vector3 baseScale = prefabScale * limitedFireScale * intensityScale;
                
                // Only reduce scale if container is extremely large (to prevent massive fires)
                Vector3 finalScale = baseScale;
                if (useContainer && containerScale > 100f)
                {
                    float scaleReduction = Mathf.Min(containerScale / 100f, 10f); // Max 10x reduction
                    finalScale = baseScale / scaleReduction;
                    UnityEngine.Debug.Log($"  - Reducing scale by {scaleReduction:F2}x due to large container");
                }
                
                UnityEngine.Debug.Log($"  - Prefab Scale: {prefabScale}");
                UnityEngine.Debug.Log($"  - Container Scale: {containerScale:F6}");
                UnityEngine.Debug.Log($"  - Original Fire Scale: {fireScale}");
                UnityEngine.Debug.Log($"  - Limited Fire Scale: {limitedFireScale}");
                UnityEngine.Debug.Log($"  - Intensity Scale: {intensityScale:F2}");
                UnityEngine.Debug.Log($"  - Final Scale: {finalScale}");
                
                // Apply the calculated scale
                fireObj.transform.localScale = finalScale;
                
                // Configure fire marker
                var fireMarker = fireObj.GetComponent<FireMarker>();
                if (fireMarker != null)
                {
                    fireMarker.SetFireData(fireData);
                }
                
                fireGameObjects.Add(fireObj);
                displayedFireCount++;
                
                UnityEngine.Debug.Log($"🔥 Fire {i}: {fireData.latitude:F4}, {fireData.longitude:F4} -> {worldPos} (Final Scale: {finalScale})");
            }
            
            UnityEngine.Debug.Log($"🔥 Displayed {displayedFireCount} fires on Cesium world");
            UnityEngine.Debug.Log($"🔥 Fire container position: {fireContainer.position}, scale: {fireContainer.lossyScale}");
        }
        
        private Vector3 ConvertLatLonToWorldPosition(float latitude, float longitude)
        {
            // Use GeospatialManager for proper Cesium coordinate conversion
            UnityEngine.Debug.Log($"🔧 GeospatialManager available: {geospatialManager != null}, Initialized: {geospatialManager?.IsInitialized ?? false}");
            
            if (geospatialManager != null && geospatialManager.IsInitialized)
            {
                Vector3 worldPos = geospatialManager.ConvertLatLonToUnityPosition(latitude, longitude, 0);
                UnityEngine.Debug.Log($"🌍 Converted {latitude}, {longitude} to Unity position: {worldPos}");
                return worldPos;
            }
            else
            {
                UnityEngine.Debug.LogWarning("⚠️ GeospatialManager not available - using fallback coordinate conversion");
                
                // Improved fallback conversion that places fires on the surface of the Cesium globe
                // Scale coordinates to be more appropriate for Cesium's coordinate system
                float x = longitude * 111320f; // Convert degrees to approximate meters
                float z = latitude * 110540f;   // Convert degrees to approximate meters
                float y = 500f; // Elevated above ground for visibility
                
                Vector3 fallbackPos = new Vector3(x, y, z);
                UnityEngine.Debug.Log($"🔧 Fallback conversion: {latitude}, {longitude} to {fallbackPos}");
                return fallbackPos;
            }
        }
        
        private void ClearDisplayedFires()
        {
            foreach (var fireObj in fireGameObjects)
            {
                if (fireObj != null)
                    DestroyImmediate(fireObj);
            }
            fireGameObjects.Clear();
            displayedFireCount = 0;
        }
        
        private IEnumerator AutoUpdateFires()
        {
            while (true)
            {
                yield return new WaitForSeconds(updateInterval);
                
                if (!isLoading)
                {
                    LoadFireData();
                }
            }
        }
        
        [ContextMenu("🔥 Clear All Fires")]
        public void ClearAllFires()
        {
            ClearDisplayedFires();
            currentFires.Clear();
            loadedFireCount = 0;
            UnityEngine.Debug.Log("🔥 All fires cleared");
        }
        
        [ContextMenu("📊 Show Fire Stats")]
        public void ShowFireStats()
        {
            UnityEngine.Debug.Log($"🔥 Fire Statistics:");
            UnityEngine.Debug.Log($"   Loaded fires: {loadedFireCount}");
            UnityEngine.Debug.Log($"   Displayed fires: {displayedFireCount}");
            UnityEngine.Debug.Log($"   Fire prefab: {(fireParticlePrefab != null ? fireParticlePrefab.name : "None")}");
            UnityEngine.Debug.Log($"   Container: {(fireContainer != null ? fireContainer.name : "None")}");
            UnityEngine.Debug.Log($"   GeospatialManager: {(geospatialManager != null ? "Available" : "Missing")}");
            
            if (currentFires.Count > 0)
            {
                UnityEngine.Debug.Log($"   Sample fire: {currentFires[0].latitude}, {currentFires[0].longitude}");
            }
        }
        
        private void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }
    }
    
    /// <summary>
    /// Fire Marker component for individual fire objects
    /// </summary>
    public class FireMarker : MonoBehaviour
    {
        private CesiumFireLoader.FireData fireData;
        
        public void SetFireData(CesiumFireLoader.FireData data)
        {
            fireData = data;
            gameObject.name = $"Fire_{data.latitude:F2}_{data.longitude:F2}";
        }
        
        private void OnMouseDown()
        {
            if (fireData != null)
            {
                UnityEngine.Debug.Log($"🔥 Fire clicked: {fireData.latitude}, {fireData.longitude} - Confidence: {fireData.confidence}%");
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Drone"))
            {
                UnityEngine.Debug.Log($"🚁 Drone entered fire area: {fireData.latitude}, {fireData.longitude}");
            }
        }
    }
}
