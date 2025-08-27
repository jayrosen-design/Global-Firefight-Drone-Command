using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlobalFirefight.Data
{
    [System.Serializable]
    public enum CountryType
    {
        USA,
        Canada,
        Australia,
        Brazil,
        China,
        France,
        Germany,
        Russia,
        International
    }
    
    [System.Serializable]
    public enum DroneType
    {
        Reconnaissance,
        HighAltitudeSurveillance,
        AerialIgnition,
        HeavyLiftSuppression,
        FastReconnaissance,
        LongRangeSurveillance,
        UrbanHighRiseSuppression
    }
    
    [System.Serializable]
    public enum DroneState
    {
        Idle,
        Active,
        Deploying,
        EnRoute,
        OnStation,
        Suppressing,
        Returning,
        Refueling,
        Reloading,
        Maintenance,
        Emergency,
        Destroyed
    }
    
    [System.Serializable]
    public enum SuppressantType
    {
        Water,
        LongTermRetardant,
        ClassAFoam,
        AerialIgnitionSpheres
    }
    
    [System.Serializable]
    public class DroneSpecifications
    {
        [Header("Basic Info")]
        public string modelName;
        public string agency;
        public CountryType country;
        public DroneType type;
        
        [Header("Capabilities")]
        public int payloadCapacity; // Suppressant units
        public float flightEndurance; // Seconds
        public float maxSpeed; // m/s
        public float operationalAltitude; // meters
        public float maxRange; // meters from base
        
        [Header("Special Abilities")]
        public bool hasThermalImaging;
        public bool canSwarm;
        public bool hasLongRangeDetection;
        public bool canVTOL;
        public bool hasHighPressureNozzle;
        
        [Header("Visual Properties")]
        public Color primaryColor;
        public Color secondaryColor;
        public string liveryDescription;
        
        [Header("Costs")]
        public float deploymentCost;
        public float operationalCostPerMinute;
        public float suppressantCostPerUnit;
    }
    
    [System.Serializable]
    public class DroneUnit
    {
        [Header("Identification")]
        public string unitId;
        public string callSign;
        public string country; // Added missing property
        public DroneSpecifications specifications;
        
        [Header("Current State")]
        public Vector3 position;
        public Vector3 targetPosition;
        public Quaternion rotation;
        public DroneState state;
        
        [Header("Operational Status")]
        public float batteryLife; // 0-100%
        public int currentSuppressantUnits;
        public float missionTime; // Current mission duration in seconds
        public bool isPlayerControlled;
        
        // Compatibility properties for legacy code
        public float currentBatteryLife => batteryLife;
        public int currentPayload => currentSuppressantUnits;
        
        [Header("Mission Data")]
        public string assignedFireId;
        public string commandVehicleId;
        public List<Vector3> flightPath;
        public float distanceToTarget;
        public float estimatedTimeToTarget;
        
        [Header("Performance Metrics")]
        public int successfulDrops;
        public float totalSuppressionDealt;
        public int firesExtinguished;
        public float totalFlightTime;
        
        // Constructor
        public DroneUnit()
        {
            unitId = System.Guid.NewGuid().ToString();
            state = DroneState.Idle;
            batteryLife = 100f;
            flightPath = new List<Vector3>();
        }
        
        // Initialize drone with specifications
        public void Initialize(DroneSpecifications specs, string vehicleId)
        {
            specifications = specs;
            commandVehicleId = vehicleId;
            currentSuppressantUnits = specs.payloadCapacity;
            callSign = GenerateCallSign();
        }
        
        // Generate military-style call sign
        private string GenerateCallSign()
        {
            string[] prefixes = { "FIRE", "RESCUE", "ANGEL", "GUARDIAN", "EAGLE", "HAWK" };
            string prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
            int number = UnityEngine.Random.Range(1, 99);
            return $"{prefix}-{number:D2}";
        }
        
        // Update drone systems each frame
        public void UpdateSystems(float deltaTime)
        {
            // Update mission time
            if (state != DroneState.Idle)
            {
                missionTime += deltaTime;
                totalFlightTime += deltaTime;
            }
            
            // Update battery life based on activity
            UpdateBatteryLife(deltaTime);
            
            // Update position if moving
            UpdateMovement(deltaTime);
            
            // Update distance to target
            if (targetPosition != Vector3.zero)
            {
                distanceToTarget = Vector3.Distance(position, targetPosition);
                estimatedTimeToTarget = distanceToTarget / specifications.maxSpeed;
            }
        }
        
        // Update battery consumption
        private void UpdateBatteryLife(float deltaTime)
        {
            if (state == DroneState.Idle || state == DroneState.Maintenance)
                return;
                
            float consumptionRate = 0.1f; // Base consumption per second
            
            // Higher consumption during active operations
            switch (state)
            {
                case DroneState.Deploying:
                case DroneState.EnRoute:
                    consumptionRate = 0.15f;
                    break;
                case DroneState.Suppressing:
                    consumptionRate = 0.3f;
                    break;
                case DroneState.Returning:
                    consumptionRate = 0.12f;
                    break;
            }
            
            batteryLife = Mathf.Max(0f, batteryLife - consumptionRate * deltaTime);
            
            // Force return if battery is low
            if (batteryLife <= 20f && state != DroneState.Returning && state != DroneState.Refueling)
            {
                ForceReturn("Low battery");
            }
        }
        
        // Update movement towards target
        private void UpdateMovement(float deltaTime)
        {
            if (targetPosition == Vector3.zero || state == DroneState.Idle)
                return;
                
            Vector3 direction = (targetPosition - position).normalized;
            float moveDistance = specifications.maxSpeed * deltaTime;
            
            // Move towards target
            position = Vector3.MoveTowards(position, targetPosition, moveDistance);
            
            // Update rotation to face movement direction
            if (direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(direction);
            }
            
            // Check if reached target
            if (Vector3.Distance(position, targetPosition) < 10f)
            {
                OnReachedTarget();
            }
        }
        
        // Handle reaching target destination
        private void OnReachedTarget()
        {
            switch (state)
            {
                case DroneState.Deploying:
                case DroneState.EnRoute:
                    state = DroneState.OnStation;
                    break;
                case DroneState.Returning:
                    state = DroneState.Refueling;
                    batteryLife = 100f;
                    currentSuppressantUnits = specifications.payloadCapacity;
                    break;
            }
        }
        
        // Deploy suppressant
        public bool DropSuppressant(SuppressantType type, Vector3 dropPosition)
        {
            if (currentSuppressantUnits <= 0 || state != DroneState.OnStation)
                return false;
                
            currentSuppressantUnits--;
            successfulDrops++;
            state = DroneState.Suppressing;
            
            return true;
        }
        
        // Force drone to return to base
        public void ForceReturn(string reason)
        {
            UnityEngine.Debug.Log($"Drone {callSign} returning to base: {reason}");
            state = DroneState.Returning;
            // Target position should be set to command vehicle position
        }
        
        // Check if drone needs maintenance
        public bool NeedsMaintenance()
        {
            return batteryLife <= 10f || currentSuppressantUnits <= 0;
        }
        
        // Get operational efficiency percentage
        public float GetOperationalEfficiency()
        {
            float batteryFactor = batteryLife / 100f;
            float payloadFactor = (float)currentSuppressantUnits / specifications.payloadCapacity;
            return (batteryFactor + payloadFactor) / 2f;
        }
        
        // Calculate operational cost for current mission
        public float GetCurrentMissionCost()
        {
            float timeCost = (missionTime / 60f) * specifications.operationalCostPerMinute;
            float suppressantCost = (specifications.payloadCapacity - currentSuppressantUnits) * specifications.suppressantCostPerUnit;
            return specifications.deploymentCost + timeCost + suppressantCost;
        }
        
        // Get status description for UI
        public string GetStatusDescription()
        {
            switch (state)
            {
                case DroneState.Idle: return "Standing by";
                case DroneState.Deploying: return "Launching";
                case DroneState.EnRoute: return $"En route ({distanceToTarget:F0}m)";
                case DroneState.OnStation: return "On station";
                case DroneState.Suppressing: return "Suppressing fire";
                case DroneState.Returning: return "Returning to base";
                case DroneState.Refueling: return "Refueling";
                case DroneState.Reloading: return "Reloading suppressant";
                case DroneState.Maintenance: return "Maintenance required";
                default: return state.ToString();
            }
        }
        
        // Get current speed for UI display
        public float GetCurrentSpeed()
        {
            if (state == DroneState.Idle || state == DroneState.OnStation || state == DroneState.Refueling)
                return 0f;
            return specifications.maxSpeed;
        }
    }
}

