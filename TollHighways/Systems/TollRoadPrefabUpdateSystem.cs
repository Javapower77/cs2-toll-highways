using Game;
using Game.Prefabs;
using TollHighways.Domain.Components;
using TollHighways.Utilities;

namespace TollHighways.Systems
{
    public partial class TollRoadPrefabUpdateSystem : GameSystemBase
    {
        private PrefabSystem prefabSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            AddTollComponentToRoad();
        }

        protected override void OnUpdate()
        {
            return;
        }

        // This method is called to initialize the custom road prefab and add the TollRoadPrefabInfo component
        private void AddTollComponentToRoad()
        {
            // Check if the prefab for the toll road exists
            if (this.prefabSystem.TryGetPrefab(new PrefabID("RoadPrefab", "Highway Oneway - 1 lane (Toll 60kph)"), out PrefabBase tollRoadPrefab))
            {
                // Check if the prefab already has the TollRoadPrefabInfo component
                if (tollRoadPrefab.GetComponent<TollRoadPrefabInfo>())
                {
                    // If the prefab already has the TollRoadPrefabInfo component, skip it
                    return;
                }
                else
                {
                    // If the prefab does not have the TollRoadPrefabInfo component, add it
                    tollRoadPrefab.AddComponent<TollRoadPrefabInfo>();
                    LogUtil.Info($"TollHighways::UpdateTollRoadsSystem::AddTollComponentToRoad() - Added TollRoadPrefabInfo to {tollRoadPrefab.name}");

                    // Update the prefab with the new component added to the prefab system in order to be used later in a entity query
                    this.prefabSystem.UpdatePrefab(tollRoadPrefab); 
                }
            }
        }

    }
}