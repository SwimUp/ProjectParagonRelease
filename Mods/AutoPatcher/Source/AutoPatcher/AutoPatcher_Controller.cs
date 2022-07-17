using AutoPatcher.Utility;
using HarmonyLib;
using HugsLib;
using System.Linq;
using Verse;

namespace AutoPatcher
{
    internal class Controller : ModBase
    {
        public static bool fromSave = false;
        public override void DefsLoaded()
        {
            // Run patch trees
            Settings settings;
            try
            {
                settings = AutoPatcher.thisMod.settings ?? AutoPatcher.thisMod.GetSettings<Settings>();
                AutoPatcher.thisMod.settings = settings;
            }
            catch
            {
                Log.Warning($"[[LC]AutoPatcher] TempWarning 575646: could not load settings");
                settings = new Settings();
                fromSave = false;
            }
            var currMods = ModsConfig.ActiveModsInLoadOrder.Select(t => t.PackageId);
            if (fromSave)
            {
                var savedMods = settings.modList;
                if (savedMods.EnumerableNullOrEmpty() || savedMods.SetEquals(currMods))
                    fromSave = false;
            }
            var patchTrees = DefDatabase<PatchTreeDef>.AllDefs;
            NodeUtility.allNodeDefs.Do(t => t.Initialize(fromSave));
            foreach (var patchTree in patchTrees)
            {
                patchTree.Initialize(fromSave);
                patchTree.Perform();
            }
            settings.modList = currMods.ToHashSet();
            settings.Write();
        }
    }
}
