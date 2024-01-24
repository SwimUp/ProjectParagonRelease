using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RocketMan.Patches
{
    [RocketPatch(typeof(Pawn_AbilityTracker), nameof(Pawn_AbilityTracker.ExposeData))]
    public static class Pawn_AbilityTracker_ExposeData_Patch
    {
        public static Exception Finalizer(Pawn_AbilityTracker __instance, Exception __exception)
        {
            if (__exception != null && __instance.abilities == null)
            {
                Log.Warning($"RocketMan: Fixed loading Pawn_AbilityTracker for {__instance.pawn}");
                __instance.abilities               = new List<Ability>();
                __instance.allAbilitiesCachedDirty = true;
            }
            return null;
        }
    }
    
    [RocketStartupPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class Map_FinalizeInit_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(Map __instance)
        {
            Main.MapLoaded(__instance);
        }
    }

    [RocketStartupPatch(typeof(Map), nameof(Map.ConstructComponents))]
    public static class Map_ConstructComponents_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(Map __instance)
        {
            Main.MapComponentsInitializing(__instance);
        }
    }
}
