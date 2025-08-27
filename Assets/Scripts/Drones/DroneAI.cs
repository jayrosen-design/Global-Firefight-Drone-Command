using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GlobalFirefight.Data;
using GlobalFirefight.Core;
using GlobalFirefight.Fire;
using GlobalFirefight.Geospatial;

namespace GlobalFirefight.Drones
{
    public class DroneAI : MonoBehaviour
    {
        [Header("AI Configuration")]
        [SerializeField] private DroneController droneController;
        [SerializeField] private DroneAIBehavior currentBehavior = DroneAIBehavior.Idle;
        [SerializeField] private float decisionInterval = 2f;
        [SerializeField] private float maxSearchRadius = 1000f;
        [SerializeField] private float priorityThreshold = 0.5f;
        
        [Header("Behavior Weights")]
        [SerializeField] private float fireProximityWeight = 0.4f;
        [SerializeField] private float fireSizeWeight = 0.3f;
        [SerializeField] private float firePriorityWeight = 0.2f;
        [SerializeField] private float batteryConservationWeight = 0.1f;
        
        [Header("Formation Flying")]
        [SerializeField] private bool enableFormationFlying = true;
        [SerializeField] private float formationRadius = 50f;
        [SerializeField] private float separationDistance = 20f;
        [SerializeField] private List<DroneAI> nearbyDrones = new List<DroneAI>();
        
        [Header("Patrol Configuration")]
        [SerializeField] private List<Vector3> patrolPoints = new List<Vector3>();
        [SerializeField] private int currentPatrolIndex = 0;
        [SerializeField] private float patrolRadius = 500f;
        [SerializeField] private bool randomPatrol = true;
        
        [Header("Target Selection")]
        [SerializeField] private FireIncident currentTargetFire;
        [SerializeField] private Vector3 lastKnownFireLocation;
        [SerializeField] private float targetEvaluationCooldown = 5f;
        [SerializeField] private float lastTargetEvaluation;
        
        [Header("Coordination")]
        [SerializeField] private bool enableSwarmBehavior = false;
        [SerializeField] private string swarmLeaderId;
        [SerializeField] private bool isSwarmLeader = false;
        [SerializeField] private List<string> swarmMemberIds = new List<string>();
        
        [Header("Performance")]
        [SerializeField] private bool enableAdvancedPathfinding = true;
        [SerializeField] private float pathUpdateInterval = 1f;
        [SerializeField] private Queue<Vector3> plannedPath = new Queue<Vector3>();
        
        // Private fields
        private GameManager gameManager;
        private FireSimulation fireSimulation;
        private DroneFleetManager fleetManager;
        private GeospatialManager geospatialManager;
        private Coroutine behaviorCoroutine;
        private Coroutine pathfindingCoroutine;
        private bool isInitialized = false;
        private float lastDecisionTime;
        
        // Properties
        public DroneAIBehavior CurrentBehavior => currentBehavior;
        public FireIncident CurrentTargetFire => currentTargetFire;
        public bool IsSwarmLeader => isSwarmLeader;
        public List<string> SwarmMembers => swarmMemberIds;
        
        #region Initialization
        
        public void Initialize(DroneController controller)
        {
            droneController = controller;
            
            // Get required components
            gameManager = FindFirstObjectByType<GameManager>();
            fireSimulation = FindFirstObjectByType<FireSimulation>();
            fleetManager = FindFirstObjectByType<DroneFleetManager>();
            geospatialManager = FindFirstObjectByType<GeospatialManager>();
            
            // Initialize behavior
            currentBehavior = DroneAIBehavior.Idle;
            
            // Setup swarm behavior if applicable
            if (droneController.DroneUnit.specifications.canSwarm)
            {
                enableSwarmBehavior = true;
                SetupSwarmBehavior();
            }
            
            // Generate initial patrol points
            GeneratePatrolPoints();
            
            // Start AI behavior
            StartAIBehavior();
            
            isInitialized = true;
            UnityEngine.Debug.Log($"DroneAI initialized for {droneController.DroneUnit.callSign}");
        }
        
