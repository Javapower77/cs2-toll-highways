using Colossal.Serialization.Entities;
using Unity.Entities;

namespace TollHighways.Domain.Components
{
    // This component is used to mark a prefab as a toll road.
    // And also to serialize it when the game is saved.
    public struct TollRoadPrefabData : IComponentData, IEmptySerializable
    {
    }

}