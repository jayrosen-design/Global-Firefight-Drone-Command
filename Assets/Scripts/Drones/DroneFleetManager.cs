using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GlobalFirefight.Data;
using GlobalFirefight.Core;
using GlobalFirefight.Geospatial;
using GlobalFirefight.Fire;

namespace GlobalFirefight.Drones
{
    public class DroneFleetManager : MonoBehaviour
    {
        [Header("Fleet Configuration")]
        [SerializeField] private Dictionary<CountryType, List<CommandVehicle>> countryFleets;
        [SerializeField] private List<DroneSpecifications> availableDroneTypes;
        [SerializeField] private Queue<DroneDeploymentOrder> deploymentQueue;
        [SerializeField] private int maxActiveFleets = 5;
        [SerializeField] private int maxDronesPerFleet = 4;
        
        [Header("Deployment Settings")]
        [SerializeField] private float deploymentTime = 30f; // seconds to deploy
        [SerializeField] private float maxDeploymentRange = 5000000f; // 5000km global range
        [SerializeField] private bool allowGlobalDeployment = true;
        [SerializeField] private float commandVehicleSpeed = 50f; // m/s for positioning
        
        [Header("Fleet Status")]
        [SerializeField] private int totalActiveFleets = 0;
        [SerializeField] private int totalActiveDrones = 0;
        [SerializeField] private int totalMissions = 0;
        [SerializeField] private float totalOperationalCost = 0f;
        
        [Header("Performance")]
        [SerializeField] private int dronesPerFrameUpdate = 5;
        [SerializeField] private float fleetUpdateInterval = 0.5f;
        [SerializeField] private bool enableAIControl = true;
        
        [Header("References")]
        [SerializeField] private GameObject commandVehiclePrefab;
        [SerializeField] private GameObject dronePrefab;
        [SerializeField] private Transform fleetParent;
        
        // Private fields
        private GeospatialManager geospatialManager;
        private Dictionary<string, GameObject> commandVehicleObjects;
        private Dictionary<string, GameObject> droneObjects;
        private Queue<string> droneUpdateQueue;
        private Coroutine fleetUpdateCoroutine;
        private bool isInitialized = false;
        
        // Events
        public static event Action<CommandVehicle> OnFleetDeployed;
        public static event Action<CommandVehicle> OnFleetRecalled;
        public static event Action<DroneUnit> OnDroneDeployed;
        public static event Action<DroneUnit> OnDroneReturned;
        public static event Action<DroneUnit, string> OnMissionComplete;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public int TotalActiveFleets => totalActiveFleets;
        public int TotalActiveDrones => totalActiveDrones;
        public Dictionary<CountryType, List<CommandVehicle>> CountryFleets => countryFleets;
        
        #region Initialization
        
        public void Initialize()
        {
            UnityEngine.Debug.Log("Initializing Drone Fleet Manager...");
            
            // Initialize collections
            countryFleets = new Dictionary<CountryType, List<CommandVehicle>>();
            deploymentQueue = new Queue<DroneDeploymentOrder>();
            commandVehicleObjects = new Dictionary<string, GameObject>();
            droneObjects = new Dictionary<string, GameObject>();
            droneUpdateQueue = new Queue<string>();
            
            // Get required components
            geospatialManager = FindFirstObjectByType<GeospatialManager>();
            
            // Create fleet parent if needed
            if (fleetParent == null)
            {
                GameObject fleetParentObj = new GameObject("DroneFleets");
                fleetParent = fleetParentObj.transform;
            }
            
            // Initialize drone specifications
            InitializeDroneSpecifications();
            
            // Setup initial fleets for each country
            SetupInitialFleets();
            
            // Start fleet management coroutine
            StartFleetManagement();
            
            isInitialized = true;
            UnityEngine.Debug.Log("Drone Fleet Manager initialized successfully");
        }
        
