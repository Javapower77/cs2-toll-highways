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
using Unity.Collections;
using Unity.Entities;
using SubLane = Game.Net.SubLane;

namespace TollHighways
{
    public partial class UpdateTollRoads : GameSystemBase
    {
        private PrefabSystem prefabSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery roadsQuery;
        private EntityQuery tollRoadsQuery;
        


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
        }

        private void InitializeQueries()
        {
            roadsQuery = SystemAPI.QueryBuilder()
                     .WithAll<Game.Net.Curve, Game.Net.Edge, Game.Net.Road, PrefabRef>()
                     .WithNone<Game.Net.TrainTrack, Game.Net.Waterway, Game.Net.ElectricityConnection>()
                     .Build();

            tollRoadsQuery = SystemAPI.QueryBuilder()
                    .WithAll<TollHighways.RoadToll>()
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
                            if (!EntityManager.HasComponent<RoadToll>(roadsArray[i]))
                            {
                                // Add the component of RoadToll to the entity representing a road with toll
                                EntityManager.AddComponent<RoadToll>(roadsArray[i]);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            TimeSystem timeSystem = new();
            DateTime currentTime = timeSystem.GetCurrentDateTime();
            long timeTicks = currentTime.Ticks;
            PrefabSystem prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            foreach (Entity e in this.tollRoadsQuery.ToEntityArray(Allocator.Temp))
            {
                if (EntityManager.TryGetBuffer(e, true, out DynamicBuffer<SubLane> sublaneObjects))
                {
                    if (EntityManager.TryGetBuffer(sublaneObjects[0].m_SubLane, true, out DynamicBuffer<LaneObject> laneObjects))
                    {
                        if (laneObjects.Length > 0)
                        {
                            for (int i = 0; i < laneObjects.Length; i++)
                            {
                                if (EntityManager.TryGetComponent(laneObjects[i].m_LaneObject, out PrefabRef prefabRef))
                                {
                                    if(prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabVehicle))
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
