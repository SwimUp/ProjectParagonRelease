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
    public class JobDriver_TakeProductOutOfUF : JobDriver
    {
        private const TargetIndex FermenterInd = TargetIndex.A;

        private const TargetIndex ProductToHaulInd = TargetIndex.B;

        private const TargetIndex StorageCellInd = TargetIndex.C;

        private const int Duration = 200;

        protected Thing Fermenter => job.GetTarget(TargetIndex.A).Thing;

        protected Thing Product => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return ReservationUtility.Reserve(pawn, (LocalTargetInfo)Fermenter, job, 1, -1, (ReservationLayerDef)null, true);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompUniversalFermenter comp = Fermenter.TryGetComp<CompUniversalFermenter>();
            this.FailOn(() => !comp.Finished);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1, -1, (ReservationLayerDef)null);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_General.Wait(200).FailOnDestroyedNullOrForbidden(TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Thing thing = comp.TakeOutProduct();
                GenPlace.TryPlaceThing(thing, pawn.Position, base.Map, ThingPlaceMode.Near);
                StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(thing);
                if (StoreUtility.TryFindBestBetterStoreCellFor(thing, pawn, base.Map, currentPriority, pawn.Faction, out var foundCell))
                {
                    job.SetTarget(TargetIndex.B, thing);
                    job.count = thing.stackCount;
                    job.SetTarget(TargetIndex.C, foundCell);
                }
                else
                {
                    EndJobWith(JobCondition.Ongoing | JobCondition.Succeeded);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            yield return Toils_Reserve.Reserve(TargetIndex.B, 1, -1, (ReservationLayerDef)null);
            yield return Toils_Reserve.Reserve(TargetIndex.C, 1, -1, (ReservationLayerDef)null);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true);
            Toil carry = Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
            yield return carry;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, carry, storageMode: true);
        }
    }
}