        private void InitializeDroneSpecifications()
        {
            availableDroneTypes = new List<DroneSpecifications>();
            
            // USA - DJI Matrice 350 RTK (Reconnaissance)
            availableDroneTypes.Add(new DroneSpecifications
            {
                modelName = "DJI Matrice 350 RTK",
                agency = "USFS / CAL FIRE",
                country = CountryType.USA,
                type = DroneType.Reconnaissance,
                payloadCapacity = 0,
                flightEndurance = 3300f,
                maxSpeed = 17f,
                operationalAltitude = 7000f,
                maxRange = 15000f,
                hasThermalImaging = true,
                canSwarm = false,
                hasLongRangeDetection = true,
                canVTOL = true,
                hasHighPressureNozzle = false,
                primaryColor = Color.white,
                secondaryColor = Color.gray,
                liveryDescription = "US Forest Service shield",
                deploymentCost = 75000f,
                operationalCostPerMinute = 150f,
                suppressantCostPerUnit = 0f
            });
            
            // USA - MQ-9A Reaper (High-Altitude Surveillance)
            availableDroneTypes.Add(new DroneSpecifications
            {
                modelName = "General Atomics MQ-9A Reaper",
                agency = "CAL FIRE",
                country = CountryType.USA,
                type = DroneType.HighAltitudeSurveillance,
                payloadCapacity = 0,
                flightEndurance = 97200f,
                maxSpeed = 75f,
                operationalAltitude = 15000f,
                maxRange = 1800000f,
                hasThermalImaging = true,
                canSwarm = false,
                hasLongRangeDetection = true,
                canVTOL = false,
                hasHighPressureNozzle = false,
                primaryColor = new Color(0.7f, 0.7f, 0.7f),
                secondaryColor = Color.black,
                liveryDescription = "California Air National Guard markings",
                deploymentCost = 500000f,
                operationalCostPerMinute = 2000f,
                suppressantCostPerUnit = 0f
            });
            
            // Canada - FireSwarm Thunder Wasp (Heavy-Lift Suppression)
            availableDroneTypes.Add(new DroneSpecifications
            {
                modelName = "Fireswarm Thunder Wasp",
                agency = "BC Wildfire Service",
                country = CountryType.Canada,
                type = DroneType.HeavyLiftSuppression,
                payloadCapacity = 400,
                flightEndurance = 7200f,
                maxSpeed = 25f,
                operationalAltitude = 3000f,
                maxRange = 50000f,
                hasThermalImaging = false,
                canSwarm = true,
                hasLongRangeDetection = false,
                canVTOL = true,
                hasHighPressureNozzle = true,
                primaryColor = Color.white,
                secondaryColor = new Color(1f, 0f, 0f),
                liveryDescription = "Canadian flag and BC Wildfire Service insignia",
                deploymentCost = 200000f,
                operationalCostPerMinute = 800f,
                suppressantCostPerUnit = 25f
            });
            
            // Australia - DJI Mavic 3 Thermal (Fast Reconnaissance)
            availableDroneTypes.Add(new DroneSpecifications
            {
                modelName = "DJI Mavic 3 Thermal",
                agency = "NSW RFS",
                country = CountryType.Australia,
                type = DroneType.FastReconnaissance,
                payloadCapacity = 0,
                flightEndurance = 2700f,
                maxSpeed = 21f,
                operationalAltitude = 6000f,
                maxRange = 15000f,
                hasThermalImaging = true,
                canSwarm = false,
                hasLongRangeDetection = false,
                canVTOL = true,
                hasHighPressureNozzle = false,
                primaryColor = new Color(0.3f, 0.3f, 0.3f),
                secondaryColor = new Color(1f, 0f, 0f),
                liveryDescription = "NSW RFS red and white logo",
                deploymentCost = 50000f,
                operationalCostPerMinute = 100f,
                suppressantCostPerUnit = 0f
            });
            
            // Brazil - Nauru 500C ISR (Long-Range Surveillance)
            availableDroneTypes.Add(new DroneSpecifications
            {
                modelName = "Nauru 500C ISR",
                agency = "CBMGO / Xmobots",
                country = CountryType.Brazil,
                type = DroneType.LongRangeSurveillance,
                payloadCapacity = 0,
                flightEndurance = 14400f,
                maxSpeed = 30f,
                operationalAltitude = 5000f,
                maxRange = 100000f,
                hasThermalImaging = true,
                canSwarm = false,
                hasLongRangeDetection = true,
                canVTOL = true,
                hasHighPressureNozzle = false,
                primaryColor = Color.white,
                secondaryColor = new Color(0f, 0.5f, 0f),
                liveryDescription = "CBMGO logo with Brazilian flag colors",
                deploymentCost = 120000f,
                operationalCostPerMinute = 300f,
                suppressantCostPerUnit = 0f
            });
            
            // China - EHang 216F (Urban High-Rise Suppression)
            availableDroneTypes.Add(new DroneSpecifications
            {
                modelName = "EHang 216F",
                agency = "EHang",
                country = CountryType.China,
                type = DroneType.UrbanHighRiseSuppression,
                payloadCapacity = 106, // 100 foam + 6 projectiles
                flightEndurance = 1260f,
                maxSpeed = 18f,
                operationalAltitude = 1000f,
                maxRange = 35000f,
                hasThermalImaging = false,
                canSwarm = false,
                hasLongRangeDetection = false,
                canVTOL = true,
                hasHighPressureNozzle = true,
                primaryColor = Color.white,
                secondaryColor = Color.red,
                liveryDescription = "Firefighting text in English and Mandarin",
                deploymentCost = 180000f,
                operationalCostPerMinute = 400f,
                suppressantCostPerUnit = 35f
            });
            
            UnityEngine.Debug.Log($"Initialized {availableDroneTypes.Count} drone specifications");
        }
        
