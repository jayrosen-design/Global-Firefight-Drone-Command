using System.Collections;
using UnityEngine;
using GlobalFirefight.Data;
using GlobalFirefight.Core;
using GlobalFirefight.Drones;

namespace GlobalFirefight.Systems
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform cameraRig;
        
        [Header("RTS Camera Settings")]
        [SerializeField] private float rtsMovementSpeed = 20f;
        [SerializeField] private float rtsRotationSpeed = 2f;
        [SerializeField] private float rtsZoomSpeed = 5f;
        [SerializeField] private float rtsMinZoom = 5f;
        [SerializeField] private float rtsMaxZoom = 100f;
        [SerializeField] private float rtsMaxAltitude = 50000f; // 50km max altitude
        
        [Header("Drone Camera Settings")]
        [SerializeField] private float droneFollowSpeed = 5f;
        [SerializeField] private float droneRotationSpeed = 3f;
        [SerializeField] private Vector3 droneFollowOffset = new Vector3(0, 5, -10);
        [SerializeField] private float droneCameraHeight = 100f;
        
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 2f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Input Settings")]
        [SerializeField] private KeyCode switchViewKey = KeyCode.T;
        [SerializeField] private string mouseXAxis = "Mouse X";
        [SerializeField] private string mouseYAxis = "Mouse Y";
        [SerializeField] private string scrollAxis = "Mouse ScrollWheel";
        
        [Header("Current State")]
        [SerializeField] private CameraMode currentMode = CameraMode.RTS;
        [SerializeField] private DroneUnit followedDrone;
        [SerializeField] private bool isTransitioning = false;
        [SerializeField] private float currentAltitude = 1000f;
        
        // Private fields
        private Vector3 rtsTargetPosition;
        private Vector3 rtsCurrentVelocity;
        private float rtsCurrentZoom;
        private Quaternion rtsTargetRotation;
        
        private Transform droneTarget;
        private Vector3 droneDesiredPosition;
        private Quaternion droneDesiredRotation;
        
        // Cesium camera reference (if available)
        private Component cesiumCamera;
        
        public enum CameraMode
        {
            RTS,
            Drone,
            Transitioning
        }
        
        // Events
        public static event System.Action<CameraMode> OnCameraModeChanged;
        public static event System.Action<float> OnAltitudeChanged;
        
        // Properties
        public CameraMode CurrentMode => currentMode;
        public float CurrentAltitude => currentAltitude;
        public DroneUnit FollowedDrone => followedDrone;
        public bool IsTransitioning => isTransitioning;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Get main camera if not assigned
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            if (mainCamera == null)
                mainCamera = FindFirstObjectByType<Camera>();
            
            // Try to find Cesium dynamic camera
            cesiumCamera = FindFirstObjectByType<Component>();
            
            // Initialize camera rig
            if (cameraRig == null)
            {
                cameraRig = mainCamera.transform.parent;
                if (cameraRig == null)
                {
                    // Create camera rig
                    GameObject rigObject = new GameObject("CameraRig");
                    cameraRig = rigObject.transform;
                    mainCamera.transform.SetParent(cameraRig);
                }
            }
        }
        
        private void Start()
        {
            InitializeCamera();
        }
        
        private void Update()
        {
            HandleInput();
            UpdateCamera();
            UpdateAltitude();
        }
        
        #endregion
        
        #region Initialization
        
        public void Initialize()
        {
            InitializeCamera();
            UnityEngine.Debug.Log("Camera Controller initialized");
        }
        
        private void InitializeCamera()
        {
            // Set initial RTS position
            rtsTargetPosition = new Vector3(0, 1000, 0);
            rtsCurrentZoom = 20f;
            rtsTargetRotation = Quaternion.Euler(45, 0, 0);
            
            // Apply initial settings
            SwitchToRTSView();
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            if (isTransitioning)
                return;
                
            // Handle view switching
            if (Input.GetKeyDown(switchViewKey))
            {
                ToggleCameraMode();
            }
            
            // Handle mode-specific input
            switch (currentMode)
            {
                case CameraMode.RTS:
                    HandleRTSInput();
                    break;
                case CameraMode.Drone:
                    HandleDroneInput();
                    break;
            }
        }
        
        private void HandleRTSInput()
        {
            // Mouse drag for rotation/movement
            if (Input.GetMouseButton(1)) // Right mouse button
            {
                float mouseX = Input.GetAxis(mouseXAxis);
                float mouseY = Input.GetAxis(mouseYAxis);
                
                // Rotate around target
                rtsTargetRotation *= Quaternion.AngleAxis(mouseX * rtsRotationSpeed, Vector3.up);
                rtsTargetRotation *= Quaternion.AngleAxis(-mouseY * rtsRotationSpeed, Vector3.right);
            }
            
            // Middle mouse drag for panning
            if (Input.GetMouseButton(2)) // Middle mouse button
            {
                float mouseX = Input.GetAxis(mouseXAxis);
                float mouseY = Input.GetAxis(mouseYAxis);
                
                Vector3 movement = new Vector3(-mouseX, 0, -mouseY) * rtsMovementSpeed;
                movement = cameraRig.TransformDirection(movement);
                movement.y = 0; // Keep movement horizontal
                
                rtsTargetPosition += movement;
            }
            
            // Scroll wheel for zoom
            float scroll = Input.GetAxis(scrollAxis);
            if (Mathf.Abs(scroll) > 0.01f)
            {
                rtsCurrentZoom = Mathf.Clamp(rtsCurrentZoom - scroll * rtsZoomSpeed, rtsMinZoom, rtsMaxZoom);
            }
            
            // WASD movement
            Vector3 movement2 = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) movement2 += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) movement2 += Vector3.back;
            if (Input.GetKey(KeyCode.A)) movement2 += Vector3.left;
            if (Input.GetKey(KeyCode.D)) movement2 += Vector3.right;
            
            if (movement2 != Vector3.zero)
            {
                movement2 = cameraRig.TransformDirection(movement2) * rtsMovementSpeed * Time.deltaTime;
                movement2.y = 0;
                rtsTargetPosition += movement2;
            }
        }
        
        private void HandleDroneInput()
        {
            if (followedDrone == null)
                return;
                
            // Mouse look for camera rotation around drone
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis(mouseXAxis);
                float mouseY = Input.GetAxis(mouseYAxis);
                
                // Rotate around drone
                droneDesiredRotation *= Quaternion.AngleAxis(mouseX * droneRotationSpeed, Vector3.up);
                droneDesiredRotation *= Quaternion.AngleAxis(-mouseY * droneRotationSpeed, Vector3.right);
            }
            
            // Scroll for distance from drone
            float scroll = Input.GetAxis(scrollAxis);
            if (Mathf.Abs(scroll) > 0.01f)
            {
                droneFollowOffset = Vector3.ClampMagnitude(droneFollowOffset * (1f - scroll * 0.1f), 50f);
            }
        }
        
        #endregion
        
        #region Camera Mode Switching
        
        public void ToggleCameraMode()
        {
            switch (currentMode)
            {
                case CameraMode.RTS:
                    // Try to switch to drone view - find an active drone
                    DroneUnit activeDrone = FindActiveDrone();
                    if (activeDrone != null)
                    {
                        SwitchToDroneView(activeDrone);
                    }
                    break;
                    
                case CameraMode.Drone:
                    SwitchToRTSView();
                    break;
            }
        }
        
        public void SwitchToRTSView()
        {
            if (currentMode == CameraMode.RTS && !isTransitioning)
                return;
                
            StartCoroutine(TransitionToRTS());
        }
        
        public void SwitchToDroneView(DroneUnit drone = null)
        {
            if (drone == null)
                drone = FindActiveDrone();
                
            if (drone == null)
            {
                UnityEngine.Debug.LogWarning("No drone available for drone view");
                return;
            }
            
            followedDrone = drone;
            StartCoroutine(TransitionToDrone());
        }
        
        private DroneUnit FindActiveDrone()
        {
            // This would typically interface with the DroneFleetManager
            // For now, return null - this will be implemented when DroneFleetManager is available
            var fleetManager = FindFirstObjectByType<GlobalFirefight.Drones.DroneFleetManager>();
            if (fleetManager != null)
            {
                var availableDrones = fleetManager.GetAllActiveDrones();
                if (availableDrones.Count > 0)
                    return availableDrones[0];
            }
            
            return null;
        }
        
        #endregion
        
        #region Camera Transitions
        
        private IEnumerator TransitionToRTS()
        {
            isTransitioning = true;
            currentMode = CameraMode.Transitioning;
            
            Vector3 startPosition = cameraRig.position;
            Quaternion startRotation = cameraRig.rotation;
            
            // Calculate target RTS position
            Vector3 targetPosition = rtsTargetPosition;
            Quaternion targetRotation = rtsTargetRotation;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < transitionDuration)
            {
                float t = elapsedTime / transitionDuration;
                float easedT = transitionCurve.Evaluate(t);
                
                // Interpolate position and rotation
                cameraRig.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                cameraRig.rotation = Quaternion.Lerp(startRotation, targetRotation, easedT);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final position
            cameraRig.position = targetPosition;
            cameraRig.rotation = targetRotation;
            
            currentMode = CameraMode.RTS;
            isTransitioning = false;
            
            OnCameraModeChanged?.Invoke(currentMode);
            UnityEngine.Debug.Log("Switched to RTS view");
        }
        
        private IEnumerator TransitionToDrone()
        {
            if (followedDrone == null)
                yield break;
                
            isTransitioning = true;
            currentMode = CameraMode.Transitioning;
            
            Vector3 startPosition = cameraRig.position;
            Quaternion startRotation = cameraRig.rotation;
            
            // Calculate target drone position
            Vector3 dronePos = followedDrone.position;
            Vector3 targetPosition = dronePos + droneFollowOffset;
            Quaternion targetRotation = Quaternion.LookRotation(dronePos - targetPosition);
            
            float elapsedTime = 0f;
            
            while (elapsedTime < transitionDuration)
            {
                if (followedDrone == null)
                {
                    // Drone was destroyed during transition
                    SwitchToRTSView();
                    yield break;
                }
                
                float t = elapsedTime / transitionDuration;
                float easedT = transitionCurve.Evaluate(t);
                
                // Update target position based on moving drone
                dronePos = followedDrone.position;
                targetPosition = dronePos + droneFollowOffset;
                targetRotation = Quaternion.LookRotation(dronePos - targetPosition);
                
                // Interpolate position and rotation
                cameraRig.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                cameraRig.rotation = Quaternion.Lerp(startRotation, targetRotation, easedT);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            currentMode = CameraMode.Drone;
            isTransitioning = false;
            
            OnCameraModeChanged?.Invoke(currentMode);
            UnityEngine.Debug.Log($"Switched to drone view following {followedDrone.callSign}");
        }
        
        #endregion
        
        #region Camera Updates
        
        private void UpdateCamera()
        {
            switch (currentMode)
            {
                case CameraMode.RTS:
                    UpdateRTSCamera();
                    break;
                case CameraMode.Drone:
                    UpdateDroneCamera();
                    break;
            }
        }
        
        private void UpdateRTSCamera()
        {
            // Smooth movement to target position
            cameraRig.position = Vector3.SmoothDamp(cameraRig.position, rtsTargetPosition, 
                ref rtsCurrentVelocity, 0.3f);
            
            // Smooth rotation to target rotation
            cameraRig.rotation = Quaternion.Slerp(cameraRig.rotation, rtsTargetRotation, 
                Time.deltaTime * rtsRotationSpeed);
            
            // Update camera local position for zoom
            mainCamera.transform.localPosition = Vector3.back * rtsCurrentZoom;
        }
        
        private void UpdateDroneCamera()
        {
            if (followedDrone == null)
            {
                SwitchToRTSView();
                return;
            }
            
            // Calculate desired position relative to drone
            Vector3 dronePos = followedDrone.position;
            droneDesiredPosition = dronePos + droneFollowOffset;
            droneDesiredRotation = Quaternion.LookRotation(dronePos - droneDesiredPosition);
            
            // Smooth follow
            cameraRig.position = Vector3.Lerp(cameraRig.position, droneDesiredPosition, 
                Time.deltaTime * droneFollowSpeed);
            cameraRig.rotation = Quaternion.Slerp(cameraRig.rotation, droneDesiredRotation, 
                Time.deltaTime * droneRotationSpeed);
            
            // Reset camera local position
            mainCamera.transform.localPosition = Vector3.zero;
        }
        
        private void UpdateAltitude()
        {
            // Calculate current altitude above sea level
            float newAltitude = cameraRig.position.y;
            
            if (Mathf.Abs(newAltitude - currentAltitude) > 10f) // Only update if significant change
            {
                currentAltitude = newAltitude;
                OnAltitudeChanged?.Invoke(currentAltitude);
            }
        }
        
        #endregion
        
        #region Public API
        
        public void FocusOnPosition(Vector3 worldPosition, float zoom = -1f)
        {
            if (currentMode != CameraMode.RTS)
                return;
                
            rtsTargetPosition = worldPosition + Vector3.up * 1000f; // Hover above target
            
            if (zoom > 0)
            {
                rtsCurrentZoom = Mathf.Clamp(zoom, rtsMinZoom, rtsMaxZoom);
            }
        }
        
        public void FocusOnFireLocation(Vector3 fireWorldPosition)
        {
            if (currentMode == CameraMode.RTS)
            {
                // In RTS mode, move camera above the fire
                FocusOnPosition(fireWorldPosition, 20f); // Closer zoom for fire focus
            }
            else if (currentMode == CameraMode.Drone)
            {
                // In drone mode, move drone to hover above fire
                Vector3 dronePosition = fireWorldPosition + Vector3.up * 100f; // 100 units above fire
                
                if (followedDrone != null)
                {
                    // Update the DroneUnit data position
                    followedDrone.position = dronePosition;
                    followedDrone.targetPosition = dronePosition;
                    
                    // If there's a DroneController, update it too
                    DroneController controller = FindFirstObjectByType<DroneController>();
                    if (controller != null && controller.DroneUnit == followedDrone)
                    {
                        controller.transform.position = dronePosition;
                    }
                    
                    UnityEngine.Debug.Log($"📍 Moved drone to fire location: {dronePosition}");
                }
            }
        }
        
        public void SwitchToDroneViewAtFireLocation(Vector3 fireWorldPosition, DroneUnit drone = null)
        {
            // Position the drone above the fire location first
            if (drone == null)
                drone = FindActiveDrone();
                
            if (drone != null)
            {
                Vector3 dronePosition = fireWorldPosition + Vector3.up * 100f; // 100 units above fire
                
                // Update the DroneUnit data position
                drone.position = dronePosition;
                drone.targetPosition = dronePosition;
                
                // Find and update the corresponding DroneController GameObject
                DroneController[] controllers = FindObjectsByType<DroneController>(FindObjectsSortMode.None);
                foreach (var controller in controllers)
                {
                    if (controller.DroneUnit == drone)
                    {
                        controller.transform.position = dronePosition;
                        break;
                    }
                }
                
                UnityEngine.Debug.Log($"🚁 Positioning drone at fire location: {dronePosition}");
                
                // Now switch to drone view
                SwitchToDroneView(drone);
            }
            else
            {
                UnityEngine.Debug.LogWarning("⚠️ No drone available for fire location deployment!");
            }
        }
        
        public void FocusOnFire(FireIncident fire)
        {
            Vector3 firePosition = new Vector3((float)fire.longitude, 0, (float)fire.latitude);
            // Note: This needs proper coordinate conversion via GeospatialManager
            FocusOnPosition(firePosition);
        }
        
        public void SetFollowedDrone(DroneUnit drone)
        {
            followedDrone = drone;
            if (currentMode == CameraMode.Drone && drone != null)
            {
                // Update camera to follow new drone
                droneTarget = null; // This would be the drone's transform in the scene
            }
        }
        
        public void SetRTSMovementSpeed(float speed)
        {
            rtsMovementSpeed = speed;
        }
        
        public void SetDroneFollowDistance(float distance)
        {
            droneFollowOffset = droneFollowOffset.normalized * distance;
        }
        
        public Ray GetCameraRay()
        {
            return mainCamera.ScreenPointToRay(Input.mousePosition);
        }
        
        public Vector3 GetScreenToWorldPoint(Vector3 screenPosition)
        {
            return mainCamera.ScreenToWorldPoint(screenPosition);
        }
        
        public bool IsPositionVisible(Vector3 worldPosition)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
            return screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= Screen.width && 
                   screenPoint.y >= 0 && screenPoint.y <= Screen.height;
        }
        
        #endregion
        
        #region Settings
        
        public void ApplySettings(GameSettings settings)
        {
            // Apply any camera-related settings
            transitionDuration = settings.lodSwitchDistance * 0.001f; // Example adjustment
        }
        
        #endregion
    }
}

