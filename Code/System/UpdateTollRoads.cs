using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using System.Runtime.ExceptionServices;
using Unity.Collections;
using Unity.Entities;

namespace TollHighways
{
    public partial class AppliedRoadTollsModification : GameSystemBase
    {
        private PrefabSystem prefabSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery tollRoadsQuery;

        protected override void OnUpdate()
        {
            LogUtil.Info("TollHighways::AppliedRoadTollsModification::OnUpdate()");


        }

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
            tollRoadsQuery = SystemAPI.QueryBuilder()
                     .WithAll<Game.Net.Curve, Game.Net.Edge, Game.Net.Road, PrefabRef>()
                     .WithNone<Game.Net.TrainTrack, Game.Net.Waterway, Game.Net.ElectricityConnection>()
                     .Build();
        }

        private void InitializeRoadPrefabs()
        {
            // Get all entities from the toll roads query
            NativeArray<Entity> tollRoadsApplied = tollRoadsQuery.ToEntityArray(Allocator.Temp);

            // Prepare the prefabSystem to get info about the Prefab Base
            this.prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // First validate that the asset for the road toll exists
            if (this.prefabSystem.TryGetPrefab(new PrefabID("RoadPrefab", "Highway Oneway - 1 lane (Toll 60kph)"), out PrefabBase tollRoadPrefab))
            {                
                // Loop into all the entities of the array
                for (int i = 0; i < tollRoadsApplied.Length; i++)
                {
                    // Get the prefab base of each entity in order to compare with the road toll. thanks to yenyang for the help!
                    if ((EntityManager.TryGetComponent(tollRoadsApplied[i], out PrefabRef prefabRef) && this.prefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabBase)) && prefabBase is not null)
                    {
                        // Check if the entity is a road toll
                        if (prefabBase.name == tollRoadPrefab.name)
                        {
                            // Check if already not having the RoadToll component attached
                            if (!EntityManager.HasComponent<RoadToll>(tollRoadsApplied[i]))
                            {
                                // Add the component of RoadToll to the entity representing a road with toll
                                EntityManager.AddComponent<RoadToll>(tollRoadsApplied[i]);
                            }
                        }
                    }
                }
            }
        }
    }
}