        public void SetupInitialFleets()
        {
            foreach (CountryType country in Enum.GetValues(typeof(CountryType)))
            {
                if (country == CountryType.International)
                    continue;
                    
                countryFleets[country] = new List<CommandVehicle>();
                
                // Create initial command vehicles for each country
                var commandVehicle = CreateCommandVehicle(country);
                if (commandVehicle != null)
                {
                    countryFleets[country].Add(commandVehicle);
                    
                    // Add drones to the command vehicle
                    AddDronesToCommandVehicle(commandVehicle, country);
                }
            }
            
            UpdateFleetCounts();
            UnityEngine.Debug.Log($"Setup initial fleets for {countryFleets.Count} countries");
        }
        
        #endregion
        
        #region Fleet Creation
        
        private CommandVehicle CreateCommandVehicle(CountryType country)
        {
            var commandVehicle = new CommandVehicle();
            commandVehicle.Initialize(country, GetCountryBasePosition(country));
            
            // Create visual representation
            if (commandVehiclePrefab != null)
            {
                GameObject cvObject = Instantiate(commandVehiclePrefab, fleetParent);
                cvObject.name = $"CV_{commandVehicle.designation}";
                
                // Anchor to Earth if geospatial manager is available
                if (geospatialManager != null)
                {
                    var (lat, lon) = GetCountryCoordinates(country);
                    geospatialManager.AnchorObjectToEarth(cvObject, lat, lon, 0);
                }
                
                commandVehicleObjects[commandVehicle.vehicleId] = cvObject;
            }
            
            return commandVehicle;
        }
        
        private void AddDronesToCommandVehicle(CommandVehicle commandVehicle, CountryType country)
        {
            // Get available drone types for this country
            var countryDrones = availableDroneTypes.Where(d => d.country == country).ToList();
            
            foreach (var droneSpec in countryDrones)
            {
                // Create multiple drones of each type
                int droneCount = GetDroneCountForType(droneSpec.type);
                
                for (int i = 0; i < droneCount; i++)
                {
                    var drone = new DroneUnit();
                    drone.Initialize(droneSpec, commandVehicle.vehicleId);
                    drone.position = commandVehicle.position;
                    
                    commandVehicle.AddDrone(drone);
                    
                    // Create visual representation
                    CreateDroneObject(drone);
                }
            }
        }
        
        private GameObject CreateDroneObject(DroneUnit drone)
        {
            if (dronePrefab == null)
                return null;
                
            GameObject droneObject = Instantiate(dronePrefab, fleetParent);
            droneObject.name = $"Drone_{drone.callSign}";
            
            // Configure drone controller
            var droneController = droneObject.GetComponent<DroneController>();
            if (droneController == null)
            {
                droneController = droneObject.AddComponent<DroneController>();
            }
            droneController.Initialize(drone);
            
            // Position drone at command vehicle initially
            droneObject.transform.position = drone.position;
            
            droneObjects[drone.unitId] = droneObject;
            return droneObject;
        }
        
