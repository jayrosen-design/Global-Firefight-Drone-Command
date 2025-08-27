using System;
using System.Collections.Generic;
using UnityEngine;
using GlobalFirefight.Data;
using GlobalFirefight.Core;

namespace GlobalFirefight.Systems
{
    public class ScoringSystem : MonoBehaviour
    {
        [Header("Economic Constants")]
        [SerializeField] private float baseCostPerSquareMeter = 100f;
        [SerializeField] private float timberValuePerAcre = 1500f;
        [SerializeField] private float ecosystemValuePerAcre = 500f;
        [SerializeField] private float valueOfStatisticalLife = 10000000f; // $10M per life
        
        [Header("Building Density Factors")]
        [SerializeField] private float ruralDensityFactor = 0.1f;
        [SerializeField] private float suburbanDensityFactor = 0.5f;
        [SerializeField] private float urbanDensityFactor = 1.0f;
        [SerializeField] private float industrialDensityFactor = 0.8f;
        
        [Header("Operational Costs")]
        [SerializeField] private float baseDroneDeploymentCost = 50000f;
        [SerializeField] private float operationalCostPerMinute = 500f;
        [SerializeField] private float waterCostPerUnit = 10f;
        [SerializeField] private float retardantCostPerUnit = 25f;
        [SerializeField] private float foamCostPerUnit = 15f;
        
        [Header("Current Score")]
        [SerializeField] private ScoreData currentScore;
        [SerializeField] private Dictionary<string, float> firePropertyValues;
        [SerializeField] private Dictionary<string, int> firePopulationAtRisk;
        [SerializeField] private float totalScoreThisFrame;
        
        [Header("Performance Tracking")]
        [SerializeField] private List<ScoreEvent> scoreEvents;
        [SerializeField] private float lastUpdateTime;
        [SerializeField] private bool isInitialized = false;
        
        // Events
        public static event Action<ScoreData> OnScoreUpdated;
        public static event Action<ScoreEvent> OnScoreEvent;
        public static event Action<float> OnPropertySaved;
        public static event Action<int> OnLivesSaved;
        
        // Properties
        public ScoreData CurrentScore => currentScore;
        public bool IsInitialized => isInitialized;
        public float TotalScore => currentScore?.finalScore ?? 0f;
        
        #region Initialization
        
        public void Initialize()
        {
            currentScore = new ScoreData();
            firePropertyValues = new Dictionary<string, float>();
            firePopulationAtRisk = new Dictionary<string, int>();
            scoreEvents = new List<ScoreEvent>();
            
            lastUpdateTime = Time.time;
            isInitialized = true;
            
            UnityEngine.Debug.Log("Scoring System initialized");
        }
        
        #endregion
        
        #region Score Updates
        
        public void UpdateScoring(float deltaTime)
        {
            if (!isInitialized)
                return;
                
            // Update operational costs for active operations
            UpdateOperationalCosts(deltaTime);
            
            // Calculate current total score
            currentScore.CalculateFinalScore();
            totalScoreThisFrame = currentScore.finalScore;
            
            // Notify listeners if score changed significantly
            float timeSinceLastUpdate = Time.time - lastUpdateTime;
            if (timeSinceLastUpdate >= 1f) // Update every second
            {
                OnScoreUpdated?.Invoke(currentScore);
                lastUpdateTime = Time.time;
            }
        }
        
        private void UpdateOperationalCosts(float deltaTime)
        {
            // Get active drones from fleet manager
            var fleetManager = FindFirstObjectByType<GlobalFirefight.Drones.DroneFleetManager>();
            if (fleetManager != null)
            {
                var activeDrones = fleetManager.GetAllActiveDrones();
                
                foreach (var drone in activeDrones)
                {
                    if (drone.state != DroneState.Idle && drone.state != DroneState.Maintenance)
                    {
                        float costPerSecond = drone.specifications.operationalCostPerMinute / 60f;
                        currentScore.AddOperationalCost(costPerSecond * deltaTime);
                    }
                }
            }
        }
        
        #endregion
        
