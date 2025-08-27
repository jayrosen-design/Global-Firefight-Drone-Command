using System;
using System.Collections;
using UnityEngine;
using GlobalFirefight.Data;
using GlobalFirefight.Core;
using GlobalFirefight.Fire;

namespace GlobalFirefight.Drones
{
    public class DroneController : MonoBehaviour
    {
        [Header("Drone Configuration")]
        [SerializeField] private DroneUnit droneUnit;
        [SerializeField] private bool isPlayerControlled = false;
        [SerializeField] private float smoothTime = 0.3f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float arrivalThreshold = 5f;
        
        [Header("Movement")]
        [SerializeField] private Vector3 currentVelocity;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private Quaternion targetRotation;
        [SerializeField] private float currentSpeed;
        [SerializeField] private float altitudeOffset = 50f; // Height above ground
        
        [Header("Navigation")]
        [SerializeField] private bool hasTarget = false;
        [SerializeField] private string assignedFireId;
        [SerializeField] private Vector3 lastKnownTarget;
        [SerializeField] private float distanceToTarget;
        [SerializeField] private float timeSinceLastUpdate;
        
        [Header("Visual Elements")]
        [SerializeField] private GameObject droneModel;
        [SerializeField] private GameObject propellerGroup;
        [SerializeField] private ParticleSystem exhaustEffect;
        [SerializeField] private Light navigationLight;
        [SerializeField] private Renderer[] droneRenderers;
        
        [Header("Audio")]
        [SerializeField] private AudioSource propellerSound;
        [SerializeField] private AudioSource systemSound;
        [SerializeField] private AudioClip deploySound;
        [SerializeField] private AudioClip suppressantReleaseSound;
        [SerializeField] private AudioClip returnSound;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem suppressantEffect;
        [SerializeField] private LineRenderer suppressantStream;
        [SerializeField] private bool isReleasingSupressant = false;
        [SerializeField] private float suppressantReleaseRate = 10f; // units per second
        
