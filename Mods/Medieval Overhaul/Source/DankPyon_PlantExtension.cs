using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace DankPyon
{

    public class PlantExtension : DefModExtension
    {
        public bool transparencyWhenPawnOrItemIsBehind;
        public IntVec2 firstArea;
        public IntVec2 firstAreaOffset;
        public IntVec2 secondArea;
        public IntVec2 secondAreaOffset;

        public List<ThingDef> ignoredThings;
    }

}

