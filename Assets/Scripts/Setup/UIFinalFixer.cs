using System.Collections;
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
    /// Final UI Fixer - Comprehensive solution for UI activation AND proper positioning
    /// This script force-activates ALL UI panels and ensures proper positioning
    /// </summary>
    public class UIFinalFixer : MonoBehaviour
    {
        [Header("UI Final Fix Status")]
        [SerializeField] private bool isFixed = false;
        [SerializeField] private string fixStatus = "Ready to fix all UI issues";
        
        [Header("Fix Options")]
        [SerializeField] private bool fixOnStart = true;
        [SerializeField] private bool forceActivateEverything = true;
        [SerializeField] private bool fixAllPositioning = true;
        [SerializeField] private bool showDetailedLogs = true;
        
        [Header("Found Components")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private TacticalHUD tacticalHUD;
        [SerializeField] private RTSInterface rtsInterface;
        
        [Header("Fix Results")]
        [SerializeField] private int totalPanelsActivated = 0;
        [SerializeField] private int totalPanelsRepositioned = 0;
        [SerializeField] private int totalCanvasesFixed = 0;
        
        private void Start()
        {
            if (fixOnStart && !isFixed)
            {
                StartCoroutine(FixUIWithDelay());
            }
        }
        
        private IEnumerator FixUIWithDelay()
        {
            // Wait a frame to ensure everything is loaded
            yield return new WaitForEndOfFrame();
            FixAllUIIssues();
        }
        
        [ContextMenu("🔧 Fix All UI Issues")]
        public void FixAllUIIssues()
        {
            fixStatus = "Fixing all UI issues...";
            
            DiscoverComponents();
            ForceActivateAllUI();
            FixAllPositioning();
            InitializeManagers();
            
            isFixed = true;
            fixStatus = "All UI issues fixed!";
            
            if (showDetailedLogs)
            {
                UnityEngine.Debug.Log("🔧 UI FINAL FIX COMPLETE!");
                UnityEngine.Debug.Log($"📊 Results: {totalPanelsActivated} panels activated, {totalPanelsRepositioned} panels repositioned, {totalCanvasesFixed} canvases fixed");
            }
        }
        
        private void DiscoverComponents()
        {
            uiManager = FindFirstObjectByType<UIManager>();
            tacticalHUD = FindFirstObjectByType<TacticalHUD>();
            rtsInterface = FindFirstObjectByType<RTSInterface>();
            mainCanvas = FindFirstObjectByType<Canvas>();
            
            if (showDetailedLogs)
            {
                UnityEngine.Debug.Log("=== UI FINAL FIXER DISCOVERY ===");
                UnityEngine.Debug.Log($"UIManager: {(uiManager != null ? "✅" : "❌")}");
                UnityEngine.Debug.Log($"TacticalHUD: {(tacticalHUD != null ? "✅" : "❌")}");
                UnityEngine.Debug.Log($"RTSInterface: {(rtsInterface != null ? "✅" : "❌")}");
                UnityEngine.Debug.Log($"Main Canvas: {(mainCanvas != null ? "✅" : "❌")}");
            }
        }
        
        [ContextMenu("🚀 Force Activate ALL UI")]
        public void ForceActivateAllUI()
        {
            if (!forceActivateEverything) return;
            
            fixStatus = "Force activating all UI...";
            totalPanelsActivated = 0;
            
            // Find and activate ALL GameObjects with UI-related names
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (var go in allGameObjects)
            {
                string name = go.name.ToLower();
                
                // Check if this is a UI-related GameObject
                if (IsUIGameObject(name))
                {
                    if (!go.activeInHierarchy)
                    {
                        go.SetActive(true);
                        totalPanelsActivated++;
                        
                        if (showDetailedLogs)
                        {
                            UnityEngine.Debug.Log($"🚀 Force activated: {go.name}");
                        }
                    }
                    
                    // Also activate all children recursively
                    ActivateAllChildrenRecursive(go.transform);
                }
            }
            
            // Ensure main canvas and its hierarchy are fully active
            if (mainCanvas != null)
            {
                mainCanvas.gameObject.SetActive(true);
                ActivateAllChildrenRecursive(mainCanvas.transform);
            }
            
            if (showDetailedLogs)
            {
                UnityEngine.Debug.Log($"🚀 Force activated {totalPanelsActivated} UI panels");
            }
        }
        
        private bool IsUIGameObject(string name)
        {
            return name.Contains("panel") || 
                   name.Contains("hud") || 
                   name.Contains("interface") || 
                   name.Contains("menu") || 
                   name.Contains("canvas") || 
                   name.Contains("ui") || 
                   name.Contains("tactical") || 
                   name.Contains("rts") || 
                   name.Contains("button") || 
                   name.Contains("slider") || 
                   name.Contains("text") || 
                   name.Contains("image") || 
                   name.Contains("crosshair") || 
                   name.Contains("instruments") || 
                   name.Contains("status") || 
                   name.Contains("mission") || 
                   name.Contains("fleet") || 
                   name.Contains("fire") || 
                   name.Contains("globe") || 
                   name.Contains("drone");
        }
        
        private void ActivateAllChildrenRecursive(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                
                if (!child.gameObject.activeInHierarchy)
                {
                    child.gameObject.SetActive(true);
                    totalPanelsActivated++;
                    
                    if (showDetailedLogs)
                    {
                        UnityEngine.Debug.Log($"🚀 Force activated child: {child.name}");
                    }
                }
                
                // Recursively activate children
                if (child.childCount > 0)
                {
                    ActivateAllChildrenRecursive(child);
                }
            }
        }
        
        [ContextMenu("📐 Fix All Positioning")]
        public void FixAllPositioning()
        {
            if (!fixAllPositioning) return;
            
            fixStatus = "Fixing all UI positioning...";
            totalPanelsRepositioned = 0;
            totalCanvasesFixed = 0;
            
            // Fix main canvas settings
            if (mainCanvas != null)
            {
                FixCanvas(mainCanvas);
                totalCanvasesFixed++;
            }
            
            // Find and fix all RectTransforms
            var allRectTransforms = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
            
            foreach (var rectTransform in allRectTransforms)
            {
                if (rectTransform.gameObject == mainCanvas?.gameObject) continue; // Skip main canvas
                
                string name = rectTransform.name.ToLower();
                
                // Apply specific positioning based on panel type
                if (ApplySpecificPositioning(rectTransform, name))
                {
                    totalPanelsRepositioned++;
                }
            }
            
            if (showDetailedLogs)
            {
                UnityEngine.Debug.Log($"📐 Fixed positioning for {totalPanelsRepositioned} panels and {totalCanvasesFixed} canvases");
            }
        }
        
        private void FixCanvas(Canvas canvas)
        {
            // Ensure proper canvas settings
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            // Add/configure CanvasScaler
            var canvasScaler = canvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // Add/configure GraphicRaycaster
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Fix main canvas RectTransform to fill screen
            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.one;
                canvasRect.offsetMin = Vector2.zero;
                canvasRect.offsetMax = Vector2.zero;
                canvasRect.localScale = Vector3.one;
                canvasRect.localPosition = Vector3.zero;
            }
            
            if (showDetailedLogs)
            {
                UnityEngine.Debug.Log($"🛠️ Fixed canvas: {canvas.name}");
            }
        }
        
        private bool ApplySpecificPositioning(RectTransform rectTransform, string name)
        {
            // Flight instruments (bottom-left)
            if (name.Contains("flight") || name.Contains("instruments") || name.Contains("speedometer"))
            {
                SetPanelAnchors(rectTransform, new Vector2(0, 0), new Vector2(0.25f, 0.3f), new Vector2(0, 0));
                return true;
            }
            
            // Drone status (left side)
            if (name.Contains("drone") && name.Contains("status"))
            {
                SetPanelAnchors(rectTransform, new Vector2(0, 0.3f), new Vector2(0.25f, 0.7f), new Vector2(0, 0.5f));
                return true;
            }
            
            // Mission panel (top center)
            if (name.Contains("mission"))
            {
                SetPanelAnchors(rectTransform, new Vector2(0.3f, 0.8f), new Vector2(0.7f, 1f), new Vector2(0.5f, 1f));
                return true;
            }
            
            // Fleet panel (right side)
            if (name.Contains("fleet"))
            {
                SetPanelAnchors(rectTransform, new Vector2(0.75f, 0.3f), new Vector2(1f, 0.7f), new Vector2(1f, 0.5f));
                return true;
            }
            
            // Fire info panel (left side, lower)
            if (name.Contains("fire") && name.Contains("info"))
            {
                SetPanelAnchors(rectTransform, new Vector2(0, 0.6f), new Vector2(0.3f, 1f), new Vector2(0, 1f));
                return true;
            }
            
            // Globe controls (bottom-left corner)
            if (name.Contains("globe") && name.Contains("control"))
            {
                SetPanelAnchors(rectTransform, new Vector2(0, 0), new Vector2(0.2f, 0.3f), new Vector2(0, 0));
                return true;
            }
            
            // Status bar (bottom full width)
            if (name.Contains("status") && name.Contains("bar"))
            {
                SetPanelAnchors(rectTransform, new Vector2(0, 0), new Vector2(1f, 0.1f), new Vector2(0.5f, 0));
                return true;
            }
            
            // Crosshair (center)
            if (name.Contains("crosshair"))
            {
                SetPanelAnchors(rectTransform, new Vector2(0.45f, 0.45f), new Vector2(0.55f, 0.55f), new Vector2(0.5f, 0.5f));
                return true;
            }
            
            // Main tactical HUD or RTS interface (full screen)
            if ((name.Contains("tactical") && name.Contains("hud")) || 
                (name.Contains("rts") && name.Contains("interface")))
            {
                SetPanelAnchors(rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                return true;
            }
            
            // Main panels (full screen)
            if (name.Contains("panel") && (name.Contains("hud") || name.Contains("tactical") || name.Contains("rts")))
            {
                SetPanelAnchors(rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                return true;
            }
            
            return false;
        }
        
        private void SetPanelAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            
            // Reset offsets to make it fit the anchors exactly
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // Ensure proper scale and rotation
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            
            if (showDetailedLogs)
            {
                UnityEngine.Debug.Log($"📐 Repositioned: {rectTransform.name} to anchors ({anchorMin}, {anchorMax})");
            }
        }
        
        [ContextMenu("🎮 Initialize Managers")]
        public void InitializeManagers()
        {
            fixStatus = "Initializing UI managers...";
            
            // Initialize UIManager
            if (uiManager != null)
            {
                try
                {
                    uiManager.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
                    if (showDetailedLogs) UnityEngine.Debug.Log("🎮 UIManager initialized");
                }
                catch (System.Exception e)
                {
                    if (showDetailedLogs) UnityEngine.Debug.LogWarning($"UIManager initialization warning: {e.Message}");
                }
            }
            
            // Initialize TacticalHUD
            if (tacticalHUD != null)
            {
                try
                {
                    tacticalHUD.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
                    tacticalHUD.SendMessage("ShowHUD", SendMessageOptions.DontRequireReceiver);
                    if (showDetailedLogs) UnityEngine.Debug.Log("🎮 TacticalHUD initialized and shown");
                }
                catch (System.Exception e)
                {
                    if (showDetailedLogs) UnityEngine.Debug.LogWarning($"TacticalHUD initialization warning: {e.Message}");
                }
            }
            
            // Initialize RTSInterface
            if (rtsInterface != null)
            {
                try
                {
                    rtsInterface.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
                    rtsInterface.SendMessage("ShowRTSInterface", SendMessageOptions.DontRequireReceiver);
                    if (showDetailedLogs) UnityEngine.Debug.Log("🎮 RTSInterface initialized and shown");
                }
                catch (System.Exception e)
                {
                    if (showDetailedLogs) UnityEngine.Debug.LogWarning($"RTSInterface initialization warning: {e.Message}");
                }
            }
        }
        
        [ContextMenu("📋 Show UI Hierarchy Status")]
        public void ShowUIHierarchyStatus()
        {
            UnityEngine.Debug.Log("=== UI HIERARCHY STATUS AFTER FIX ===");
            
            if (mainCanvas != null)
            {
                UnityEngine.Debug.Log($"Main Canvas: {mainCanvas.name} - Active: {mainCanvas.gameObject.activeInHierarchy}");
                LogHierarchyStatus(mainCanvas.transform, 1);
            }
            
            // Also check for any UI objects outside the main canvas
            var uiObjects = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
            foreach (var uiObj in uiObjects)
            {
                if (uiObj.transform.parent == null && uiObj.gameObject != mainCanvas?.gameObject)
                {
                    if (IsUIGameObject(uiObj.name.ToLower()))
                    {
                        UnityEngine.Debug.Log($"Standalone UI Object: {uiObj.name} - Active: {uiObj.gameObject.activeInHierarchy}");
                    }
                }
            }
        }
        
        private void LogHierarchyStatus(Transform parent, int depth)
        {
            if (depth > 4) return; // Limit depth to prevent spam
            
            string indent = new string(' ', depth * 2);
            
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string status = child.gameObject.activeInHierarchy ? "✅" : "❌";
                
                UnityEngine.Debug.Log($"{indent}{status} {child.name}");
                
                if (child.childCount > 0)
                {
                    LogHierarchyStatus(child, depth + 1);
                }
            }
        }
        
        [ContextMenu("🎯 Test UI Positioning")]
        public void TestUIPositioning()
        {
            UnityEngine.Debug.Log("=== TESTING UI POSITIONING ===");
            
            if (mainCanvas != null)
            {
                var canvasRect = mainCanvas.GetComponent<RectTransform>();
                UnityEngine.Debug.Log($"Main Canvas Rect: Size={canvasRect.rect.size}, Position={canvasRect.anchoredPosition}");
                
                // Check some key panels
                var panels = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
                foreach (var panel in panels)
                {
                    string name = panel.name.ToLower();
                    if (name.Contains("panel") && panel.gameObject.activeInHierarchy)
                    {
                        UnityEngine.Debug.Log($"Panel '{panel.name}': Anchors=({panel.anchorMin}, {panel.anchorMax}), Size={panel.rect.size}");
                    }
                }
            }
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(UIFinalFixer))]
    public class UIFinalFixerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("🔧 Fix All UI Issues", GUILayout.Height(40)))
            {
                ((UIFinalFixer)target).FixAllUIIssues();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("🚀 Force Activate ALL UI", GUILayout.Height(30)))
            {
                ((UIFinalFixer)target).ForceActivateAllUI();
            }
            
            if (GUILayout.Button("📐 Fix All Positioning", GUILayout.Height(30)))
            {
                ((UIFinalFixer)target).FixAllPositioning();
            }
            
            if (GUILayout.Button("🎮 Initialize Managers", GUILayout.Height(30)))
            {
                ((UIFinalFixer)target).InitializeManagers();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("📋 Show UI Hierarchy Status", GUILayout.Height(25)))
            {
                ((UIFinalFixer)target).ShowUIHierarchyStatus();
            }
            
            if (GUILayout.Button("🎯 Test UI Positioning", GUILayout.Height(25)))
            {
                ((UIFinalFixer)target).TestUIPositioning();
            }
        }
    }
#endif
}

