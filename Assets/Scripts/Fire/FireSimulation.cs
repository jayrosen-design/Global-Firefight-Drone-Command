using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GlobalFirefight.Data;
using GlobalFirefight.Core;
using GlobalFirefight.Geospatial;

namespace GlobalFirefight.Fire
{
    public class FireSimulation : MonoBehaviour
    {
        [Header("Fire Management")]
        [SerializeField] private Dictionary<string, FireIncident> activeFires;
        [SerializeField] private List<FireIncident> extinguishedFires;
        [SerializeField] private GameObject fireVisualizationPrefab;
        [SerializeField] private Transform fireParent;
        [SerializeField] private int maxDisplayedFires = 500;
        
        [Header("Simulation Settings")]
        [SerializeField] private float fireUpdateInterval = 1f; // Update fires every second
        [SerializeField] private float spreadSimulationRate = 0.1f; // Spread calculation rate
        [SerializeField] private bool enableFireSpread = true;
        [SerializeField] private bool enableFireGrowth = true;
        [SerializeField] private float globalSpreadMultiplier = 1f;
        
        [Header("Environmental Factors")]
        [SerializeField] private Vector2 windDirection = Vector2.right;
        [SerializeField] private float windSpeed = 5f; // m/s
        [SerializeField] private float humidity = 50f; // 0-100%
        [SerializeField] private float temperature = 25f; // Celsius
        [SerializeField] private float fuelMoisture = 30f; // 0-100%
        
        [Header("Visual Settings")]
        [SerializeField] private bool enableParticleEffects = true;
        [SerializeField] private int particleQuality = 2; // 0=Low, 1=Medium, 2=High
        [SerializeField] private float maxParticleDistance = 2000f;
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Performance")]
        [SerializeField] private int firesPerFrameUpdate = 10;
        [SerializeField] private float cullingDistance = 5000f;
        [SerializeField] private bool enableLODOptimization = true;
        