        #region Fire Assessment
        
        public void AssessFireThreat(FireIncident fire)
        {
            if (fire == null || firePropertyValues.ContainsKey(fire.id))
                return; // Already assessed
                
            // Calculate property value at risk
            float propertyValue = CalculatePropertyValueAtRisk(fire);
            firePropertyValues[fire.id] = propertyValue;
            
            // Calculate population at risk
            int populationAtRisk = CalculatePopulationAtRisk(fire);
            firePopulationAtRisk[fire.id] = populationAtRisk;
            
            // Update fire object
            fire.propertyValue = propertyValue;
            fire.populationAtRisk = populationAtRisk;
            fire.threateningPopulation = populationAtRisk > 0;
            
            UnityEngine.Debug.Log($"Fire {fire.id} assessed: ${propertyValue:N0} property, {populationAtRisk} people at risk");
        }
        
        private float CalculatePropertyValueAtRisk(FireIncident fire)
        {
            // Calculate area affected by fire spread
            float affectedAreaSqM = Mathf.PI * fire.spreadRadius * fire.spreadRadius;
            
            // Determine building density factor based on location
            float densityFactor = GetBuildingDensityFactor(fire.latitude, fire.longitude);
            
            // Calculate total property value
            float propertyValue = affectedAreaSqM * densityFactor * baseCostPerSquareMeter;
            
            // Add timber and ecosystem value for wilderness areas
            if (densityFactor <= ruralDensityFactor)
            {
                float acres = affectedAreaSqM * 0.000247105f; // Convert sq meters to acres
                float timberValue = acres * timberValuePerAcre;
                float ecosystemValue = acres * ecosystemValuePerAcre;
                
                fire.resourceValue = timberValue + ecosystemValue;
                propertyValue += fire.resourceValue;
            }
            
            return propertyValue;
        }
        
        private int CalculatePopulationAtRisk(FireIncident fire)
        {
            // Simplified population density calculation
            // In a real implementation, this would use actual population data
            
            float densityFactor = GetBuildingDensityFactor(fire.latitude, fire.longitude);
            
            if (densityFactor >= urbanDensityFactor)
            {
                // Urban area: high population density
                return UnityEngine.Random.Range(1000, 10000);
            }
            else if (densityFactor >= suburbanDensityFactor)
            {
                // Suburban area: moderate population
                return UnityEngine.Random.Range(100, 2000);
            }
            else if (densityFactor >= ruralDensityFactor)
            {
                // Rural area: low population
                return UnityEngine.Random.Range(10, 200);
            }
            else
            {
                // Wilderness: no population
                return 0;
            }
        }
        
        private float GetBuildingDensityFactor(double latitude, double longitude)
        {
            // Simplified density calculation based on coordinates
            // In a real implementation, this would use actual land use data
            
            // Major urban areas (approximate coordinates)
            if (IsNearUrbanArea(latitude, longitude))
            {
                return urbanDensityFactor;
            }
            
            // Check if in developed areas (rough heuristic)
            float latAbs = Mathf.Abs((float)latitude);
            float lonAbs = Mathf.Abs((float)longitude);
            
            // Areas with moderate development
            if (latAbs > 25f && latAbs < 50f && lonAbs > 70f && lonAbs < 130f)
            {
                return suburbanDensityFactor;
            }
            
            // Rural/agricultural areas
            if (latAbs > 20f && latAbs < 60f)
            {
                return ruralDensityFactor;
            }
            
            // Wilderness/undeveloped
            return 0.01f;
        }
        
