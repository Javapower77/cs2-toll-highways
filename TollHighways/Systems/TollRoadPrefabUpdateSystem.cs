using Game;
using Game.Prefabs;
using Game.Tools;
using Game.UI.InGame;
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

            // Add the Toll Component to the two custom roads prefabs
            // in order to be used later in the entity query
            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            AddTollComponentToRoad("Highway Oneway - 1 lane (Toll 60kph)");
            AddTollComponentToRoad("Highway Oneway - 1 lane - Public Transport (Toll 60kph)");
            EnableSelectForStaticObjectPrefab("TollBooth");
        }

        protected override void OnUpdate()
        {
            return;
        }

        private void EnableSelectForStaticObjectPrefab(string StaticObjectPrefabName)
        {
            if (this.prefabSystem.TryGetPrefab(new PrefabID("StaticObjectPrefab", StaticObjectPrefabName), out PrefabBase m_staticObjectPrefab))
            {
                int i = 0;

            }

        }

        // This method is called to initialize the custom road prefab and add the TollRoadPrefabInfo component
        private void AddTollComponentToRoad(string TollRoadName)
        {
            // Check if the prefab for the toll road exists
            if (this.prefabSystem.TryGetPrefab(new PrefabID("RoadPrefab", TollRoadName), out PrefabBase tollRoadPrefab))
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