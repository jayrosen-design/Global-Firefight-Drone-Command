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
    public class RTSInterface : MonoBehaviour
    {
        [Header("Main UI Panels")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject rtsPanel;
        [SerializeField] private GameObject tacticalPanel;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject settingsPanel;
        
        [Header("Global Map Controls")]
        [SerializeField] private GameObject globeControlPanel;
        [SerializeField] private Slider zoomSlider;
        [SerializeField] private Button resetViewButton;
        [SerializeField] private Button satelliteViewButton;
        [SerializeField] private Button terrainViewButton;
        [SerializeField] private Toggle showFiresToggle;
        [SerializeField] private Toggle showDronesToggle;
        [SerializeField] private Toggle showWeatherToggle;
        
        [Header("Fire Information")]
        [SerializeField] private GameObject fireInfoPanel;
        [SerializeField] private TextMeshProUGUI fireNameText;
        [SerializeField] private TextMeshProUGUI fireLocationText;
        [SerializeField] private TextMeshProUGUI fireIntensityText;
        [SerializeField] private TextMeshProUGUI fireAreaText;
        [SerializeField] private TextMeshProUGUI economicImpactText;
        [SerializeField] private Button deployDronesButton;
        [SerializeField] private Button closeFireInfoButton;
        
        [Header("Fleet Management")]
        [SerializeField] private GameObject fleetPanel;
        [SerializeField] private Transform fleetListParent;
        [SerializeField] private GameObject fleetItemPrefab;
        [SerializeField] private Button refreshFleetButton;
        [SerializeField] private Dropdown countryFilterDropdown;
        [SerializeField] private Toggle showIdleOnlyToggle;
        
        // Private fields
        private GameManager gameManager;
        private DroneFleetManager fleetManager;
        private ScoringSystem scoringSystem;
        private CameraController cameraController;
        private FireIncident selectedFire;
        private List<DroneUnit> selectedDrones = new List<DroneUnit>();
        private bool isInitialized = false;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public FireIncident SelectedFire => selectedFire;
        
        #region Initialization
        
        public void Initialize()
        {
            UnityEngine.Debug.Log("Initializing RTS Interface...");
            
            // Get required components
            gameManager = FindFirstObjectByType<GameManager>();
            fleetManager = FindFirstObjectByType<DroneFleetManager>();
            scoringSystem = FindFirstObjectByType<ScoringSystem>();
            cameraController = FindFirstObjectByType<CameraController>();
            
            // Setup UI panels
            SetupMainPanels();
            SetupEventListeners();
            
            // Initialize UI state
            ShowRTSInterface();
            
            isInitialized = true;
            UnityEngine.Debug.Log("RTS Interface initialized successfully");
        }
        
        private void SetupMainPanels()
        {
            // Ensure proper panel hierarchy
            if (mainCanvas == null)
            {
                mainCanvas = GetComponent<Canvas>();
                if (mainCanvas == null)
                {
                    mainCanvas = gameObject.AddComponent<Canvas>();
                    mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }
            
            // Set initial panel states
            if (rtsPanel != null) rtsPanel.SetActive(true);
            if (tacticalPanel != null) tacticalPanel.SetActive(false);
            if (menuPanel != null) menuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (fireInfoPanel != null) fireInfoPanel.SetActive(false);
        }
        
        private void SetupEventListeners()
        {
            // Globe controls
            if (resetViewButton != null)
                resetViewButton.onClick.AddListener(ResetGlobeView);
            if (satelliteViewButton != null)
                satelliteViewButton.onClick.AddListener(() => SetMapView(MapViewType.Satellite));
            if (terrainViewButton != null)
                terrainViewButton.onClick.AddListener(() => SetMapView(MapViewType.Terrain));
            
            // Fire info panel
            if (deployDronesButton != null)
                deployDronesButton.onClick.AddListener(StartDeploymentMode);
            if (closeFireInfoButton != null)
                closeFireInfoButton.onClick.AddListener(CloseFireInfo);
            
            // Fleet management
            if (refreshFleetButton != null)
                refreshFleetButton.onClick.AddListener(UpdateFleetDisplay);
        }
        
        #endregion
        
        #region UI State Management
        
        public void ShowRTSInterface()
        {
            if (rtsPanel != null) rtsPanel.SetActive(true);
            if (tacticalPanel != null) tacticalPanel.SetActive(false);
        }
        
        public void ShowTacticalInterface()
        {
            if (rtsPanel != null) rtsPanel.SetActive(false);
            if (tacticalPanel != null) tacticalPanel.SetActive(true);
        }
        
        #endregion
        
        #region Fire Information
        
        public void ShowFireInfo(FireIncident fire)
        {
            selectedFire = fire;
            
            if (fireInfoPanel != null)
                fireInfoPanel.SetActive(true);
            
            if (fireNameText != null)
                fireNameText.text = $"Fire: {fire.id}";
            
            if (fireLocationText != null)
                fireLocationText.text = $"Location: {fire.latitude:F4}, {fire.longitude:F4}";
            
            if (fireIntensityText != null)
                fireIntensityText.text = $"Intensity: {fire.intensity:F1}";
            
            if (fireAreaText != null)
                fireAreaText.text = $"Area: {(fire.spreadRadius * fire.spreadRadius * Mathf.PI / 1000000f):F1} km²";
            
            if (economicImpactText != null)
                economicImpactText.text = $"Economic Impact: ${fire.propertyValue:N0}";
        }
        
        public void CloseFireInfo()
        {
            selectedFire = null;
            
            if (fireInfoPanel != null)
                fireInfoPanel.SetActive(false);
        }
        
        #endregion
        
        #region Fleet Management
        
        public void UpdateFleetDisplay()
        {
            if (fleetManager == null || fleetListParent == null) return;
            
            // Basic fleet display update
            UnityEngine.Debug.Log("Updating fleet display");
        }
        
        #endregion
        
        #region Event Handlers
        
        private void StartDeploymentMode()
        {
            UnityEngine.Debug.Log("Starting deployment mode");
        }
        
        private void ResetGlobeView()
        {
            if (cameraController != null)
            {
                // Reset camera view
                UnityEngine.Debug.Log("Resetting globe view");
            }
        }
        
        private void SetMapView(MapViewType viewType)
        {
            UnityEngine.Debug.Log($"Setting map view to: {viewType}");
        }
        
        #endregion
        
        #region Supporting Types
        
        public enum MapViewType
        {
            Satellite,
            Terrain,
            Political
        }
        
        #endregion
    }
}

