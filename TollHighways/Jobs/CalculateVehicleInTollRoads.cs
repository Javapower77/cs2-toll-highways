using Game.Net;
using Game.Prefabs;
using TollHighways.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Colossal.Entities;
using SubLane = Game.Net.SubLane;

namespace TollHighways.Jobs
{
    // Job to calculate the vehicles passing through a toll road
    [BurstCompile]
    internal struct CalculateVehicleInTollRoads : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> tollRoadEntities;
        [ReadOnly] public BufferLookup<SubLane> SubLaneObjectData;
        [ReadOnly] public BufferLookup<LaneObject> LaneObjectData;
        [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefData;
        [ReadOnly] public PrefabSystem prefabSystem;

        [WriteOnly] public NativeList<VehicleInTollRoadResult> Results;

        public void Execute(int index)
        {
            string[] tollRoadVehicle;
            
            // Loop in all Road of type Toll
            foreach (Entity e in tollRoadEntities)
            {
                // Get the Sublanes asociated with the toll road (buffered)
                if (SubLaneObjectData.TryGetBuffer(e, out DynamicBuffer<SubLane> sublaneObjects))
                {
                    // Get the LaneObjects from the first Sublane of the road that represent the location
                    // where vehicles passthrough. This is only for this custom made road
                    if (LaneObjectData.TryGetBuffer(sublaneObjects[0].m_SubLane, out DynamicBuffer<LaneObject> laneObjects))
                    {
                        // It will only objects if a vehicle is present on the lane
                        if (laneObjects.Length > 0)
                        {
                            // It can be more than one, per example, if the vehicle has a truck or is a cargo truck
                            for (int i = 0; i < laneObjects.Length; i++)
                            {
                                // Get the PrefabRef of the vehicle present in the lane object
                                if (PrefabRefData.TryGetComponent(laneObjects[i].m_LaneObject, out PrefabRef prefabRef))
                                {
                                    // Now get the PrefabBase of the vehicle
                                    if (prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabVehicle))
                                    {
                                        LogUtil.Info($"Vehicle({i})::{prefabVehicle.name}--Road::{e.Index}:{e.Version}");
                                        
                                        if (i == 0)
                                        {
                                            vehiclePrefab1 = prefabVehicle.name;
                                        } 
                                        else
                                        {
                                            vehiclePrefab2 = prefabVehicle.name;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Results.Add(
                new VehicleInTollRoadResult
                {
                    VehiclePrefab1 = tollRoadVehicle,
                    VehiclePrefab2 = vehiclePrefab2
                }
                );
        }
    }
}