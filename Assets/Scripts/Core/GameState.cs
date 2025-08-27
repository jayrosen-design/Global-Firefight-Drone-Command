using System;
using UnityEngine;

namespace GlobalFirefight.Core
{
    [System.Serializable]
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        RTSView,
        DroneView,
        Paused,
        GameOver,
        Victory,
        PostMission
    }
    
    [System.Serializable]
    public enum GameMode
    {
        Campaign,
        Scenario,
        Tutorial,
        Sandbox
    }
    
    [System.Serializable]
    public class GameSettings
    {
        [Header("Difficulty Settings")]
        public float fireSpreadMultiplier = 1.0f;
        public float droneEfficiencyMultiplier = 1.0f;
        public int maxSimultaneousFleets = 3;
        public bool realTimeWeather = true;
        
        [Header("Performance Settings")]
        public int maxFiresDisplayed = 500;
        public float lodSwitchDistance = 1000f;
        public bool enableParticleEffects = true;
        public int particleQuality = 2; // 0=Low, 1=Medium, 2=High
        
        [Header("API Settings")]
        public string nasaAPIKey = "";
        public string googleAPIKey = "";
        public float dataRefreshInterval = 1800f; // 30 minutes
        public bool useRealTimeData = true;
        
        [Header("Mission Settings")]
        public float missionDuration = 600f; // 10 minutes default
        public bool infiniteMode = false;
        public float scoreMultiplier = 1.0f;
        
        [Header("Audio Settings")]
        public float masterVolume = 1.0f;
        public float sfxVolume = 1.0f;
        public float musicVolume = 0.7f;
        public bool enableVoiceLines = true;
    }
}