        private void SetupSwarmBehavior()
        {
            // Check if there's already a swarm leader for this country
            var countryDrones = fleetManager?.GetDronesByCountry(droneController.DroneUnit.specifications.country);
            if (countryDrones != null)
            {
                var existingLeader = countryDrones.FirstOrDefault(d => 
                    d.specifications.canSwarm && 
                    d.unitId != droneController.DroneUnit.unitId
                );
                
                if (existingLeader == null)
                {
                    // Become swarm leader
                    isSwarmLeader = true;
                    swarmLeaderId = droneController.DroneUnit.unitId;
                }
                else
                {
                    // Join existing swarm
                    swarmLeaderId = existingLeader.unitId;
                }
            }
        }
        
        private void GeneratePatrolPoints()
        {
            patrolPoints.Clear();
            Vector3 basePosition = droneController.transform.position;
            
            // Generate patrol points in a circle around the base position
            int numPoints = 6;
            for (int i = 0; i < numPoints; i++)
            {
                float angle = (360f / numPoints) * i * Mathf.Deg2Rad;
                Vector3 patrolPoint = basePosition + new Vector3(
                    Mathf.Cos(angle) * patrolRadius,
                    UnityEngine.Random.Range(50f, 200f), // Varying altitudes
                    Mathf.Sin(angle) * patrolRadius
                );
                patrolPoints.Add(patrolPoint);
            }
        }
        
        #endregion
        
        #region AI Behavior Management
        
        private void StartAIBehavior()
        {
            if (behaviorCoroutine != null)
            {
                StopCoroutine(behaviorCoroutine);
            }
            
            behaviorCoroutine = StartCoroutine(AIBehaviorLoop());
            
            if (enableAdvancedPathfinding)
            {
                pathfindingCoroutine = StartCoroutine(PathfindingLoop());
            }
        }
        
        private IEnumerator AIBehaviorLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(decisionInterval);
                
                if (droneController.IsPlayerControlled)
                    continue;
                
