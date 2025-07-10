using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using System;
using System.Runtime.ExceptionServices;
using TollHighways.Jobs;
using TollHighways.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using SubLane = Game.Net.SubLane;


namespace TollHighways.Systems
{
    public partial class UpdateTollRoadsSystem : GameSystemBase
    {
        private PrefabSystem prefabSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery roadsQuery;
        private EntityQuery tollRoadsQuery;
        private readonly NativeList<VehicleInTollRoadResult> _vehicleInTollRoadResult = new(Allocator.Persistent);
        private NativeArray<Entity> _tollRoadEntities = new(0, Allocator.Persistent);

        // Entity Command Buffer System for immediate updates
        private TollHighwaysEntityCommandBufferSystem _tollHighwaysECBSystem;

        protected override void OnGameLoaded(Context serializationContext)
        {
            LogUtil.Info("TollHighways::AppliedRoadTollsModification::OnGameLoadingComplete()");

            LogUtil.Info("TollHighways::AppliedRoadTollsModification::OnCreate()::Call Function InitializeRoadPrefabs()");
            InitializeRoadPrefabs();
        }

        protected override void OnCreate()
        {
            LogUtil.Info("TollHighways::AppliedRoadTollsModification::OnCreate()");
            base.OnCreate();


            LogUtil.Info("TollHighways::AppliedRoadTollsModification::OnCreate()::Call Function InitializeQueries()");
            InitializeQueries();

            LogUtil.Info("TollHighways::AppliedRoadTollsModification::OnCreate()::Create TollHighwaysEntityCommandBufferSystem");
            _tollHighwaysECBSystem = World.GetOrCreateSystemManaged<TollHighwaysEntityCommandBufferSystem>();
        }

        private void InitializeQueries()
        {
            roadsQuery = SystemAPI.QueryBuilder()
                     .WithAll<Game.Net.Curve, Game.Net.Edge, Game.Net.Road, PrefabRef>()
                     .WithNone<Game.Net.TrainTrack, Game.Net.Waterway, Game.Net.ElectricityConnection>()
                     .Build();

            tollRoadsQuery = SystemAPI.QueryBuilder()
                    .WithAll<TollHighways.Domain.Components.TollRoadPrefabData>()
                    .Build();
        }

        private void InitializeRoadPrefabs()
        {
            // Get all entities from the toll roads query
            NativeArray<Entity> roadsArray = roadsQuery.ToEntityArray(Allocator.Temp);

            // Prepare the prefabSystem to get info about the Prefab Base
            this.prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // First validate that the asset for the road toll exists
            if (this.prefabSystem.TryGetPrefab(new PrefabID("RoadPrefab", "Highway Oneway - 1 lane (Toll 60kph)"), out PrefabBase tollRoadPrefab))
            {                
                // Loop into all the entities of the array
                for (int i = 0; i < roadsArray.Length; i++)
                {
                    // Get the prefab base of each entity in order to compare with the road toll. thanks to yenyang for the help!
                    if ((EntityManager.TryGetComponent(roadsArray[i], out PrefabRef prefabRef) && this.prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabBase)) && prefabBase is not null)
                    {
                        // Check if the entity is a road toll
                        if (prefabBase.name == tollRoadPrefab.name)
                        {
                            // Check if already not having the RoadToll component attached
                            if (!EntityManager.HasComponent<TollHighways.Domain.Components.TollRoadPrefabData>(roadsArray[i]))
                            {
                                // Add the component of RoadToll to the entity representing a road with toll
                                EntityManager.AddComponent<TollHighways.Domain.Components.TollRoadPrefabData>(roadsArray[i]);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            PrefabSystem prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // Get ECB from the system for immediate updates
            EntityCommandBuffer ecb = _tollHighwaysECBSystem.CreateCommandBuffer();

            _tollRoadEntities = tollRoadsQuery.ToEntityArray(Allocator.Persistent);

            JobHandle combinedJobHandle = default;

            if (_tollRoadEntities.Length > 0)
            {
                var vehicleTollJob = new CalculateVehicleInTollRoads
                {
                    tollRoadEntities = _tollRoadEntities,
                    LaneObjectData = SystemAPI.GetBufferLookup<LaneObject>(true),
                    SubLaneObjectData = SystemAPI.GetBufferLookup<SubLane>(true),
                    PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(true),
                    Results = _vehicleInTollRoadResult
                };


                JobHandle vehicleTollJobHandle = vehicleTollJob.Schedule(_tollRoadEntities.Length, 1);
                combinedJobHandle = JobHandle.CombineDependencies(combinedJobHandle, vehicleTollJobHandle);
            }

            // Register the job with the ECB system for completion and playback
            _tollHighwaysECBSystem.AddJobHandleForProducer(combinedJobHandle);

            LogUtil.Info($"_vehicleInTollRoadResult::{_vehicleInTollRoadResult} - Toll Roads::{_tollRoadEntities.Length}");
            
            //if (prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabVehicle))
        }
    }
}
