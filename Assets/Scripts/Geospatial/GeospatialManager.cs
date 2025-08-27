using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GlobalFirefight.Data;
using GlobalFirefight.Core;

namespace GlobalFirefight.Geospatial
{
    public class GeospatialManager : MonoBehaviour
    {
        [Header("Cesium Components")]
        [SerializeField] private Component georeference;
        [SerializeField] private Component dynamicCamera;
        [SerializeField] private List<Component> tilesets;
        
        [Header("LOD Configuration")]
        [SerializeField] private float globalViewThreshold = 10000f; // 10km altitude
        [SerializeField] private float regionalViewThreshold = 1000f; // 1km altitude
        [SerializeField] private float tacticalViewThreshold = 100f; // 100m altitude
        [SerializeField] private LODLevel currentLOD = LODLevel.Global;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxTilesLoaded = 200;
        [SerializeField] private float tileLoadTimeout = 30f;
        [SerializeField] private bool enableFrustumCulling = true;
        [SerializeField] private bool enableOcclusionCulling = false;
        
        [Header("World Settings")]
        [SerializeField] private Vector3 worldOrigin = Vector3.zero;
        [SerializeField] private double originLatitude = 0.0;
        [SerializeField] private double originLongitude = 0.0;
        [SerializeField] private double originHeight = 0.0;
        
