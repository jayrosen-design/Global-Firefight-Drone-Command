using UnityEngine;
using GlobalFirefight.UI;
using GlobalFirefight.Core;

namespace GlobalFirefight.Setup
{
    /// <summary>
    /// UI Debug Helper - Quick debugging and testing for UI visibility issues
    /// </summary>
    public class UIDebugHelper : MonoBehaviour
    {
        [Header("Debug Controls")]
        [SerializeField] private bool showAllUIOnStart = true;
        [SerializeField] private bool enableDebugKeys = true;
        
        [Header("Key Bindings")]
        [SerializeField] private KeyCode toggleAllUIKey = KeyCode.F1;
        [SerializeField] private KeyCode showTacticalHUDKey = KeyCode.F2;
        [SerializeField] private KeyCode showRTSInterfaceKey = KeyCode.F3;
        [SerializeField] private KeyCode listUIHierarchyKey = KeyCode.F4;
        
        private UIManager uiManager;
        private TacticalHUD tacticalHUD;
        private RTSInterface rtsInterface;
        private Canvas mainCanvas;
        
        private void Start()
        {
            DiscoverComponents();
            
            if (showAllUIOnStart)
            {
                Invoke(nameof(ForceShowAllUI), 0.5f); // Delay to ensure everything is loaded
            }
        }
        
        private void Update()
        {
            if (!enableDebugKeys) return;
            
            if (Input.GetKeyDown(toggleAllUIKey))
            {
                ForceShowAllUI();
            }
            
            if (Input.GetKeyDown(showTacticalHUDKey))
            {
                ShowTacticalHUD();
            }
            
            if (Input.GetKeyDown(showRTSInterfaceKey))
            {
                ShowRTSInterface();
            }
            
            if (Input.GetKeyDown(listUIHierarchyKey))
            {
                ListUIHierarchy();
            }
        }
        
