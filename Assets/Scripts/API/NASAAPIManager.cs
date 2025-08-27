using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using GlobalFirefight.Data;

namespace GlobalFirefight.API
{
    [System.Serializable]
    public class FIRMSDataPoint
    {
        public double latitude;
        public double longitude;
        public float brightness;
        public string acq_date;
        public string acq_time;
        public string satellite;
        public float confidence;
        public string version;
        public float bright_t31;
        public float frp;
        public string daynight;
    }
    
    [System.Serializable]
    public class EONETEvent
    {
        public string id;
        public string title;
        public string description;
        public string link;
        public List<EONETCategory> categories;
        public List<EONETSource> sources;
        public List<EONETGeometry> geometry;
    }
    
    [System.Serializable]
    public class EONETCategory
    {
        public int id;
        public string title;
    }
    
    [System.Serializable]
    public class EONETSource
    {
        public string id;
        public string url;
    }
    
    [System.Serializable]
    public class EONETGeometry
    {
        public string date;
        public string type;
        public List<float> coordinates;
    }
    
    public class NASAAPIManager : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string firmsAPIKey = "";
        [SerializeField] private string baseURL = "https://firms.modaps.eosdis.nasa.gov/api/active_fire/";
        [SerializeField] private string eonetURL = "https://eonet.gsfc.nasa.gov/api/v3/events";
        
        [Header("Data Settings")]
        [SerializeField] private string dataSource = "VIIRS_SNPP_NRT"; // VIIRS or MODIS
        [SerializeField] private string area = "world";
        [SerializeField] private string timeRange = "24h"; // 24h, 48h, 7d
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private float retryDelay = 5f;
        
        [Header("Caching")]
        [SerializeField] private bool enableCaching = true;
        [SerializeField] private float cacheExpireTime = 1800f; // 30 minutes
        [SerializeField] private DateTime lastFetchTime;
        [SerializeField] private List<FireIncident> cachedFireData;
        [SerializeField] private List<EONETEvent> cachedEONETData;
        
        [Header("Status")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private bool isFetching = false;
        [SerializeField] private int totalFiresLoaded = 0;
        [SerializeField] private int totalEventsLoaded = 0;
        
        // Events
        public static event Action<List<FireIncident>> OnFireDataLoaded;
        public static event Action<List<EONETEvent>> OnEONETDataLoaded;
        public static event Action<string> OnAPIError;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public bool IsFetching => isFetching;
        public int TotalFiresLoaded => totalFiresLoaded;
        public int TotalEventsLoaded => totalEventsLoaded;
        
        #region Initialization
        
        public void Initialize(string apiKey = "")
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                firmsAPIKey = apiKey;
            }
            
            // Initialize cached data lists
            cachedFireData = new List<FireIncident>();
            cachedEONETData = new List<EONETEvent>();
            
            isInitialized = true;
            UnityEngine.Debug.Log("NASA API Manager initialized");
            
