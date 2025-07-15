using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TollHighways.Domain
{
    public struct Vehicle
    {
        public string Name { get; set; }
        public TollHighways.Domain.Enums.VehicleType Type { get; set; }
    }
}
