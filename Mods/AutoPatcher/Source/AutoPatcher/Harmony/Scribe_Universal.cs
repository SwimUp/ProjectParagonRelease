using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace AutoPatcher.Harmony
{
    [HarmonyPatch(typeof(Scribe_Universal))]
    [HarmonyPatch(nameof(Scribe_Universal.TryResolveLookMode))]
    public static class Patch_Scribe_Universal_TryResolveLookMode
    {
        public static void Postfix(ref bool __result, Type type, ref LookMode lookMode)
        {
            if (!__result && typeof(MethodInfo).IsAssignableFrom(type))
            {
                lookMode = LookMode.Value;
                __result = true;
            }
        }
    }
}
