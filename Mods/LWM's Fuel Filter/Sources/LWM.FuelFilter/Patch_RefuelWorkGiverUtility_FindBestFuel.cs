using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LWM.FuelFilter
{
    [HarmonyPatch(typeof(RefuelWorkGiverUtility), "FindBestFuel")]
    public static class Patch_RefuelWorkGiverUtility_FindBestFuel
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> list = instructions.ToList();
            bool flag = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Callvirt && (MethodInfo)list[i].operand == AccessTools.Method(typeof(CompRefuelable), "get_Props"))
                {
                    flag = true;
                    list.RemoveAt(i - 1);
                    list.RemoveAt(i - 1);
                    list.RemoveAt(i - 1);
                    list.Insert(i - 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThingCompUtility), "TryGetComp", null, new Type[1] { typeof(Comp_FuelFilter) })));
                    list.Insert(i, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Comp_FuelFilter), "filter")));
                    break;
                }
            }
            if (!flag)
            {
                Log.Error("LWM.FuelFilter: could not find Harmony injection site");
            }
            foreach (CodeInstruction item in list)
            {
                yield return item;
            }
        }
    }
}
