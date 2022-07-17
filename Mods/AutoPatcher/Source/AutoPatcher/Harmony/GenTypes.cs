using System;
using System.Collections.Generic;
using Verse;

namespace AutoPatcher.Harmony
{
    // Fix Rimworld generic Def issue related to DefDatabase creation.
    // To be run manually early in the mod launch to prevent issues.
    // [HarmonyPatch(typeof(PlayDataLoader))]
    // [HarmonyPatch("DoPlayLoad")]
    public static class Patch_GenTypes_AllSubclasses
    {
        public static IEnumerable<Type> Postfix(IEnumerable<Type> values, Type baseType)
        {
            bool flag = baseType == typeof(Def);
            foreach (Type type in values)
            {
                if (!flag || !type.IsGenericType)
                    yield return type;
            }
        }
    }
}
