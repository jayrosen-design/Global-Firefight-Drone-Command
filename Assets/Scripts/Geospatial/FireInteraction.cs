using UnityEngine;
using GlobalFirefight.Systems;
using GlobalFirefight.Drones;

namespace GlobalFirefight.Geospatial
{
    /// <summary>
    /// Handles interaction with individual fire objects for drone deployment
    /// </summary>
    public class FireInteraction : MonoBehaviour
    {
        [Header("Fire Information")]
        [SerializeField] private float latitude;
        [SerializeField] private float longitude;
        [SerializeField] private float brightness;
        [SerializeField] private float confidence;
        [SerializeField] private string fireId;
        
        [Header("Interaction Settings")]
        [SerializeField] private float clickRange = 10f;
        [SerializeField] private bool isSelected = false;
        [SerializeField] private GameObject selectionIndicator;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = Color.orange;
        [SerializeField] private Color selectedColor = Color.cyan;
        [SerializeField] private Color hoverColor = Color.yellow;
        
        // Components
        private Collider fireCollider;
        private Renderer fireRenderer;
        private ParticleSystem fireParticles;
        
        // References
        private CameraController cameraController;
        
        // Events
        public static event System.Action<FireInteraction> OnFireSelected;
        public static event System.Action<FireInteraction> OnFireClicked;
        
        private void Awake()
        {
            // Get components
            fireCollider = GetComponent<Collider>();
            fireRenderer = GetComponent<Renderer>();
            fireParticles = GetComponent<ParticleSystem>();
            
            // Ensure collider exists for interaction
            if (fireCollider == null)
            {
                fireCollider = gameObject.AddComponent<CapsuleCollider>();
                fireCollider.isTrigger = true;
            }
            
            // Find system references
            cameraController = FindFirstObjectByType<CameraController>();
        }
        
        private void Start()
        {
            SetupSelectionIndicator();
            UpdateVisualState();
        }
        
        private void OnMouseDown()
        {
            HandleFireClick();
        }
        
        private void OnMouseEnter()
        {
            if (!isSelected)
            {
                UpdateFireColor(hoverColor);
            }
        }
        
        private void OnMouseExit()
        {
            if (!isSelected)
            {
                UpdateFireColor(normalColor);
            }
        }
        
        public void SetFireData(CesiumFireLoader.FireData fireData)
        {
            latitude = fireData.latitude;
            longitude = fireData.longitude;
            brightness = fireData.brightness;
            confidence = fireData.confidence;
            fireId = $"Fire_{latitude:F4}_{longitude:F4}";
            
            UnityEngine.Debug.Log($"🔥 Fire interaction setup: {fireId} at {latitude:F4}, {longitude:F4}");
        }
        
        private void HandleFireClick()
        {
            UnityEngine.Debug.Log($"🔥 Fire clicked: {fireId} at coordinates ({latitude:F4}, {longitude:F4})");
            
            // Select this fire
            SelectFire();
            
            // Notify listeners
            OnFireClicked?.Invoke(this);
            
            // Transition to drone view at this location
            TransitionToDroneView();
        }
        
        public void SelectFire()
        {
            isSelected = true;
            UpdateVisualState();
            OnFireSelected?.Invoke(this);
            
            UnityEngine.Debug.Log($"🎯 Fire selected for drone deployment: {fireId}");
        }
        
        public void DeselectFire()
        {
            isSelected = false;
            UpdateVisualState();
        }
        
        private void TransitionToDroneView()
        {
            if (cameraController == null)
            {
                UnityEngine.Debug.LogError("❌ CameraController not found! Cannot transition to drone view.");
                return;
            }
            
            // Use the new enhanced camera controller method
            cameraController.SwitchToDroneViewAtFireLocation(transform.position);
        }
        
        private void SetupSelectionIndicator()
        {
            if (selectionIndicator == null)
            {
                // Create a simple ring indicator
                selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                selectionIndicator.transform.SetParent(transform);
                selectionIndicator.transform.localPosition = Vector3.down * 0.5f;
                selectionIndicator.transform.localScale = new Vector3(3f, 0.1f, 3f);
                
                // Remove collider and make it just visual
                Destroy(selectionIndicator.GetComponent<Collider>());
                
                // Make it glow
                Renderer indicatorRenderer = selectionIndicator.GetComponent<Renderer>();
                indicatorRenderer.material.color = selectedColor;
                indicatorRenderer.material.EnableKeyword("_EMISSION");
                indicatorRenderer.material.SetColor("_EmissionColor", selectedColor * 0.5f);
                
                selectionIndicator.SetActive(false);
            }
        }
        
        private void UpdateVisualState()
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(isSelected);
            }
            
            Color targetColor = isSelected ? selectedColor : normalColor;
            UpdateFireColor(targetColor);
        }
        
        private void UpdateFireColor(Color color)
        {
            // Update particle system color if available
            if (fireParticles != null)
            {
                var main = fireParticles.main;
                main.startColor = color;
            }
            
            // Update renderer color if available
            if (fireRenderer != null && fireRenderer.material != null)
            {
                fireRenderer.material.color = color;
            }
        }
        
        // Public getters
        public float Latitude => latitude;
        public float Longitude => longitude;
        public float Brightness => brightness;
        public float Confidence => confidence;
        public string FireId => fireId;
        public bool IsSelected => isSelected;
        public Vector3 WorldPosition => transform.position;
    }
}
