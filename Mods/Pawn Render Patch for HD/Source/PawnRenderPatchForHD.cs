using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace PawnRenderPatchForHD
{

    [StaticConstructorOnStartup]
    public class PawnRenderPatchForHDMain
    {
        static PawnRenderPatchForHDMain()
        {
            Log.Message("[PawnRenderPatchForHD] patched");
            var harmony = new Harmony("PawnRenderPatchForHD");
            harmony.PatchAll();
        }
    }


    [HarmonyPatch(typeof(CameraDriver), "Update")]
    [HarmonyAfter("net.pardeike.rimworld.mod.camera+")]
    public class CameraDriverPatch
    {
        public static void Postfix(CameraDriver __instance, float ___rootSize)
        {
            PawnRenderDistance.rootSize = ___rootSize;
        }
    }

    public static class PawnRenderDistance
    {
        //static FieldInfo rootSizeFieldInfo = AccessTools.Field(typeof(CameraDriver), "rootSize");

        public static float rootSize = 0;





        public static bool DrawWithNewMethod(Pawn pawn , bool originalResult, ref PawnRenderFlags renderFlags)
        {
            if (Find.CameraDriver == null)
                return originalResult;
            //if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
            //    return false;
            //float rootSize = (float)rootSizeFieldInfo.GetValue(Find.CameraDriver);
            if (rootSize < PawnRenderPatchForHDSetting.ZoomValue)
            {

                //renderFlags |= PawnRenderFlags.Clothes;
                //renderFlags |= PawnRenderFlags.Headgear;

                //renderFlags |= PawnRenderFlags.StylingStation; // ???

                return false;

            }

            return originalResult;


        }
    }

    [HarmonyPatch(typeof(PawnRenderer))]
    [HarmonyPatch("RenderPawnAt")]
    public class PawnRenderPatch_RenderPawnAt_Patch
    {
        //static FieldInfo pawnField = AccessTools.Field(typeof(Pawn), "pawn");
        //static MethodInfo m_MyExtraMethod = SymbolExtensions.GetMethodInfo(() => PawnRenderDistance.DrawWithOldMethod());
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Log.Message("PawnRenderPatch for HD :: starting patch");
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                //if (code.opcode == OpCodes.Ret)
                //    break;

                if (code.opcode == OpCodes.Ldfld && (FieldInfo)code.operand == AccessTools.Field(typeof(PawnRenderer), "pawn"))// flag (has pawnatlas)
                {

                    if (i +1 <  codes.Count && codes[i+1].opcode == OpCodes.Call && (MethodInfo)codes[i + 1].operand == AccessTools.Method(typeof(PawnUtility), "GetPosture"))
                    {

                        Log.Message("PawnRenderPatch for HD :: found method. patching...");


                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderer), "pawn"));
                        yield return new CodeInstruction(OpCodes.Ldloc_3);
                        yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PawnRenderDistance), "DrawWithNewMethod"));
                        yield return new CodeInstruction(OpCodes.Stloc_3);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);

                    }
                }
                    yield return code;

            }
            //return codes.AsEnumerable();
            Log.Message("PawnRenderPatch for HD :: patch completed");
            yield break;

        }


    }

}
