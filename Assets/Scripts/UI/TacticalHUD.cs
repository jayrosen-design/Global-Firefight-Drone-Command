using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalFirefight.Core;
using GlobalFirefight.Data;
using GlobalFirefight.Drones;
using GlobalFirefight.Fire;
using GlobalFirefight.Systems;

namespace GlobalFirefight.UI
{
    public class TacticalHUD : MonoBehaviour
    {
        [Header("Main HUD Elements")]
        [SerializeField] private Canvas tacticalCanvas;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject crosshairUI;
        [SerializeField] private GameObject speedometerPanel;
        [SerializeField] private GameObject altimeterPanel;
        [SerializeField] private GameObject compassPanel;
        
        [Header("Drone Status")]
        [SerializeField] private GameObject droneInfoPanel;
        [SerializeField] private TextMeshProUGUI droneNameText;
        [SerializeField] private TextMeshProUGUI droneTypeText;
        [SerializeField] private Image droneAvatarImage;
        [SerializeField] private Slider batterySlider;
        [SerializeField] private Slider payloadSlider;
        [SerializeField] private TextMeshProUGUI batteryPercentText;
        [SerializeField] private TextMeshProUGUI payloadAmountText;
        
        [Header("Flight Instruments")]
        [SerializeField] private RectTransform speedNeedle;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private RectTransform altitudeNeedle;
        [SerializeField] private TextMeshProUGUI altitudeText;
        [SerializeField] private RectTransform compassNeedle;
        [SerializeField] private TextMeshProUGUI headingText;
        [SerializeField] private float maxSpeedDisplayed = 100f;
        [SerializeField] private float maxAltitudeDisplayed = 1000f;
        
        [Header("Target Information")]
        [SerializeField] private GameObject targetPanel;
        [SerializeField] private TextMeshProUGUI targetNameText;
        [SerializeField] private TextMeshProUGUI distanceToTargetText;
        [SerializeField] private TextMeshProUGUI etaText;
        [SerializeField] private Image targetDirectionArrow;
        [SerializeField] private GameObject targetMarker3D;
        
        [Header("Mission Status")]
        [SerializeField] private GameObject missionPanel;
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI missionObjectiveText;
        [SerializeField] private Slider missionProgressSlider;
        [SerializeField] private TextMeshProUGUI missionTimeText;
        [SerializeField] private Button abortMissionButton;
        
        [Header("Suppression Controls")]
        [SerializeField] private GameObject suppressionPanel;
        [SerializeField] private Button suppressionToggleButton;
        [SerializeField] private Slider suppressionIntensitySlider;
        [SerializeField] private TextMeshProUGUI suppressionModeText;
        [SerializeField] private Image suppressionReticle;
        [SerializeField] private GameObject suppressionEffectIndicator;
        
        [Header("Camera Controls")]
        [SerializeField] private GameObject cameraControlPanel;
        [SerializeField] private Button switchToRTSButton;
        [SerializeField] private Button thermalVisionButton;
        [SerializeField] private Button nightVisionButton;
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        [SerializeField] private Slider cameraZoomSlider;
        
        [Header("Communication")]
        [SerializeField] private GameObject commPanel;
        [SerializeField] private TextMeshProUGUI lastMessageText;
        [SerializeField] private Button requestSupportButton;
        [SerializeField] private Button reportStatusButton;
        [SerializeField] private Dropdown communicationTargetDropdown;
        
        [Header("Warnings and Alerts")]
        [SerializeField] private GameObject warningPanel;
        [SerializeField] private Image warningIcon;
        [SerializeField] private TextMeshProUGUI warningText;
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        
        [Header("Mini Radar")]
        [SerializeField] private GameObject radarPanel;
        [SerializeField] private RectTransform radarScope;
        [SerializeField] private GameObject radarBlipPrefab;
        [SerializeField] private float radarRange = 1000f;
        [SerializeField] private List<GameObject> radarBlips = new List<GameObject>();
        
