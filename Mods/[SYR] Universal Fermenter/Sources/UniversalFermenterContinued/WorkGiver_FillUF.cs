using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace UniversalFermenter
{
    public class WorkGiver_FillUF : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool Prioritized => true;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.GetComponent<MapComponent_UF>().thingsWithUFComp;
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            CompUniversalFermenter compUniversalFermenter = t.Thing.TryGetComp<CompUniversalFermenter>();
            if (compUniversalFermenter != null)
            {
                return 1 / compUniversalFermenter.SpaceLeftForIngredient;
            }
            return 0f;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompUniversalFermenter compUniversalFermenter = t.TryGetComp<CompUniversalFermenter>();
            if (compUniversalFermenter == null || compUniversalFermenter.Finished || compUniversalFermenter.SpaceLeftForIngredient <= 0)
            {
                return false;
            }
            float ambientTemperature = compUniversalFermenter.parent.AmbientTemperature;
            if (ambientTemperature < compUniversalFermenter.CurrentProcess.temperatureSafe.min + 2f || ambientTemperature > compUniversalFermenter.CurrentProcess.temperatureSafe.max - 2f)
            {
                JobFailReason.Is("BadTemperature".Translate().ToLower());
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced))
            {
                return false;
            }
            if (FindIngredient(pawn, t) == null)
            {
                JobFailReason.Is("UF_NoIngredient".Translate());
                return false;
            }
            return !t.IsBurning();
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Thing thing = FindIngredient(pawn, t);
            return new Job(UF_DefOf.FillUniversalFermenter, t, thing);
        }

        private Thing FindIngredient(Pawn pawn, Thing fermenter)
        {
            ThingFilter filter = fermenter.TryGetComp<CompUniversalFermenter>().CurrentProcess.ingredientFilter;
            Predicate<Thing> validator = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x) && filter.Allows(x);
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, filter.BestThingRequest, PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
        }
    }
}