        [Header("Status")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private int activeTilesets = 0;
        [SerializeField] private float currentAltitude = 0f;
        
        // Cached objects for performance
        private Dictionary<string, GameObject> anchoredObjects;
        private Dictionary<LODLevel, List<Component>> lodTilesets;
        
        public enum LODLevel
        {
            Global,    // Orbital view - low detail
            Regional,  // Continental view - medium detail
            Tactical   // Local view - high detail
        }
        
        // Events
        public static event System.Action<LODLevel> OnLODChanged;
        public static event System.Action<Vector3> OnWorldOriginChanged;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public LODLevel CurrentLOD => currentLOD;
        public Vector3 WorldOrigin => worldOrigin;
        public Component Georeference => georeference;
        
        #region Initialization
        
        public void Initialize()
        {
            UnityEngine.Debug.Log("Initializing Geospatial Manager...");
            
            // Initialize collections
            anchoredObjects = new Dictionary<string, GameObject>();
            lodTilesets = new Dictionary<LODLevel, List<Component>>();
            
            // Find Cesium components if not assigned
            FindCesiumComponents();
            
            // Initialize Cesium system
            InitializeCesiumWorld();
            
            // Setup LOD system
            SetupLODSystem();
            
            isInitialized = true;
            UnityEngine.Debug.Log("Geospatial Manager initialized successfully");
        }
        
        private void FindCesiumComponents()
        {
            UnityEngine.Debug.Log("FindCesiumComponents: Starting search...");
            
            // Find georeference using direct search first
            if (georeference == null)
            {
                // Look for existing CesiumGeoreference components in the scene
                var sceneComponents = FindObjectsByType<Component>(FindObjectsSortMode.None);
                foreach (var comp in sceneComponents)
                {
                    if (comp.GetType().FullName == "CesiumForUnity.CesiumGeoreference")
                    {
                        georeference = comp;
                        UnityEngine.Debug.Log($"Found CesiumGeoreference directly: {comp.gameObject.name}");
                        UnityEngine.Debug.Log($"Georeference type: {comp.GetType().FullName}");
                        break;
                    }
                }
            }
            
            // Fallback: Try reflection-based search
            if (georeference == null)
            {
                var georefType = System.Type.GetType("CesiumForUnity.CesiumGeoreference, Cesium");
                UnityEngine.Debug.Log($"CesiumGeoreference type found via reflection: {georefType != null}");
                
                if (georefType != null)
                {
                    georeference = FindFirstObjectByType(georefType) as Component;
                    UnityEngine.Debug.Log($"Found CesiumGeoreference via reflection: {georeference != null}");
                    if (georeference != null)
                    {
                        UnityEngine.Debug.Log($"Georeference gameObject: {georeference.gameObject.name}");
                        UnityEngine.Debug.Log($"Georeference type: {georeference.GetType().FullName}");
                    }
                }
            }
            
            // Create georeference if it doesn't exist and type is available
            if (georeference == null)
            {
                var georefType = System.Type.GetType("CesiumForUnity.CesiumGeoreference, Cesium");
                if (georefType != null)
                {
                    UnityEngine.Debug.Log("Creating new CesiumGeoreference component...");
                    GameObject georefObject = new GameObject("CesiumGeoreference");
                    georeference = georefObject.AddComponent(georefType);
                    UnityEngine.Debug.Log($"Created CesiumGeoreference component: {georeference.GetType().Name}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("CesiumGeoreference type not found - Cesium package may not be installed");
                }
            }
            
            // Find dynamic camera
            if (dynamicCamera == null)
            {
                var cameraType = System.Type.GetType("CesiumForUnity.CesiumDynamicCamera, Cesium");
                if (cameraType != null)
                {
                    dynamicCamera = FindFirstObjectByType(cameraType) as Component;
                    UnityEngine.Debug.Log($"Found CesiumDynamicCamera: {dynamicCamera != null}");
                }
            }
            
            // Find all tilesets
            if (tilesets == null || tilesets.Count == 0)
            {
                tilesets = new List<Component>();
                var tilesetType = System.Type.GetType("CesiumForUnity.Cesium3DTileset, Cesium");
                if (tilesetType != null)
                {
                    var foundTilesets = FindObjectsByType(tilesetType, FindObjectsSortMode.None);
                    tilesets.AddRange(foundTilesets.Cast<Component>());
                    UnityEngine.Debug.Log($"Found {foundTilesets.Length} Cesium3DTileset components");
                }
            }
            
            UnityEngine.Debug.Log($"Final: Found {tilesets.Count} Cesium tilesets");
            
            // Additional diagnostic: Check what objects are in the scene
            var allComponents = FindObjectsByType<Component>(FindObjectsSortMode.None);
            var cesiumComponents = allComponents.Where(c => c.GetType().FullName.Contains("Cesium")).ToList();
            UnityEngine.Debug.Log($"All Cesium components in scene: {cesiumComponents.Count}");
            foreach (var comp in cesiumComponents)
            {
                UnityEngine.Debug.Log($"  - {comp.GetType().FullName} on {comp.gameObject.name}");
            }
        }
        
        private void InitializeCesiumWorld()
        {
            // Set world origin
            if (georeference != null)
            {
                // Use reflection to set properties safely
                var georefType = georeference.GetType();
                
                var latProperty = georefType.GetProperty("latitude");
                if (latProperty != null)
                    latProperty.SetValue(georeference, originLatitude);
                    
                var lonProperty = georefType.GetProperty("longitude");
                if (lonProperty != null)
                    lonProperty.SetValue(georeference, originLongitude);
                    
                var heightProperty = georefType.GetProperty("height");
                if (heightProperty != null)
                    heightProperty.SetValue(georeference, originHeight);
                
                UnityEngine.Debug.Log($"Set world origin to {originLatitude}, {originLongitude}, {originHeight}");
            }
            
            // Configure dynamic camera if available
            if (dynamicCamera != null)
            {
                // Set initial camera position for global view
                dynamicCamera.transform.position = new Vector3(0, 10000, 0);
                UnityEngine.Debug.Log("Configured Cesium dynamic camera");
            }
            
            // Configure tilesets
            ConfigureTilesets();
        }
        
        private void ConfigureTilesets()
        {
            foreach (var tileset in tilesets)
            {
                if (tileset != null)
                {
                    // Use reflection to configure performance settings safely
                    var tilesetType = tileset.GetType();
                    
                    var errorProperty = tilesetType.GetProperty("maximumScreenSpaceError");
                    if (errorProperty != null)
                        errorProperty.SetValue(tileset, 16.0);
                        
                    var loadsProperty = tilesetType.GetProperty("maximumSimultaneousTileLoads");
                    if (loadsProperty != null)
                        loadsProperty.SetValue(tileset, 20);
                        
                    var descendantProperty = tilesetType.GetProperty("loadingDescendantLimit");
                    if (descendantProperty != null)
                        descendantProperty.SetValue(tileset, 20);
                        
                    var frustumProperty = tilesetType.GetProperty("enableFrustumCulling");
                    if (frustumProperty != null)
                        frustumProperty.SetValue(tileset, enableFrustumCulling);
                        
                    var occlusionProperty = tilesetType.GetProperty("enableOcclusionCulling");
                    if (occlusionProperty != null)
                        occlusionProperty.SetValue(tileset, enableOcclusionCulling);
                    
                    UnityEngine.Debug.Log($"Configured tileset: {tileset.name}");
                }
            }
        }
        
        #endregion
        
        #region LOD System
        
        private void SetupLODSystem()
        {
            // Initialize LOD tileset groups
            foreach (LODLevel level in System.Enum.GetValues(typeof(LODLevel)))
            {
                lodTilesets[level] = new List<Component>();
            }
            
            // Assign tilesets to LOD levels based on their names or configuration
            foreach (var tileset in tilesets)
            {
                if (tileset.name.ToLower().Contains("global") || tileset.name.ToLower().Contains("low"))
                {
                    lodTilesets[LODLevel.Global].Add(tileset);
                }
                else if (tileset.name.ToLower().Contains("regional") || tileset.name.ToLower().Contains("medium"))
                {
                    lodTilesets[LODLevel.Regional].Add(tileset);
                }
                else if (tileset.name.ToLower().Contains("photorealistic") || tileset.name.ToLower().Contains("high"))
                {
                    lodTilesets[LODLevel.Tactical].Add(tileset);
                }
                else
                {
                    // Default to global LOD
                    lodTilesets[LODLevel.Global].Add(tileset);
                }
            }
            
            // Set initial LOD
            SetLOD(LODLevel.Global);
        }
        
        public void UpdateLOD(float cameraAltitude)
        {
            currentAltitude = cameraAltitude;
            
            LODLevel newLOD = currentLOD;
            
            if (cameraAltitude > globalViewThreshold)
            {
                newLOD = LODLevel.Global;
            }
            else if (cameraAltitude > regionalViewThreshold)
            {
                newLOD = LODLevel.Regional;
            }
            else
            {
                newLOD = LODLevel.Tactical;
            }
            
            if (newLOD != currentLOD)
            {
                SetLOD(newLOD);
            }
        }
        
        private void SetLOD(LODLevel newLOD)
        {
            UnityEngine.Debug.Log($"Switching LOD from {currentLOD} to {newLOD}");
            
            // Disable current LOD tilesets
            if (lodTilesets.ContainsKey(currentLOD))
            {
                foreach (var tileset in lodTilesets[currentLOD])
                {
                    if (tileset != null)
                    {
                        tileset.gameObject.SetActive(false);
                    }
                }
            }
            
            // Enable new LOD tilesets
            if (lodTilesets.ContainsKey(newLOD))
            {
                foreach (var tileset in lodTilesets[newLOD])
                {
                    if (tileset != null)
                    {
                        tileset.gameObject.SetActive(true);
                    }
                }
            }
            
            currentLOD = newLOD;
            OnLODChanged?.Invoke(currentLOD);
            
            // Update active tileset count
            activeTilesets = lodTilesets[currentLOD].Count;
        }
        
        public void SetGlobalView()
        {
            SetLOD(LODLevel.Global);
        }
        
        public void SetRegionalView()
        {
            SetLOD(LODLevel.Regional);
        }
        
        public void SetTacticalView()
        {
            SetLOD(LODLevel.Tactical);
        }
        
        #endregion
        
        #region Coordinate Conversion
        
        public Vector3 ConvertLatLonToUnityPosition(double latitude, double longitude, double height = 0.0)
        {
            if (georeference == null)
            {
                UnityEngine.Debug.LogError("Cannot convert coordinates - no CesiumGeoreference available");
                return Vector3.zero;
            }
            
            UnityEngine.Debug.Log($"🔧 Converting {latitude}, {longitude} at height {height}");
            
            // Try direct Cesium conversion first
            try 
            {
                // Check if this is a CesiumGeoreference component
                var georefType = georeference.GetType();
                UnityEngine.Debug.Log($"🔧 Georeference type: {georefType.Name} (Full: {georefType.FullName})");
                
                // Try to use the Cesium transformation method
                var transformMethod = georefType.GetMethod("TransformLongitudeLatitudeHeightToUnity");
                
                if (transformMethod != null)
                {
                    UnityEngine.Debug.Log("🔧 Found Cesium transformation method");
                    
                    // Create the longitude-latitude-height coordinate
                    var double3Type = System.Type.GetType("CesiumForUnity.double3, Cesium");
                    if (double3Type != null)
                    {
                        // Note: Cesium expects longitude, latitude, height order
                        var llaCoordinate = System.Activator.CreateInstance(double3Type, longitude, latitude, height);
                        var result = transformMethod.Invoke(georeference, new object[] { llaCoordinate });
                        
                        if (result != null)
                        {
                            // Extract Unity position from result
                            var resultType = result.GetType();
                            
                            // Try property first, then field
                            object xValue = null, yValue = null, zValue = null;
                            
                            var xProp = resultType.GetProperty("x");
                            if (xProp != null)
                                xValue = xProp.GetValue(result);
                            else
                            {
                                var xField = resultType.GetField("x");
                                if (xField != null) xValue = xField.GetValue(result);
                            }
                            
                            var yProp = resultType.GetProperty("y");
                            if (yProp != null)
                                yValue = yProp.GetValue(result);
                            else
                            {
                                var yField = resultType.GetField("y");
                                if (yField != null) yValue = yField.GetValue(result);
                            }
                            
                            var zProp = resultType.GetProperty("z");
                            if (zProp != null)
                                zValue = zProp.GetValue(result);
                            else
                            {
                                var zField = resultType.GetField("z");
                                if (zField != null) zValue = zField.GetValue(result);
                            }
                            
                            if (xValue != null && yValue != null && zValue != null)
                            {
                                float x = System.Convert.ToSingle(xValue);
                                float y = System.Convert.ToSingle(yValue);
                                float z = System.Convert.ToSingle(zValue);
                                
                                var cesiumWorldPos = new Vector3(x, y, z);
                                UnityEngine.Debug.Log($"🔧 Cesium native conversion result: {cesiumWorldPos}");
                                
                                // Account for any manual repositioning/scaling of the Cesium globe
                                // Apply the georeference transform to get the final world position
                                Vector3 finalWorldPos = georeference.transform.TransformPoint(cesiumWorldPos);
                                UnityEngine.Debug.Log($"🔧 Final world position (after globe transform): {finalWorldPos}");
                                
                                return finalWorldPos;
                            }
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("🔧 Cesium transformation method not found, using fallback");
                }
                
                // Fallback to our custom conversion
                Vector3 worldPos = ConvertLatLonToUnityFallback(latitude, longitude, height);
                UnityEngine.Debug.Log($"🔧 Fallback conversion result: {worldPos}");
                return worldPos;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"🔧 Conversion error: {ex.Message}");
                return ConvertLatLonToUnityFallback(latitude, longitude, height);
            }
        }
        
        private Vector3 ConvertLatLonToUnityFallback(double latitude, double longitude, double height)
        {
            // Find the actual visible Cesium globe object
            GameObject cesiumGlobe = null;
            
            // Look for common Cesium globe names
            string[] globeNames = { "CesiumGeoreference", "Cesium World", "Globe", "Earth", "CesiumGlobe" };
            foreach (string name in globeNames)
            {
                cesiumGlobe = GameObject.Find(name);
                if (cesiumGlobe != null) break;
            }
            
            // If not found, use the georeference transform
            if (cesiumGlobe == null && georeference != null)
            {
                cesiumGlobe = georeference.gameObject;
            }
            
            Vector3 globePosition = Vector3.zero;
            Vector3 globeScale = Vector3.one;
            
            if (cesiumGlobe != null)
            {
                globePosition = cesiumGlobe.transform.position;
                globeScale = cesiumGlobe.transform.lossyScale;
                
                UnityEngine.Debug.Log($"🌍 Found actual Cesium globe: {cesiumGlobe.name}");
                UnityEngine.Debug.Log($"  - Globe Position: {globePosition}");
                UnityEngine.Debug.Log($"  - Globe Scale: {globeScale}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("🌍 No Cesium globe found! Using default values.");
                globePosition = new Vector3(-3f, 506.9f, 8.04f); // From your logs
                globeScale = new Vector3(1000f, 1000f, 1000f); // Reasonable default
            }
            
            // Use a reasonable radius - if scale is too small, use a default
            float globeRadius = Mathf.Max(globeScale.x * 0.5f, 100f); // At least 100 units radius
            
            // Convert to radians
            double latRad = latitude * System.Math.PI / 180.0;
            double lonRad = longitude * System.Math.PI / 180.0;
            
            // Spherical to Cartesian conversion relative to globe center
            float x = (float)(globeRadius * System.Math.Cos(latRad) * System.Math.Cos(lonRad));
            float z = (float)(globeRadius * System.Math.Cos(latRad) * System.Math.Sin(lonRad));
            float y = (float)(globeRadius * System.Math.Sin(latRad));
            
            // Position relative to the actual globe center
            Vector3 relativePosition = new Vector3(x, y, z);
            Vector3 worldPosition = globePosition + relativePosition;
            
            // Add small height offset to place fires on surface
            Vector3 surfaceNormal = relativePosition.normalized;
            worldPosition += surfaceNormal * (float)height * 0.01f; // Small height offset
            
            UnityEngine.Debug.Log($"🌍 Fallback conversion:");
            UnityEngine.Debug.Log($"  - Input: Lat={latitude:F6}, Lon={longitude:F6}, Height={height:F2}");
            UnityEngine.Debug.Log($"  - Globe Radius: {globeRadius:F3}");
            UnityEngine.Debug.Log($"  - Relative Position: {relativePosition}");
            UnityEngine.Debug.Log($"  - Final World Position: {worldPosition}");
            
            return worldPosition;
        }
        
        public (double latitude, double longitude, double height) ConvertUnityPositionToLatLon(Vector3 unityPosition)
        {
            if (georeference == null)
            {
                UnityEngine.Debug.LogError("Cannot convert coordinates - no CesiumGeoreference available");
                return (0.0, 0.0, 0.0);
            }
            
            // Convert Unity world coordinates to geodetic coordinates using reflection
            var georefType = georeference.GetType();
            var transformMethod = georefType.GetMethod("TransformUnityToLongitudeLatitudeHeight");
            
            if (transformMethod != null)
            {
                var double3Type = System.Type.GetType("CesiumForUnity.double3, Cesium");
                if (double3Type != null)
                {
                    var double3Instance = System.Activator.CreateInstance(double3Type, 
                        (double)unityPosition.x, (double)unityPosition.y, (double)unityPosition.z);
                    var result = transformMethod.Invoke(georeference, new object[] { double3Instance });
                    
                    if (result != null)
                    {
                        var resultType = result.GetType();
                        var xProp = resultType.GetProperty("x");
                        var yProp = resultType.GetProperty("y");
                        var zProp = resultType.GetProperty("z");
                        
                        if (xProp != null && yProp != null && zProp != null)
                        {
                            return ((double)yProp.GetValue(result), (double)xProp.GetValue(result), (double)zProp.GetValue(result));
                        }
                    }
                }
            }
            
            return (0.0, 0.0, 0.0);
        }
        
        public float CalculateDistanceOnEarth(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for calculating distance between two points on Earth
            const double R = 6371000; // Earth's radius in meters
            
            double lat1Rad = lat1 * Mathf.Deg2Rad;
            double lat2Rad = lat2 * Mathf.Deg2Rad;
            double deltaLatRad = (lat2 - lat1) * Mathf.Deg2Rad;
            double deltaLonRad = (lon2 - lon1) * Mathf.Deg2Rad;
            
            double a = Mathf.Sin((float)(deltaLatRad / 2)) * Mathf.Sin((float)(deltaLatRad / 2)) +
                      Mathf.Cos((float)lat1Rad) * Mathf.Cos((float)lat2Rad) *
                      Mathf.Sin((float)(deltaLonRad / 2)) * Mathf.Sin((float)(deltaLonRad / 2));
            
            double c = 2 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1 - a)));
            
            return (float)(R * c);
        }
        
        #endregion
        
        #region Object Anchoring
        
        public GameObject AnchorObjectToEarth(GameObject obj, double latitude, double longitude, double height = 0.0)
        {
            if (obj == null)
            {
                UnityEngine.Debug.LogError("Cannot anchor null object");
                return null;
            }
            
            // Add CesiumGlobeAnchor component if it doesn't exist using reflection
            var globeAnchorType = System.Type.GetType("CesiumForUnity.CesiumGlobeAnchor, Cesium");
            Component globeAnchor = null;
            
            if (globeAnchorType != null)
            {
                globeAnchor = obj.GetComponent(globeAnchorType);
                if (globeAnchor == null)
                {
                    globeAnchor = obj.AddComponent(globeAnchorType);
                }
                
                // Set the anchor position using reflection
                var positionProperty = globeAnchorType.GetProperty("longitudeLatitudeHeight");
                if (positionProperty != null)
                {
                    var double3Type = System.Type.GetType("CesiumForUnity.double3, Cesium");
                    if (double3Type != null)
                    {
                        var double3Instance = System.Activator.CreateInstance(double3Type, longitude, latitude, height);
                        positionProperty.SetValue(globeAnchor, double3Instance);
                    }
                }
            }
            
            // Store reference
            string key = $"{obj.name}_{obj.GetInstanceID()}";
            anchoredObjects[key] = obj;
            
            UnityEngine.Debug.Log($"Anchored {obj.name} to {latitude}, {longitude}, {height}");
            return obj;
        }
        
        public void RemoveAnchoredObject(GameObject obj)
        {
            if (obj == null)
                return;
                
            string key = $"{obj.name}_{obj.GetInstanceID()}";
            if (anchoredObjects.ContainsKey(key))
            {
                anchoredObjects.Remove(key);
            }
            
            // Remove globe anchor component
            var globeAnchorType = System.Type.GetType("CesiumForUnity.CesiumGlobeAnchor, Cesium");
            if (globeAnchorType != null)
            {
                var globeAnchor = obj.GetComponent(globeAnchorType);
                if (globeAnchor != null)
                {
                    DestroyImmediate(globeAnchor);
                }
            }
        }
        
        public List<GameObject> GetAnchoredObjects()
        {
            return new List<GameObject>(anchoredObjects.Values);
        }
        
        #endregion
        
        #region World Origin Management
        
        public void SetWorldOrigin(double latitude, double longitude, double height = 0.0)
        {
            originLatitude = latitude;
            originLongitude = longitude;
            originHeight = height;
            
            if (georeference != null)
            {
                // Use reflection to set properties safely
                var georefType = georeference.GetType();
                
                var latProperty = georefType.GetProperty("latitude");
                if (latProperty != null)
                    latProperty.SetValue(georeference, latitude);
                    
                var lonProperty = georefType.GetProperty("longitude");
                if (lonProperty != null)
                    lonProperty.SetValue(georeference, longitude);
                    
                var heightProperty = georefType.GetProperty("height");
                if (heightProperty != null)
                    heightProperty.SetValue(georeference, height);
            }
            
            worldOrigin = new Vector3((float)longitude, (float)height, (float)latitude);
            OnWorldOriginChanged?.Invoke(worldOrigin);
            
            UnityEngine.Debug.Log($"World origin set to {latitude}, {longitude}, {height}");
        }
        
        public void CenterOnPosition(double latitude, double longitude)
        {
            SetWorldOrigin(latitude, longitude, originHeight);
        }
        
        #endregion
        
        #region Performance Management
        
        public void SetPerformanceSettings(int maxTiles, float timeout, bool frustumCulling, bool occlusionCulling)
        {
            maxTilesLoaded = maxTiles;
            tileLoadTimeout = timeout;
            enableFrustumCulling = frustumCulling;
            enableOcclusionCulling = occlusionCulling;
            
            // Apply settings to all tilesets
            foreach (var tileset in tilesets)
            {
                if (tileset != null)
                {
                    var tilesetType = tileset.GetType();
                    
                    var maxTilesProperty = tilesetType.GetProperty("maximumSimultaneousTileLoads");
                    if (maxTilesProperty != null)
                        maxTilesProperty.SetValue(tileset, maxTiles / 10); // Distribute across tilesets
                        
                    var frustumProperty = tilesetType.GetProperty("enableFrustumCulling");
                    if (frustumProperty != null)
                        frustumProperty.SetValue(tileset, frustumCulling);
                        
                    var occlusionProperty = tilesetType.GetProperty("enableOcclusionCulling");
                    if (occlusionProperty != null)
                        occlusionProperty.SetValue(tileset, occlusionCulling);
                }
            }
            
            UnityEngine.Debug.Log($"Performance settings updated: MaxTiles={maxTiles}, Frustum={frustumCulling}, Occlusion={occlusionCulling}");
        }
        
        public void OptimizeForWebGL()
        {
            // Reduce quality settings for WebGL performance
            SetPerformanceSettings(50, 15f, true, false);
            
            foreach (var tileset in tilesets)
            {
                if (tileset != null)
                {
                    var tilesetType = tileset.GetType();
                    
                    var screenSpaceErrorProperty = tilesetType.GetProperty("maximumScreenSpaceError");
                    if (screenSpaceErrorProperty != null)
                        screenSpaceErrorProperty.SetValue(tileset, 32.0); // Lower quality
                        
                    var maxTilesProperty = tilesetType.GetProperty("maximumSimultaneousTileLoads");
                    if (maxTilesProperty != null)
                        maxTilesProperty.SetValue(tileset, 5); // Fewer concurrent loads
                }
            }
            
            UnityEngine.Debug.Log("Optimized settings for WebGL");
        }
        
        #endregion
        
        #region Status and Debugging
        
        public string GetStatusReport()
        {
            return $"Geospatial Status:\n" +
                   $"Initialized: {isInitialized}\n" +
                   $"Current LOD: {currentLOD}\n" +
                   $"Altitude: {currentAltitude:F1}m\n" +
                   $"Active Tilesets: {activeTilesets}\n" +
                   $"Anchored Objects: {anchoredObjects.Count}\n" +
                   $"World Origin: {originLatitude:F6}, {originLongitude:F6}";
        }
        
        public int GetTotalTileCount()
        {
            int totalTiles = 0;
            foreach (var tileset in tilesets)
            {
                if (tileset != null && tileset.gameObject.activeInHierarchy)
                {
                    // This would require access to Cesium's internal tile count
                    // For now, return estimated count
                    totalTiles += 50; // Placeholder
                }
            }
            return totalTiles;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Update()
        {
            if (!isInitialized)
                return;
                
            // Update LOD based on camera altitude
            if (dynamicCamera != null)
            {
                float altitude = dynamicCamera.transform.position.y;
                UpdateLOD(altitude);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up anchored objects
            anchoredObjects?.Clear();
            lodTilesets?.Clear();
        }
        
        #endregion
    }
}

