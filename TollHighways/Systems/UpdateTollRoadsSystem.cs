using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Routes;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.Vehicles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using TollHighways.Domain.Components;
using TollHighways.Jobs;
using TollHighways.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using SubLane = Game.Net.SubLane;

namespace TollHighways.Systems
{
    public partial class UpdateTollRoadsSystem : GameSystemBase
    {
        private EntityQuery tollRoadsQuery;
        public static bool hasRunThisSession = false;

        private NativeArray<Entity> _tollRoadEntities = new(0, Allocator.TempJob);
        private NativeHashMap<Entity, Entity> lastVehiclesOnTollRoad; // TollRoadEntity -> VehicleEntity

        protected override void OnCreate()
        {
            LogUtil.Info("TollHighways::UpdateTollRoadsSystem::OnCreate()");
            base.OnCreate();

            LogUtil.Info("TollHighways::UpdateTollRoadsSystem::OnCreate()::Call Function InitializeQueries()");
            InitializeQueries();

            LogUtil.Info("TollHighways::UpdateTollRoadsSystem::OnCreate()::Initializing lastVehiclesOnTollRoad");   
            lastVehiclesOnTollRoad = new NativeHashMap<Entity, Entity>(16, Allocator.Persistent);
        }

        // This method is to initialize the queries used in this system and cache them for later use.
        private void InitializeQueries()
        {
            // Query all toll roads that have the TollRoadPrefabData component.
            // This is used to identify the roads that are toll roads.
            tollRoadsQuery = SystemAPI.QueryBuilder()
                    .WithAll<Game.Prefabs.PrefabRef, TollRoadPrefabData>()
                    .WithNone<Game.Prefabs.RoadComposition>()
                    .Build();
        }

        protected override void OnUpdate()
        {
            //StartAsyncUpdate();           
            NativeList<(Entity tollRoad, Entity vehicle)> currentEntries = GetCurrentVehiclesOnTollRoad();

            foreach (var (tollRoad, vehicle) in currentEntries)
            {
                if (!lastVehiclesOnTollRoad.TryGetValue(tollRoad, out var lastVehicle) || lastVehicle != vehicle)
                {
                    // Vehicle just entered the toll road
                    TriggerTollAction(tollRoad, vehicle);
                    lastVehiclesOnTollRoad[tollRoad] = vehicle;
                }
            }
        }

        private void TriggerTollAction(Entity tollRoad, Entity vehicle)
        {
            // Your custom logic here (e.g., charge toll, log, update stats)
            
            

            LogUtil.Info($"Vehicle {vehicle.Index} entered toll road {tollRoad.Index}");
        }

        private NativeList<(Entity tollRoad, Entity vehicle)> GetCurrentVehiclesOnTollRoad()
        {
            NativeList<(Entity tollRoad, Entity vehicle)> currentEntries = new NativeList<(Entity, Entity)>(Allocator.TempJob);
            NativeArray<Entity> tollRoadEntities = tollRoadsQuery.ToEntityArray(Allocator.TempJob);

            // Create the job
            CalculateVehicleInTollRoads vehicleTollJob = new CalculateVehicleInTollRoads
            {
                tollRoadEntities = tollRoadEntities,
                EdgeObjectData = SystemAPI.GetComponentLookup<Game.Net.Edge>(true),
                LaneObjectData = SystemAPI.GetBufferLookup<Game.Net.LaneObject>(true),
                SubLaneObjectData = SystemAPI.GetBufferLookup<Game.Net.SubLane>(true),
                PrefabRefData = SystemAPI.GetComponentLookup<Game.Prefabs.PrefabRef>(true),
                Results = currentEntries // Pass the currentEntries list to the job
            };

            // Schedule the job
            JobHandle vehicleTollJobHandle = vehicleTollJob.Schedule(tollRoadEntities.Length, 1);

            // Complete the job
            vehicleTollJobHandle.Complete();

            tollRoadEntities.Dispose(); // Dispose of the array after use

            return currentEntries;
        }

        private void StartAsyncUpdate()
        {
            JobHandle combinedJobHandle = default;

            NativeArray<Entity> _tollRoadEntities = tollRoadsQuery.ToEntityArray(Allocator.TempJob);
            NativeList<Entity> _vehiclePrefabEntities = new NativeList<Entity>(Allocator.TempJob);

            // Check if there are toll roads to process and if the job has not run this session
            // Then execute the job to calculate vehicles in toll roads
            if (_tollRoadEntities.Length > 0)
            {
                var vehicleTollJob = new CalculateVehicleInTollRoads
                {
                    tollRoadEntities = _tollRoadEntities,
                    EdgeObjectData = SystemAPI.GetComponentLookup<Game.Net.Edge>(true),
                    LaneObjectData = SystemAPI.GetBufferLookup<LaneObject>(true),
                    SubLaneObjectData = SystemAPI.GetBufferLookup<SubLane>(true),
                    PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(true)
                    //vehiclePrefabEntities = _vehiclePrefabEntities
                };

                JobHandle vehicleTollJobHandle = vehicleTollJob.Schedule(_tollRoadEntities.Length, 1);
                combinedJobHandle = JobHandle.CombineDependencies(combinedJobHandle, vehicleTollJobHandle);

                // Complete the job and convert results
                combinedJobHandle.Complete();

                if ((combinedJobHandle.IsCompleted) && (_vehiclePrefabEntities.Length > 0))
                {
                    Entity vehicleInTollRoad = _vehiclePrefabEntities[0];
                    LogUtil.Info($"TollHighways::UpdateTollRoadsSystem::StartAsyncUpdate() - Vehicle :: {vehicleInTollRoad}.");

                    if (_vehiclePrefabEntities.Length > 1)
                    {
                        Entity vehicleTruckInTollRoad = _vehiclePrefabEntities[1];
                        LogUtil.Info($"TollHighways::UpdateTollRoadsSystem::StartAsyncUpdate() - Vehicle Truck :: {vehicleTruckInTollRoad}.");
                    }
                }
            }                     

        }

    }
}