        private void DiscoverComponents()
        {
            uiManager = FindFirstObjectByType<UIManager>();
            tacticalHUD = FindFirstObjectByType<TacticalHUD>();
            rtsInterface = FindFirstObjectByType<RTSInterface>();
            mainCanvas = FindFirstObjectByType<Canvas>();
            
            UnityEngine.Debug.Log("=== UI DEBUG HELPER DISCOVERY ===");
            UnityEngine.Debug.Log($"UIManager: {(uiManager != null ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"TacticalHUD: {(tacticalHUD != null ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"RTSInterface: {(rtsInterface != null ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"Main Canvas: {(mainCanvas != null ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"Press {toggleAllUIKey} to force show all UI");
            UnityEngine.Debug.Log($"Press {showTacticalHUDKey} to show Tactical HUD");
            UnityEngine.Debug.Log($"Press {showRTSInterfaceKey} to show RTS Interface");
            UnityEngine.Debug.Log($"Press {listUIHierarchyKey} to list UI hierarchy");
        }
        
        [ContextMenu("🚀 Force Show All UI")]
        public void ForceShowAllUI()
        {
            UnityEngine.Debug.Log("🚀 Force showing ALL UI in entire scene...");
            
            // First, find and activate ALL UI objects in the entire scene
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int activatedCount = 0;
            
            foreach (var go in allGameObjects)
            {
                string name = go.name.ToLower();
                
                // Check if this is any kind of UI-related GameObject
                if (name.Contains("panel") || name.Contains("hud") || name.Contains("interface") || 
                    name.Contains("menu") || name.Contains("canvas") || name.Contains("ui") || 
                    name.Contains("tactical") || name.Contains("rts") || name.Contains("button") || 
                    name.Contains("slider") || name.Contains("text") || name.Contains("image") || 
                    name.Contains("crosshair") || name.Contains("instruments") || name.Contains("status") || 
                    name.Contains("mission") || name.Contains("fleet") || name.Contains("fire") || 
                    name.Contains("globe") || name.Contains("drone"))
                {
                    if (!go.activeInHierarchy)
                    {
                        go.SetActive(true);
                        activatedCount++;
                        UnityEngine.Debug.Log($"🚀 Force activated: {go.name}");
                    }
                    
                    // Also activate all children recursively
                    ActivateAllChildren(go.transform);
                }
            }
            
            // Activate main canvas and all its children
            if (mainCanvas != null)
            {
                mainCanvas.gameObject.SetActive(true);
                ActivateAllChildren(mainCanvas.transform);
                
                // Fix main canvas positioning
                var canvasRect = mainCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    canvasRect.anchorMin = Vector2.zero;
                    canvasRect.anchorMax = Vector2.one;
                    canvasRect.offsetMin = Vector2.zero;
                    canvasRect.offsetMax = Vector2.zero;
                    canvasRect.localScale = Vector3.one;
                    canvasRect.localPosition = Vector3.zero;
                }
                
                UnityEngine.Debug.Log($"✅ Activated main canvas: {mainCanvas.name}");
            }
            
            // Force show UI manager panels
            if (uiManager != null)
            {
                var uiGO = uiManager.gameObject;
                uiGO.SetActive(true);
                ActivateAllChildren(uiGO.transform);
                
                // Try to set UI state to show something
                try
                {
                    uiManager.SendMessage("SetUIState", 2, SendMessageOptions.DontRequireReceiver); // Game state
                }
                catch { }
                
                UnityEngine.Debug.Log($"✅ Activated UIManager: {uiGO.name}");
            }
            
            // Force show tactical HUD
            if (tacticalHUD != null)
            {
                var hudGO = tacticalHUD.gameObject;
                hudGO.SetActive(true);
                ActivateAllChildren(hudGO.transform);
                
                try
                {
                    tacticalHUD.SendMessage("ShowHUD", SendMessageOptions.DontRequireReceiver);
                    tacticalHUD.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
                }
                catch { }
                
                UnityEngine.Debug.Log($"✅ Activated TacticalHUD: {hudGO.name}");
            }
            
            // Force show RTS interface
            if (rtsInterface != null)
            {
                var rtsGO = rtsInterface.gameObject;
                rtsGO.SetActive(true);
                ActivateAllChildren(rtsGO.transform);
                
                try
                {
                    rtsInterface.SendMessage("ShowRTSInterface", SendMessageOptions.DontRequireReceiver);
                    rtsInterface.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
                }
                catch { }
                
                UnityEngine.Debug.Log($"✅ Activated RTSInterface: {rtsGO.name}");
            }
            
            // Apply positioning fixes to key panels
            ApplyUIPositioningFixes();
            
            UnityEngine.Debug.Log($"🎉 Force activated {activatedCount} UI objects! All UI should now be visible and properly positioned!");
        }
        
        private void ApplyUIPositioningFixes()
        {
            var allRectTransforms = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
            
            foreach (var rect in allRectTransforms)
            {
                string name = rect.name.ToLower();
                
                // Apply specific positioning fixes
                if (name.Contains("tactical") && name.Contains("panel") && !name.Contains("hud"))
                {
                    // Main tactical panel should fill screen
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }
                else if (name.Contains("hud") && name.Contains("panel"))
                {
                    // HUD panel should fill screen
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }
                else if (name.Contains("rts") && name.Contains("panel"))
                {
                    // RTS panel should fill screen
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }
                
                // Ensure proper scale
                rect.localScale = Vector3.one;
            }
            
            UnityEngine.Debug.Log("📐 Applied positioning fixes to main panels");
        }
        
        private void ActivateAllChildren(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(true);
                
                // Recursively activate children
                if (child.childCount > 0)
                {
                    ActivateAllChildren(child);
                }
            }
        }
        
        [ContextMenu("📺 Show Tactical HUD")]
        public void ShowTacticalHUD()
        {
            if (tacticalHUD != null)
            {
                tacticalHUD.gameObject.SetActive(true);
                ActivateAllChildren(tacticalHUD.transform);
                
                try
                {
                    tacticalHUD.SendMessage("ShowHUD", SendMessageOptions.DontRequireReceiver);
                }
                catch { }
                
                UnityEngine.Debug.Log("📺 Tactical HUD activated");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ TacticalHUD not found!");
            }
        }
        
        [ContextMenu("🗺️ Show RTS Interface")]
        public void ShowRTSInterface()
        {
            if (rtsInterface != null)
            {
                rtsInterface.gameObject.SetActive(true);
                ActivateAllChildren(rtsInterface.transform);
                
                try
                {
                    rtsInterface.SendMessage("ShowRTSInterface", SendMessageOptions.DontRequireReceiver);
                }
                catch { }
                
                UnityEngine.Debug.Log("🗺️ RTS Interface activated");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ RTSInterface not found!");
            }
        }
        
        [ContextMenu("📋 List UI Hierarchy")]
        public void ListUIHierarchy()
        {
            UnityEngine.Debug.Log("=== UI HIERARCHY STATUS ===");
            
            if (mainCanvas != null)
            {
                UnityEngine.Debug.Log($"Main Canvas: {mainCanvas.name} - Active: {mainCanvas.gameObject.activeInHierarchy}");
                LogChildrenStatus(mainCanvas.transform, 1);
            }
            
            // Also check if UI managers are in the scene but not under canvas
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGameObjects)
            {
                if (go.name.ToLower().Contains("ui") && go.name.ToLower().Contains("system"))
                {
                    UnityEngine.Debug.Log($"UI System Object: {go.name} - Active: {go.activeInHierarchy}");
                    LogChildrenStatus(go.transform, 1);
                }
            }
        }
        
        private void LogChildrenStatus(Transform parent, int depth)
        {
            string indent = new string(' ', depth * 2);
            
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string status = child.gameObject.activeInHierarchy ? "✅" : "❌";
                string components = "";
                
                // List important components
                if (child.GetComponent<UIManager>()) components += "[UIManager]";
                if (child.GetComponent<TacticalHUD>()) components += "[TacticalHUD]";
                if (child.GetComponent<RTSInterface>()) components += "[RTSInterface]";
                if (child.GetComponent<Canvas>()) components += "[Canvas]";
                
                UnityEngine.Debug.Log($"{indent}{status} {child.name} {components}");
                
                if (child.childCount > 0 && depth < 3) // Limit depth to avoid spam
                {
                    LogChildrenStatus(child, depth + 1);
                }
            }
        }
        
        private void OnGUI()
        {
            if (!enableDebugKeys) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Box("UI Debug Helper");
            
            if (GUILayout.Button($"Toggle All UI ({toggleAllUIKey})"))
            {
                ForceShowAllUI();
            }
            
            if (GUILayout.Button($"Show Tactical HUD ({showTacticalHUDKey})"))
            {
                ShowTacticalHUD();
            }
            
            if (GUILayout.Button($"Show RTS Interface ({showRTSInterfaceKey})"))
            {
                ShowRTSInterface();
            }
            
            if (GUILayout.Button($"List UI Hierarchy ({listUIHierarchyKey})"))
            {
                ListUIHierarchy();
            }
            
            GUILayout.EndArea();
        }
    }
}

