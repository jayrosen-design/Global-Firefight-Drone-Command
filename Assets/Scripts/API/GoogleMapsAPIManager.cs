using UnityEngine;

namespace GlobalFirefight.API
{
    public class GoogleMapsAPIManager : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string googleAPIKey = "";
        [SerializeField] private string photorealistic3DTilesURL = "https://tile.googleapis.com/v1/3dtiles/root.json";
        
        [Header("Cesium Integration")]
        [SerializeField] private bool useCesiumIonIntegration = true;
        [SerializeField] private string cesiumIonAccessToken = "";
        
        [Header("Attribution")]
        [SerializeField] private bool showCreditsOnScreen = true;
        [SerializeField] private string attributionText = "Map data ©2024 Google";
        
        [Header("Performance")]
        [SerializeField] private int maxConcurrentRequests = 6;
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private bool enableCaching = true;
        
        [Header("Status")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private bool isConfigured = false;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public bool IsConfigured => isConfigured;
        public string APIKey => googleAPIKey;
        public string Photorealistic3DTilesURL => GetPhotorealistic3DTilesURL();
        
        #region Initialization
        
        public void Initialize()
        {
            ValidateConfiguration();
            SetupGoogleMapsIntegration();
            isInitialized = true;
            
            UnityEngine.Debug.Log("Google Maps API Manager initialized");
        }
        
        private void ValidateConfiguration()
        {
            // Check if API key is provided
            if (string.IsNullOrEmpty(googleAPIKey))
            {
                UnityEngine.Debug.LogWarning("Google Maps API key not provided. Some features may not work.");
                isConfigured = false;
                return;
            }
            
            // Validate API key format (should be 39 characters starting with AIza)
            if (!googleAPIKey.StartsWith("AIza") || googleAPIKey.Length != 39)
            {
                UnityEngine.Debug.LogWarning("Google Maps API key format appears invalid.");
            }
            
            isConfigured = true;
            UnityEngine.Debug.Log("Google Maps API configuration validated");
        }
        
        #endregion
        
        #region Setup Methods
        
        public void SetupGoogleMapsIntegration()
        {
            if (!isConfigured)
            {
                UnityEngine.Debug.LogError("Cannot setup Google Maps integration - API not configured");
                return;
            }
            
            if (useCesiumIonIntegration)
            {
                SetupCesiumIonIntegration();
            }
            else
            {
                SetupDirectIntegration();
            }
        }
        
        private void SetupCesiumIonIntegration()
        {
            UnityEngine.Debug.Log("Setting up Google Maps integration via Cesium Ion");
            
            // This would typically involve:
            // 1. Configuring Cesium Ion with Google Maps Platform credentials
            // 2. Setting up the appropriate asset IDs
            // 3. Configuring the Cesium3DTileset component
            
            // Note: This requires the Cesium for Unity package to be installed
            // and proper Cesium Ion account setup
        }
        
        private void SetupDirectIntegration()
        {
            UnityEngine.Debug.Log("Setting up direct Google Maps Platform integration");
            
            // For direct integration, we need to:
            // 1. Configure the Cesium3DTileset to use the direct URL
            // 2. Ensure proper attribution is displayed
            // 3. Handle authentication via API key
        }
        
        public void SetupPhotorealistic3DTiles()
        {
            if (!isConfigured)
            {
                UnityEngine.Debug.LogError("Cannot setup Photorealistic 3D Tiles - Google Maps API not configured");
                return;
            }
            
            UnityEngine.Debug.Log("Configuring Photorealistic 3D Tiles");
            
            // Find Cesium3DTileset component in scene
            var tilesetType = System.Type.GetType("CesiumForUnity.Cesium3DTileset, Cesium");
            Component cesium3DTileset = null;
            
            if (tilesetType != null)
            {
                cesium3DTileset = FindFirstObjectByType(tilesetType) as Component;
            }
            
            if (cesium3DTileset != null)
            {
                ConfigureCesium3DTileset(cesium3DTileset);
            }
            else
            {
                UnityEngine.Debug.LogWarning("No Cesium3DTileset found in scene. Please add one to use Photorealistic 3D Tiles.");
            }
        }
        
        private void ConfigureCesium3DTileset(Component tileset)
        {
            // Configure the tileset for Google Photorealistic 3D Tiles
            var tilesetType = tileset.GetType();
            
            var sourceProperty = tilesetType.GetProperty("tilesetSource");
            if (sourceProperty != null)
            {
                var dataSourceType = System.Type.GetType("CesiumForUnity.CesiumDataSource, Cesium");
                if (dataSourceType != null)
                {
                    var fromUrlValue = System.Enum.Parse(dataSourceType, "FromUrl");
                    sourceProperty.SetValue(tileset, fromUrlValue);
                }
            }
            
            var urlProperty = tilesetType.GetProperty("url");
            if (urlProperty != null)
            {
                urlProperty.SetValue(tileset, GetPhotorealistic3DTilesURL());
            }
            
            // Enable attribution display (required by Google's terms)
            var creditsProperty = tilesetType.GetProperty("showCreditsOnScreen");
            if (creditsProperty != null)
            {
                creditsProperty.SetValue(tileset, showCreditsOnScreen);
            }
            
            // Get URL using reflection for debug output
            var debugUrlProperty = tilesetType.GetProperty("url");
            string url = debugUrlProperty?.GetValue(tileset)?.ToString() ?? "Unknown";
            UnityEngine.Debug.Log($"Cesium3DTileset configured with URL: {url}");
        }
        
        #endregion
        
        #region URL Generation
        
        private string GetPhotorealistic3DTilesURL()
        {
            if (string.IsNullOrEmpty(googleAPIKey))
            {
                return photorealistic3DTilesURL; // Return base URL without key
            }
            
            return $"{photorealistic3DTilesURL}?key={googleAPIKey}";
        }
        
        #endregion
        
        #region API Management
        
        public void SetAPIKey(string newAPIKey)
        {
            googleAPIKey = newAPIKey;
            ValidateConfiguration();
            
            if (isInitialized)
            {
                // Reconfigure any existing tilesets
                SetupPhotorealistic3DTiles();
            }
            
            UnityEngine.Debug.Log("Google Maps API key updated");
        }
        
        public void EnableCesiumIonIntegration(bool enable, string ionToken = "")
        {
            useCesiumIonIntegration = enable;
            
            if (enable && !string.IsNullOrEmpty(ionToken))
            {
                cesiumIonAccessToken = ionToken;
            }
            
            if (isInitialized)
            {
                SetupGoogleMapsIntegration();
            }
        }
        
        #endregion
        
        #region Attribution Management
        
        public void SetAttributionVisibility(bool visible)
        {
            showCreditsOnScreen = visible;
            
            // Update all existing Cesium3DTilesets
            var tilesetType = System.Type.GetType("CesiumForUnity.Cesium3DTileset, Cesium");
            if (tilesetType != null)
            {
                var tilesets = FindObjectsByType(tilesetType, FindObjectsSortMode.None);
                foreach (Component tileset in tilesets)
                {
                    var creditsProperty = tilesetType.GetProperty("showCreditsOnScreen");
                    if (creditsProperty != null)
                    {
                        creditsProperty.SetValue(tileset, visible);
                    }
                }
            }
            
            UnityEngine.Debug.Log($"Attribution visibility set to: {visible}");
        }
        
        public string GetAttributionText()
        {
            return attributionText;
        }
        
        #endregion
        
        #region Validation and Testing
        
        public bool ValidateAPIKey()
        {
            if (string.IsNullOrEmpty(googleAPIKey))
            {
                UnityEngine.Debug.LogError("Google Maps API key is not set");
                return false;
            }
            
            // Basic format validation
            if (!googleAPIKey.StartsWith("AIza") || googleAPIKey.Length != 39)
            {
                UnityEngine.Debug.LogWarning("Google Maps API key format appears to be invalid");
                return false;
            }
            
            UnityEngine.Debug.Log("Google Maps API key format validation passed");
            return true;
        }
        
        public void TestAPIConnection()
        {
            if (!ValidateAPIKey())
            {
                UnityEngine.Debug.LogError("Cannot test API connection - invalid API key");
                return;
            }
            
            // In a real implementation, this would make a test request
            // to verify the API key is valid and has the required permissions
            UnityEngine.Debug.Log("Testing Google Maps API connection...");
            UnityEngine.Debug.Log("Note: Implement actual API test request in production");
        }
        
        #endregion
        
        #region Performance Settings
        
        public void SetPerformanceSettings(int maxRequests, float timeout, bool caching)
        {
            maxConcurrentRequests = maxRequests;
            requestTimeout = timeout;
            enableCaching = caching;
            
            UnityEngine.Debug.Log($"Performance settings updated: MaxRequests={maxRequests}, Timeout={timeout}s, Caching={caching}");
        }
        
        #endregion
        
        #region Status and Debugging
        
        public string GetStatusReport()
        {
            return $"Google Maps API Status:\n" +
                   $"Initialized: {isInitialized}\n" +
                   $"Configured: {isConfigured}\n" +
                   $"API Key: {(string.IsNullOrEmpty(googleAPIKey) ? "Not Set" : "Set")}\n" +
                   $"Cesium Ion: {useCesiumIonIntegration}\n" +
                   $"Attribution: {showCreditsOnScreen}\n" +
                   $"Max Requests: {maxConcurrentRequests}\n" +
                   $"Timeout: {requestTimeout}s\n" +
                   $"Caching: {enableCaching}";
        }
        
        #endregion
        
        #region Unity Editor Support
        
        #if UNITY_EDITOR
        [ContextMenu("Validate Configuration")]
        private void EditorValidateConfiguration()
        {
            ValidateConfiguration();
            UnityEngine.Debug.Log(GetStatusReport());
        }
        
        [ContextMenu("Test API Key")]
        private void EditorTestAPIKey()
        {
            TestAPIConnection();
        }
        #endif
        
        #endregion
    }
}

