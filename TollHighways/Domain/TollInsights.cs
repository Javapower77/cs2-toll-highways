using Colossal.Serialization.Entities;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace TollHighways.Domain
{
    public struct TollInsights : IComponentData, ISerializable
    {
        /// <summary>
        /// The prefab of the toll road.
        /// </summary>
        public Entity TollRoadPrefab;

        /// <summary>
        /// The type of vehicle passing the toll road.
        /// </summary>
        public Domain.Enums.VehicleType VehicleType;

        /// <summary>
        /// The name of the vehicle passing the toll.
        /// </summary>
        public FixedString64Bytes VehicleName;

        /// <summary>
        /// The number of times this type of vehicle has passed through the toll road.
        /// </summary>
        public int PassThroughCount;

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out TollRoadPrefab);
            reader.Read(out int vehicleTypeInt);
            VehicleType = (Domain.Enums.VehicleType)vehicleTypeInt; // Deserialize enum from int
            reader.Read(out string VehicleName);
            reader.Read(out PassThroughCount);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(TollRoadPrefab);
            writer.Write((int)VehicleType); // Serialize enum as int
            writer.Write(VehicleName.ToString());
            writer.Write(PassThroughCount);
        }
    }
}
