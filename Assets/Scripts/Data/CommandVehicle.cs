using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlobalFirefight.Data
{
    [System.Serializable]
    public enum VehicleStatus
    {
        Idle,
        Deployed,
        UnderAttack,
        Destroyed,
        Maintenance
    }
    
    [System.Serializable]
    public class CommandVehicle
    {
        [Header("Vehicle Identification")]
        public string vehicleId;
        public string designation;
        public CountryType country;
        
        [Header("Position & Status")]
        public Vector3 position;
        public VehicleStatus status;
        public bool isDeployed;
        public DateTime deploymentTime;
        
        [Header("Drone Management")]
        public List<DroneUnit> assignedDrones;
        public int maxDroneCapacity;
        public Queue<DroneUnit> maintenanceQueue;
        
        [Header("Operational Capabilities")]
        public float communicationRange; // meters
        public float supportRadius; // area of operation
        public bool hasRepairFacilities;
        public bool hasRefuelCapabilities;
        public bool hasSuppressantReload;
        
        [Header("Resources")]
        public float fuelReserves; // 0-100%
        public int suppressantStockpile;
        public float crewReadiness; // 0-100%
        
        [Header("Mission Statistics")]
        public int dronesDeployed;
        public int successfulMissions;
        public float totalOperationalTime;
        public float totalCostIncurred;
        
        // Constructor
        public CommandVehicle()
        {
            vehicleId = System.Guid.NewGuid().ToString();
            assignedDrones = new List<DroneUnit>();
            maintenanceQueue = new Queue<DroneUnit>();
            status = VehicleStatus.Idle;
            maxDroneCapacity = 4;
            fuelReserves = 100f;
            suppressantStockpile = 1000;
            crewReadiness = 100f;
        }
        
        // Initialize command vehicle
        public void Initialize(CountryType countryType, Vector3 deployPosition)
        {
            country = countryType;
            position = deployPosition;
            designation = GenerateDesignation();
            communicationRange = 50000f; // 50km default
            supportRadius = 25000f; // 25km operational radius
            hasRepairFacilities = true;
            hasRefuelCapabilities = true;
            hasSuppressantReload = true;
        }
        
        // Generate military-style designation
        private string GenerateDesignation()
        {
            string countryCode = country.ToString().Substring(0, Math.Min(3, country.ToString().Length));
            int number = UnityEngine.Random.Range(100, 999);
            return $"{countryCode}-CV-{number}";
        }
        
        // Deploy vehicle to specified location
        public bool DeployToLocation(Vector3 targetLocation)
        {
            if (status != VehicleStatus.Idle)
                return false;
                
            position = targetLocation;
            status = VehicleStatus.Deployed;
            isDeployed = true;
            deploymentTime = DateTime.Now;
            
            UnityEngine.Debug.Log($"Command Vehicle {designation} deployed to {targetLocation}");
            return true;
        }
        
        // Add drone to vehicle
        public bool AddDrone(DroneUnit drone)
        {
            if (assignedDrones.Count >= maxDroneCapacity)
                return false;
                
            assignedDrones.Add(drone);
            drone.commandVehicleId = vehicleId;
            return true;
        }
        
        // Remove drone from vehicle
        public bool RemoveDrone(string droneId)
        {
            for (int i = 0; i < assignedDrones.Count; i++)
            {
                if (assignedDrones[i].unitId == droneId)
                {
                    assignedDrones.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        // Launch drone for mission
        public bool LaunchDrone(string droneId, Vector3 targetLocation)
        {
            DroneUnit drone = GetDrone(droneId);
            if (drone == null || drone.state != DroneState.Idle)
                return false;
                
            // Check if drone is operational
            if (drone.batteryLife < 30f || drone.currentSuppressantUnits <= 0)
            {
                UnityEngine.Debug.Log($"Drone {drone.callSign} not ready for deployment");
                return false;
            }
            
            // Launch the drone
            drone.state = DroneState.Deploying;
            drone.targetPosition = targetLocation;
            drone.missionTime = 0f;
            
            dronesDeployed++;
            
            UnityEngine.Debug.Log($"Launched drone {drone.callSign} to {targetLocation}");
            return true;
        }
        
        // Recall drone to base
        public void RecallDrone(string droneId)
        {
            DroneUnit drone = GetDrone(droneId);
            if (drone != null)
            {
                drone.ForceReturn("Recalled by command");
                drone.targetPosition = position;
            }
        }
        
        // Service returning drone
        public void ServiceDrone(DroneUnit drone)
        {
            if (Vector3.Distance(drone.position, position) > 100f)
                return; // Drone must be at base
                
            maintenanceQueue.Enqueue(drone);
            drone.state = DroneState.Maintenance;
        }
        
        // Process maintenance queue
        public void ProcessMaintenance(float deltaTime)
        {
            if (maintenanceQueue.Count == 0)
                return;
                
            DroneUnit drone = maintenanceQueue.Peek();
            
            // Simulate maintenance time (5 seconds per drone)
            drone.missionTime += deltaTime;
            if (drone.missionTime >= 5f)
            {
                // Complete maintenance
                drone.batteryLife = 100f;
                drone.currentSuppressantUnits = drone.specifications.payloadCapacity;
                drone.state = DroneState.Idle;
                drone.missionTime = 0f;
                
                maintenanceQueue.Dequeue();
                UnityEngine.Debug.Log($"Drone {drone.callSign} maintenance complete");
            }
        }
        
        // Update vehicle systems
        public void UpdateSystems(float deltaTime)
        {
            if (!isDeployed)
                return;
                
            totalOperationalTime += deltaTime;
            
            // Process drone maintenance
            ProcessMaintenance(deltaTime);
            
            // Update resource consumption
            UpdateResources(deltaTime);
            
            // Check operational status
            CheckOperationalStatus();
        }
        
        // Update vehicle resources
        private void UpdateResources(float deltaTime)
        {
            // Consume fuel based on operational activity
            float fuelConsumption = 0.01f * deltaTime; // Base consumption
            
            // Additional consumption for active drones
            int activeDrones = GetActiveDroneCount();
            fuelConsumption += activeDrones * 0.005f * deltaTime;
            
            fuelReserves = Mathf.Max(0f, fuelReserves - fuelConsumption);
            
            // Crew fatigue over time
            float fatigueRate = 0.1f * deltaTime;
            crewReadiness = Mathf.Max(50f, crewReadiness - fatigueRate);
        }
        
        // Check if vehicle is operational
        private void CheckOperationalStatus()
        {
            if (fuelReserves <= 10f)
            {
                UnityEngine.Debug.LogWarning($"Command Vehicle {designation} low on fuel!");
            }
            
            if (crewReadiness <= 60f)
            {
                UnityEngine.Debug.LogWarning($"Command Vehicle {designation} crew needs rest!");
            }
            
            if (suppressantStockpile <= 100)
            {
                UnityEngine.Debug.LogWarning($"Command Vehicle {designation} low on suppressant!");
            }
        }
        
        // Get drone by ID
        public DroneUnit GetDrone(string droneId)
        {
            return assignedDrones.Find(d => d.unitId == droneId);
        }
        
        // Get count of active drones
        public int GetActiveDroneCount()
        {
            int count = 0;
            foreach (var drone in assignedDrones)
            {
                if (drone.state != DroneState.Idle && drone.state != DroneState.Maintenance)
                    count++;
            }
            return count;
        }
        
        // Get available drones for deployment
        public List<DroneUnit> GetAvailableDrones()
        {
            List<DroneUnit> available = new List<DroneUnit>();
            foreach (var drone in assignedDrones)
            {
                if (drone.state == DroneState.Idle && drone.batteryLife >= 30f && drone.currentSuppressantUnits > 0)
                    available.Add(drone);
            }
            return available;
        }
        
        // Calculate total operational cost
        public float GetTotalOperationalCost()
        {
            float cost = totalCostIncurred;
            
            // Add current mission costs for all drones
            foreach (var drone in assignedDrones)
            {
                cost += drone.GetCurrentMissionCost();
            }
            
            return cost;
        }
        
        // Get vehicle efficiency rating
        public float GetEfficiencyRating()
        {
            float fuelFactor = fuelReserves / 100f;
            float readinessFactor = crewReadiness / 100f;
            float operationalFactor = (float)GetAvailableDrones().Count / maxDroneCapacity;
            
            return (fuelFactor + readinessFactor + operationalFactor) / 3f;
        }
        
        // Get status report for UI
        public string GetStatusReport()
        {
            int available = GetAvailableDrones().Count;
            int active = GetActiveDroneCount();
            int maintenance = maintenanceQueue.Count;
            
            return $"Status: {status}\n" +
                   $"Drones: {available} Available, {active} Active, {maintenance} Maintenance\n" +
                   $"Fuel: {fuelReserves:F1}%\n" +
                   $"Crew: {crewReadiness:F1}%\n" +
                   $"Suppressant: {suppressantStockpile} units";
        }
    }
}