                // Make AI decision
                MakeAIDecision();
                ExecuteCurrentBehavior();
            }
        }
        
        private void MakeAIDecision()
        {
            lastDecisionTime = Time.time;
            
            // Get drone status
            var droneUnit = droneController.DroneUnit;
            
            // Check critical conditions first
            if (ShouldReturn())
            {
                SetBehavior(DroneAIBehavior.Returning);
                return;
            }
            
            // Check for emergency situations
            if (ShouldRespondsToEmergency())
            {
                SetBehavior(DroneAIBehavior.Emergency);
                return;
            }
            
            // Evaluate target priorities
            if (Time.time - lastTargetEvaluation > targetEvaluationCooldown)
            {
                EvaluateTargets();
                lastTargetEvaluation = Time.time;
            }
            
            // Choose behavior based on drone type and situation
            ChooseBehavior();
        }
        
        private void ChooseBehavior()
        {
            var droneUnit = droneController.DroneUnit;
            var droneType = droneUnit.specifications.type;
            
            // If we have a high-priority target, engage
            if (currentTargetFire != null && currentTargetFire.GetPriorityScore() > priorityThreshold)
            {
                if (droneType == DroneType.HeavyLiftSuppression || droneType == DroneType.UrbanHighRiseSuppression)
                {
                    SetBehavior(DroneAIBehavior.Suppressing);
                }
                else
                {
                    SetBehavior(DroneAIBehavior.Investigating);
                }
                return;
            }
            
            // Default behaviors based on drone type
            switch (droneType)
            {
                case DroneType.Reconnaissance:
                case DroneType.FastReconnaissance:
                    SetBehavior(DroneAIBehavior.Scouting);
                    break;
                    
                case DroneType.HighAltitudeSurveillance:
                case DroneType.LongRangeSurveillance:
                    SetBehavior(DroneAIBehavior.Surveying);
                    break;
                    
                case DroneType.HeavyLiftSuppression:
                case DroneType.UrbanHighRiseSuppression:
                    SetBehavior(DroneAIBehavior.Patrolling);
                    break;
                    
                default:
                    SetBehavior(DroneAIBehavior.Patrolling);
                    break;
            }
        }
        
        private void SetBehavior(DroneAIBehavior newBehavior)
        {
            if (currentBehavior != newBehavior)
            {
                UnityEngine.Debug.Log($"Drone {droneController.DroneUnit.callSign} changing behavior: {currentBehavior} -> {newBehavior}");
                currentBehavior = newBehavior;
                OnBehaviorChanged();
            }
        }
        
        private void OnBehaviorChanged()
        {
            // Reset behavior-specific state
            switch (currentBehavior)
            {
                case DroneAIBehavior.Patrolling:
                    if (randomPatrol)
                    {
                        currentPatrolIndex = UnityEngine.Random.Range(0, patrolPoints.Count);
                    }
                    break;
                    
                case DroneAIBehavior.Returning:
                    var commandVehicle = GetCommandVehicle();
                    if (commandVehicle != null)
                    {
                        droneController.SetTarget(commandVehicle.position);
                    }
                    break;
            }
        }
        
        #endregion
        
        #region Behavior Execution
        
        private void ExecuteCurrentBehavior()
        {
            switch (currentBehavior)
            {
                case DroneAIBehavior.Idle:
                    ExecuteIdleBehavior();
                    break;
                    
                case DroneAIBehavior.Patrolling:
                    ExecutePatrolBehavior();
                    break;
                    
                case DroneAIBehavior.Scouting:
                    ExecuteScoutingBehavior();
                    break;
                    
                case DroneAIBehavior.Surveying:
                    ExecuteSurveyingBehavior();
                    break;
                    
                case DroneAIBehavior.Investigating:
                    ExecuteInvestigatingBehavior();
                    break;
                    
                case DroneAIBehavior.Suppressing:
                    ExecuteSuppressingBehavior();
                    break;
                    
                case DroneAIBehavior.Returning:
                    ExecuteReturningBehavior();
                    break;
                    
                case DroneAIBehavior.Emergency:
                    ExecuteEmergencyBehavior();
                    break;
                    
                case DroneAIBehavior.Formation:
                    ExecuteFormationBehavior();
                    break;
            }
        }
        
        private void ExecuteIdleBehavior()
        {
            // Stay at current position, minimal movement
            // Check for tasks or threats
            if (ShouldStartPatrolling())
            {
                SetBehavior(DroneAIBehavior.Patrolling);
            }
        }
        
        private void ExecutePatrolBehavior()
        {
            if (patrolPoints.Count == 0)
            {
                GeneratePatrolPoints();
                return;
            }
            
            Vector3 targetPatrolPoint = patrolPoints[currentPatrolIndex];
            float distanceToPatrol = Vector3.Distance(droneController.transform.position, targetPatrolPoint);
            
            if (distanceToPatrol <= 10f) // Reached patrol point
            {
                // Move to next patrol point
                if (randomPatrol)
                {
                    currentPatrolIndex = UnityEngine.Random.Range(0, patrolPoints.Count);
                }
                else
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
                }
                
                targetPatrolPoint = patrolPoints[currentPatrolIndex];
            }
            
            droneController.SetTarget(targetPatrolPoint);
        }
        
        private void ExecuteScoutingBehavior()
        {
            // Look for new fires or investigate suspicious areas
            var nearbyFires = FindNearbyFires(maxSearchRadius);
            
            if (nearbyFires.Count > 0)
            {
                // Investigate the closest active fire
                var targetFire = nearbyFires
                    .Where(f => f.status == FireStatus.Active)
                    .OrderBy(f => Vector3.Distance(droneController.transform.position, new Vector3((float)f.latitude, 0, (float)f.longitude)))
                    .FirstOrDefault();
                
                if (targetFire != null)
                {
                    currentTargetFire = targetFire;
                    Vector3 firePosition = GetFirePosition(targetFire);
                    droneController.SetTarget(firePosition, targetFire.id);
                    SetBehavior(DroneAIBehavior.Investigating);
                }
            }
            else
            {
                // No fires nearby, continue patrol
                SetBehavior(DroneAIBehavior.Patrolling);
            }
        }
        
        private void ExecuteSurveyingBehavior()
        {
            // High-altitude surveillance pattern
            Vector3 surveyCenter = droneController.transform.position;
            float surveyAltitude = droneController.DroneUnit.specifications.operationalAltitude * 0.8f;
            
            // Create survey pattern (figure-8 or grid)
            Vector3 surveyTarget = GenerateSurveyTarget(surveyCenter, surveyAltitude);
            droneController.SetTarget(surveyTarget);
        }
        
        private void ExecuteInvestigatingBehavior()
        {
            if (currentTargetFire == null)
            {
                SetBehavior(DroneAIBehavior.Scouting);
                return;
            }
            
            // Move to fire location for investigation
            Vector3 firePosition = GetFirePosition(currentTargetFire);
            droneController.SetTarget(firePosition, currentTargetFire.id);
            
            // Check if investigation is complete
            float distanceToFire = Vector3.Distance(droneController.transform.position, firePosition);
            if (distanceToFire <= 50f)
            {
                // Investigation complete, fire status can be updated in FireSimulation
                // (isConfirmed property doesn't exist, so we skip this step)
                
                // Decide next action based on drone type
                if (droneController.DroneUnit.specifications.hasHighPressureNozzle)
                {
                    SetBehavior(DroneAIBehavior.Suppressing);
                }
                else
                {
                    // Call for suppression support
                    RequestSuppressionSupport();
                    SetBehavior(DroneAIBehavior.Scouting);
                }
            }
        }
        
        private void ExecuteSuppressingBehavior()
        {
            if (currentTargetFire == null)
            {
                SetBehavior(DroneAIBehavior.Patrolling);
                return;
            }
            
            // Move to optimal suppression position
            Vector3 suppressionPosition = CalculateOptimalSuppressionPosition(currentTargetFire);
            droneController.SetTarget(suppressionPosition, currentTargetFire.id);
            
            // Start suppression if in range
            Vector3 firePosition = GetFirePosition(currentTargetFire);
            float distanceToFire = Vector3.Distance(droneController.transform.position, firePosition);
            if (distanceToFire <= 50f)
            {
                droneController.StartSuppression(firePosition);
            }
            
            // Check if fire is suppressed or payload depleted
            if (currentTargetFire.intensity <= 0.1f || droneController.DroneUnit.currentSuppressantUnits <= 0)
            {
                currentTargetFire = null;
                SetBehavior(DroneAIBehavior.Returning);
            }
        }
        
        private void ExecuteReturningBehavior()
        {
            var commandVehicle = GetCommandVehicle();
            if (commandVehicle != null)
            {
                float distanceToCV = Vector3.Distance(droneController.transform.position, commandVehicle.position);
                if (distanceToCV <= 20f)
                {
                    // Returned successfully
                    droneController.DroneUnit.state = DroneState.Idle;
                    SetBehavior(DroneAIBehavior.Idle);
                }
                else
                {
                    droneController.SetTarget(commandVehicle.position);
                }
            }
        }
        
        private void ExecuteEmergencyBehavior()
        {
            // Emergency return to base
            var commandVehicle = GetCommandVehicle();
            if (commandVehicle != null)
            {
                droneController.SetTarget(commandVehicle.position);
                droneController.DroneUnit.state = DroneState.Returning;
            }
        }
        
        private void ExecuteFormationBehavior()
        {
            if (!enableFormationFlying || !enableSwarmBehavior)
            {
                SetBehavior(DroneAIBehavior.Patrolling);
                return;
            }
            
            if (isSwarmLeader)
            {
                // Lead the formation
                ExecuteSwarmLeaderBehavior();
            }
            else
            {
                // Follow formation
                ExecuteSwarmFollowerBehavior();
            }
        }
        
        #endregion
        
        #region Target Evaluation
        
        private void EvaluateTargets()
        {
            var nearbyFires = FindNearbyFires(maxSearchRadius);
            if (nearbyFires.Count == 0)
            {
                currentTargetFire = null;
                return;
            }
            
            // Score each fire based on priority factors
            var scoredFires = nearbyFires.Select(fire => new
            {
                Fire = fire,
                Score = CalculateFirePriority(fire)
            }).OrderByDescending(x => x.Score);
            
            var bestTarget = scoredFires.FirstOrDefault();
            if (bestTarget != null && bestTarget.Score > priorityThreshold)
            {
                currentTargetFire = bestTarget.Fire;
                lastKnownFireLocation = GetFirePosition(bestTarget.Fire);
            }
            else
            {
                currentTargetFire = null;
            }
        }
        
        private float CalculateFirePriority(FireIncident fire)
        {
            float score = 0f;
            Vector3 dronePosition = droneController.transform.position;
            
            // Proximity factor
            Vector3 firePosition = GetFirePosition(fire);
            float distance = Vector3.Distance(dronePosition, firePosition);
            float proximityScore = Mathf.Clamp01(1f - (distance / maxSearchRadius));
            score += proximityScore * fireProximityWeight;
            
            // Fire size factor
            float sizeScore = Mathf.Clamp01(fire.intensity);
            score += sizeScore * fireSizeWeight;
            
            // Fire priority factor
            float priorityScore = fire.GetPriorityScore();
            score += priorityScore * firePriorityWeight;
            
            // Battery conservation factor
            float batteryPercent = droneController.DroneUnit.batteryLife / 100f;
            float batteryScore = batteryPercent;
            score += batteryScore * batteryConservationWeight;
            
            return score;
        }
        
        #endregion
        
        #region Decision Conditions
        
        private bool ShouldReturn()
        {
            var droneUnit = droneController.DroneUnit;
            
            // Low battery
            float batteryPercent = droneUnit.batteryLife / 100f;
            if (batteryPercent <= 0.25f)
                return true;
            
            // Payload depleted for suppression drones
            if (droneUnit.specifications.hasHighPressureNozzle && droneUnit.currentSuppressantUnits <= 0)
                return true;
            
            // Manual recall order
            if (droneUnit.state == DroneState.Returning)
                return true;
            
            return false;
        }
        
        private bool ShouldRespondsToEmergency()
        {
            // Check for critical fires or emergency situations
            var criticalFires = FindNearbyFires(maxSearchRadius * 2f)
                .Where(f => f.GetPriorityScore() > 0.8f)
                .ToList();
            
            return criticalFires.Count > 0;
        }
        
        private bool ShouldStartPatrolling()
        {
            // Start patrolling if idle for too long
            return Time.time - lastDecisionTime > 30f;
        }
        
        #endregion
        
        #region Swarm Behavior
        
        private void ExecuteSwarmLeaderBehavior()
        {
            // Implement swarm leader logic
            if (currentTargetFire != null)
            {
                // Lead swarm to target
                Vector3 attackPosition = CalculateSwarmAttackPosition(currentTargetFire);
                droneController.SetTarget(attackPosition, currentTargetFire.id);
                
                // Coordinate swarm members
                CoordinateSwarmMembers();
            }
            else
            {
                // Lead patrol formation
                ExecutePatrolBehavior();
            }
        }
        
        private void ExecuteSwarmFollowerBehavior()
        {
            // Find swarm leader
            var leader = FindSwarmLeader();
            if (leader == null)
            {
                SetBehavior(DroneAIBehavior.Patrolling);
                return;
            }
            
            // Maintain formation position relative to leader
            Vector3 formationPosition = CalculateFormationPosition(leader.transform.position);
            droneController.SetTarget(formationPosition);
        }
        
        private DroneAI FindSwarmLeader()
        {
            if (string.IsNullOrEmpty(swarmLeaderId))
                return null;
            
            // Find leader by ID
            var allDrones = fleetManager?.GetAllActiveDrones();
            if (allDrones != null)
            {
                var leaderDrone = allDrones.FirstOrDefault(d => d.unitId == swarmLeaderId);
                if (leaderDrone != null)
                {
                    // Find the DroneAI component
                    // This would require a reference system or manager
                }
            }
            
            return null;
        }
        
        private void CoordinateSwarmMembers()
        {
            // Send formation commands to swarm members
            // This would be implemented with a communication system
        }
        
        private Vector3 CalculateFormationPosition(Vector3 leaderPosition)
        {
            // Calculate position in formation based on drone index
            float angle = swarmMemberIds.IndexOf(droneController.DroneUnit.unitId) * 60f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * formationRadius,
                0f,
                Mathf.Sin(angle) * formationRadius
            );
            
            return leaderPosition + offset;
        }
        
        private Vector3 CalculateSwarmAttackPosition(FireIncident fire)
        {
            // Calculate optimal attack position for swarm
            Vector3 firePosition = GetFirePosition(fire);
            return firePosition + Vector3.up * 100f;
        }
        
        #endregion
        
        #region Pathfinding
        
        private IEnumerator PathfindingLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(pathUpdateInterval);
                
                if (enableAdvancedPathfinding && droneController.HasTarget)
                {
                    UpdatePlannedPath();
                }
            }
        }
        
        private void UpdatePlannedPath()
        {
            // Simple pathfinding implementation
            // In a full implementation, this would use A* or similar algorithms
            
            plannedPath.Clear();
            Vector3 start = droneController.transform.position;
            Vector3 target = droneController.DroneUnit.targetPosition;
            
            // Add waypoints to avoid obstacles
            Vector3 midpoint = Vector3.Lerp(start, target, 0.5f) + Vector3.up * 50f;
            plannedPath.Enqueue(midpoint);
            plannedPath.Enqueue(target);
        }
        
        #endregion
        
        #region Utility Methods
        
        private List<FireIncident> FindNearbyFires(float radius)
        {
            var allFires = new List<FireIncident>();
            
            if (fireSimulation != null)
            {
                // Get fires from fire simulation
                // This would require an interface to FireSimulation
            }
            
            Vector3 dronePosition = droneController.transform.position;
            return allFires.Where(f => Vector3.Distance(dronePosition, GetFirePosition(f)) <= radius).ToList();
        }
        
        private CommandVehicle GetCommandVehicle()
        {
            var droneUnit = droneController.DroneUnit;
            var countryFleets = fleetManager?.CountryFleets;
            
            if (countryFleets != null && countryFleets.ContainsKey(droneUnit.specifications.country))
            {
                return countryFleets[droneUnit.specifications.country]
                    .FirstOrDefault(cv => cv.assignedDrones.Any(d => d.unitId == droneUnit.unitId));
            }
            
            return null;
        }
        
        private Vector3 GenerateSurveyTarget(Vector3 center, float altitude)
        {
            // Generate survey pattern point
            float time = Time.time * 0.1f;
            float x = center.x + Mathf.Sin(time) * 200f;
            float z = center.z + Mathf.Cos(time * 2f) * 200f;
            
            return new Vector3(x, altitude, z);
        }
        
        private Vector3 CalculateOptimalSuppressionPosition(FireIncident fire)
        {
            // Calculate best position for fire suppression
            Vector3 firePosition = GetFirePosition(fire);
            Vector3 upwind = Vector3.left * 30f; // Simplified wind calculation
            return firePosition + upwind + Vector3.up * 50f;
        }
        
        private void RequestSuppressionSupport()
        {
            // Request suppression drones from fleet manager
            UnityEngine.Debug.Log($"Drone {droneController.DroneUnit.callSign} requesting suppression support");
            
            if (fleetManager != null && currentTargetFire != null)
            {
                // This would trigger deployment of suppression-capable drones
            }
        }
        
        #endregion
        
        #region Public API
        
        public void OnTargetReached(Vector3 location)
        {
            // Called by DroneController when target is reached
            switch (currentBehavior)
            {
                case DroneAIBehavior.Investigating:
                    // Investigation complete
                    break;
                    
                case DroneAIBehavior.Patrolling:
                    // Continue to next patrol point
                    break;
            }
        }
        
        public void SetSwarmTarget(FireIncident fire)
        {
            currentTargetFire = fire;
            SetBehavior(DroneAIBehavior.Formation);
        }
        
        public void JoinSwarm(string leaderId)
        {
            swarmLeaderId = leaderId;
            isSwarmLeader = false;
            SetBehavior(DroneAIBehavior.Formation);
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnDestroy()
        {
            if (behaviorCoroutine != null)
            {
                StopCoroutine(behaviorCoroutine);
            }
            
            if (pathfindingCoroutine != null)
            {
                StopCoroutine(pathfindingCoroutine);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private Vector3 GetFirePosition(FireIncident fire)
        {
            if (geospatialManager != null)
            {
                return geospatialManager.ConvertLatLonToUnityPosition(fire.latitude, fire.longitude, 0);
            }
            else
            {
                // Fallback to simple conversion if no geospatial manager
                return new Vector3((float)fire.longitude, 0, (float)fire.latitude);
            }
        }
        
        #endregion
    }
    
    #region Supporting Enums
    
    public enum DroneAIBehavior
    {
        Idle,
        Patrolling,
        Scouting,
        Surveying,
        Investigating,
        Suppressing,
        Returning,
        Emergency,
        Formation
    }
    
    #endregion
}

