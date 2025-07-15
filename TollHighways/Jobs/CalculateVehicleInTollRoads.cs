using Colossal.Entities;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using System.Collections.Generic;
using TollHighways.Domain;
using TollHighways.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
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
        [ReadOnly] public ComponentLookup<Game.Net.Edge> EdgeObjectData;
        [ReadOnly] public ComponentLookup<Game.Vehicles.CarTrailerLane> VehicleTrailerData;
        //[WriteOnly] public NativeList<Entity> vehiclePrefabEntities;
        [WriteOnly] public NativeList<(Entity tollRoad, Entity vehicle)> Results;

        public void Execute(int index)
        {
            // Process the toll road entity according to the posititon in the array of roads when this job is called
            Entity e = tollRoadEntities[index];

            // Variable to store the index position of the Sublane object that represents the road
            int subLaneTypeRoad = 0;

            // Check if the entity has the Edge component, which is used to represent the road
            // and the point of check where the vehicles pass through
            if (EdgeObjectData.TryGetComponent(e, out Edge edgeComponent))
            {
                // get the Sublane objects from the Stard Edge component of the road
                if (SubLaneObjectData.TryGetBuffer(edgeComponent.m_Start, out DynamicBuffer<SubLane> sublaneObjects))
                {
                    for (int x = 0; x < sublaneObjects.Length; x++)
                    {
                        // Check if the Sublane object is a road, if so, store the index position
                        // to use it later to get the LaneObjects from the Sublane object
                        if (sublaneObjects[x].m_PathMethods == Game.Pathfind.PathMethod.Road)
                        {
                            subLaneTypeRoad = x;
                            break;
                        }
                    }                   

                    // Get the LaneObjects from the second Sublane of the road that represent the location
                    // where vehicles passthrough. This is only for this custom made road
                    if (LaneObjectData.TryGetBuffer(sublaneObjects[subLaneTypeRoad].m_SubLane, out DynamicBuffer<LaneObject> laneObjects))
                    {
                        // It will only objects if a vehicle is present on the lane
                        if (laneObjects.Length > 0) 
                        {
                            Entity vehicleEntity = laneObjects[0].m_LaneObject;

                            // To avoid getting the trailer of a car or a truck
                            // we ask for the CarLineTrailer component and discard if found it
                            if (!VehicleTrailerData.TryGetComponent(vehicleEntity, out _))
                            {
                                // Add the vehicle-toll road mapping to the Results list
                                Results.Add((e, vehicleEntity));
                            }                          
                        }
                    }
                }
            }
                                     
        }
    }
}