        [Header("Performance")]
        [SerializeField] private bool enableDynamicUpdates = true;
        [SerializeField] private float hudUpdateInterval = 0.1f;
        [SerializeField] private float radarUpdateInterval = 1f;
        [SerializeField] private bool enableAdvancedInstruments = true;
        
        // Private fields
        private DroneController controlledDrone;
        private CameraController cameraController;
        private FireSimulation fireSimulation;
        private bool isInitialized = false;
        private bool isSuppressionActive = false;
        private float lastHUDUpdate = 0f;
        private float lastRadarUpdate = 0f;
        private Camera droneCamera;
        private Vector3 lastTargetPosition;
        private string currentMissionId;
        private float missionStartTime;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public DroneController ControlledDrone => controlledDrone;
        public bool IsSuppressionActive => isSuppressionActive;
        
        #region Initialization
        
        public void Initialize()
        {
            UnityEngine.Debug.Log("Initializing Tactical HUD...");
            
            // Get required components
            cameraController = FindFirstObjectByType<CameraController>();
            fireSimulation = FindFirstObjectByType<FireSimulation>();
            droneCamera = Camera.main;
            
            // Setup UI panels
            SetupMainPanels();
            SetupEventListeners();
            SetupInstruments();
            
            // Initialize UI state
            if (hudPanel != null) hudPanel.SetActive(true);
            UpdateSuppressionControls();
            PopulateCommunicationTargets();
            
            isInitialized = true;
            UnityEngine.Debug.Log("Tactical HUD initialized successfully");
        }
        
