using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    [HarmonyPatch(typeof(Building_FermentingBarrel), "GetInspectString")]
    public class OldBarrel_GetInspectStringPatch
    {
        [HarmonyPrefix]
        public static bool OldBarrel_GetInspectString_Postfix(ref string __result)
        {
            __result = "UF_OldBarrelInspectString".Translate();
            return false;
        }
    }
}