        [Header("Status")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private int totalFiresLoaded = 0;
        [SerializeField] private int activeFireCount = 0;
        [SerializeField] private int extinguishedFireCount = 0;
        [SerializeField] private float lastUpdateTime = 0f;
        
        // Private fields
        private GeospatialManager geospatialManager;
        private Dictionary<string, GameObject> fireVisualizations;
        private Queue<string> fireUpdateQueue;
        private Coroutine simulationCoroutine;
        private Camera mainCamera;
        
        // Events
        public static event Action<FireIncident> OnFireSpawned;
        public static event Action<FireIncident> OnFireExtinguished;
        public static event Action<FireIncident> OnFireSpread;
        public static event Action<FireIncident, float> OnFireSuppressed;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public int ActiveFireCount => activeFireCount;
        public int ExtinguishedFireCount => extinguishedFireCount;
        public int TotalFireCount => totalFiresLoaded;
        public Dictionary<string, FireIncident> ActiveFires => activeFires;
        
        #region Initialization
        
        public void Initialize()
        {
            UnityEngine.Debug.Log("Initializing Fire Simulation...");
            
            // Initialize collections
            activeFires = new Dictionary<string, FireIncident>();
            extinguishedFires = new List<FireIncident>();
            fireVisualizations = new Dictionary<string, GameObject>();
            fireUpdateQueue = new Queue<string>();
            
            // Get required components
            geospatialManager = FindFirstObjectByType<GeospatialManager>();
            mainCamera = Camera.main;
            
            // Create fire parent if needed
            if (fireParent == null)
            {
                GameObject fireParentObj = new GameObject("Fires");
                fireParent = fireParentObj.transform;
            }
            
            // Create default fire prefab if needed
            if (fireVisualizationPrefab == null)
            {
                CreateDefaultFirePrefab();
            }
            
            // Start simulation
            StartSimulation();
            
            isInitialized = true;
            UnityEngine.Debug.Log("Fire Simulation initialized successfully");
        }
        
        private void CreateDefaultFirePrefab()
        {
            // Create a basic fire visualization prefab
            GameObject firePrefab = new GameObject("DefaultFire");
            
            // Add particle system for fire effect
            var particleSystem = firePrefab.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startLifetime = 2f;
            main.startSpeed = 5f;
            main.startSize = 2f;
            main.startColor = Color.red;
            main.maxParticles = 100;
            
            var emission = particleSystem.emission;
            emission.rateOverTime = 50f;
            
            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1f;
            
            // Add light component
            var light = firePrefab.AddComponent<Light>();
            light.color = Color.red;
            light.intensity = 2f;
            light.range = 50f;
            light.type = LightType.Point;
            
            // Add fire controller component
            firePrefab.AddComponent<FireVisualization>();
            
            fireVisualizationPrefab = firePrefab;
            UnityEngine.Debug.Log("Created default fire prefab");
        }
        
        #endregion
        
        #region Fire Management
        
        public void LoadFireData(List<FireIncident> fireData)
        {
            if (fireData == null || fireData.Count == 0)
            {
                UnityEngine.Debug.LogWarning("No fire data to load");
                return;
            }
            
            UnityEngine.Debug.Log($"Loading {fireData.Count} fire incidents...");
            
            // Clear existing fires
            ClearAllFires();
            
            // Load new fires
            int loaded = 0;
            foreach (var fire in fireData)
            {
                if (loaded >= maxDisplayedFires)
                {
                    UnityEngine.Debug.LogWarning($"Reached maximum fire display limit ({maxDisplayedFires})");
                    break;
                }
                
                if (AddFire(fire))
                {
                    loaded++;
                }
            }
            
            totalFiresLoaded = loaded;
            activeFireCount = activeFires.Count;
            
            UnityEngine.Debug.Log($"Successfully loaded {loaded} fires");
        }
        
        public bool AddFire(FireIncident fire)
        {
            if (fire == null || activeFires.ContainsKey(fire.id))
                return false;
            
            // Add to active fires
            activeFires[fire.id] = fire;
            fireUpdateQueue.Enqueue(fire.id);
            
            // Create visualization
            CreateFireVisualization(fire);
            
            // Notify systems
            OnFireSpawned?.Invoke(fire);
            
            return true;
        }
        
        public void RemoveFire(string fireId)
        {
            if (!activeFires.ContainsKey(fireId))
                return;
            
            FireIncident fire = activeFires[fireId];
            
            // Move to extinguished list
            extinguishedFires.Add(fire);
            activeFires.Remove(fireId);
            
            // Remove visualization
            DestroyFireVisualization(fireId);
            
            // Update counts
            activeFireCount = activeFires.Count;
            extinguishedFireCount = extinguishedFires.Count;
            
            // Notify systems
            OnFireExtinguished?.Invoke(fire);
            
            UnityEngine.Debug.Log($"Fire {fireId} removed from simulation");
        }
        
        public void ClearAllFires()
        {
            // Clear visualizations
            foreach (var fireId in activeFires.Keys.ToList())
            {
                DestroyFireVisualization(fireId);
            }
            
            // Clear collections
            activeFires.Clear();
            extinguishedFires.Clear();
            fireUpdateQueue.Clear();
            
            // Reset counts
            activeFireCount = 0;
            extinguishedFireCount = 0;
            
            UnityEngine.Debug.Log("All fires cleared from simulation");
        }
        
        #endregion
        
        #region Fire Visualization
        
        private void CreateFireVisualization(FireIncident fire)
        {
            if (fireVisualizationPrefab == null || geospatialManager == null)
                return;
            
            // Instantiate fire visualization
            GameObject fireObj = Instantiate(fireVisualizationPrefab, fireParent);
            fireObj.name = $"Fire_{fire.id}";
            
            // Anchor to Earth coordinates
            geospatialManager.AnchorObjectToEarth(fireObj, fire.latitude, fire.longitude, 0);
            
            // Configure fire visualization component
            var fireVis = fireObj.GetComponent<FireVisualization>();
            if (fireVis != null)
            {
                fireVis.Initialize(fire);
            }
            
            // Store reference
            fireVisualizations[fire.id] = fireObj;
        }
        
        private void DestroyFireVisualization(string fireId)
        {
            if (fireVisualizations.ContainsKey(fireId))
            {
                GameObject fireObj = fireVisualizations[fireId];
                if (fireObj != null)
                {
                    // Remove geospatial anchoring
                    if (geospatialManager != null)
                    {
                        geospatialManager.RemoveAnchoredObject(fireObj);
                    }
                    
                    Destroy(fireObj);
                }
                
                fireVisualizations.Remove(fireId);
            }
        }
        
        private void UpdateFireVisualizations()
        {
            foreach (var kvp in fireVisualizations)
            {
                string fireId = kvp.Key;
                GameObject fireObj = kvp.Value;
                
                if (fireObj != null && activeFires.ContainsKey(fireId))
                {
                    FireIncident fire = activeFires[fireId];
                    var fireVis = fireObj.GetComponent<FireVisualization>();
                    
                    if (fireVis != null)
                    {
                        fireVis.UpdateVisualization(fire);
                    }
                    
                    // LOD optimization
                    if (enableLODOptimization)
                    {
                        UpdateFireLOD(fireObj, fire);
                    }
                }
            }
        }
        
        private void UpdateFireLOD(GameObject fireObj, FireIncident fire)
        {
            if (mainCamera == null)
                return;
            
            float distance = Vector3.Distance(fireObj.transform.position, mainCamera.transform.position);
            
            // Disable particle effects for distant fires
            var particleSystem = fireObj.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                bool shouldEmit = distance < maxParticleDistance && enableParticleEffects;
                var emission = particleSystem.emission;
                emission.enabled = shouldEmit;
            }
            
            // Adjust light range based on distance
            var light = fireObj.GetComponent<Light>();
            if (light != null)
            {
                light.enabled = distance < cullingDistance;
                light.intensity = fire.lightIntensity * Mathf.Clamp01(1f - (distance / cullingDistance));
            }
        }
        
