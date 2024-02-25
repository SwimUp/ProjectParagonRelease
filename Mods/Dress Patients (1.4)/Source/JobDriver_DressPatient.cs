using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;
using Verse.AI;

namespace DressPatient
{
    class JobDriver_DressPatient : JobDriver
    {

        public bool TargetIsCorpse => TargetThingA as Corpse != null;

        public Pawn TargetPawn
        {
            get
            {
                if (targetPawn == null)
                {
                    if (TargetIsCorpse)
                    {
                        targetPawn = (TargetThingA as Corpse).InnerPawn;
                    } else
                    {
                        targetPawn = TargetThingA as Pawn;
                    }
                }
                return targetPawn;
            }
        }

        public Apparel Apparel
        {
            get
            {
                if (apparel == null)
                {
                    apparel = TargetThingB as Apparel;
                }
                return apparel;
            }
        }

        private Pawn targetPawn = null;
        private Apparel apparel = null;
        private int duration;
        private int unequipBuffer = 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", 0, false);
            Scribe_Values.Look(ref unequipBuffer, "unequipBuffer", 0, false);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Try to reserve patient.
            if (!pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed))
                return false;

            // Try to reserve apparel.
            if (!pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed))
                return false;

            return true;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();

            // Job duration based on equip time of target apparel.
            duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
            List<Apparel> wornApparel = TargetPawn.apparel.WornApparel;
            foreach (Apparel apparel in wornApparel)
            {
                if (!ApparelUtility.CanWearTogether(Apparel.def, apparel.def, TargetPawn.RaceProps.body))
                {
                    // Add equip time of all apparel that must be removed.
                    duration += (int)(apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
                }
            }
            job.count = 1;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            if (!TargetIsCorpse)
                this.FailOn(() => !TargetPawn.IsPatient());
            this.FailOnBurningImmobile(TargetIndex.B);

            // Fetch apparel and carry to patient.
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.A);
            yield return Toils_Haul.PlaceCarriedThingInCellFacing(TargetIndex.A);

            // Wait duration, strip conflicting clothes periodically.
            Toil stripAndDress = new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = duration,
                tickAction = delegate ()
                {
                    unequipBuffer++;
                    TryUnequipSomething();
                }
            };
            stripAndDress.WithProgressBarToilDelay(TargetIndex.A);
            stripAndDress.FailOnDespawnedOrNull(TargetIndex.A);
            if (!TargetIsCorpse)
                stripAndDress.FailOn(() => !TargetPawn.IsPatient());
            yield return stripAndDress;

            // Equip apparel.
            yield return Toils_General.Do(() => TargetPawn.apparel.Wear(Apparel));
            yield break;
        }

        private void TryUnequipSomething()
        {
            foreach (Apparel wornApparel in TargetPawn.apparel.WornApparel)
            {
                if (!ApparelUtility.CanWearTogether(Apparel.def, wornApparel.def, TargetPawn.RaceProps.body))
                {
                    int equipDelay = (int)(wornApparel.GetStatValue(StatDefOf.EquipDelay, true) * 60f);
                    if (unequipBuffer >= equipDelay)
                    {
                        if (!TargetPawn.apparel.TryDrop(wornApparel, out Apparel droppedApparel, TargetPawn.PositionHeld, false))
                        {
                            Log.Error(TargetPawn + " could not drop " + wornApparel.ToStringSafe());
                            EndJobWith(JobCondition.Errored);
                            return;
                        }
                        unequipBuffer -= equipDelay;
                    }
                    break;
                }
            }
        }
    }
}
