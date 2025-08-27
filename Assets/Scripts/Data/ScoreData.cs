using System;
using UnityEngine;

namespace GlobalFirefight.Data
{
    [System.Serializable]
    public class ScoreData
    {
        [Header("Economic Impact")]
        public float propertyValueSaved;
        public float resourceValueSaved;
        public float livesSavedBonus;
        public float totalValueSaved;
        
        [Header("Operational Costs")]
        public float deploymentCosts;
        public float operationalCosts;
        public float suppressantCosts;
        public float totalSuppressionCost;
        
        [Header("Performance Metrics")]
        public int firesDetected;
        public int firesExtinguished;
        public int firesContained;
        public int firesFailed;
        public float extinguishmentRate;
        
        [Header("Resource Efficiency")]
        public int dronesDeployed;
        public int totalDrops;
        public float averageResponseTime;
        public float resourceEfficiency;
        
        [Header("Lives and Property")]
        public int livesAtRiskSaved;
        public int propertiesProtected;
        public float acresProtected;
        public float economicImpactPrevented;
        
        [Header("Final Score")]
        public float finalScore;
        public string performanceGrade;
        public DateTime missionStartTime;
        public DateTime missionEndTime;
        public float missionDuration;
        
        // Constructor
        public ScoreData()
        {
            missionStartTime = DateTime.Now;
            performanceGrade = "F";
        }
        
        // Calculate final score
        public void CalculateFinalScore()
        {
            totalValueSaved = propertyValueSaved + resourceValueSaved + livesSavedBonus;
            totalSuppressionCost = deploymentCosts + operationalCosts + suppressantCosts;
            
            finalScore = totalValueSaved - totalSuppressionCost;
            
            // Bonus for efficiency
            if (dronesDeployed > 0)
            {
                resourceEfficiency = (float)firesExtinguished / dronesDeployed;
                finalScore += resourceEfficiency * 1000f; // Efficiency bonus
            }
            
            // Calculate performance grade
            CalculatePerformanceGrade();
            
            // Calculate other metrics
            if (firesDetected > 0)
            {
                extinguishmentRate = (float)firesExtinguished / firesDetected;
            }
            
            missionEndTime = DateTime.Now;
            missionDuration = (float)(missionEndTime - missionStartTime).TotalMinutes;
        }
        
        // Calculate performance grade based on score and efficiency
        private void CalculatePerformanceGrade()
        {
            float efficiency = extinguishmentRate;
            float costEffectiveness = finalScore / Mathf.Max(1f, totalSuppressionCost);
            
            // Combined score consideration
            if (finalScore >= 100000f && efficiency >= 0.9f)
                performanceGrade = "A+";
            else if (finalScore >= 75000f && efficiency >= 0.8f)
                performanceGrade = "A";
            else if (finalScore >= 50000f && efficiency >= 0.7f)
                performanceGrade = "B+";
            else if (finalScore >= 25000f && efficiency >= 0.6f)
                performanceGrade = "B";
            else if (finalScore >= 10000f && efficiency >= 0.5f)
                performanceGrade = "C+";
            else if (finalScore >= 5000f && efficiency >= 0.4f)
                performanceGrade = "C";
            else if (finalScore >= 0f && efficiency >= 0.3f)
                performanceGrade = "D";
            else
                performanceGrade = "F";
        }
        
        // Add property value saved
        public void AddPropertyValueSaved(float value)
        {
            propertyValueSaved += value;
            propertiesProtected++;
        }
        
        // Add resource value saved
        public void AddResourceValueSaved(float value, float acres)
        {
            resourceValueSaved += value;
            acresProtected += acres;
        }
        
        // Add lives saved bonus
        public void AddLivesSaved(int lives, float bonusValue)
        {
            livesAtRiskSaved += lives;
            livesSavedBonus += bonusValue;
        }
        
        // Add operational costs
        public void AddDeploymentCost(float cost)
        {
            deploymentCosts += cost;
        }
        
        public void AddOperationalCost(float cost)
        {
            operationalCosts += cost;
        }
        