        private bool IsNearUrbanArea(double latitude, double longitude)
        {
            // Check proximity to major cities (simplified)
            Vector2[] majorCities = {
                new Vector2(34.0522f, -118.2437f), // Los Angeles
                new Vector2(40.7128f, -74.0060f),  // New York
                new Vector2(41.8781f, -87.6298f),  // Chicago
                new Vector2(29.7604f, -95.3698f),  // Houston
                new Vector2(33.4484f, -112.0740f), // Phoenix
                new Vector2(39.7392f, -104.9903f), // Denver
                new Vector2(47.6062f, -122.3321f), // Seattle
                new Vector2(37.7749f, -122.4194f), // San Francisco
            };
            
            Vector2 firePos = new Vector2((float)latitude, (float)longitude);
            
            foreach (var city in majorCities)
            {
                float distance = Vector2.Distance(firePos, city);
                if (distance < 2f) // Within ~200km
                {
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Fire Outcomes
        
        public void RecordFireExtinguished(FireIncident fire)
        {
            if (fire == null || !firePropertyValues.ContainsKey(fire.id))
                return;
                
            // Calculate value saved
            float propertyValueSaved = firePropertyValues[fire.id] * (fire.health / fire.maxHealth);
            float resourceValueSaved = fire.resourceValue * (fire.health / fire.maxHealth);
            
            // Add to score
            currentScore.AddPropertyValueSaved(propertyValueSaved);
            currentScore.AddResourceValueSaved(resourceValueSaved, 0f);
            
            // Lives saved bonus
            if (fire.threateningPopulation && firePopulationAtRisk.ContainsKey(fire.id))
            {
                int livesSaved = firePopulationAtRisk[fire.id];
                float livesBonus = livesSaved * (valueOfStatisticalLife / 1000f); // Scaled down for gameplay
                
                currentScore.AddLivesSaved(livesSaved, livesBonus);
                OnLivesSaved?.Invoke(livesSaved);
            }
            
            currentScore.RecordFireExtinguished();
            
            // Create score event
            var scoreEvent = new ScoreEvent
            {
                eventType = ScoreEventType.FireExtinguished,
                timestamp = DateTime.Now,
                fireId = fire.id,
                valueChange = propertyValueSaved + resourceValueSaved,
                description = $"Fire extinguished: ${propertyValueSaved + resourceValueSaved:N0} saved"
            };
            
            AddScoreEvent(scoreEvent);
            OnPropertySaved?.Invoke(propertyValueSaved + resourceValueSaved);
            
            UnityEngine.Debug.Log($"Fire {fire.id} extinguished: ${propertyValueSaved + resourceValueSaved:N0} value saved");
        }
        
        public void RecordFireContained(FireIncident fire)
        {
            if (fire == null)
                return;
                
            currentScore.RecordFireContained();
            
            var scoreEvent = new ScoreEvent
            {
                eventType = ScoreEventType.FireContained,
                timestamp = DateTime.Now,
                fireId = fire.id,
                valueChange = 0f,
                description = "Fire contained - spread stopped"
            };
            
            AddScoreEvent(scoreEvent);
        }
        
        public void RecordFireFailed(FireIncident fire)
        {
            if (fire == null)
                return;
                
            currentScore.RecordFireFailed();
            
            // Calculate value lost
            float valueLost = 0f;
            if (firePropertyValues.ContainsKey(fire.id))
            {
                valueLost = firePropertyValues[fire.id];
            }
            
            var scoreEvent = new ScoreEvent
            {
                eventType = ScoreEventType.FireFailed,
                timestamp = DateTime.Now,
                fireId = fire.id,
                valueChange = -valueLost,
                description = $"Fire suppression failed: ${valueLost:N0} lost"
            };
            
            AddScoreEvent(scoreEvent);
            
            UnityEngine.Debug.Log($"Fire {fire.id} suppression failed: ${valueLost:N0} value lost");
        }
        
        #endregion
        
        #region Operational Costs
        
        public void RecordDroneDeployment(DroneUnit drone)
        {
            if (drone == null)
                return;
                
            float deploymentCost = drone.specifications.deploymentCost;
            currentScore.AddDeploymentCost(deploymentCost);
            currentScore.RecordDroneDeployment();
            
            var scoreEvent = new ScoreEvent
            {
                eventType = ScoreEventType.DroneDeployed,
                timestamp = DateTime.Now,
                droneId = drone.unitId,
                valueChange = -deploymentCost,
                description = $"Drone {drone.callSign} deployed: ${deploymentCost:N0}"
            };
            
            AddScoreEvent(scoreEvent);
        }
        
        public void RecordSuppressantDrop(DroneUnit drone, SuppressantType type, int unitsUsed)
        {
            if (drone == null)
                return;
                
            float costPerUnit = GetSuppressantCost(type);
            float totalCost = costPerUnit * unitsUsed;
            
            currentScore.AddSuppressantCost(totalCost);
            currentScore.RecordSuppressantDrop();
            
            var scoreEvent = new ScoreEvent
            {
                eventType = ScoreEventType.SuppressantDrop,
                timestamp = DateTime.Now,
                droneId = drone.unitId,
                valueChange = -totalCost,
                description = $"{type} drop: {unitsUsed} units, ${totalCost:N0}"
            };
            
            AddScoreEvent(scoreEvent);
        }
        
        private float GetSuppressantCost(SuppressantType type)
        {
            switch (type)
            {
                case SuppressantType.Water:
                    return waterCostPerUnit;
                case SuppressantType.LongTermRetardant:
                    return retardantCostPerUnit;
                case SuppressantType.ClassAFoam:
                    return foamCostPerUnit;
                case SuppressantType.AerialIgnitionSpheres:
                    return retardantCostPerUnit * 2f; // More expensive
                default:
                    return waterCostPerUnit;
            }
        }
        
        #endregion
        
        #region Score Events
        
        private void AddScoreEvent(ScoreEvent scoreEvent)
        {
            scoreEvents.Add(scoreEvent);
            OnScoreEvent?.Invoke(scoreEvent);
            
            // Limit event history to prevent memory issues
            if (scoreEvents.Count > 1000)
            {
                scoreEvents.RemoveAt(0);
            }
        }
        
        public List<ScoreEvent> GetRecentEvents(int count = 10)
        {
            int startIndex = Mathf.Max(0, scoreEvents.Count - count);
            int actualCount = Mathf.Min(count, scoreEvents.Count);
            
            return scoreEvents.GetRange(startIndex, actualCount);
        }
        
        public List<ScoreEvent> GetEventsByType(ScoreEventType type)
        {
            return scoreEvents.FindAll(e => e.eventType == type);
        }
        
        #endregion
        
        #region Public API
        
        public void ResetScore()
        {
            currentScore = new ScoreData();
            firePropertyValues.Clear();
            firePopulationAtRisk.Clear();
            scoreEvents.Clear();
            
            UnityEngine.Debug.Log("Score reset");
        }
        
        public ScoreData GetFinalScore()
        {
            currentScore.CalculateFinalScore();
            return currentScore;
        }
        
        public string GetScoreSummary()
        {
            return currentScore.GetScoreSummary();
        }
        
        public float GetEfficiencyRating()
        {
            if (currentScore.totalSuppressionCost <= 0f)
                return 1f;
                
            return currentScore.totalValueSaved / currentScore.totalSuppressionCost;
        }
        
        public void SetEconomicConstants(float costPerMeter, float timberValue, float ecosystemValue, float lifeValue)
        {
            baseCostPerSquareMeter = costPerMeter;
            timberValuePerAcre = timberValue;
            ecosystemValuePerAcre = ecosystemValue;
            valueOfStatisticalLife = lifeValue;
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    [System.Serializable]
    public enum ScoreEventType
    {
        FireExtinguished,
        FireContained,
        FireFailed,
        DroneDeployed,
        SuppressantDrop,
        LivesSaved,
        PropertySaved,
        ResourceSaved
    }
    
    [System.Serializable]
    public class ScoreEvent
    {
        public ScoreEventType eventType;
        public DateTime timestamp;
        public string fireId;
        public string droneId;
        public float valueChange;
        public string description;
        
        public string GetDisplayText()
        {
            string time = timestamp.ToString("HH:mm:ss");
            string value = valueChange >= 0 ? $"+${valueChange:N0}" : $"-${Mathf.Abs(valueChange):N0}";
            return $"[{time}] {description} ({value})";
        }
    }
    
    #endregion
}

