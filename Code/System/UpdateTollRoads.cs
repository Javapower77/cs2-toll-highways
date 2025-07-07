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
            base.OnCreate();

            this.prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            this.prefabSystem.TryGetPrefab(new PrefabID("RoadPrefab", "Highway Oneway - 1 lane (Toll 60kph)"), out PrefabBase tollRoadPrefab);

            tollRoadsQuery = SystemAPI.QueryBuilder()
                .WithAll<Applied, Game.Net.Curve, Game.Net.Edge, PrefabRef>()
                .WithNone<Temp, Deleted>()
                .Build();

            NativeArray<Entity> tollRoadsApplied = tollRoadsQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity tollRoad in tollRoadsApplied)
            {
               // if (tollroadsPrefabs.t)
            }

        }
    }

}
