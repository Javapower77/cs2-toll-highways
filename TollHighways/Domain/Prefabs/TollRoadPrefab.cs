using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;
using TollHighways.Domain.Components;
using TollHighways.Utilities;

namespace TollHighways.Domain.Prefabs
{
    public class TollRoadPrefab : RoadPrefab
    {        
        public override void GetPrefabComponents(HashSet<ComponentType> components)
        {
            LogUtil.Info("TollHighways.Domain.Prefabs::TollRoadPrefab::GetPrefabComponents()");
            base.GetPrefabComponents(components);
            components.Add(ComponentType.ReadOnly<TollRoadPrefabData>());
        }
    }
}
