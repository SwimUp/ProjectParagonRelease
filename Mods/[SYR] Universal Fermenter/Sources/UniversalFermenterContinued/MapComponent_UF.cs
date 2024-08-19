using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class MapComponent_UF : MapComponent
    {
        [Unsaved(false)]
        public List<ThingWithComps> thingsWithUFComp = new List<ThingWithComps>();

        public MapComponent_UF(Map map)
            : base(map)
        {
        }

        public void Register(ThingWithComps thing)
        {
            thingsWithUFComp.Add(thing);
        }

        public void Deregister(ThingWithComps thing)
        {
            thingsWithUFComp.Remove(thing);
        }
    }
}
