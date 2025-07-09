using Game.Prefabs;
using Unity.Entities;

namespace TollHighways.Jobs
{
    // Result structure for job communication
    public struct VehicleInTollRoadResult
    {
        public string VehiclePrefab1;
        public string VehiclePrefab2;
    }
}