using Game;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace TollHighways
{
    public partial class AppliedRoadTollsModification : GameSystemBase
    {
        private PrefabSystem prefabSystem;
        private EntityQuery tollRoadsQuery;

        protected override void OnUpdate()
        {
            if (Mod.Settings == null)
            {
                LogUtil.Warn("Mod settings not initialized, skipping update");
                return;
            }
        }
        protected override void OnCreate()
        {
            LogUtil.Info("TollHighways::AppliedRoadTollsModification::OnCreate()");
            base.OnCreate();

            this.prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            this.prefabSystem.TryGetPrefab(new PrefabID("RoadPrefab", "Highway Oneway - 1 lane (Toll 60kph)"), out PrefabBase tollRoadPrefab);
            LogUtil.Info($"TollHighways::AppliedRoadTollsModification::OnCreate()::tollRoadPrefab->{tollRoadPrefab}");

            tollRoadsQuery = SystemAPI.QueryBuilder()
                .WithAll<Applied, Game.Net.Curve, Game.Net.Edge, PrefabRef>()
                .WithNone<Temp, Deleted>()
                .Build();

            NativeArray<Entity> tollRoadsApplied = tollRoadsQuery.ToEntityArray(Allocator.Temp);
            LogUtil.Info($"TollHighways::AppliedRoadTollsModification::OnCreate()::tollRoadsApplied->{tollRoadsApplied}");

            foreach (Entity tollRoad in tollRoadsApplied)
            {
               // if (tollroadsPrefabs.t)
            }

        }
    }

}
