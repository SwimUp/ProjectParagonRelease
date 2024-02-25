using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DressPatient
{
    [StaticConstructorOnStartup]
    static class Order_DressPatientOrCorpse
    {

        static Order_DressPatientOrCorpse()
        {
            Harmony harmony = new Harmony("eagle0600.dressPatients");
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), prefix: null,
                postfix: new HarmonyMethod(typeof(Order_DressPatientOrCorpse), nameof(DressPatientFloatMenuOption)));
        }

        public static TargetingParameters TargetParametersBody
        {
            get
            {
                if (targetParametersBody == null)
                {
                    targetParametersBody = new TargetingParameters()
                    {
                        canTargetPawns = true,
                        canTargetItems = true,
                        mapObjectTargetsMustBeAutoAttackable = false,
                        validator = ((TargetInfo target) => {
                            if (!target.HasThing)
                                return false;

                            if (target.Thing is Pawn pawn)
                            {
                                return pawn.apparel != null && pawn.IsPatient();
                            }
                            else
                            {
                                Corpse corpse = target.Thing as Corpse;
                                if (corpse == null)
                                    return false;
                                pawn = corpse.InnerPawn;
                                if (pawn == null)
                                    return false;
                                return pawn.apparel != null;
                            }
                        })
                    };
                }
                return targetParametersBody;
            }
        }

        private static TargetingParameters targetParametersBody = null;

        public static TargetingParameters TargetParemetersApparel(LocalTargetInfo targetBody)
        {
            return new TargetingParameters()
            {
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = ((TargetInfo target) =>
                {
                    if (!target.HasThing)
                        return false;
                    Apparel apparel = target.Thing as Apparel;
                    if (apparel == null)
                        return false;
                    if (!targetBody.HasThing)
                    {
                        Log.Error("Attempted to find apparel to dress nonexistent body.");
                        return false;
                    }
                    Pawn targetPawn = targetBody.Thing as Pawn;
                    if (targetBody.Thing is Corpse targetCorpse)
                        targetPawn = targetCorpse.InnerPawn;
                    if (targetPawn == null)
                    {
                        Log.Error("Attempted to find apparel to dress nonexistent body.");
                        return false;
                    }
                    if (!ApparelUtility.HasPartsToWear(targetPawn, apparel.def))
                        return false;

                    // If using Humanoid Alien Races 2.0, ensure that the target pawn's race can wear the apparel.
                    // Otherwise, default to true, and try not to generate any errors.
                    if (DressPatientUtility.usingHumanoidAlienRaces)
                    {
                        try
                        {
                            bool canWear = true;
                            ((Action)(() =>
                            {
                                bool CheckRaceCanWear()
                                {
                                    return AlienRace.RaceRestrictionSettings.CanWear(apparel.def, targetPawn.def);
                                }
                                if (!CheckRaceCanWear())
                                    canWear = false;
                            }))();
                            if (!canWear)
                                return false;
                        }
                        catch (TypeLoadException) { }
                    }
                    return true;
                })
            };
        }

        private static void DressPatientFloatMenuOption(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            // If the pawn in question cannot take jobs, don't bother.
            if (pawn.jobs == null)
                return;

            // Find valid patients.
            foreach (LocalTargetInfo targetBody in GenUI.TargetsAt(clickPos, TargetParametersBody))
            {

                // Ensure target is reachable.
                if (!pawn.CanReach(targetBody, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    //option = new FloatMenuOption("CannotDress".Translate(targetBody.Thing.LabelCap, targetBody.Thing) + " (" + "NoPath".Translate() + ")", null);
                    continue;
                }

                // Add menu option to dress patient. User will be asked to select a target.
                FloatMenuOption option = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("DressOther".Translate(targetBody.Thing.LabelCap, targetBody.Thing), delegate()
                {
                    Find.Targeter.BeginTargeting(TargetParemetersApparel(targetBody), (LocalTargetInfo targetApparel) => {
                        targetApparel.Thing.SetForbidden(false);
                        pawn.jobs.TryTakeOrderedJob(new Job(DefDatabase<JobDef>.GetNamed("DressPatient"), targetBody, targetApparel));
                    });
                }, MenuOptionPriority.High), pawn, targetBody);
                opts.Add(option);
            }
        }
    }
}
