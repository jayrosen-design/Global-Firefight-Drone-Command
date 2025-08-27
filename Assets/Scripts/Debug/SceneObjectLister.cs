using UnityEngine;

namespace GlobalFirefight.Debug
{
    public class SceneObjectLister : MonoBehaviour
    {
        [ContextMenu("List All Scene Objects")]
        public void ListAllSceneObjects()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            UnityEngine.Debug.Log($"🔍 Found {allObjects.Length} objects in scene:");
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.transform.parent == null) // Root objects only
                {
                    LogObjectHierarchy(obj, 0);
                }
            }
        }
        
        [ContextMenu("Find Cesium Objects")]
        public void FindCesiumObjects()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            UnityEngine.Debug.Log("🌍 Searching for Cesium-related objects:");
            
            foreach (GameObject obj in allObjects)
            {
                string name = obj.name.ToLower();
                if (name.Contains("cesium") || name.Contains("globe") || name.Contains("earth") || name.Contains("world"))
                {
                    UnityEngine.Debug.Log($"🎯 Found: {obj.name}");
                    UnityEngine.Debug.Log($"   - Position: {obj.transform.position}");
                    UnityEngine.Debug.Log($"   - Scale: {obj.transform.lossyScale}");
                    UnityEngine.Debug.Log($"   - Parent: {(obj.transform.parent?.name ?? "None")}");
                    
                    // Check for Cesium components
                    Component[] components = obj.GetComponents<Component>();
                    foreach (Component comp in components)
                    {
                        if (comp.GetType().Name.Contains("Cesium"))
                        {
                            UnityEngine.Debug.Log($"   - Component: {comp.GetType().Name}");
                        }
                    }
                }
            }
        }
        
        private void LogObjectHierarchy(GameObject obj, int depth)
        {
            string indent = new string(' ', depth * 2);
            UnityEngine.Debug.Log($"{indent}- {obj.name} (Pos: {obj.transform.position}, Scale: {obj.transform.lossyScale})");
            
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                LogObjectHierarchy(obj.transform.GetChild(i).gameObject, depth + 1);
            }
        }
    }
}