        public void AddSuppressantCost(float cost)
        {
            suppressantCosts += cost;
        }
        
        // Record fire outcome
        public void RecordFireExtinguished()
        {
            firesExtinguished++;
        }
        
        public void RecordFireContained()
        {
            firesContained++;
        }
        
        public void RecordFireFailed()
        {
            firesFailed++;
        }
        
        public void RecordFireDetected()
        {
            firesDetected++;
        }
        
        // Record drone deployment
        public void RecordDroneDeployment()
        {
            dronesDeployed++;
        }
        
        // Record suppressant drop
        public void RecordSuppressantDrop()
        {
            totalDrops++;
        }
        
        // Get detailed score breakdown for UI
        public string GetScoreBreakdown()
        {
            return $"=== MISSION PERFORMANCE REPORT ===\n\n" +
                   $"ECONOMIC IMPACT:\n" +
                   $"Property Value Saved: ${propertyValueSaved:N0}\n" +
                   $"Resource Value Saved: ${resourceValueSaved:N0}\n" +
                   $"Lives Saved Bonus: ${livesSavedBonus:N0}\n" +
                   $"Total Value Saved: ${totalValueSaved:N0}\n\n" +
                   
                   $"OPERATIONAL COSTS:\n" +
                   $"Deployment Costs: ${deploymentCosts:N0}\n" +
                   $"Operational Costs: ${operationalCosts:N0}\n" +
                   $"Suppressant Costs: ${suppressantCosts:N0}\n" +
                   $"Total Costs: ${totalSuppressionCost:N0}\n\n" +
                   
                   $"PERFORMANCE METRICS:\n" +
                   $"Fires Detected: {firesDetected}\n" +
                   $"Fires Extinguished: {firesExtinguished}\n" +
                   $"Fires Contained: {firesContained}\n" +
                   $"Extinguishment Rate: {extinguishmentRate:P1}\n\n" +
                   
                   $"RESOURCE UTILIZATION:\n" +
                   $"Drones Deployed: {dronesDeployed}\n" +
                   $"Total Drops: {totalDrops}\n" +
                   $"Resource Efficiency: {resourceEfficiency:F2}\n\n" +
                   
                   $"LIVES AND PROPERTY:\n" +
                   $"Lives at Risk Saved: {livesAtRiskSaved:N0}\n" +
                   $"Properties Protected: {propertiesProtected}\n" +
                   $"Acres Protected: {acresProtected:F1}\n\n" +
                   
                   $"FINAL ASSESSMENT:\n" +
                   $"Net Score: ${finalScore:N0}\n" +
                   $"Performance Grade: {performanceGrade}\n" +
                   $"Mission Duration: {missionDuration:F1} minutes";
        }
        
        // Get summary for quick display
        public string GetScoreSummary()
        {
            return $"Score: ${finalScore:N0} | Grade: {performanceGrade} | " +
                   $"Fires: {firesExtinguished}/{firesDetected} | " +
                   $"Efficiency: {extinguishmentRate:P0}";
        }
        
        // Get performance rating (0-1)
        public float GetPerformanceRating()
        {
            switch (performanceGrade)
            {
                case "A+": return 1.0f;
                case "A": return 0.9f;
                case "B+": return 0.85f;
                case "B": return 0.8f;
                case "C+": return 0.75f;
                case "C": return 0.7f;
                case "D": return 0.6f;
                default: return 0.0f;
            }
        }
        
        // Check if this is a high score
        public bool IsHighScore(float threshold = 50000f)
        {
            return finalScore >= threshold;
        }
        
        // Get medal based on performance
        public string GetMedalAward()
        {
            if (performanceGrade == "A+" && livesAtRiskSaved > 1000)
                return "Medal of Heroism";
            else if (performanceGrade == "A+" || performanceGrade == "A")
                return "Distinguished Service Medal";
            else if (performanceGrade == "B+" || performanceGrade == "B")
                return "Commendation Medal";
            else if (performanceGrade == "C+" || performanceGrade == "C")
                return "Achievement Medal";
            else
                return "Participation Certificate";
        }
    }
}