        [Header("Performance")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance = 1000f;
        [SerializeField] private bool isInLODRange = true;
        [SerializeField] private int updateFrequency = 60; // Updates per second
        
        // Private fields
        private Rigidbody droneRigidbody;
        private Collider droneCollider;
        private DroneAI droneAI;
        private Camera playerCamera;
        private bool isInitialized = false;
        private float lastUpdateTime;
        private Coroutine movementCoroutine;
        private Coroutine suppressionCoroutine;
        
        // Properties
        public DroneUnit DroneUnit => droneUnit;
        public bool IsPlayerControlled => isPlayerControlled;
        public bool HasTarget => hasTarget;
        public string AssignedFireId => assignedFireId;
        public Vector3 CurrentPosition => transform.position;
        public float DistanceToTarget => distanceToTarget;
        
        #region Initialization
        
        public void Initialize(DroneUnit unit)
        {
            droneUnit = unit;
            
            // Get required components
            droneRigidbody = GetComponent<Rigidbody>();
            if (droneRigidbody == null)
            {
                droneRigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            droneCollider = GetComponent<Collider>();
            if (droneCollider == null)
            {
                droneCollider = gameObject.AddComponent<BoxCollider>();
            }
            
            // Configure rigidbody
            droneRigidbody.useGravity = false; // Drones maintain altitude
            droneRigidbody.linearDamping = 1f;
            droneRigidbody.angularDamping = 5f;
            droneRigidbody.mass = GetDroneMass();
            
            // Initialize AI if not player controlled
            if (!isPlayerControlled)
            {
                droneAI = gameObject.AddComponent<DroneAI>();
                droneAI.Initialize(this);
            }
            
            // Setup visual elements
            InitializeVisualElements();
            InitializeAudio();
            InitializeEffects();
            
            // Set initial position
            transform.position = droneUnit.position;
            targetPosition = droneUnit.position;
            
            isInitialized = true;
            UnityEngine.Debug.Log($"DroneController initialized for {droneUnit.callSign}");
        }
        
        private void InitializeVisualElements()
        {
            // Find or create drone model
            if (droneModel == null)
            {
                droneModel = transform.GetChild(0)?.gameObject;
            }
            
            // Apply drone livery
            ApplyDroneLivery();
            
            // Setup navigation light
            if (navigationLight == null)
            {
                GameObject lightObj = new GameObject("NavigationLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.up * 0.5f;
                
                navigationLight = lightObj.AddComponent<Light>();
                navigationLight.type = LightType.Point;
                navigationLight.color = Color.red;
                navigationLight.intensity = 1f;
                navigationLight.range = 50f;
            }
            
            // Find propeller group for animation
            if (propellerGroup == null)
            {
                propellerGroup = transform.Find("Propellers")?.gameObject;
            }
        }
        
        private void InitializeAudio()
        {
            // Create propeller sound
            if (propellerSound == null)
            {
                GameObject propellerSoundObj = new GameObject("PropellerSound");
                propellerSoundObj.transform.SetParent(transform);
                propellerSound = propellerSoundObj.AddComponent<AudioSource>();
                propellerSound.loop = true;
                propellerSound.volume = 0.3f;
                propellerSound.pitch = 1f;
                propellerSound.spatialBlend = 1f; // 3D sound
                propellerSound.rolloffMode = AudioRolloffMode.Logarithmic;
                propellerSound.maxDistance = 500f;
            }
            
            // Create system sound
            if (systemSound == null)
            {
                GameObject systemSoundObj = new GameObject("SystemSound");
                systemSoundObj.transform.SetParent(transform);
                systemSound = systemSoundObj.AddComponent<AudioSource>();
                systemSound.volume = 0.5f;
                systemSound.spatialBlend = 1f;
            }
        }
        
        private void InitializeEffects()
        {
            // Create exhaust effect
            if (exhaustEffect == null && droneUnit.specifications.type != DroneType.Reconnaissance)
            {
                GameObject exhaustObj = new GameObject("ExhaustEffect");
                exhaustObj.transform.SetParent(transform);
                exhaustObj.transform.localPosition = Vector3.down * 0.5f;
                
                exhaustEffect = exhaustObj.AddComponent<ParticleSystem>();
                var main = exhaustEffect.main;
                main.startLifetime = 1f;
                main.startSpeed = 5f;
                main.startSize = 0.1f;
                main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                main.maxParticles = 50;
                
                var emission = exhaustEffect.emission;
                emission.rateOverTime = 20f;
            }
            
            // Create suppressant effect if drone has suppression capability
            if (droneUnit.specifications.hasHighPressureNozzle)
            {
                CreateSupressantSystem();
            }
        }
        
        private void CreateSupressantSystem()
        {
            // Suppressant particle effect
            if (suppressantEffect == null)
            {
                GameObject suppressantObj = new GameObject("SupressantEffect");
                suppressantObj.transform.SetParent(transform);
                suppressantObj.transform.localPosition = Vector3.down;
                
                suppressantEffect = suppressantObj.AddComponent<ParticleSystem>();
                var main = suppressantEffect.main;
                main.startLifetime = 3f;
                main.startSpeed = 15f;
                main.startSize = 0.3f;
                main.startColor = new Color(1f, 1f, 1f, 0.8f);
                main.maxParticles = 200;
                
                var emission = suppressantEffect.emission;
                emission.rateOverTime = 0f; // Controlled manually
                
                var shape = suppressantEffect.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 15f;
                
                suppressantEffect.Stop();
            }
            
            // Suppressant stream line renderer
            if (suppressantStream == null)
            {
                GameObject streamObj = new GameObject("SupressantStream");
                streamObj.transform.SetParent(transform);
                
                suppressantStream = streamObj.AddComponent<LineRenderer>();
                suppressantStream.material = new Material(Shader.Find("Standard"));
                suppressantStream.material.color = new Color(1f, 1f, 1f, 0.6f);
                suppressantStream.startWidth = 0.5f;
                suppressantStream.endWidth = 2f;
                suppressantStream.positionCount = 2;
                suppressantStream.enabled = false;
            }
        }
        
        private void ApplyDroneLivery()
        {
            droneRenderers = GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in droneRenderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = droneUnit.specifications.primaryColor;
                    // Apply secondary color to specific parts if needed
                }
            }
        }
        
        private float GetDroneMass()
        {
            switch (droneUnit.specifications.type)
            {
                case DroneType.Reconnaissance:
                case DroneType.FastReconnaissance:
                    return 4f; // Light drones
                case DroneType.HeavyLiftSuppression:
                    return 50f; // Heavy drones
                case DroneType.UrbanHighRiseSuppression:
                    return 25f; // Medium drones
                case DroneType.HighAltitudeSurveillance:
                case DroneType.LongRangeSurveillance:
                    return 15f; // Surveillance drones
                default:
                    return 10f;
            }
        }
        
        #endregion
        
        #region Movement Control
        
        public void SetTarget(Vector3 target, string fireId = null)
        {
            targetPosition = target + Vector3.up * altitudeOffset;
            assignedFireId = fireId;
            hasTarget = true;
            lastKnownTarget = target;
            
            // Update drone unit
            droneUnit.targetPosition = targetPosition;
            droneUnit.assignedFireId = fireId;
            
            // Calculate target rotation
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction);
            }
            
