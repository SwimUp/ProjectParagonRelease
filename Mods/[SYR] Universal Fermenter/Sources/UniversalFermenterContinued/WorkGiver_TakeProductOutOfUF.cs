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
    public class WorkGiver_TakeProductOutOfUF : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.GetComponent<MapComponent_UF>().thingsWithUFComp;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompUniversalFermenter compUniversalFermenter = t.TryGetComp<CompUniversalFermenter>();
            if (compUniversalFermenter != null && compUniversalFermenter.Finished && !t.IsBurning() && !t.IsForbidden(pawn))
            {
                return pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced);
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(UF_DefOf.TakeProductOutOfUniversalFermenter, t);
        }
    }
}