        #endregion
        
        #region Fire Simulation
        
        private void StartSimulation()
        {
            if (simulationCoroutine != null)
            {
                StopCoroutine(simulationCoroutine);
            }
            
            simulationCoroutine = StartCoroutine(SimulationLoop());
        }
        
        private IEnumerator SimulationLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(fireUpdateInterval);
                
                if (activeFires.Count > 0)
                {
                    UpdateFireSimulation();
                    UpdateFireVisualizations();
                }
                
                lastUpdateTime = Time.time;
            }
        }
        
        public void UpdateSimulation(float deltaTime)
        {
            // This method is called from GameManager for frame-based updates
            // The main simulation runs in the coroutine above
        }
        
        private void UpdateFireSimulation()
        {
            int firesUpdated = 0;
            
            // Process fires in queue
            while (fireUpdateQueue.Count > 0 && firesUpdated < firesPerFrameUpdate)
            {
                string fireId = fireUpdateQueue.Dequeue();
                
                if (activeFires.ContainsKey(fireId))
                {
                    FireIncident fire = activeFires[fireId];
                    UpdateSingleFire(fire);
                    
                    // Re-queue if fire is still active
                    if (fire.status == FireStatus.Active || fire.status == FireStatus.Spreading)
                    {
                        fireUpdateQueue.Enqueue(fireId);
                    }
                }
                
                firesUpdated++;
            }
        }
        
        private void UpdateSingleFire(FireIncident fire)
        {
            if (fire.status == FireStatus.Extinguished)
                return;
            
            float deltaTime = fireUpdateInterval;
            
            // Update fire spread
            if (enableFireSpread)
            {
                UpdateFireSpread(fire, deltaTime);
            }
            
            // Update fire growth
            if (enableFireGrowth)
            {
                UpdateFireGrowth(fire, deltaTime);
            }
            
            // Check if fire should be extinguished
            if (fire.health <= 0f)
            {
                fire.status = FireStatus.Extinguished;
                RemoveFire(fire.id);
            }
        }
        
        private void UpdateFireSpread(FireIncident fire, float deltaTime)
        {
            // Calculate spread rate based on environmental factors
            float spreadRate = CalculateSpreadRate(fire);
            
            // Update spread radius
            float oldRadius = fire.spreadRadius;
            fire.spreadRadius += spreadRate * deltaTime * globalSpreadMultiplier;
            
            // Notify if spread increased significantly
            if (fire.spreadRadius - oldRadius > 10f)
            {
                OnFireSpread?.Invoke(fire);
            }
            
            // Update damage radius (area affecting property/lives)
            fire.damageRadius = fire.spreadRadius * 0.8f;
        }
        
        private void UpdateFireGrowth(FireIncident fire, float deltaTime)
        {
            // Fire grows in intensity if not being suppressed
            float growthRate = CalculateGrowthRate(fire);
            
            if (fire.status != FireStatus.BeingExtinguished)
            {
                fire.health = Mathf.Min(fire.maxHealth, fire.health + growthRate * deltaTime);
                fire.intensity = Mathf.Min(10f, fire.intensity + growthRate * 0.1f * deltaTime);
            }
            
            // Update visual properties
            fire.UpdateVisualProperties();
        }
        
        private float CalculateSpreadRate(FireIncident fire)
        {
            // Base spread rate from fire intensity
            float baseRate = fire.intensity * 0.5f; // meters per second
            
            // Wind factor
            float windFactor = 1f + (windSpeed / 10f);
            
            // Humidity factor (lower humidity = faster spread)
            float humidityFactor = 1f + ((100f - humidity) / 100f);
            
            // Temperature factor (higher temp = faster spread)
            float tempFactor = 1f + ((temperature - 20f) / 30f);
            
            // Fuel moisture factor (drier fuel = faster spread)
            float fuelFactor = 1f + ((100f - fuelMoisture) / 100f);
            
            return baseRate * windFactor * humidityFactor * tempFactor * fuelFactor;
        }
        
        private float CalculateGrowthRate(FireIncident fire)
        {
            // Base growth rate
            float baseGrowth = fire.intensity * 0.1f;
            
            // Environmental factors
            float envFactor = (1f + (windSpeed / 20f)) * (1f + ((100f - humidity) / 200f));
            
            return baseGrowth * envFactor;
        }
        
        #endregion
        
        #region Fire Suppression
        
        public void ApplySuppressionToFire(string fireId, SuppressantType type, float amount, Vector3 dropPosition)
        {
            if (!activeFires.ContainsKey(fireId))
                return;
            
            FireIncident fire = activeFires[fireId];
            float effectiveness = CalculateSuppressionEffectiveness(fire, type, dropPosition);
            float damage = amount * effectiveness;
            
            // Apply suppression damage
            fire.ApplySuppressionDamage(damage);
            fire.status = FireStatus.BeingExtinguished;
            
            // Create visual effect at drop position
            CreateSuppressionEffect(dropPosition, type);
            
            // Notify systems
            OnFireSuppressed?.Invoke(fire, damage);
            
            UnityEngine.Debug.Log($"Applied {damage:F1} suppression damage to fire {fireId} using {type}");
        }
        
        private float CalculateSuppressionEffectiveness(FireIncident fire, SuppressantType type, Vector3 dropPosition)
        {
            // Base effectiveness by suppressant type
            float baseEffectiveness = 1f;
            
            switch (type)
            {
                case SuppressantType.Water:
                    baseEffectiveness = 1f;
                    break;
                case SuppressantType.LongTermRetardant:
                    baseEffectiveness = 1.5f;
                    break;
                case SuppressantType.ClassAFoam:
                    baseEffectiveness = 2f;
                    break;
                case SuppressantType.AerialIgnitionSpheres:
                    baseEffectiveness = 0.5f; // Used for backburns, not direct suppression
                    break;
            }
            
            // Distance factor (closer drops are more effective)
            if (fireVisualizations.ContainsKey(fire.id))
            {
                Vector3 firePosition = fireVisualizations[fire.id].transform.position;
                float distance = Vector3.Distance(dropPosition, firePosition);
                float distanceFactor = Mathf.Clamp01(1f - (distance / fire.spreadRadius));
                baseEffectiveness *= distanceFactor;
            }
            
            // Environmental factors
            float windFactor = 1f - (windSpeed / 50f); // Wind reduces effectiveness
            float humidityFactor = 1f + (humidity / 200f); // Humidity helps
            
            return baseEffectiveness * windFactor * humidityFactor;
        }
        
        private void CreateSuppressionEffect(Vector3 position, SuppressantType type)
        {
            // Create particle effect for suppressant drop
            // This would be expanded with proper VFX
            
            Color effectColor = Color.blue;
            switch (type)
            {
                case SuppressantType.LongTermRetardant:
                    effectColor = Color.red;
                    break;
                case SuppressantType.ClassAFoam:
                    effectColor = Color.white;
                    break;
                case SuppressantType.AerialIgnitionSpheres:
                    effectColor = Color.orange;
                    break;
            }
            
            // Simple debug visualization
            UnityEngine.Debug.DrawRay(position, Vector3.up * 10f, effectColor, 2f);
        }
        
        #endregion
        
        #region Environmental Controls
        
        public void SetWindConditions(Vector2 direction, float speed)
        {
            windDirection = direction.normalized;
            windSpeed = Mathf.Clamp(speed, 0f, 50f);
            
            UnityEngine.Debug.Log($"Wind conditions updated: {windDirection} at {windSpeed} m/s");
        }
        
        public void SetWeatherConditions(float temp, float humid, float fuel)
        {
            temperature = Mathf.Clamp(temp, -10f, 50f);
            humidity = Mathf.Clamp(humid, 0f, 100f);
            fuelMoisture = Mathf.Clamp(fuel, 0f, 100f);
            
            UnityEngine.Debug.Log($"Weather updated: {temperature}°C, {humidity}% humidity, {fuelMoisture}% fuel moisture");
        }
        
        #endregion
        
        #region Public API
        
        public FireIncident GetFire(string fireId)
        {
            return activeFires.ContainsKey(fireId) ? activeFires[fireId] : null;
        }
        
        public List<FireIncident> GetFiresInRadius(Vector3 center, float radius)
        {
            List<FireIncident> firesInRadius = new List<FireIncident>();
            
            foreach (var fire in activeFires.Values)
            {
                if (fireVisualizations.ContainsKey(fire.id))
                {
                    Vector3 firePos = fireVisualizations[fire.id].transform.position;
                    if (Vector3.Distance(center, firePos) <= radius)
                    {
                        firesInRadius.Add(fire);
                    }
                }
            }
            
            return firesInRadius;
        }
        
        public void SetMaxFireDisplay(int maxFires)
        {
            maxDisplayedFires = maxFires;
        }
        
        public string GetSimulationStatus()
        {
            return $"Fire Simulation Status:\n" +
                   $"Active Fires: {activeFireCount}\n" +
                   $"Extinguished: {extinguishedFireCount}\n" +
                   $"Total Loaded: {totalFiresLoaded}\n" +
                   $"Update Interval: {fireUpdateInterval}s\n" +
                   $"Wind: {windDirection} at {windSpeed} m/s\n" +
                   $"Weather: {temperature}°C, {humidity}% humidity";
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnDestroy()
        {
            if (simulationCoroutine != null)
            {
                StopCoroutine(simulationCoroutine);
            }
            
            ClearAllFires();
        }
        
        #endregion
    }
    
    #region Fire Visualization Component
    
    public class FireVisualization : MonoBehaviour
    {
        private FireIncident fireData;
        private ParticleSystem fireParticles;
        private Light fireLight;
        private AudioSource fireAudio;
        
        public void Initialize(FireIncident fire)
        {
            fireData = fire;
            
            // Get components
            fireParticles = GetComponent<ParticleSystem>();
            fireLight = GetComponent<Light>();
            fireAudio = GetComponent<AudioSource>();
            
            // Initial setup
            UpdateVisualization(fire);
        }
        
        public void UpdateVisualization(FireIncident fire)
        {
            if (fire == null)
                return;
            
            fireData = fire;
            
            // Update particle system
            if (fireParticles != null)
            {
                var main = fireParticles.main;
                main.startColor = fire.fireColor;
                main.startSize = fire.intensity * 0.5f;
                
                var emission = fireParticles.emission;
                emission.rateOverTime = fire.smokeIntensity * 50f;
            }
            
            // Update lighting
            if (fireLight != null)
            {
                fireLight.color = fire.fireColor;
                fireLight.intensity = fire.lightIntensity;
                fireLight.range = fire.spreadRadius * 0.1f;
            }
            
            // Update scale based on spread radius
            transform.localScale = Vector3.one * (fire.spreadRadius / 100f);
        }
    }
    
    #endregion
}

