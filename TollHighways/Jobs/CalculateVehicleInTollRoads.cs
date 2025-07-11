using Colossal.Entities;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using System.Collections.Generic;
using TollHighways.Domain;
using TollHighways.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using SubLane = Game.Net.SubLane;

namespace TollHighways.Jobs
{
    // Job to calculate the vehicles passing through a toll road
    // thanks to krzychu124 to pointing me to disable burst compilation at build level of the mod
#if WITH_BURST
    [BurstCompile]
#endif
    public struct CalculateVehicleInTollRoads : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> tollRoadEntities;
        [ReadOnly] public BufferLookup<SubLane> SubLaneObjectData;
        [ReadOnly] public BufferLookup<LaneObject> LaneObjectData;
        [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefData;

        [WriteOnly] public NativeList<VehicleInTollRoadResult> Results;

        public void Execute(int index)
        {
            // Process the toll road entity according to the posititon in the array of roads when this job is called
            Entity e = tollRoadEntities[index];

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

                                // Last step is adding to the Result List the entity representing the road
                                // and the PrefabRef of the vehicle, so then can be calculate the pricing according
                                // to the vehicle type
                                Results.Add(
                                    new VehicleInTollRoadResult
                                    {
                                        TollRoadEntity = e,
                                        VehiclePrefabRef = prefabRef
                                    }
                                    );
                            }
                        }
                    }
                }
            }
                                     
        }
    }
}