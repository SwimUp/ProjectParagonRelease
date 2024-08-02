using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LWM.FuelFilter
{
    public class Comp_FuelFilter : ThingComp
    {
        public ThingFilter filter;

        public override void Initialize(CompProperties props)
        {
            filter = new ThingFilter(FilterChanged);
            ThingFilter thingFilter = parent.GetComp<CompRefuelable>()?.Props.fuelFilter;
            if (thingFilter != null)
            {
                filter.CopyAllowancesFrom(thingFilter);
            }
            else
            {
                Log.Warning("LWM.FuelFilter: Could not find any allowed fuels for " + parent?.ToString() + "; this is probably a mistake on someone's part (maybe not mine)");
            }
        }

        public override void PostExposeData()
        {
            Scribe_Deep.Look(ref filter, "LWMFF_filter", new Action(FilterChanged));
            if (Scribe.mode == LoadSaveMode.PostLoadInit && filter == null)
            {
                Initialize(props);
            }
        }

        public void FilterChanged()
        {
            if (!parent.Spawned)
            {
                return;
            }
            foreach (Pawn item in parent.Map.mapPawns.FreeColonistsSpawned)
            {
                if (parent.Map.reservationManager.ReservedBy(parent, item) && item.CurJobDef == JobDefOf.Refuel && item.CurJob.targetB.Thing != null && !filter.Allows(item.CurJob.targetB.Thing))
                {
                    item.jobs.StopAll();
                }
            }
        }
    }
}
