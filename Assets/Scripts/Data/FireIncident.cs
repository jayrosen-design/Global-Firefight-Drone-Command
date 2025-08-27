using System;
using UnityEngine;

namespace GlobalFirefight.Data
{
    [System.Serializable]
    public enum FireStatus
    {
        Active,
        BeingExtinguished,
        Extinguished,
        Spreading
    }

    [System.Serializable]
    public class FireIncident
    {
        [Header("Fire Identification")]
        public string id;
        public string incidentId; // Added for compatibility
        
        [Header("Location Data")]
        public double latitude;
        public double longitude;
        public float altitude;
        public Vector3 location; // Added for compatibility
        
        [Header("NASA FIRMS Data")]
        public float confidence; // 0-100 detection confidence
        public float frp; // Fire Radiative Power in MW
        public DateTime acquisitionTime;
        public string satellite; // VIIRS or MODIS
        
        [Header("Game Mechanics")]
        public float health; // Current fire health (0-100)
        public float maxHealth; // Initial health based on confidence
        public float intensity; // Derived from FRP, affects spread rate
        public float currentIntensity; // Added for compatibility
        public FireStatus status;
        public float spreadRadius; // Current spread radius in meters
        public float damageRadius; // Area affecting property/lives
        public bool isConfirmed; // Added for compatibility
        
        [Header("Economic Impact")]
        public float propertyValue; // Value of property at risk
        public float resourceValue; // Timber/ecosystem value
        public bool threateningPopulation; // Lives at risk flag
        public int populationAtRisk; // Number of people at risk
        
        [Header("Visual Properties")]
        public Color fireColor;
        public float smokeIntensity;
        public float lightIntensity;
        
        // Constructor
        public FireIncident()
        {
            id = System.Guid.NewGuid().ToString();
            incidentId = id; // Initialize compatibility property
            status = FireStatus.Active;
            fireColor = Color.red;
            smokeIntensity = 1.0f;
            lightIntensity = 1.0f;
            isConfirmed = true; // Default to confirmed
            currentIntensity = intensity; // Initialize compatibility property
            location = new Vector3((float)longitude, 0f, (float)latitude); // Initialize compatibility property
        }
        
        // Initialize from NASA FIRMS data
        public void InitializeFromFIRMS(double lat, double lon, float conf, float firePower, DateTime acqTime)
        {
            latitude = lat;
            longitude = lon;
            confidence = conf;
            frp = firePower;
            acquisitionTime = acqTime;
            
            // Calculate initial game values from real data
            maxHealth = Mathf.Clamp(confidence, 20f, 100f);
            health = maxHealth;
            intensity = Mathf.Clamp(frp / 100f, 0.1f, 10f); // Normalize FRP to intensity
            spreadRadius = Mathf.Clamp(frp * 10f, 50f, 1000f); // Initial spread based on power
            
            // Set visual properties based on intensity
            UpdateVisualProperties();
        }
        
        // Update visual properties based on current state
        public void UpdateVisualProperties()
        {
            float healthPercent = health / maxHealth;
            float intensityFactor = intensity / 10f;
            
            // Fire color gets more intense with higher FRP and health
            fireColor = Color.Lerp(Color.yellow, Color.red, intensityFactor);
            smokeIntensity = Mathf.Lerp(0.3f, 2.0f, intensityFactor * healthPercent);
            lightIntensity = Mathf.Lerp(0.5f, 3.0f, intensityFactor * healthPercent);
        }
        
        // Apply suppressant damage to fire
        public void ApplySuppressionDamage(float damage)
        {
            health = Mathf.Max(0f, health - damage);
            if (health <= 0f)
            {
                status = FireStatus.Extinguished;
            }
            UpdateVisualProperties();
        }
        
        // Calculate fire spread over time
        public void UpdateSpread(float deltaTime)
        {
            if (status == FireStatus.Active || status == FireStatus.Spreading)
            {
                // Fire spreads based on intensity and health
                float spreadRate = intensity * (health / maxHealth) * 0.1f; // meters per second
                spreadRadius += spreadRate * deltaTime;
                
                // Health regeneration (fire getting worse if not suppressed)
                float regenRate = intensity * 0.05f;
                health = Mathf.Min(maxHealth, health + regenRate * deltaTime);
                
                UpdateVisualProperties();
            }
        }
        
        // Check if fire is threatening a position
        public bool IsThreateningPosition(Vector3 position, Vector3 firePosition)
        {
            float distance = Vector3.Distance(position, firePosition);
            return distance <= damageRadius;
        }
        
        // Get fire severity level for UI display
        public string GetSeverityLevel()
        {
            if (intensity >= 8f) return "Extreme";
            if (intensity >= 5f) return "High";
            if (intensity >= 2f) return "Moderate";
            return "Low";
        }
        
        // Calculate suppression priority score
        public float GetPriorityScore()
        {
            float score = 0f;
            
            // Higher priority for fires threatening population
            if (threateningPopulation)
                score += populationAtRisk * 100f;
            
            // Property value at risk
            score += propertyValue * 0.001f;
            
            // Fire intensity and spread potential
            score += intensity * 10f;
            
            // Health percentage (higher health = more urgent)
            score += (health / maxHealth) * 20f;
            
            return score;
        }
        
        // Get affected area for compatibility
        public float GetAffectedArea()
        {
            return Mathf.PI * spreadRadius * spreadRadius; // Area = π * r²
        }
    }
}
