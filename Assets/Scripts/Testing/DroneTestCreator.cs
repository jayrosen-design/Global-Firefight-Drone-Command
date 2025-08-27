using UnityEngine;
using GlobalFirefight.Systems;
using GlobalFirefight.Data;
using GlobalFirefight.Drones;

namespace GlobalFirefight.Testing
{
    /// <summary>
    /// Simple test script to create a basic drone for testing the fire interaction system
    /// </summary>
    public class DroneTestCreator : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool createDroneOnStart = true;
        [SerializeField] private Vector3 initialDronePosition = new Vector3(0, 100, 0);
        
        private void Start()
        {
            if (createDroneOnStart)
            {
                CreateTestDrone();
            }
        }
        
        [ContextMenu("Create Test Drone")]
        public void CreateTestDrone()
        {
            // Create a test drone GameObject
            GameObject droneObj = new GameObject("TestDrone_ForFireSelection");
            droneObj.transform.position = initialDronePosition;
            
            // Add a visible model (simple cube for now)
            GameObject droneModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            droneModel.transform.SetParent(droneObj.transform);
            droneModel.transform.localScale = Vector3.one * 5f; // Make it clearly visible
            droneModel.GetComponent<Renderer>().material.color = Color.blue;
            
            // Create drone specifications
            DroneSpecifications specs = new DroneSpecifications();
            specs.modelName = "Test Fire Response Drone";
            specs.agency = "Unity Test Agency";
            specs.type = DroneType.HeavyLiftSuppression;
            specs.maxSpeed = 25f;
            specs.flightEndurance = 1800f; // 30 minutes
            specs.payloadCapacity = 100;
            specs.maxRange = 5000f;
            specs.hasHighPressureNozzle = true;
            specs.canVTOL = true;
            specs.primaryColor = Color.blue;
            
            // Create drone unit data
            DroneUnit droneUnit = new DroneUnit();
            droneUnit.Initialize(specs, "TestCommandVehicle");
            droneUnit.position = initialDronePosition;
            
            // Add drone controller
            DroneController controller = droneObj.AddComponent<DroneController>();
            controller.Initialize(droneUnit);
            
            UnityEngine.Debug.Log($"🚁 Created test drone: {droneUnit.callSign} at position {initialDronePosition}");
            UnityEngine.Debug.Log("🔥 You can now click on fires to deploy this drone!");
        }
    }
}