            // Start movement if not already moving
            if (movementCoroutine == null)
            {
                movementCoroutine = StartCoroutine(MoveToTarget());
            }
            
            UnityEngine.Debug.Log($"Drone {droneUnit.callSign} assigned to target {target}");
        }
        
        public void UpdateDrone(float deltaTime)
        {
            if (!isInitialized)
                return;
            
            timeSinceLastUpdate += deltaTime;
            
            // Update at specified frequency
            if (timeSinceLastUpdate >= 1f / updateFrequency)
            {
                PerformDroneUpdate(timeSinceLastUpdate);
                timeSinceLastUpdate = 0f;
            }
            
            // Update visual effects
            UpdateVisualEffects(deltaTime);
            UpdateAudio(deltaTime);
        }
        
        private void PerformDroneUpdate(float deltaTime)
        {
            // Update drone unit systems
            droneUnit.UpdateSystems(deltaTime);
            
            // Update position
            droneUnit.position = transform.position;
            
            // Check if drone should be recalled
            CheckRecallConditions();
            
            // Update distance to target
            if (hasTarget)
            {
                distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                
                // Check if target reached
                if (distanceToTarget <= arrivalThreshold)
                {
                    OnTargetReached();
                }
            }
            
            // Update LOD
            if (enableLOD)
            {
                UpdateLOD();
            }
        }
        
        private IEnumerator MoveToTarget()
        {
            while (hasTarget && Vector3.Distance(transform.position, targetPosition) > arrivalThreshold)
            {
                // Calculate movement
                Vector3 direction = (targetPosition - transform.position).normalized;
                float moveSpeed = GetCurrentMoveSpeed();
                
                // Apply movement
                Vector3 targetVelocity = direction * moveSpeed;
                droneRigidbody.linearVelocity = Vector3.SmoothDamp(
                    droneRigidbody.linearVelocity, 
                    targetVelocity, 
                    ref currentVelocity, 
                    smoothTime
                );
                
                // Update rotation
                if (direction != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, 
                        lookRotation, 
                        rotationSpeed * Time.deltaTime
                    );
                }
                
                // Update drone state
                if (droneUnit.state == DroneState.Idle)
                {
                    droneUnit.state = DroneState.Deploying;
                }
                
                yield return null;
            }
            
