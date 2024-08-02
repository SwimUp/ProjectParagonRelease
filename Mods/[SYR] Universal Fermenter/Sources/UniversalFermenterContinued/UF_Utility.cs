using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UniversalFermenter
{
    [StaticConstructorOnStartup]
    public static class UF_Utility
    {
        public static List<UF_Process> allUFProcesses;

        public static Dictionary<UF_Process, Command_Action> processGizmos;

        public static Dictionary<QualityCategory, Command_Action> qualityGizmos;

        public static Dictionary<UF_Process, Material> processMaterials;

        public static Dictionary<QualityCategory, Material> qualityMaterials;

        private static int gooseAngle;

        static UF_Utility()
        {
            allUFProcesses = new List<UF_Process>();
            processGizmos = new Dictionary<UF_Process, Command_Action>();
            qualityGizmos = new Dictionary<QualityCategory, Command_Action>();
            processMaterials = new Dictionary<UF_Process, Material>();
            qualityMaterials = new Dictionary<QualityCategory, Material>();
            gooseAngle = Rand.Range(0, 360);
            CheckForErrors();
            CacheAllProcesses();
            RecacheAll();
        }

        public static void CheckForErrors()
        {
            bool flag = false;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<-- Universal Fermenter Errors -->");
            foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp(typeof(CompUniversalFermenter))))
            {
                if (!(item.comps.Find((CompProperties c) => c.compClass == typeof(CompUniversalFermenter)) is CompProperties_UniversalFermenter compProperties_UniversalFermenter))
                {
                    continue;
                }
                if (!compProperties_UniversalFermenter.products.NullOrEmpty())
                {
                    stringBuilder.AppendLine("Universal Fermenter: ThingDef '" + item.defName + "' uses outdated field 'products', please rename to 'processes'.");
                    compProperties_UniversalFermenter.processes.AddRange(compProperties_UniversalFermenter.products);
                    flag = true;
                }
                if (compProperties_UniversalFermenter.processes.Any((UF_Process p) => p.thingDef == null || p.ingredientFilter.AllowedThingDefs.EnumerableNullOrEmpty()))
                {
                    stringBuilder.AppendLine("ThingDef '" + item.defName + "' has processes with no product or no filter. These fields are required.");
                    compProperties_UniversalFermenter.processes.RemoveAll((UF_Process p) => p.thingDef == null || p.ingredientFilter.AllowedThingDefs.EnumerableNullOrEmpty());
                    flag = true;
                }
            }
            if (flag)
            {
                Log.Warning(stringBuilder.ToString().TrimEndNewlines());
            }
        }

        public static void RecacheAll()
        {
            RecacheProcessGizmos();
            RecacheProcessMaterials();
            RecacheQualityGizmos();
        }

        private static void CacheAllProcesses()
        {
            List<UF_Process> list = new List<UF_Process>();
            foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp(typeof(CompUniversalFermenter))))
            {
                if (item.comps.Find((CompProperties c) => c.compClass == typeof(CompUniversalFermenter)) is CompProperties_UniversalFermenter compProperties_UniversalFermenter)
                {
                    list.AddRange(compProperties_UniversalFermenter.processes);
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                list[i].uniqueID = i;
                allUFProcesses.Add(list[i]);
            }
        }

        public static void RecacheProcessGizmos()
        {
            processGizmos.Clear();
            foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp(typeof(CompUniversalFermenter))))
            {
                if (!(item.comps.Find((CompProperties c) => c.compClass == typeof(CompUniversalFermenter)) is CompProperties_UniversalFermenter compProperties_UniversalFermenter))
                {
                    continue;
                }
                foreach (UF_Process process in compProperties_UniversalFermenter.processes)
                {
                    Command_Process command_Process = new Command_Process
                    {
                        defaultLabel = ((process.customLabel != "") ? process.customLabel : process.thingDef.label),
                        defaultDesc = "UF_NextDesc".Translate(process.thingDef.label, IngredientFilterSummary(process.ingredientFilter)),
                        icon = GetIcon(process.thingDef, UF_Settings.singleItemIcon),
                        processToTarget = process,
                        processOptions = compProperties_UniversalFermenter.processes
                    };
                    command_Process.action = delegate
                    {
                        FloatMenu window = new FloatMenu(command_Process.RightClickFloatMenuOptions.ToList())
                        {
                            vanishIfMouseDistant = true
                        };
                        Find.WindowStack.Add(window);
                    };
                    processGizmos.Add(process, command_Process);
                }
            }
        }

        public static void RecacheProcessMaterials()
        {
            processMaterials.Clear();
            foreach (UF_Process allUFProcess in allUFProcesses)
            {
                Material value = MaterialPool.MatFrom(GetIcon(allUFProcess.thingDef, UF_Settings.singleItemIcon));
                processMaterials.Add(allUFProcess, value);
            }
            qualityMaterials.Clear();
            foreach (QualityCategory value3 in Enum.GetValues(typeof(QualityCategory)))
            {
                Material value2 = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get("UI/QualityIcons/" + value3));
                qualityMaterials.Add(value3, value2);
            }
        }

        public static void RecacheQualityGizmos()
        {
            qualityGizmos.Clear();
            foreach (QualityCategory value in Enum.GetValues(typeof(QualityCategory)))
            {
                Command_Quality command_Quality = new Command_Quality
                {
                    defaultLabel = value.GetLabel().CapitalizeFirst(),
                    defaultDesc = "UF_SetQualityDesc".Translate(),
                    icon = (Texture2D)qualityMaterials[value].mainTexture,
                    qualityToTarget = value
                };
                command_Quality.action = delegate
                {
                    FloatMenu window = new FloatMenu(command_Quality.RightClickFloatMenuOptions.ToList())
                    {
                        vanishIfMouseDistant = true
                    };
                    Find.WindowStack.Add(window);
                };
                qualityGizmos.Add(value, command_Quality);
            }
        }

        public static Command_Action DebugGizmo()
        {
            return new Command_Action
            {
                defaultLabel = "Debug: Options",
                defaultDesc = "Opens a float menu with debug options.",
                icon = ContentFinder<Texture2D>.Get("UI/DebugGoose"),
                iconAngle = gooseAngle,
                iconDrawScale = 1.25f,
                action = delegate
                {
                    FloatMenu window = new FloatMenu(DebugOptions())
                    {
                        vanishIfMouseDistant = true
                    };
                    Find.WindowStack.Add(window);
                }
            };
        }

        public static List<FloatMenuOption> DebugOptions()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            IEnumerable<ThingWithComps> source = from t in Find.Selector.SelectedObjects.OfType<ThingWithComps>()
                                                 where t.GetComp<CompUniversalFermenter>() != null
                                                 select t;
            IEnumerable<CompUniversalFermenter> comps = source.Select((ThingWithComps t) => t.TryGetComp<CompUniversalFermenter>());
            if (comps.Any((CompUniversalFermenter c) => !c.Empty && !c.Finished))
            {
                list.Add(new FloatMenuOption("Finish process", delegate
                {
                    FinishProcess(comps);
                }));
                list.Add(new FloatMenuOption("Progress one day", delegate
                {
                    ProgressOneDay(comps);
                }));
                list.Add(new FloatMenuOption("Progress half quadrum", delegate
                {
                    ProgressHalfQuadrum(comps);
                }));
            }
            if (comps.Any((CompUniversalFermenter c) => c.Finished))
            {
                list.Add(new FloatMenuOption("Empty object", delegate
                {
                    EmptyObject(comps);
                }));
            }
            if (comps.Any((CompUniversalFermenter c) => c.Empty))
            {
                list.Add(new FloatMenuOption("Fill object", delegate
                {
                    FillObject(comps);
                }));
            }
            list.Add(new FloatMenuOption("Log speed factors", LogSpeedFactors));
            return list;
        }

        internal static void FinishProcess(IEnumerable<CompUniversalFermenter> comps)
        {
            foreach (CompUniversalFermenter comp in comps)
            {
                if (comp.CurrentProcess.usesQuality)
                {
                    comp.ProgressTicks = Mathf.RoundToInt(comp.DaysToReachTargetQuality * 60000f);
                }
                else
                {
                    comp.ProgressTicks = Mathf.RoundToInt(comp.CurrentProcess.processDays * 60000f);
                }
            }
            gooseAngle = Rand.Range(0, 360);
            UF_DefOf.UF_Honk.PlayOneShotOnCamera();
        }

        internal static void ProgressOneDay(IEnumerable<CompUniversalFermenter> comps)
        {
            foreach (CompUniversalFermenter comp in comps)
            {
                comp.ProgressTicks += 60000;
            }
            gooseAngle = Rand.Range(0, 360);
            UF_DefOf.UF_Honk.PlayOneShotOnCamera();
        }

        internal static void ProgressHalfQuadrum(IEnumerable<CompUniversalFermenter> comps)
        {
            foreach (CompUniversalFermenter comp in comps)
            {
                comp.ProgressTicks += 450000;
            }
            gooseAngle = Rand.Range(0, 360);
            UF_DefOf.UF_Honk.PlayOneShotOnCamera();
        }

        internal static void EmptyObject(IEnumerable<CompUniversalFermenter> comps)
        {
            foreach (CompUniversalFermenter comp in comps)
            {
                if (comp.Finished)
                {
                    GenPlace.TryPlaceThing(comp.TakeOutProduct(), comp.parent.Position, comp.parent.Map, ThingPlaceMode.Near);
                }
            }
            gooseAngle = Rand.Range(0, 360);
            UF_DefOf.UF_Honk.PlayOneShotOnCamera();
        }

        internal static void FillObject(IEnumerable<CompUniversalFermenter> comps)
        {
            foreach (CompUniversalFermenter comp in comps)
            {
                if (comp.Empty)
                {
                    Thing thing = ThingMaker.MakeThing(comp.CurrentProcess.ingredientFilter.AnyAllowedDef);
                    thing.stackCount = comp.SpaceLeftForIngredient;
                    comp.AddIngredient(thing);
                }
            }
            gooseAngle = Rand.Range(0, 360);
            UF_DefOf.UF_Honk.PlayOneShotOnCamera();
        }

        internal static void LogSpeedFactors()
        {
            foreach (Thing item in Find.Selector.SelectedObjects.OfType<Thing>())
            {
                CompUniversalFermenter compUniversalFermenter = item.TryGetComp<CompUniversalFermenter>();
                if (compUniversalFermenter != null)
                {
                    Log.Message(compUniversalFermenter.parent.ToString() + ": sun: " + compUniversalFermenter.CurrentSunFactor.ToStringPercent() + "| rain: " + compUniversalFermenter.CurrentRainFactor.ToStringPercent() + "| snow: " + compUniversalFermenter.CurrentSnowFactor.ToStringPercent() + "| wind: " + compUniversalFermenter.CurrentWindFactor.ToStringPercent() + "| roofed: " + compUniversalFermenter.RoofCoverage.ToStringPercent());
                }
            }
            gooseAngle = Rand.Range(0, 360);
            UF_DefOf.UF_Honk.PlayOneShotOnCamera();
        }

        public static string IngredientFilterSummary(ThingFilter thingFilter)
        {
            return thingFilter.Summary;
        }

        public static string VowelTrim(string str, int limit)
        {
            int num = str.Length - limit;
            int num2 = str.Length - 1;
            while (num2 > 0 && num > 0)
            {
                if (IsVowel(str[num2]) && str[num2 - 1] != ' ')
                {
                    str = str.Remove(num2, 1);
                    num--;
                }
                num2--;
            }
            if (str.Length > limit)
            {
                str = str.Remove(limit - 2) + "..";
            }
            return str;
        }

        public static bool IsVowel(char c)
        {
            return new HashSet<char> { 'a', 'e', 'i', 'o', 'u' }.Contains(c);
        }

        public static Texture2D GetIcon(ThingDef thingDef, bool singleStack = true)
        {
            Texture2D texture2D = ContentFinder<Texture2D>.Get(thingDef.graphicData.texPath, reportFailure: false);
            if (texture2D == null)
            {
                texture2D = (singleStack ? ContentFinder<Texture2D>.GetAllInFolder(thingDef.graphicData.texPath).FirstOrDefault() : ContentFinder<Texture2D>.GetAllInFolder(thingDef.graphicData.texPath).LastOrDefault());
                if (texture2D == null)
                {
                    texture2D = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport");
                    Log.Warning("Universal Fermenter:: No texture at " + thingDef.graphicData.texPath + ".");
                }
            }
            return texture2D;
        }
    }
}
