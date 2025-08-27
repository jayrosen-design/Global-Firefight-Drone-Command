using UnityEngine;

namespace GlobalFirefight.Debug
{
    /// <summary>
    /// Debug logging utility that provides the same interface as the code expects.
    /// The existing code calls: Debug.Log(), Debug.LogWarning(), Debug.LogError(), Debug.DrawRay()
    /// where Debug refers to GlobalFirefight.Debug when using the namespace.
    /// </summary>
    
    public static class Log
    {
        public static void Info(string message)
        {
            UnityEngine.Debug.Log($"[GlobalFirefight] {message}");
        }
    }
    
    public static class LogWarning  
    {
        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning($"[GlobalFirefight WARNING] {message}");
        }
    }
    
    public static class LogError
    {
        public static void Error(string message)
        {
            UnityEngine.Debug.LogError($"[GlobalFirefight ERROR] {message}");
        }
    }
    
    public static class DrawRay
    {
        public static void Ray(Vector3 start, Vector3 direction, Color color, float duration = 0.0f)
        {
            UnityEngine.Debug.DrawRay(start, direction, color, duration);
        }
        
        public static void Ray(Vector3 start, Vector3 direction)
        {
            UnityEngine.Debug.DrawRay(start, direction, Color.white);
        }
    }
}