            // Stop movement
            droneRigidbody.linearVelocity = Vector3.zero;
            movementCoroutine = null;
        }
        
        private float GetCurrentMoveSpeed()
        {
            float baseSpeed = droneUnit.specifications.maxSpeed;
            
            // Apply efficiency factors
            float batteryFactor = droneUnit.batteryLife / 100f;
            float payloadFactor = 1f - ((float)droneUnit.currentSuppressantUnits / droneUnit.specifications.payloadCapacity * 0.3f);
            
            return baseSpeed * batteryFactor * payloadFactor;
        }
        
        #endregion
        
        #region Fire Suppression
        
        public void StartSuppression(Vector3 fireLocation)
        {
            if (!droneUnit.specifications.hasHighPressureNozzle || droneUnit.currentSuppressantUnits <= 0)
            {
                UnityEngine.Debug.LogWarning($"Drone {droneUnit.callSign} cannot perform suppression");
                return;
            }
            
            if (suppressionCoroutine != null)
            {
                StopCoroutine(suppressionCoroutine);
            }
            
            suppressionCoroutine = StartCoroutine(PerformSuppression(fireLocation));
        }
        
        private IEnumerator PerformSuppression(Vector3 fireLocation)
        {
            droneUnit.state = DroneState.Suppressing;
            isReleasingSupressant = true;
            
            // Start visual effects
            if (suppressantEffect != null)
            {
                suppressantEffect.Play();
            }
            
            if (suppressantStream != null)
            {
                suppressantStream.enabled = true;
                UpdateSupressantStream(fireLocation);
            }
            
            // Play audio
            if (systemSound != null && suppressantReleaseSound != null)
            {
                systemSound.PlayOneShot(suppressantReleaseSound);
            }
            
            // Release suppressant over time
            float suppressionDuration = droneUnit.currentSuppressantUnits / suppressantReleaseRate;
            float elapsed = 0f;
            
            while (elapsed < suppressionDuration && droneUnit.currentSuppressantUnits > 0)
            {
                elapsed += Time.deltaTime;
                
                // Consume suppressant
                float consumed = suppressantReleaseRate * Time.deltaTime;
                droneUnit.currentSuppressantUnits = Mathf.Max(0, droneUnit.currentSuppressantUnits - (int)consumed);
                
                // Apply suppression to fire
                ApplySuppressionToFire(fireLocation, consumed);
                
                // Update stream visual
                UpdateSupressantStream(fireLocation);
                
                yield return null;
            }
            
            // Stop suppression
            StopSuppression();
        }
        
        private void StopSuppression()
        {
            isReleasingSupressant = false;
            
            // Stop visual effects
            if (suppressantEffect != null)
            {
                suppressantEffect.Stop();
            }
            
            if (suppressantStream != null)
            {
                suppressantStream.enabled = false;
            }
            
            // Update drone state
            if (droneUnit.currentSuppressantUnits <= 0)
            {
                droneUnit.ForceReturn("Payload depleted");
            }
            else
            {
                droneUnit.state = DroneState.Idle;
            }
            
            suppressionCoroutine = null;
        }
        
        private void UpdateSupressantStream(Vector3 fireLocation)
        {
            if (suppressantStream == null)
                return;
            
            suppressantStream.SetPosition(0, transform.position + Vector3.down * 0.5f);
            suppressantStream.SetPosition(1, fireLocation);
        }
        
        private void ApplySuppressionToFire(Vector3 fireLocation, float amount)
        {
            // Find fire simulation component
            var fireSimulation = FindFirstObjectByType<FireSimulation>();
            if (fireSimulation != null)
            {
                fireSimulation.ApplySuppressionToFire(assignedFireId ?? "unknown", SuppressantType.Water, amount, fireLocation);
            }
        }
        
        #endregion
        
        #region Target Management
        
        private void OnTargetReached()
        {
            hasTarget = false;
            droneUnit.state = DroneState.OnStation;
            
            // If assigned to a fire, start suppression
            if (!string.IsNullOrEmpty(assignedFireId) && droneUnit.specifications.hasHighPressureNozzle)
            {
                StartSuppression(lastKnownTarget);
            }
            
            // Notify AI to choose next action
            if (droneAI != null)
            {
                droneAI.OnTargetReached(lastKnownTarget);
            }
            
            UnityEngine.Debug.Log($"Drone {droneUnit.callSign} reached target {lastKnownTarget}");
        }
        
        private void CheckRecallConditions()
        {
            // Check battery level
            float batteryPercent = droneUnit.batteryLife / 100f;
            if (batteryPercent <= 0.2f) // 20% battery
            {
                droneUnit.ForceReturn("Low battery");
                return;
            }
            
            // Check if payload depleted for suppression drones
            if (droneUnit.specifications.hasHighPressureNozzle && droneUnit.currentSuppressantUnits <= 0)
            {
                droneUnit.ForceReturn("Payload depleted");
                return;
            }
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateVisualEffects(float deltaTime)
        {
            // Animate propellers
            if (propellerGroup != null && droneUnit.state != DroneState.Idle)
            {
                float rpm = GetCurrentMoveSpeed() * 50f; // Convert to RPM
                propellerGroup.transform.Rotate(Vector3.up, rpm * deltaTime);
            }
            
            // Update navigation light
            if (navigationLight != null)
            {
                Color lightColor = GetStatusColor();
                navigationLight.color = lightColor;
                
                // Blink for certain states
                if (droneUnit.state == DroneState.Deploying || droneUnit.state == DroneState.Returning)
                {
                    float blink = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;
                    navigationLight.intensity = blink;
                }
                else
                {
                    navigationLight.intensity = 1f;
                }
            }
            
            // Update exhaust effect
            if (exhaustEffect != null)
            {
                var emission = exhaustEffect.emission;
                if (droneUnit.state == DroneState.Idle)
                {
                    emission.rateOverTime = 5f;
                }
                else
                {
                    emission.rateOverTime = 20f;
                }
            }
        }
        
        private void UpdateAudio(float deltaTime)
        {
            if (propellerSound != null)
            {
                float targetVolume = droneUnit.state == DroneState.Idle ? 0.1f : 0.3f;
                float targetPitch = droneUnit.state == DroneState.Idle ? 0.8f : 1.2f;
                
                propellerSound.volume = Mathf.Lerp(propellerSound.volume, targetVolume, deltaTime * 2f);
                propellerSound.pitch = Mathf.Lerp(propellerSound.pitch, targetPitch, deltaTime * 2f);
                
                if (!propellerSound.isPlaying)
                {
                    propellerSound.Play();
                }
            }
        }
        
        private Color GetStatusColor()
        {
            switch (droneUnit.state)
            {
                case DroneState.Idle:
                    return Color.green;
                case DroneState.Deploying:
                    return Color.yellow;
                case DroneState.OnStation:
                    return Color.blue;
                case DroneState.Returning:
                    return Color.cyan;
                case DroneState.Destroyed:
                    return Color.red;
                default:
                    return Color.white;
            }
        }
        
        private void UpdateLOD()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                    return;
            }
            
            float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
            bool wasInRange = isInLODRange;
            isInLODRange = distance <= lodDistance;
            
            if (wasInRange != isInLODRange)
            {
                // Enable/disable detailed components based on distance
                if (droneModel != null)
                {
                    droneModel.SetActive(isInLODRange);
                }
                
                if (propellerGroup != null)
                {
                    propellerGroup.SetActive(isInLODRange);
                }
                
                // Adjust audio range
                if (propellerSound != null)
                {
                    propellerSound.maxDistance = isInLODRange ? 500f : 100f;
                }
            }
        }
        
        #endregion
        
        #region Player Control
        
        public void SetPlayerControl(bool playerControlled)
        {
            isPlayerControlled = playerControlled;
            
            if (isPlayerControlled)
            {
                // Disable AI
                if (droneAI != null)
                {
                    droneAI.enabled = false;
                }
                
                // Enable player input handling
                EnablePlayerInput();
            }
            else
            {
                // Enable AI
                if (droneAI != null)
                {
                    droneAI.enabled = true;
                }
                
                // Disable player input
                DisablePlayerInput();
            }
        }
        
        private void EnablePlayerInput()
        {
            // Player input will be handled in Update when isPlayerControlled is true
        }
        
        private void DisablePlayerInput()
        {
            // Stop any player-initiated movement
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Update()
        {
            if (!isInitialized)
                return;
            
            // Handle player input if controlled by player
            if (isPlayerControlled)
            {
                HandlePlayerInput();
            }
        }
        
        private void HandlePlayerInput()
        {
            // Basic player control implementation
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            float altitude = 0f;
            
            if (Input.GetKey(KeyCode.Q))
                altitude = -1f;
            if (Input.GetKey(KeyCode.E))
                altitude = 1f;
            
            Vector3 movement = new Vector3(horizontal, altitude, vertical) * GetCurrentMoveSpeed();
            droneRigidbody.linearVelocity = movement;
            
            // Fire suppression
            if (Input.GetKey(KeyCode.Space) && droneUnit.specifications.hasHighPressureNozzle)
            {
                if (!isReleasingSupressant)
                {
                    StartSuppression(transform.position + Vector3.down * 10f);
                }
            }
            else if (isReleasingSupressant)
            {
                StopSuppression();
            }
        }
        
        private void OnDestroy()
        {
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
            }
            
            if (suppressionCoroutine != null)
            {
                StopCoroutine(suppressionCoroutine);
            }
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            if (!isInitialized)
                return;
            
            // Draw target
            if (hasTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetPosition, 5f);
                Gizmos.DrawLine(transform.position, targetPosition);
            }
            
            // Draw operational range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, droneUnit.specifications.maxRange);
            
            // Draw suppression range
            if (droneUnit.specifications.hasHighPressureNozzle)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, 50f); // Suppression range
            }
        }
        
        #endregion
    }
}

