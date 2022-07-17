using AutoPatcher.Harmony;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AutoPatcher
{
    public sealed class AutoPatcher : Mod
    {
        public Settings settings;
        public static ModContentPack ourContentPack;
        public static HarmonyLib.Harmony harmony;
        public static AutoPatcher thisMod;
        public AutoPatcher(ModContentPack content) : base(content)
        {
            thisMod = this;
            // Early patch to avoid generic Def issues
            harmony = new HarmonyLib.Harmony("AutoPatcher");
            harmony.Patch(AccessTools.Method(typeof(GenTypes), "AllSubclasses"),
                postfix: new HarmonyMethod(typeof(Patch_GenTypes_AllSubclasses), "Postfix"));
        }
        public override string SettingsCategory() => "AutoPatcher".Translate();
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings = thisMod.settings ?? thisMod.GetSettings<Settings>();
            settings.DoWindowContents(inRect);
        }
    }
}