        private int GetDroneCountForType(DroneType type)
        {
            switch (type)
            {
                case DroneType.Reconnaissance:
                case DroneType.FastReconnaissance:
                    return 2; // 2 scout drones
                case DroneType.HeavyLiftSuppression:
                case DroneType.UrbanHighRiseSuppression:
                    return 3; // 3 suppression drones
                case DroneType.HighAltitudeSurveillance:
                case DroneType.LongRangeSurveillance:
                    return 1; // 1 surveillance drone
                case DroneType.AerialIgnition:
                    return 1; // 1 special operations drone
                default:
                    return 2;
            }
        }
        
        #endregion
        
        #region Country Positioning
        
        private Vector3 GetCountryBasePosition(CountryType country)
        {
            var (lat, lon) = GetCountryCoordinates(country);
            return new Vector3((float)lon, 100f, (float)lat); // Basic positioning
        }
        
        private (double lat, double lon) GetCountryCoordinates(CountryType country)
        {
            switch (country)
            {
                case CountryType.USA:
                    return (39.8283, -98.5795); // Geographic center of USA
                case CountryType.Canada:
                    return (56.1304, -106.3468); // Geographic center of Canada
                case CountryType.Australia:
                    return (-25.2744, 133.7751); // Geographic center of Australia
                case CountryType.Brazil:
                    return (-14.2350, -51.9253); // Geographic center of Brazil
                case CountryType.China:
                    return (35.8617, 104.1954); // Geographic center of China
                case CountryType.France:
                    return (46.2276, 2.2137); // Geographic center of France
                case CountryType.Germany:
                    return (51.1657, 10.4515); // Geographic center of Germany
                case CountryType.Russia:
                    return (61.5240, 105.3188); // Geographic center of Russia
                default:
                    return (0.0, 0.0);
            }
        }
        
        #endregion
        
        #region Fleet Deployment
        
        public bool DeployFleet(CountryType country, Vector3 targetLocation, string targetFireId = null)
        {
            if (!countryFleets.ContainsKey(country) || countryFleets[country].Count == 0)
            {
                UnityEngine.Debug.LogWarning($"No available fleets for {country}");
                return false;
            }
            
            // Find available command vehicle
            var availableCV = countryFleets[country].FirstOrDefault(cv => cv.status == VehicleStatus.Idle);
            if (availableCV == null)
            {
                UnityEngine.Debug.LogWarning($"No idle command vehicles available for {country}");
                return false;
            }
            
            // Check deployment range
            float distance = Vector3.Distance(availableCV.position, targetLocation);
            if (!allowGlobalDeployment && distance > maxDeploymentRange)
            {
                UnityEngine.Debug.LogWarning($"Target location too far for deployment: {distance}m");
                return false;
            }
            
            // Create deployment order
            var deploymentOrder = new DroneDeploymentOrder
            {
                commandVehicleId = availableCV.vehicleId,
                targetLocation = targetLocation,
                targetFireId = targetFireId,
                country = country,
                deploymentTime = Time.time,
                estimatedArrival = Time.time + (distance / commandVehicleSpeed)
            };
            
            deploymentQueue.Enqueue(deploymentOrder);
            
            // Start deployment
            StartCoroutine(ProcessDeployment(deploymentOrder));
            
            totalMissions++;
            UnityEngine.Debug.Log($"Fleet deployment ordered for {country} to {targetLocation}");
            return true;
        }
        
        private IEnumerator ProcessDeployment(DroneDeploymentOrder order)
        {
            var commandVehicle = GetCommandVehicle(order.commandVehicleId);
            if (commandVehicle == null)
                yield break;
            
            commandVehicle.status = VehicleStatus.Deployed;
            
            // Move command vehicle to target location
            yield return StartCoroutine(MoveCommandVehicle(commandVehicle, order.targetLocation));
            
            // Deploy drones
            var availableDrones = commandVehicle.GetAvailableDrones();
            foreach (var drone in availableDrones)
            {
                LaunchDroneToTarget(drone, order.targetLocation, order.targetFireId);
                yield return new WaitForSeconds(2f); // Stagger launches
            }
            
            OnFleetDeployed?.Invoke(commandVehicle);
            UnityEngine.Debug.Log($"Fleet {commandVehicle.designation} deployed successfully");
        }
        