            // Start periodic data refresh if in real-time mode
            if (Application.isPlaying)
            {
                StartCoroutine(PeriodicDataRefresh());
            }
        }
        
        #endregion
        
        #region Data Fetching
        
        public IEnumerator LoadFireData()
        {
            if (isFetching)
            {
                UnityEngine.Debug.LogWarning("Fire data fetch already in progress");
                yield break;
            }
            
            // Check cache first
            if (enableCaching && IsCacheValid())
            {
                UnityEngine.Debug.Log("Using cached fire data");
                OnFireDataLoaded?.Invoke(cachedFireData);
                yield break;
            }
            
            yield return StartCoroutine(FetchFIRMSData());
        }
        
        private IEnumerator FetchFIRMSData()
        {
            isFetching = true;
            int attempts = 0;
            
            while (attempts < maxRetries)
            {
                string url = BuildFIRMSURL();
                UnityEngine.Debug.Log($"Fetching FIRMS data from: {url}");
                
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = 30;
                    yield return request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string csvData = request.downloadHandler.text;
                        List<FireIncident> fireData = ParseFIRMSCSV(csvData);
                        
                        if (fireData.Count > 0)
                        {
                            cachedFireData = fireData;
                            lastFetchTime = DateTime.Now;
                            totalFiresLoaded = fireData.Count;
                            
                            UnityEngine.Debug.Log($"Successfully loaded {fireData.Count} fire incidents");
                            OnFireDataLoaded?.Invoke(fireData);
                            break;
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("No fire data received");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"FIRMS API request failed: {request.error}");
                        OnAPIError?.Invoke($"FIRMS API Error: {request.error}");
                        
                        attempts++;
                        if (attempts < maxRetries)
                        {
                            UnityEngine.Debug.Log($"Retrying in {retryDelay} seconds... (Attempt {attempts + 1}/{maxRetries})");
                            yield return new WaitForSeconds(retryDelay);
                        }
                    }
                }
            }
            
            isFetching = false;
            
            if (attempts >= maxRetries)
            {
                UnityEngine.Debug.LogError("Failed to fetch FIRMS data after all retry attempts");
                OnAPIError?.Invoke("Failed to fetch fire data after multiple attempts");
            }
        }
        
        public IEnumerator LoadEONETData()
        {
            string url = $"{eonetURL}?category=wildfires&status=open";
            UnityEngine.Debug.Log($"Fetching EONET data from: {url}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonData = request.downloadHandler.text;
                        var eonetResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                        
                        if (eonetResponse.ContainsKey("events"))
                        {
                            string eventsJson = eonetResponse["events"].ToString();
                            List<EONETEvent> events = JsonConvert.DeserializeObject<List<EONETEvent>>(eventsJson);
                            
                            cachedEONETData = events;
                            totalEventsLoaded = events.Count;
                            
                            UnityEngine.Debug.Log($"Successfully loaded {events.Count} EONET events");
                            OnEONETDataLoaded?.Invoke(events);
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"Failed to parse EONET data: {e.Message}");
                        OnAPIError?.Invoke($"EONET Parse Error: {e.Message}");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"EONET API request failed: {request.error}");
                    OnAPIError?.Invoke($"EONET API Error: {request.error}");
                }
            }
        }
        
        #endregion
        
        #region Data Parsing
        
        private List<FireIncident> ParseFIRMSCSV(string csvData)
        {
            List<FireIncident> fires = new List<FireIncident>();
            
            string[] lines = csvData.Split('\n');
            if (lines.Length < 2)
            {
                UnityEngine.Debug.LogWarning("Invalid CSV data received");
                return fires;
            }
            
            // Parse header to get column indices
            string[] headers = lines[0].Split(',');
            Dictionary<string, int> columnIndices = new Dictionary<string, int>();
            
            for (int i = 0; i < headers.Length; i++)
            {
                columnIndices[headers[i].Trim()] = i;
            }
            
            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                    
                string[] values = line.Split(',');
                if (values.Length < headers.Length)
                    continue;
                
                try
                {
                    FireIncident fire = ParseFireIncidentFromCSV(values, columnIndices);
                    if (fire != null)
                    {
                        fires.Add(fire);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"Failed to parse fire data row {i}: {e.Message}");
                }
            }
            
            return fires;
        }
        
        private FireIncident ParseFireIncidentFromCSV(string[] values, Dictionary<string, int> columns)
        {
            FireIncident fire = new FireIncident();
            
            // Parse required fields
            if (columns.ContainsKey("latitude"))
                fire.latitude = double.Parse(values[columns["latitude"]]);
            else
                return null;
                
            if (columns.ContainsKey("longitude"))
                fire.longitude = double.Parse(values[columns["longitude"]]);
            else
                return null;
            
            // Parse optional fields with defaults
            fire.confidence = columns.ContainsKey("confidence") ? 
                float.Parse(values[columns["confidence"]]) : 50f;
                
            fire.frp = columns.ContainsKey("frp") ? 
                float.Parse(values[columns["frp"]]) : 10f;
            
            // Parse acquisition time
            if (columns.ContainsKey("acq_date") && columns.ContainsKey("acq_time"))
            {
                string dateStr = values[columns["acq_date"]];
                string timeStr = values[columns["acq_time"]];
                fire.acquisitionTime = ParseAcquisitionTime(dateStr, timeStr);
            }
            else
            {
                fire.acquisitionTime = DateTime.Now;
            }
            
            // Parse satellite source
            if (columns.ContainsKey("satellite"))
            {
                fire.satellite = values[columns["satellite"]];
            }
            
            // Initialize game-specific properties from FIRMS data
            fire.InitializeFromFIRMS(fire.latitude, fire.longitude, fire.confidence, fire.frp, fire.acquisitionTime);
            
            return fire;
        }
        
        private DateTime ParseAcquisitionTime(string dateStr, string timeStr)
        {
            try
            {
                // Expected format: YYYY-MM-DD and HHMM
                string[] dateParts = dateStr.Split('-');
                int year = int.Parse(dateParts[0]);
                int month = int.Parse(dateParts[1]);
                int day = int.Parse(dateParts[2]);
                
                int hour = int.Parse(timeStr.Substring(0, 2));
                int minute = int.Parse(timeStr.Substring(2, 2));
                
                return new DateTime(year, month, day, hour, minute, 0);
            }
            catch
            {
                return DateTime.Now;
            }
        }
        
        #endregion
        
        #region URL Building
        
        private string BuildFIRMSURL()
        {
            // Handle demo key vs real key
            string apiKey = string.IsNullOrEmpty(firmsAPIKey) ? "DEMO_KEY" : firmsAPIKey;
            
            // Build URL: /api/active_fire/c1/csv/API_KEY/SOURCE/AREA/TIME
            string url = $"{baseURL}c1/csv/{apiKey}/{dataSource}/{area}/{timeRange}";
            
            return url;
        }
        
        #endregion
        
        #region Caching
        
        private bool IsCacheValid()
        {
            if (cachedFireData == null || cachedFireData.Count == 0)
                return false;
                
            TimeSpan timeSinceLastFetch = DateTime.Now - lastFetchTime;
            return timeSinceLastFetch.TotalSeconds < cacheExpireTime;
        }
        
        public void ClearCache()
        {
            cachedFireData?.Clear();
            cachedEONETData?.Clear();
            lastFetchTime = DateTime.MinValue;
            UnityEngine.Debug.Log("API cache cleared");
        }
        
        #endregion
        
        #region Periodic Updates
        
        private IEnumerator PeriodicDataRefresh()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(cacheExpireTime);
                
                if (!isFetching)
                {
                    UnityEngine.Debug.Log("Performing periodic data refresh...");
                    yield return StartCoroutine(LoadFireData());
                    yield return StartCoroutine(LoadEONETData());
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        public void SetAPIKey(string newAPIKey)
        {
            firmsAPIKey = newAPIKey;
            UnityEngine.Debug.Log("FIRMS API key updated");
        }
        
        public void SetDataSource(string source)
        {
            dataSource = source;
            UnityEngine.Debug.Log($"Data source changed to: {source}");
        }
        
        public void SetTimeRange(string range)
        {
            timeRange = range;
            UnityEngine.Debug.Log($"Time range changed to: {range}");
        }
        
        public void ForceRefresh()
        {
            ClearCache();
            StartCoroutine(LoadFireData());
            StartCoroutine(LoadEONETData());
        }
        
        public List<FireIncident> GetCachedFireData()
        {
            return new List<FireIncident>(cachedFireData);
        }
        
        public List<EONETEvent> GetCachedEONETData()
        {
            return new List<EONETEvent>(cachedEONETData);
        }
        
        public string GetAPIStatus()
        {
            return $"FIRMS: {totalFiresLoaded} fires | EONET: {totalEventsLoaded} events | " +
                   $"Cache: {(IsCacheValid() ? "Valid" : "Expired")} | " +
                   $"Fetching: {(isFetching ? "Yes" : "No")}";
        }
        
        #endregion
    }
}

