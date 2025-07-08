using Colossal.Serialization.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace TollHighways
{

    public struct RoadToll : IComponentData, IQueryTypeParameter, ISerializable
    {
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
        }

    }

}