        private void SetupMainPanels()
        {
            // Ensure proper canvas setup
            if (tacticalCanvas == null)
            {
                tacticalCanvas = GetComponent<Canvas>();
                if (tacticalCanvas == null)
                {
                    tacticalCanvas = gameObject.AddComponent<Canvas>();
                    tacticalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }
            
            // Set initial panel states
            if (targetPanel != null) targetPanel.SetActive(false);
            if (missionPanel != null) missionPanel.SetActive(false);
            if (suppressionPanel != null) suppressionPanel.SetActive(false);
            if (warningPanel != null) warningPanel.SetActive(false);
        }
        
        private void SetupEventListeners()
        {
            // Mission controls
            if (abortMissionButton != null)
                abortMissionButton.onClick.AddListener(AbortCurrentMission);
            
            // Suppression controls
            if (suppressionToggleButton != null)
                suppressionToggleButton.onClick.AddListener(ToggleSuppression);
            if (suppressionIntensitySlider != null)
                suppressionIntensitySlider.onValueChanged.AddListener(OnSuppressionIntensityChanged);
            
            // Camera controls
            if (switchToRTSButton != null)
                switchToRTSButton.onClick.AddListener(SwitchToRTS);
            if (thermalVisionButton != null)
                thermalVisionButton.onClick.AddListener(ToggleThermalVision);
            if (nightVisionButton != null)
                nightVisionButton.onClick.AddListener(ToggleNightVision);
            if (zoomInButton != null)
                zoomInButton.onClick.AddListener(() => AdjustZoom(0.1f));
            if (zoomOutButton != null)
                zoomOutButton.onClick.AddListener(() => AdjustZoom(-0.1f));
            if (cameraZoomSlider != null)
                cameraZoomSlider.onValueChanged.AddListener(SetZoomLevel);
            
            // Communication
            if (requestSupportButton != null)
                requestSupportButton.onClick.AddListener(RequestSupport);
            if (reportStatusButton != null)
                reportStatusButton.onClick.AddListener(ReportStatus);
        }
        
        private void SetupInstruments()
        {
            // Initialize instrument displays
            if (batterySlider != null)
            {
                batterySlider.minValue = 0f;
                batterySlider.maxValue = 1f;
            }
            
            if (payloadSlider != null)
            {
                payloadSlider.minValue = 0f;
                payloadSlider.maxValue = 1f;
            }
            
            if (suppressionIntensitySlider != null)
            {
                suppressionIntensitySlider.minValue = 0f;
                suppressionIntensitySlider.maxValue = 1f;
                suppressionIntensitySlider.value = 0.5f;
            }
            
            if (cameraZoomSlider != null)
            {
                cameraZoomSlider.minValue = 1f;
                cameraZoomSlider.maxValue = 10f;
                cameraZoomSlider.value = 1f;
            }
        }
        
        private void PopulateCommunicationTargets()
        {
            if (communicationTargetDropdown == null)
                return;
            
            communicationTargetDropdown.ClearOptions();
            var options = new List<string>
            {
                "Command Center",
                "Nearby Drones",
                "Ground Control",
                "Emergency Services"
            };
            
            communicationTargetDropdown.AddOptions(options);
        }
        
        #endregion
        
        #region Drone Control
        
        public void SetControlledDrone(DroneController drone)
        {
            controlledDrone = drone;
            
            if (drone != null)
            {
                UpdateDroneInfo();
                ShowHUD();
                
                // Start mission if drone has an assignment
                if (!string.IsNullOrEmpty(drone.AssignedFireId))
                {
                    StartMission(drone.AssignedFireId);
                }
            }
            else
            {
                HideHUD();
            }
        }
        
        private void UpdateDroneInfo()
        {
            if (controlledDrone == null || droneInfoPanel == null)
                return;
            
            var droneUnit = controlledDrone.DroneUnit;
            
            if (droneNameText != null)
                droneNameText.text = droneUnit.callSign;
            
            if (droneTypeText != null)
                droneTypeText.text = droneUnit.specifications.modelName;
            
            // Set drone avatar based on country/type
            if (droneAvatarImage != null)
            {
                droneAvatarImage.color = droneUnit.specifications.primaryColor;
            }
            
            // Update suppression panel visibility
            if (suppressionPanel != null)
            {
                suppressionPanel.SetActive(droneUnit.specifications.hasHighPressureNozzle);
            }
        }
        
        public void ShowHUD()
        {
            if (hudPanel != null)
                hudPanel.SetActive(true);
        }
        
        public void HideHUD()
        {
            if (hudPanel != null)
                hudPanel.SetActive(false);
        }
        
        #endregion
        
        #region Flight Instruments
        
        private void UpdateFlightInstruments()
        {
            if (controlledDrone == null)
                return;
            
            var droneUnit = controlledDrone.DroneUnit;
            var droneTransform = controlledDrone.transform;
            
            // Update speed indicator
            UpdateSpeedometer(droneUnit);
            
            // Update altimeter
            UpdateAltimeter(droneTransform.position.y);
            
            // Update compass
            UpdateCompass(droneTransform.rotation.eulerAngles.y);
            
            // Update battery and payload
            UpdateResourceIndicators(droneUnit);
        }
        
        private void UpdateSpeedometer(DroneUnit droneUnit)
        {
            float currentSpeed = droneUnit.specifications.maxSpeed; // Use max speed as placeholder since GetCurrentSpeed doesn't exist
            float speedPercent = currentSpeed / maxSpeedDisplayed;
            
            if (speedNeedle != null)
            {
                float angle = Mathf.Lerp(-90f, 270f, speedPercent);
                speedNeedle.rotation = Quaternion.Euler(0, 0, -angle);
            }
            
            if (speedText != null)
            {
                speedText.text = $"{currentSpeed:F1} m/s";
            }
        }
        
        private void UpdateAltimeter(float altitude)
        {
            float altitudePercent = altitude / maxAltitudeDisplayed;
            
            if (altitudeNeedle != null)
            {
                float angle = Mathf.Lerp(-90f, 270f, altitudePercent);
                altitudeNeedle.rotation = Quaternion.Euler(0, 0, -angle);
            }
            
            if (altitudeText != null)
            {
                altitudeText.text = $"{altitude:F0} m";
            }
        }
        
        private void UpdateCompass(float heading)
        {
            if (compassNeedle != null)
            {
                compassNeedle.rotation = Quaternion.Euler(0, 0, -heading);
            }
            
            if (headingText != null)
            {
                headingText.text = $"{heading:F0}°";
            }
        }
        
        private void UpdateResourceIndicators(DroneUnit droneUnit)
        {
            // Battery indicator
            float batteryPercent = droneUnit.batteryLife / droneUnit.specifications.flightEndurance;
            if (batterySlider != null)
            {
                batterySlider.value = batteryPercent;
                
                // Change color based on battery level
                var sliderFill = batterySlider.fillRect.GetComponent<Image>();
                if (sliderFill != null)
                {
                    if (batteryPercent > 0.5f)
                        sliderFill.color = normalColor;
                    else if (batteryPercent > 0.2f)
                        sliderFill.color = warningColor;
                    else
                        sliderFill.color = criticalColor;
                }
            }
            
            if (batteryPercentText != null)
            {
                batteryPercentText.text = $"{batteryPercent:P0}";
            }
            
            // Payload indicator
            if (droneUnit.specifications.hasHighPressureNozzle)
            {
                float payloadPercent = droneUnit.currentSuppressantUnits / droneUnit.specifications.payloadCapacity;
                if (payloadSlider != null)
                {
                    payloadSlider.value = payloadPercent;
                }
                
                if (payloadAmountText != null)
                {
                    payloadAmountText.text = $"{droneUnit.currentSuppressantUnits:F0}L";
                }
            }
        }
        
        #endregion
        
        #region Target Management
        
        public void SetTarget(Vector3 targetPosition, string targetName = "Target")
        {
            lastTargetPosition = targetPosition;
            
            if (targetPanel != null)
                targetPanel.SetActive(true);
            
            if (targetNameText != null)
                targetNameText.text = targetName;
            
            UpdateTargetInformation();
            
            // Create 3D target marker
            Update3DTargetMarker(targetPosition);
        }
        
        public void ClearTarget()
        {
            if (targetPanel != null)
                targetPanel.SetActive(false);
            
            if (targetMarker3D != null)
                targetMarker3D.SetActive(false);
        }
        
        private void UpdateTargetInformation()
        {
            if (controlledDrone == null || targetPanel == null || !targetPanel.activeSelf)
                return;
            
            Vector3 dronePosition = controlledDrone.transform.position;
            float distance = Vector3.Distance(dronePosition, lastTargetPosition);
            
            if (distanceToTargetText != null)
            {
                distanceToTargetText.text = $"Distance: {distance:F0}m";
            }
            
            // Calculate ETA
            float speed = controlledDrone.DroneUnit.specifications.maxSpeed; // Use max speed as placeholder
            if (speed > 0)
            {
                float eta = distance / speed;
                if (etaText != null)
                {
                    etaText.text = $"ETA: {eta:F0}s";
                }
            }
            
            // Update direction arrow
            UpdateTargetDirectionArrow(dronePosition, lastTargetPosition);
        }
        
        private void UpdateTargetDirectionArrow(Vector3 fromPos, Vector3 toPos)
        {
            if (targetDirectionArrow == null)
                return;
            
            Vector3 direction = (toPos - fromPos).normalized;
            float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
            
            targetDirectionArrow.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
        
        private void Update3DTargetMarker(Vector3 position)
        {
            if (targetMarker3D == null)
            {
                // Create target marker if it doesn't exist
                targetMarker3D = new GameObject("TargetMarker3D");
                // Add visual components (mesh, material, etc.)
            }
            
            targetMarker3D.transform.position = position;
            targetMarker3D.SetActive(true);
        }
        
        #endregion
        
        #region Mission Management
        
        private void StartMission(string fireId)
        {
            currentMissionId = fireId;
            missionStartTime = Time.time;
            
            if (missionPanel != null)
                missionPanel.SetActive(true);
            
            if (missionTitleText != null)
                missionTitleText.text = $"Mission: Fire {fireId}";
            
            if (missionObjectiveText != null)
                missionObjectiveText.text = "Suppress fire and minimize damage";
            
            // Set target to fire location
            var fireSimulation = FindFirstObjectByType<FireSimulation>();
            if (fireSimulation != null)
            {
                // Get fire location from simulation
                SetTarget(Vector3.zero, $"Fire {fireId}"); // Placeholder
            }
        }
        
        private void UpdateMissionStatus()
        {
            if (string.IsNullOrEmpty(currentMissionId) || missionPanel == null || !missionPanel.activeSelf)
                return;
            
            float missionTime = Time.time - missionStartTime;
            
            if (missionTimeText != null)
            {
                int minutes = Mathf.FloorToInt(missionTime / 60f);
                int seconds = Mathf.FloorToInt(missionTime % 60f);
                missionTimeText.text = $"Time: {minutes:D2}:{seconds:D2}";
            }
            
            // Update mission progress (simplified)
            if (missionProgressSlider != null)
            {
                float progress = Mathf.Clamp01(missionTime / 300f); // 5 minute missions
                missionProgressSlider.value = progress;
            }
        }
        
        private void AbortCurrentMission()
        {
            currentMissionId = null;
            
            if (missionPanel != null)
                missionPanel.SetActive(false);
            
            // Return drone to base
            if (controlledDrone != null)
            {
                controlledDrone.DroneUnit.ForceReturn("Mission aborted by pilot");
            }
            
            ShowWarning("Mission Aborted", 3f);
        }
        
        #endregion
        
        #region Suppression Controls
        
        private void UpdateSuppressionControls()
        {
            if (controlledDrone == null || suppressionPanel == null)
                return;
            
            bool hasSuppressionCapability = controlledDrone.DroneUnit.specifications.hasHighPressureNozzle;
            suppressionPanel.SetActive(hasSuppressionCapability);
            
            if (!hasSuppressionCapability)
                return;
            
            // Update suppression button state
            if (suppressionToggleButton != null)
            {
                var buttonText = suppressionToggleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isSuppressionActive ? "Stop Suppression" : "Start Suppression";
                }
                
                suppressionToggleButton.GetComponent<Image>().color = 
                    isSuppressionActive ? criticalColor : normalColor;
            }
            
            // Update suppression mode text
            if (suppressionModeText != null)
            {
                string mode = isSuppressionActive ? "ACTIVE" : "STANDBY";
                suppressionModeText.text = $"Mode: {mode}";
                suppressionModeText.color = isSuppressionActive ? criticalColor : normalColor;
            }
            
            // Show/hide suppression reticle
            if (suppressionReticle != null)
            {
                suppressionReticle.gameObject.SetActive(isSuppressionActive);
            }
            
            // Update effect indicator
            if (suppressionEffectIndicator != null)
            {
                suppressionEffectIndicator.SetActive(isSuppressionActive);
            }
        }
        
        private void ToggleSuppression()
        {
            if (controlledDrone == null || !controlledDrone.DroneUnit.specifications.hasHighPressureNozzle)
                return;
            
            isSuppressionActive = !isSuppressionActive;
            
            if (isSuppressionActive)
            {
                // Start suppression
                Vector3 suppressionTarget = lastTargetPosition;
                if (suppressionTarget == Vector3.zero)
                {
                    suppressionTarget = controlledDrone.transform.position + Vector3.down * 50f;
                }
                
                controlledDrone.StartSuppression(suppressionTarget);
                ShowWarning("Suppression Active", 2f);
            }
            else
            {
                // Stop suppression would be handled by DroneController
                ShowWarning("Suppression Stopped", 2f);
            }
            
            UpdateSuppressionControls();
        }
        
        private void OnSuppressionIntensityChanged(float intensity)
        {
            // Adjust suppression parameters based on intensity
            UnityEngine.Debug.Log($"Suppression intensity set to: {intensity:P0}");
        }
        
        #endregion
        
        #region Camera Controls
        
        private void SwitchToRTS()
        {
            if (cameraController != null)
            {
                cameraController.SwitchToRTSView();
            }
            
            // Switch UI to RTS interface
            var rtsInterface = FindFirstObjectByType<RTSInterface>();
            if (rtsInterface != null)
            {
                rtsInterface.ShowRTSInterface();
            }
        }
        
        private void ToggleThermalVision()
        {
            // Toggle thermal vision mode
            UnityEngine.Debug.Log("Toggling thermal vision");
            
            if (thermalVisionButton != null)
            {
                var buttonImage = thermalVisionButton.GetComponent<Image>();
                buttonImage.color = buttonImage.color == normalColor ? warningColor : normalColor;
            }
        }
        
        private void ToggleNightVision()
        {
            // Toggle night vision mode
            UnityEngine.Debug.Log("Toggling night vision");
            
            if (nightVisionButton != null)
            {
                var buttonImage = nightVisionButton.GetComponent<Image>();
                buttonImage.color = buttonImage.color == normalColor ? Color.green : normalColor;
            }
        }
        
        private void AdjustZoom(float delta)
        {
            if (cameraZoomSlider != null)
            {
                float newZoom = Mathf.Clamp(cameraZoomSlider.value + delta, 
                    cameraZoomSlider.minValue, cameraZoomSlider.maxValue);
                cameraZoomSlider.value = newZoom;
                SetZoomLevel(newZoom);
            }
        }
        
        private void SetZoomLevel(float zoomLevel)
        {
            if (droneCamera != null)
            {
                droneCamera.fieldOfView = Mathf.Lerp(60f, 10f, (zoomLevel - 1f) / 9f);
            }
        }
        
        #endregion
        
        #region Radar System
        
        private void UpdateRadar()
        {
            if (radarScope == null || radarBlipPrefab == null)
                return;
            
            // Clear existing blips
            foreach (var blip in radarBlips)
            {
                if (blip != null)
                    Destroy(blip);
            }
            radarBlips.Clear();
            
            if (controlledDrone == null)
                return;
            
            Vector3 dronePosition = controlledDrone.transform.position;
            
            // Find nearby objects (fires, other drones, etc.)
            var nearbyObjects = FindNearbyObjects(dronePosition, radarRange);
            
            foreach (var obj in nearbyObjects)
            {
                CreateRadarBlip(obj, dronePosition);
            }
        }
        
        private List<RadarObject> FindNearbyObjects(Vector3 center, float range)
        {
            var objects = new List<RadarObject>();
            
            // Find fires
            var fireSimulation = FindFirstObjectByType<FireSimulation>();
            if (fireSimulation != null)
            {
                // Add fire locations to radar
                // This would require access to fire simulation data
            }
            
            // Find other drones
            var fleetManager = FindFirstObjectByType<DroneFleetManager>();
            if (fleetManager != null)
            {
                var allDrones = fleetManager.GetAllActiveDrones();
                foreach (var drone in allDrones)
                {
                    if (drone.unitId != controlledDrone.DroneUnit.unitId)
                    {
                        float distance = Vector3.Distance(center, drone.position);
                        if (distance <= range)
                        {
                            objects.Add(new RadarObject
                            {
                                position = drone.position,
                                type = RadarObjectType.Drone,
                                name = drone.callSign
                            });
                        }
                    }
                }
            }
            
            return objects;
        }
        
        private void CreateRadarBlip(RadarObject obj, Vector3 centerPos)
        {
            GameObject blip = Instantiate(radarBlipPrefab, radarScope);
            radarBlips.Add(blip);
            
            // Calculate relative position
            Vector3 relative = obj.position - centerPos;
            Vector2 radarPos = new Vector2(relative.x, relative.z) / radarRange * (radarScope.rect.width * 0.4f);
            
            blip.GetComponent<RectTransform>().anchoredPosition = radarPos;
            
            // Set blip color based on type
            var blipImage = blip.GetComponent<Image>();
            if (blipImage != null)
            {
                switch (obj.type)
                {
                    case RadarObjectType.Fire:
                        blipImage.color = Color.red;
                        break;
                    case RadarObjectType.Drone:
                        blipImage.color = Color.blue;
                        break;
                    case RadarObjectType.Unknown:
                        blipImage.color = Color.yellow;
                        break;
                }
            }
        }
        
        #endregion
        
        #region Communication
        
        private void RequestSupport()
        {
            string target = communicationTargetDropdown.options[communicationTargetDropdown.value].text;
            string message = $"Requesting support from {target}";
            
            if (lastMessageText != null)
                lastMessageText.text = $"Sent: {message}";
            
            UnityEngine.Debug.Log($"Communication: {message}");
        }
        
        private void ReportStatus()
        {
            if (controlledDrone == null)
                return;
            
            var droneUnit = controlledDrone.DroneUnit;
            float batteryPercent = droneUnit.batteryLife / droneUnit.specifications.flightEndurance;
            
            string status = $"Status: {droneUnit.state}, Battery: {batteryPercent:P0}";
            
            if (lastMessageText != null)
                lastMessageText.text = $"Reported: {status}";
            
            UnityEngine.Debug.Log($"Status Report: {status}");
        }
        
        #endregion
        
        #region Warnings and Alerts
        
        private void CheckWarningConditions()
        {
            if (controlledDrone == null)
                return;
            
            var droneUnit = controlledDrone.DroneUnit;
            
            // Check battery level
            float batteryPercent = droneUnit.batteryLife / droneUnit.specifications.flightEndurance;
            if (batteryPercent <= 0.2f)
            {
                ShowWarning("LOW BATTERY - Return to base immediately", 0f);
                return;
            }
            else if (batteryPercent <= 0.5f)
            {
                ShowWarning("Battery level low", 0f);
                return;
            }
            
            // Check payload for suppression drones
            if (droneUnit.specifications.hasHighPressureNozzle && droneUnit.currentSuppressantUnits <= 0)
            {
                ShowWarning("Payload depleted", 0f);
                return;
            }
            
            // Check emergency state
            if (droneUnit.state == DroneState.Destroyed)
            {
                ShowWarning("EMERGENCY - Drone destroyed", 0f);
                return;
            }
            
            // No warnings - hide warning panel
            if (warningPanel != null)
                warningPanel.SetActive(false);
        }
        
        public void ShowWarning(string message, float duration = 5f)
        {
            if (warningPanel == null)
                return;
            
            warningPanel.SetActive(true);
            
            if (warningText != null)
                warningText.text = message;
            
            if (warningIcon != null)
            {
                warningIcon.color = message.Contains("LOW") || message.Contains("EMERGENCY") ? 
                    criticalColor : warningColor;
            }
            
            if (duration > 0f)
            {
                StartCoroutine(HideWarningAfterDelay(duration));
            }
        }
        
        private System.Collections.IEnumerator HideWarningAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (warningPanel != null)
                warningPanel.SetActive(false);
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Update()
        {
            if (!isInitialized)
                return;
            
            // Update HUD at specified interval
            if (enableDynamicUpdates && Time.time - lastHUDUpdate >= hudUpdateInterval)
            {
                UpdateFlightInstruments();
                UpdateTargetInformation();
                UpdateMissionStatus();
                UpdateSuppressionControls();
                CheckWarningConditions();
                lastHUDUpdate = Time.time;
            }
            
            // Update radar less frequently
            if (Time.time - lastRadarUpdate >= radarUpdateInterval)
            {
                UpdateRadar();
                lastRadarUpdate = Time.time;
            }
            
            // Handle input
            HandleTacticalInput();
        }
        
        private void HandleTacticalInput()
        {
            // Space for suppression toggle
            if (Input.GetKeyDown(KeyCode.Space) && controlledDrone != null && 
                controlledDrone.DroneUnit.specifications.hasHighPressureNozzle)
            {
                ToggleSuppression();
            }
            
            // T for thermal vision
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleThermalVision();
            }
            
            // N for night vision
            if (Input.GetKeyDown(KeyCode.N))
            {
                ToggleNightVision();
            }
            
            // Return to RTS view
            if (Input.GetKeyDown(KeyCode.R))
            {
                SwitchToRTS();
            }
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    [System.Serializable]
    public class RadarObject
    {
        public Vector3 position;
        public RadarObjectType type;
        public string name;
    }
    
    public enum RadarObjectType
    {
        Fire,
        Drone,
        Unknown
    }
    
    #endregion
}