        private IEnumerator MoveCommandVehicle(CommandVehicle cv, Vector3 targetLocation)
        {
            Vector3 startPosition = cv.position;
            float distance = Vector3.Distance(startPosition, targetLocation);
            float travelTime = distance / commandVehicleSpeed;
            float elapsed = 0f;
            
            while (elapsed < travelTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / travelTime;
                
                cv.position = Vector3.Lerp(startPosition, targetLocation, t);
                
                // Update visual representation
                if (commandVehicleObjects.ContainsKey(cv.vehicleId))
                {
                    commandVehicleObjects[cv.vehicleId].transform.position = cv.position;
                }
                
                yield return null;
            }
            
            cv.position = targetLocation;
        }
        
        private void LaunchDroneToTarget(DroneUnit drone, Vector3 targetLocation, string fireId)
        {
            drone.assignedFireId = fireId;
            drone.targetPosition = targetLocation;
            drone.state = DroneState.Deploying;
            
            // Update drone object
            if (droneObjects.ContainsKey(drone.unitId))
            {
                var droneController = droneObjects[drone.unitId].GetComponent<DroneController>();
                if (droneController != null)
                {
                    droneController.SetTarget(targetLocation, fireId);
                }
            }
            
            OnDroneDeployed?.Invoke(drone);
        }
        
        #endregion
        
        #region Fleet Management
        
        private void StartFleetManagement()
        {
            if (fleetUpdateCoroutine != null)
            {
                StopCoroutine(fleetUpdateCoroutine);
            }
            
            fleetUpdateCoroutine = StartCoroutine(FleetManagementLoop());
        }
        
        private IEnumerator FleetManagementLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(fleetUpdateInterval);
                
