using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TollHighways.Utilities;
using Unity.Entities;
using UnityEngine;

namespace TollHighways.Domain.Components
{
    [ComponentMenu("TollHighways/", new Type[] { typeof(WithNoneAttribute) })]
    public class TollRoadPrefabInfo : ComponentBase
    {
        public override void GetArchetypeComponents(HashSet<ComponentType> components)
        {
            components.Add(ComponentType.ReadWrite<TollRoadPrefabData>());
        }

        public override void GetPrefabComponents(HashSet<ComponentType> components)
        {
            components.Add(ComponentType.ReadWrite<TollRoadPrefabData>());
        }
    }
}
