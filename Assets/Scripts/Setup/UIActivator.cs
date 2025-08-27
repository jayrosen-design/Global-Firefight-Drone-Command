using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalFirefight.UI;
using GlobalFirefight.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GlobalFirefight.Setup
{
    /// <summary>
    /// Enhanced UI Activator - Connects UI components AND properly activates all panels
    /// This script ensures all UI elements are both connected and visible when the game starts
    /// </summary>
    public class UIActivator : MonoBehaviour
    {
        [Header("UI Activation Status")]
        [SerializeField] private bool isActivated = false;
        [SerializeField] private string activationStatus = "Ready to activate UI components";
        
        [Header("Activation Options")]
        [SerializeField] private bool activateOnStart = true;
        [SerializeField] private bool forceActivateAllPanels = true;
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool fixCanvasSettings = true;
        
        [Header("Found UI Systems")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private TacticalHUD tacticalHUD;
        [SerializeField] private RTSInterface rtsInterface;
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject uiSystemParent;
        
        [Header("Activation Results")]
        [SerializeField] private int panelsActivated = 0;
        [SerializeField] private int componentsConnected = 0;
        [SerializeField] private int canvasesFixed = 0;
        
        private List<GameObject> activatedPanels = new List<GameObject>();
        
        private void Start()
        {
            if (activateOnStart && !isActivated)
            {
                ActivateAllUI();
            }
        }
        
        [ContextMenu("🚀 Activate All UI Systems")]
        public void ActivateAllUI()
        {
            activationStatus = "Discovering UI systems...";
            
            DiscoverUIComponents();
            FixCanvasHierarchy();
            ConnectUIComponents();
            ActivateUIHierarchy();
            InitializeUIManagers();
            
            isActivated = true;
            activationStatus = "UI activation complete!";
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("🚀 UI Activation Complete! All UI systems are now active and connected.");
                UnityEngine.Debug.Log($"📊 Results: {panelsActivated} panels activated, {componentsConnected} components connected, {canvasesFixed} canvases fixed");
            }
        }
        
        [ContextMenu("🔍 Discover UI Components")]
        public void DiscoverUIComponents()
        {
            // Find main UI managers
            uiManager = FindFirstObjectByType<UIManager>();
            tacticalHUD = FindFirstObjectByType<TacticalHUD>();
            rtsInterface = FindFirstObjectByType<RTSInterface>();
            
            // Find main canvas and UI system parent
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.name.ToLower().Contains("main") || 
                    canvas.gameObject.name.ToLower().Contains("ui") ||
                    canvas.transform.childCount > 0)
                {
                    mainCanvas = canvas;
                    break;
                }
            }
            
            if (mainCanvas == null && canvases.Length > 0)
                mainCanvas = canvases[0];
            
            // Find UI system parent
            var gameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in gameObjects)
            {
                if (go.name.ToLower().Contains("ui") && go.name.ToLower().Contains("system"))
                {
                    uiSystemParent = go;
                    break;
                }
            }
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log($"🔍 UI Discovery Results:");
                UnityEngine.Debug.Log($"  UIManager: {(uiManager != null ? "✅ Found" : "❌ Missing")}");
                UnityEngine.Debug.Log($"  TacticalHUD: {(tacticalHUD != null ? "✅ Found" : "❌ Missing")}");
                UnityEngine.Debug.Log($"  RTSInterface: {(rtsInterface != null ? "✅ Found" : "❌ Missing")}");
                UnityEngine.Debug.Log($"  Main Canvas: {(mainCanvas != null ? "✅ Found" : "❌ Missing")}");
                UnityEngine.Debug.Log($"  UI System Parent: {(uiSystemParent != null ? "✅ Found" : "❌ Missing")}");
            }
        }
        
        [ContextMenu("🛠️ Fix Canvas Hierarchy")]
        public void FixCanvasHierarchy()
        {
            if (!fixCanvasSettings) return;
            
            activationStatus = "Fixing canvas hierarchy...";
            
            if (mainCanvas != null)
            {
                // Ensure canvas is active
                mainCanvas.gameObject.SetActive(true);
                
                // Configure canvas properly
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 0;
                
                // Add/configure CanvasScaler
                var canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
                if (canvasScaler == null)
                {
                    canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
                }
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
                
                // Add/configure GraphicRaycaster
                if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
                {
                    mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
                }
                
                canvasesFixed++;
                
                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log($"🛠️ Fixed canvas: {mainCanvas.name}");
                }
            }
            
            // Ensure EventSystem exists
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                
                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log("🛠️ Created EventSystem");
                }
            }
        }
        
        [ContextMenu("🔗 Connect UI Components")]
        public void ConnectUIComponents()
        {
            activationStatus = "Connecting UI components...";
            
            if (mainCanvas != null)
            {
                // Connect UI managers to canvas
                if (uiManager != null)
                {
                    SetPrivateField(uiManager, "mainCanvas", mainCanvas);
                    componentsConnected++;
                }
                
                if (tacticalHUD != null)
                {
                    SetPrivateField(tacticalHUD, "tacticalCanvas", mainCanvas);
                    componentsConnected++;
                }
                
                if (rtsInterface != null)
                {
                    SetPrivateField(rtsInterface, "mainCanvas", mainCanvas);
                    componentsConnected++;
                }
            }
            
            // Connect managers to each other
            if (uiManager != null)
            {
                if (tacticalHUD != null)
                {
                    SetPrivateField(uiManager, "tacticalHUD", tacticalHUD);
                    componentsConnected++;
                }
                
                if (rtsInterface != null)
                {
                    SetPrivateField(uiManager, "rtsInterface", rtsInterface);
                    componentsConnected++;
                }
            }
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log($"🔗 Connected {componentsConnected} UI components");
            }
        }
        
        [ContextMenu("⚡ Activate UI Hierarchy")]
        public void ActivateUIHierarchy()
        {
            activationStatus = "Activating UI hierarchy...";
            activatedPanels.Clear();
            panelsActivated = 0;
            
            // Ensure UI System parent is active
            if (uiSystemParent != null)
            {
                uiSystemParent.SetActive(true);
                activatedPanels.Add(uiSystemParent);
                panelsActivated++;
                
                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log($"⚡ Activated UI System Parent: {uiSystemParent.name}");
                }
            }
            
            // Ensure main canvas is active
            if (mainCanvas != null)
            {
                mainCanvas.gameObject.SetActive(true);
                activatedPanels.Add(mainCanvas.gameObject);
                panelsActivated++;
                
                // Activate all direct children of main canvas
                for (int i = 0; i < mainCanvas.transform.childCount; i++)
                {
                    var child = mainCanvas.transform.GetChild(i).gameObject;
                    if (!child.activeInHierarchy)
                    {
                        child.SetActive(true);
                        activatedPanels.Add(child);
                        panelsActivated++;
                        
                        if (showDebugLogs)
                        {
                            UnityEngine.Debug.Log($"⚡ Activated canvas child: {child.name}");
                        }
                    }
                }
            }
            
            // Specifically activate UI manager components
            ActivateUIManagerPanels();
            ActivateTacticalHUDPanels();
            ActivateRTSInterfacePanels();
            
            if (forceActivateAllPanels)
            {
                ForceActivateAllUIPanels();
            }
            
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log($"⚡ Activated {panelsActivated} UI panels total");
            }
        }
        
        private void ActivateUIManagerPanels()
        {
            if (uiManager == null) return;
            
            // Get all panel fields from UIManager and activate them
            var fields = typeof(UIManager).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject) && field.Name.ToLower().Contains("panel"))
                {
                    var panel = field.GetValue(uiManager) as GameObject;
                    if (panel != null)
                    {
                        // For now, activate main menu panel and hide others (proper game state management)
                        if (field.Name.ToLower().Contains("mainmenu"))
                        {
                            panel.SetActive(true);
                            activatedPanels.Add(panel);
                            panelsActivated++;
                        }
                        
                        if (showDebugLogs)
                        {
                            UnityEngine.Debug.Log($"⚡ UIManager panel {field.Name}: {(panel.activeInHierarchy ? "Active" : "Inactive")}");
                        }
                    }
                }
            }
        }
        
        private void ActivateTacticalHUDPanels()
        {
            if (tacticalHUD == null) return;
            
            // Get all panel fields from TacticalHUD and activate them
            var fields = typeof(TacticalHUD).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject) && field.Name.ToLower().Contains("panel"))
                {
                    var panel = field.GetValue(tacticalHUD) as GameObject;
                    if (panel != null)
                    {
                        panel.SetActive(true);
                        activatedPanels.Add(panel);
                        panelsActivated++;
                        
                        if (showDebugLogs)
                        {
                            UnityEngine.Debug.Log($"⚡ TacticalHUD panel {field.Name}: Activated");
                        }
                    }
                }
            }
        }
        
        private void ActivateRTSInterfacePanels()
        {
            if (rtsInterface == null) return;
            
            // Get all panel fields from RTSInterface and activate them
            var fields = typeof(RTSInterface).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject) && field.Name.ToLower().Contains("panel"))
                {
                    var panel = field.GetValue(rtsInterface) as GameObject;
                    if (panel != null)
                    {
                        panel.SetActive(true);
                        activatedPanels.Add(panel);
                        panelsActivated++;
                        
                        if (showDebugLogs)
                        {
                            UnityEngine.Debug.Log($"⚡ RTSInterface panel {field.Name}: Activated");
                        }
                    }
                }
            }
        }
        
        private void ForceActivateAllUIPanels()
        {
            if (mainCanvas == null) return;
            
            // Recursively activate all GameObjects with "panel", "hud", "menu", etc. in their names
            ActivateUIChildrenRecursive(mainCanvas.transform);
        }
        
        private void ActivateUIChildrenRecursive(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var childName = child.name.ToLower();
                
                if (childName.Contains("panel") || childName.Contains("hud") || 
                    childName.Contains("menu") || childName.Contains("interface") ||
                    childName.Contains("control") || childName.Contains("info"))
                {
                    if (!child.gameObject.activeInHierarchy)
                    {
                        child.gameObject.SetActive(true);
                        activatedPanels.Add(child.gameObject);
                        panelsActivated++;
                        
                        if (showDebugLogs)
                        {
                            UnityEngine.Debug.Log($"⚡ Force activated: {child.name}");
                        }
                    }
                }
                
                // Recursively check children
                ActivateUIChildrenRecursive(child);
            }
        }
        
        [ContextMenu("🎮 Initialize UI Managers")]
        public void InitializeUIManagers()
        {
            activationStatus = "Initializing UI managers...";
            
            // Initialize UIManager
            if (uiManager != null)
            {
                try
                {
                    uiManager.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
                    if (showDebugLogs)
                    {
                        UnityEngine.Debug.Log("🎮 UIManager initialized");
                    }
                }
                catch (System.Exception e)
                {
                    if (showDebugLogs)
                    {
                        UnityEngine.Debug.LogWarning($"⚠️ UIManager initialization failed: {e.Message}");
                    }
                }
            }
            
            // Initialize TacticalHUD
            if (tacticalHUD != null)
            {
                try
                {
                    tacticalHUD.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
                    if (showDebugLogs)
                    {
                        UnityEngine.Debug.Log("🎮 TacticalHUD initialized");
                    }
                }
                catch (System.Exception e)
                {
                    if (showDebugLogs)
                    {
                        UnityEngine.Debug.LogWarning($"⚠️ TacticalHUD initialization failed: {e.Message}");
                    }
                }
            }
            
            // Initialize RTSInterface
            if (rtsInterface != null)
            {
                try
                {
                    rtsInterface.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
                    if (showDebugLogs)
                    {
                        UnityEngine.Debug.Log("🎮 RTSInterface initialized");
                    }
                }
                catch (System.Exception e)
                {
                    if (showDebugLogs)
                    {
                        UnityEngine.Debug.LogWarning($"⚠️ RTSInterface initialization failed: {e.Message}");
                    }
                }
            }
        }
        
        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;
            
            var field = target.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(target, value);
                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log($"🔗 Connected {fieldName} to {target.GetType().Name}");
                }
            }
        }
        
        [ContextMenu("📋 List All UI GameObjects")]
        public void ListAllUIGameObjects()
        {
            UnityEngine.Debug.Log("=== ALL UI GAMEOBJECTS IN SCENE ===");
            
            if (mainCanvas != null)
            {
                UnityEngine.Debug.Log($"Main Canvas: {mainCanvas.name} - Active: {mainCanvas.gameObject.activeInHierarchy}");
                ListChildrenRecursive(mainCanvas.transform, 1);
            }
            
            if (uiSystemParent != null)
            {
                UnityEngine.Debug.Log($"UI System Parent: {uiSystemParent.name} - Active: {uiSystemParent.activeInHierarchy}");
                ListChildrenRecursive(uiSystemParent.transform, 1);
            }
        }
        
        private void ListChildrenRecursive(Transform parent, int depth)
        {
            string indent = new string(' ', depth * 2);
            
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string status = child.gameObject.activeInHierarchy ? "✅" : "❌";
                UnityEngine.Debug.Log($"{indent}{status} {child.name}");
                
                if (child.childCount > 0)
                {
                    ListChildrenRecursive(child, depth + 1);
                }
            }
        }
        
        [ContextMenu("❓ Debug Activation Status")]
        public void DebugActivationStatus()
        {
            UnityEngine.Debug.Log("=== UI ACTIVATION STATUS ===");
            UnityEngine.Debug.Log($"Activation Status: {activationStatus}");
            UnityEngine.Debug.Log($"Is Activated: {isActivated}");
            UnityEngine.Debug.Log($"Panels Activated: {panelsActivated}");
            UnityEngine.Debug.Log($"Components Connected: {componentsConnected}");
            UnityEngine.Debug.Log($"Canvases Fixed: {canvasesFixed}");
            UnityEngine.Debug.Log($"UIManager: {(uiManager != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"TacticalHUD: {(tacticalHUD != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"RTSInterface: {(rtsInterface != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"Main Canvas: {(mainCanvas != null ? "✅" : "❌")}");
            UnityEngine.Debug.Log($"UI System Parent: {(uiSystemParent != null ? "✅" : "❌")}");
            
            if (mainCanvas != null)
            {
                UnityEngine.Debug.Log($"Main Canvas Active: {mainCanvas.gameObject.activeInHierarchy}");
                UnityEngine.Debug.Log($"Main Canvas Children Count: {mainCanvas.transform.childCount}");
            }
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(UIActivator))]
    public class UIActivatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            UIActivator activator = (UIActivator)target;
            
            EditorGUILayout.HelpBox(
                "🚀 UI Activation Helper\n\n" +
                "This script will discover, connect, and ACTIVATE all UI components in your scene. " +
                "It ensures that all UI panels are not only connected but also visible and active.\n\n" +
                "This should solve the issue of UI components being inactive even though they're connected.", 
                MessageType.Info
            );
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("🔍 Discover UI Components", GUILayout.Height(30)))
            {
                activator.DiscoverUIComponents();
            }
            
            if (GUILayout.Button("🛠️ Fix Canvas Hierarchy", GUILayout.Height(30)))
            {
                activator.FixCanvasHierarchy();
            }
            
            if (GUILayout.Button("🚀 Activate All UI Systems", GUILayout.Height(40)))
            {
                activator.ActivateAllUI();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("📋 List All UI GameObjects", GUILayout.Height(25)))
            {
                activator.ListAllUIGameObjects();
            }
            
            if (GUILayout.Button("❓ Debug Activation Status", GUILayout.Height(25)))
            {
                activator.DebugActivationStatus();
            }
            
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
#endif
}

