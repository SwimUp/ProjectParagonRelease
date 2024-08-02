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
    public class JobDriver_FillUF : JobDriver
    {
        private const TargetIndex FermenterInd = TargetIndex.A;

        private const TargetIndex IngredientInd = TargetIndex.B;

        private const int Duration = 200;

        protected Thing Fermenter => job.GetTarget(TargetIndex.A).Thing;

        protected Thing Ingredient => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (ReservationUtility.Reserve(pawn, (LocalTargetInfo)Fermenter, job, 1, -1, (ReservationLayerDef)null, errorOnFailed))
            {
                return ReservationUtility.Reserve(pawn, (LocalTargetInfo)Ingredient, job, 1, -1, (ReservationLayerDef)null, errorOnFailed);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompUniversalFermenter comp = Fermenter.TryGetComp<CompUniversalFermenter>();
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            AddEndCondition(() => (comp.SpaceLeftForIngredient > 0) ? JobCondition.Ongoing : JobCondition.Succeeded);
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = comp.SpaceLeftForIngredient;
            });
            Toil reserveIngredient = Toils_Reserve.Reserve(TargetIndex.B, 1, -1, (ReservationLayerDef)null);
            yield return reserveIngredient;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false, true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIngredient, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(200, TargetIndex.A).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
                .WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    comp.AddIngredient(Ingredient);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