                UpdateAllFleets(fleetUpdateInterval);
                ProcessDeploymentQueue();
                UpdateFleetCounts();
            }
        }
        
        public void UpdateFleets(float deltaTime)
        {
            // This method is called from GameManager
            // Main updates happen in the coroutine above
        }
        
        private void UpdateAllFleets(float deltaTime)
        {
            foreach (var countryFleet in countryFleets.Values)
            {
                foreach (var commandVehicle in countryFleet)
                {
                    commandVehicle.UpdateSystems(deltaTime);
                    UpdateCommandVehicleDrones(commandVehicle, deltaTime);
                }
            }
        }
        
        private void UpdateCommandVehicleDrones(CommandVehicle cv, float deltaTime)
        {
            foreach (var drone in cv.assignedDrones)
            {
                drone.UpdateSystems(deltaTime);
                
                // Update drone object
                if (droneObjects.ContainsKey(drone.unitId))
                {
                    var droneController = droneObjects[drone.unitId].GetComponent<DroneController>();
                    if (droneController != null)
                    {
                        droneController.UpdateDrone(deltaTime);
                    }
                }
            }
        }
        
        private void ProcessDeploymentQueue()
        {
            // Process any pending deployments
            while (deploymentQueue.Count > 0)
            {
                var order = deploymentQueue.Peek();
                
                // Check if deployment time has elapsed
                if (Time.time >= order.estimatedArrival)
                {
                    deploymentQueue.Dequeue();
                    // Deployment processing is handled by coroutines
                }
                else
                {
                    break; // Wait for next deployment
                }
            }
        }
        
        #endregion
        
        #region Fleet Recall
        
        public void RecallFleet(string commandVehicleId)
        {
            var cv = GetCommandVehicle(commandVehicleId);
            if (cv == null)
                return;
            
            // Recall all drones
            foreach (var drone in cv.assignedDrones)
            {
                RecallDrone(drone.unitId);
            }
            
            // Return command vehicle to base
            var (lat, lon) = GetCountryCoordinates(cv.country);
            Vector3 homePosition = new Vector3((float)lon, 100f, (float)lat);
            
            StartCoroutine(MoveCommandVehicle(cv, homePosition));
            cv.status = VehicleStatus.Idle;
            
            OnFleetRecalled?.Invoke(cv);
            UnityEngine.Debug.Log($"Fleet {cv.designation} recalled to base");
        }
        
        public void RecallDrone(string droneId)
        {
            var drone = GetDrone(droneId);
            if (drone == null)
                return;
            
            var cv = GetCommandVehicle(drone.commandVehicleId);
            if (cv != null)
            {
                drone.ForceReturn("Manual recall");
                drone.targetPosition = cv.position;
            }
            
            OnDroneReturned?.Invoke(drone);
        }
        
        public void RecallAllDrones()
        {
            foreach (var countryFleet in countryFleets.Values)
            {
                foreach (var cv in countryFleet)
                {
                    RecallFleet(cv.vehicleId);
                }
            }
            
            UnityEngine.Debug.Log("All drones recalled");
        }
        
        #endregion
        
        #region Utility Methods
        
        private CommandVehicle GetCommandVehicle(string vehicleId)
        {
            foreach (var countryFleet in countryFleets.Values)
            {
                var cv = countryFleet.FirstOrDefault(v => v.vehicleId == vehicleId);
                if (cv != null)
                    return cv;
            }
            return null;
        }
        
        private DroneUnit GetDrone(string droneId)
        {
            foreach (var countryFleet in countryFleets.Values)
            {
                foreach (var cv in countryFleet)
                {
                    var drone = cv.assignedDrones.FirstOrDefault(d => d.unitId == droneId);
                    if (drone != null)
                        return drone;
                }
            }
            return null;
        }
        
        private void UpdateFleetCounts()
        {
            totalActiveFleets = 0;
            totalActiveDrones = 0;
            totalOperationalCost = 0f;
            
            foreach (var countryFleet in countryFleets.Values)
            {
                foreach (var cv in countryFleet)
                {
                    if (cv.status == VehicleStatus.Deployed)
                        totalActiveFleets++;
                        
                    totalActiveDrones += cv.assignedDrones.Count;
                    totalOperationalCost += cv.GetTotalOperationalCost();
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        public List<DroneUnit> GetAllActiveDrones()
        {
            var allDrones = new List<DroneUnit>();
            
            foreach (var countryFleet in countryFleets.Values)
            {
                foreach (var cv in countryFleet)
                {
                    allDrones.AddRange(cv.assignedDrones);
                }
            }
            
            return allDrones;
        }
        
        public List<DroneUnit> GetDronesByCountry(CountryType country)
        {
            var drones = new List<DroneUnit>();
            
            if (countryFleets.ContainsKey(country))
            {
                foreach (var cv in countryFleets[country])
                {
                    drones.AddRange(cv.assignedDrones);
                }
            }
            
            return drones;
        }
        
        public List<DroneUnit> GetAvailableDrones()
        {
            var availableDrones = new List<DroneUnit>();
            
            foreach (var countryFleet in countryFleets.Values)
            {
                foreach (var cv in countryFleet)
                {
                    availableDrones.AddRange(cv.GetAvailableDrones());
                }
            }
            
            return availableDrones;
        }
        
        public string GetFleetStatus()
        {
            return $"Fleet Status:\n" +
                   $"Active Fleets: {totalActiveFleets}\n" +
                   $"Total Drones: {totalActiveDrones}\n" +
                   $"Missions: {totalMissions}\n" +
                   $"Operational Cost: ${totalOperationalCost:N0}\n" +
                   $"Available Countries: {countryFleets.Count}";
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnDestroy()
        {
            if (fleetUpdateCoroutine != null)
            {
                StopCoroutine(fleetUpdateCoroutine);
            }
            
            // Clean up objects
            foreach (var obj in commandVehicleObjects.Values)
            {
                if (obj != null)
                    Destroy(obj);
            }
            
            foreach (var obj in droneObjects.Values)
            {
                if (obj != null)
                    Destroy(obj);
            }
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    [System.Serializable]
    public class DroneDeploymentOrder
    {
        public string commandVehicleId;
        public Vector3 targetLocation;
        public string targetFireId;
        public CountryType country;
        public float deploymentTime;
        public float estimatedArrival;
        
        public float GetTimeToArrival()
        {
            return Mathf.Max(0f, estimatedArrival - Time.time);
        }
    }
    
    #endregion
}

