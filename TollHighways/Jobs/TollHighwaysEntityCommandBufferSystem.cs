using TollHighways.Systems;
using Unity.Entities;

namespace TollHighways.Jobs
{
    // Custom ECB system for parking pricing updates
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UpdateTollRoadsSystem))]
    public partial class TollHighwaysEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }
}
