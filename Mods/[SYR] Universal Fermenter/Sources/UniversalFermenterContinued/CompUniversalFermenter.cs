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
    public class CompUniversalFermenter : ThingComp
    {
        public int progressTicks;

        public float ruinedPercent;

        private Material barFilledCachedMat;

        public int currentProcessIndex;

        public int queuedProcessIndex;

        public QualityCategory targetQuality = QualityCategory.Normal;

        private int ingredientCount;

        private List<string> ingredientLabels = new List<string>();

        public List<ThingDef> inputIngredients = new List<ThingDef>();

        public bool graphicChangeQueued;

        public CompRefuelable refuelComp;

        public CompPowerTrader powerTradeComp;

        public CompFlickable flickComp;

        public CompProperties_UniversalFermenter Props => (CompProperties_UniversalFermenter)props;

        public bool Ruined => ruinedPercent >= 1f;

        public bool Empty => ingredientCount <= 0;

        public bool Finished
        {
            get
            {
                if (!Empty)
                {
                    return ProgressPercent >= 1f;
                }
                return false;
            }
        }

        public int SpaceLeftForIngredient
        {
            get
            {
                if (!Finished)
                {
                    return CurrentProcess.maxCapacity - ingredientCount;
                }
                return 0;
            }
        }

        public int ProgressTicks
        {
            get
            {
                return progressTicks;
            }
            set
            {
                if (value != progressTicks)
                {
                    progressTicks = value;
                    barFilledCachedMat = null;
                }
            }
        }

        public float ProgressDays => (float)ProgressTicks / 60000f;

        public float ProgressPercent
        {
            get
            {
                if (CurrentProcess.usesQuality)
                {
                    return ProgressDays / DaysToReachTargetQuality;
                }
                return ProgressDays / CurrentProcess.processDays;
            }
        }

        public UF_Process CurrentProcess
        {
            get
            {
                return Props.processes[currentProcessIndex];
            }
            set
            {
                if (!Props.processes.Contains(value))
                {
                    return;
                }
                if (Empty)
                {
                    currentProcessIndex = Props.processes.IndexOf(value);
                    if (!value.usesQuality)
                    {
                        TargetQuality = QualityCategory.Normal;
                    }
                    if (value.colorCoded)
                    {
                        parent.Notify_ColorChanged();
                    }
                }
                queuedProcessIndex = Props.processes.IndexOf(value);
            }
        }

        public int EstimatedTicksLeft
        {
            get
            {
                if (CurrentSpeedFactor <= 0f)
                {
                    return -1;
                }
                if (CurrentProcess.usesQuality)
                {
                    return Mathf.Max(Mathf.RoundToInt(DaysToReachTargetQuality * 60000f - (float)ProgressTicks), 0);
                }
                return Mathf.Max(Mathf.RoundToInt(CurrentProcess.processDays * 60000f - (float)ProgressTicks), 0);
            }
        }

        public QualityCategory TargetQuality
        {
            get
            {
                return targetQuality;
            }
            set
            {
                if (value != targetQuality)
                {
                    targetQuality = value;
                    barFilledCachedMat = null;
                }
            }
        }

        public QualityCategory CurrentQuality
        {
            get
            {
                if (ProgressDays < CurrentProcess.qualityDays.poor)
                {
                    return QualityCategory.Awful;
                }
                if (ProgressDays < CurrentProcess.qualityDays.normal)
                {
                    return QualityCategory.Poor;
                }
                if (ProgressDays < CurrentProcess.qualityDays.good)
                {
                    return QualityCategory.Normal;
                }
                if (ProgressDays < CurrentProcess.qualityDays.excellent)
                {
                    return QualityCategory.Good;
                }
                if (ProgressDays < CurrentProcess.qualityDays.masterwork)
                {
                    return QualityCategory.Excellent;
                }
                if (ProgressDays < CurrentProcess.qualityDays.legendary)
                {
                    return QualityCategory.Masterwork;
                }
                if (ProgressDays >= CurrentProcess.qualityDays.legendary)
                {
                    return QualityCategory.Legendary;
                }
                return QualityCategory.Normal;
            }
        }

        public float DaysToReachTargetQuality
        {
            get
            {
                if (targetQuality == QualityCategory.Awful)
                {
                    return CurrentProcess.qualityDays.awful;
                }
                else if (targetQuality == QualityCategory.Poor)
                {
                    return CurrentProcess.qualityDays.poor;
                }
                else if (targetQuality == QualityCategory.Normal)
                {
                    return CurrentProcess.qualityDays.normal;
                }
                else if (targetQuality == QualityCategory.Good)
                {
                    return CurrentProcess.qualityDays.good;
                }
                else if (targetQuality == QualityCategory.Excellent)
                {
                    return CurrentProcess.qualityDays.excellent;
                }
                else if (targetQuality == QualityCategory.Masterwork)
                {
                    return CurrentProcess.qualityDays.masterwork;
                }
                else if (targetQuality == QualityCategory.Legendary)
                {
                    return CurrentProcess.qualityDays.legendary;
                }
                return CurrentProcess.qualityDays.normal;
            }
        }

        private Material BarFilledMat
        {
            get
            {
                if (barFilledCachedMat == null)
                {
                    barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(Static_Bar.ZeroProgressColor, Static_Bar.FermentedColor, ProgressPercent));
                }
                return barFilledCachedMat;
            }
        }

        public float CurrentSpeedFactor => Mathf.Max(CurrentTemperatureFactor * CurrentSunFactor * CurrentRainFactor * CurrentSnowFactor * CurrentWindFactor, 0f);

        private float CurrentTemperatureFactor
        {
            get
            {
                if (!CurrentProcess.usesTemperature)
                {
                    return 1f;
                }
                float ambientTemperature = parent.AmbientTemperature;
                if (ambientTemperature < CurrentProcess.temperatureSafe.min)
                {
                    return CurrentProcess.speedBelowSafe;
                }
                if (ambientTemperature > CurrentProcess.temperatureSafe.max)
                {
                    return CurrentProcess.speedAboveSafe;
                }
                if (ambientTemperature < CurrentProcess.temperatureIdeal.min)
                {
                    return GenMath.LerpDouble(CurrentProcess.temperatureSafe.min, CurrentProcess.temperatureIdeal.min, CurrentProcess.speedBelowSafe, 1f, ambientTemperature);
                }
                if (ambientTemperature > CurrentProcess.temperatureIdeal.max)
                {
                    return GenMath.LerpDouble(CurrentProcess.temperatureIdeal.max, CurrentProcess.temperatureSafe.max, 1f, CurrentProcess.speedAboveSafe, ambientTemperature);
                }
                return 1f;
            }
        }

        public float CurrentSunFactor
        {
            get
            {
                if (parent.Map == null)
                {
                    return 0f;
                }
                if (CurrentProcess.sunFactor.Span == 0f)
                {
                    return 1f;
                }
                float x = parent.Map.skyManager.CurSkyGlow * (1f - RoofCoverage);
                return GenMath.LerpDouble(Static_Weather.SunGlowRange.TrueMin, Static_Weather.SunGlowRange.TrueMax, CurrentProcess.sunFactor.min, CurrentProcess.sunFactor.max, x);
            }
        }

        public float CurrentRainFactor
        {
            get
            {
                if (parent.Map == null)
                {
                    return 0f;
                }
                if (CurrentProcess.rainFactor.Span == 0f)
                {
                    return 1f;
                }
                if (parent.Map.weatherManager.SnowRate != 0f)
                {
                    return CurrentProcess.rainFactor.min;
                }
                float x = parent.Map.weatherManager.RainRate * (1f - RoofCoverage);
                return GenMath.LerpDouble(Static_Weather.RainRateRange.TrueMin, Static_Weather.RainRateRange.TrueMax, CurrentProcess.rainFactor.min, CurrentProcess.rainFactor.max, x);
            }
        }

        public float CurrentSnowFactor
        {
            get
            {
                if (parent.Map == null)
                {
                    return 0f;
                }
                if (CurrentProcess.snowFactor.Span == 0f)
                {
                    return 1f;
                }
                float x = parent.Map.weatherManager.SnowRate * (1f - RoofCoverage);
                return GenMath.LerpDouble(Static_Weather.SnowRateRange.TrueMin, Static_Weather.SnowRateRange.TrueMax, CurrentProcess.snowFactor.min, CurrentProcess.snowFactor.max, x);
            }
        }

        public float CurrentWindFactor
        {
            get
            {
                if (parent.Map == null)
                {
                    return 0f;
                }
                if (CurrentProcess.windFactor.Span == 0f)
                {
                    return 1f;
                }
                if (RoofCoverage != 0f)
                {
                    return CurrentProcess.windFactor.min;
                }
                return GenMath.LerpDouble(Static_Weather.WindSpeedRange.TrueMin, Static_Weather.WindSpeedRange.TrueMax, CurrentProcess.windFactor.min, CurrentProcess.windFactor.max, parent.Map.windManager.WindSpeed);
            }
        }

        public float RoofCoverage
        {
            get
            {
                if (parent.Map == null)
                {
                    return 0f;
                }
                int num = 0;
                int num2 = 0;
                foreach (IntVec3 item in parent.OccupiedRect())
                {
                    num++;
                    if (parent.Map.roofGrid.Roofed(item))
                    {
                        num2++;
                    }
                }
                return (float)num2 / (float)num;
            }
        }

        public string SummaryAddedIngredients
        {
            get
            {
                string text = "";
                if (ingredientLabels.Count > 0)
                {
                    for (int i = 0; i < ingredientLabels.Count; i++)
                    {
                        text = ((i != 0) ? (text + ", " + ingredientLabels[i]) : (text + ingredientLabels[i]));
                    }
                }
                else
                {
                    text += CurrentProcess.ingredientFilter.Summary;
                }
                int num = 60;
                int length = ("Contains " + CurrentProcess.maxCapacity + "/" + CurrentProcess.maxCapacity + " ").Length;
                int limit = num - length;
                return UF_Utility.VowelTrim(text, limit);
            }
        }

        public bool Fueled
        {
            get
            {
                if (refuelComp != null)
                {
                    return refuelComp.HasFuel;
                }
                return true;
            }
        }

        public bool Powered
        {
            get
            {
                if (powerTradeComp != null)
                {
                    return powerTradeComp.PowerOn;
                }
                return true;
            }
        }

        public bool FlickedOn
        {
            get
            {
                if (flickComp != null)
                {
                    return flickComp.SwitchIsOn;
                }
                return true;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            refuelComp = parent.GetComp<CompRefuelable>();
            powerTradeComp = parent.GetComp<CompPowerTrader>();
            flickComp = parent.GetComp<CompFlickable>();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<MapComponent_UF>().Register(parent);
            if (!Empty)
            {
                graphicChangeQueued = true;
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            map.GetComponent<MapComponent_UF>().Deregister(parent);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref ruinedPercent, "ruinedPercent", 0f);
            Scribe_Values.Look(ref ingredientCount, "UF_UniversalFermenter_IngredientCount", 0);
            Scribe_Values.Look(ref progressTicks, "UF_progressTicks", 0);
            Scribe_Values.Look(ref currentProcessIndex, "UF_currentResourceInd", 0);
            Scribe_Values.Look(ref queuedProcessIndex, "UF_queuedProcessIndex", 0);
            Scribe_Values.Look(ref targetQuality, "targetQuality", QualityCategory.Normal);
            Scribe_Collections.Look(ref ingredientLabels, "UF_ingredientLabels", LookMode.Undefined);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode)
            {
                yield return UF_Utility.DebugGizmo();
            }
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (Props.processes.Count > 1)
            {
                yield return UF_Utility.processGizmos[CurrentProcess];
            }
            if (CurrentProcess.usesQuality)
            {
                yield return UF_Utility.qualityGizmos[TargetQuality];
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (!Empty)
            {
                if (graphicChangeQueued)
                {
                    GraphicChange(toEmpty: false);
                    graphicChangeQueued = false;
                }
                bool flag = CurrentProcess.usesQuality && UF_Settings.showCurrentQualityIcon;
                Vector3 drawPos = parent.DrawPos;
                drawPos.x += Props.barOffset.x - (flag ? 0.1f : 0f);
                drawPos.y += 0.05f;
                drawPos.z += Props.barOffset.y;
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = drawPos;
                r.size = Static_Bar.Size * Props.barScale;
                r.fillPercent = (float)ingredientCount / (float)CurrentProcess.maxCapacity;
                r.filledMat = BarFilledMat;
                r.unfilledMat = Static_Bar.UnfilledMat;
                r.margin = 0.1f;
                r.rotation = Rot4.North;
                GenDraw.DrawFillableBar(r);
                if (flag)
                {
                    drawPos.y += 0.02f;
                    drawPos.x += 0.45f * Props.barScale.x;
                    Matrix4x4 matrix = default(Matrix4x4);
                    matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(0.2f * Props.barScale.x, 1f, 0.2f * Props.barScale.y));
                    Graphics.DrawMesh(MeshPool.plane10, matrix, UF_Utility.qualityMaterials[CurrentQuality], 0);
                }
            }
            if (CurrentProcess == null || !UF_Settings.showProcessIconGlobal || !Props.showProductIcon)
            {
                return;
            }
            Vector3 drawPos2 = parent.DrawPos;
            float num = UF_Settings.processIconSize * Props.productIconSize.x;
            float num2 = UF_Settings.processIconSize * Props.productIconSize.y;
            if (Props.processes.Count == 1 && CurrentProcess.usesQuality)
            {
                drawPos2.y += 0.02f;
                drawPos2.z += 0.05f;
                Matrix4x4 matrix2 = default(Matrix4x4);
                matrix2.SetTRS(drawPos2, Quaternion.identity, new Vector3(0.6f * num, 1f, 0.6f * num2));
                Graphics.DrawMesh(MeshPool.plane10, matrix2, UF_Utility.qualityMaterials[TargetQuality], 0);
            }
            else if (Props.processes.Count > 1)
            {
                drawPos2.y += 0.02f;
                drawPos2.z += 0.05f;
                Matrix4x4 matrix3 = default(Matrix4x4);
                matrix3.SetTRS(drawPos2, Quaternion.identity, new Vector3(num, 1f, num2));
                Graphics.DrawMesh(MeshPool.plane10, matrix3, UF_Utility.processMaterials[CurrentProcess], 0);
                if (CurrentProcess.usesQuality && UF_Settings.showTargetQualityIcon)
                {
                    drawPos2.y += 0.01f;
                    drawPos2.x += 0.25f * num;
                    drawPos2.z -= 0.35f * num2;
                    Matrix4x4 matrix4 = default(Matrix4x4);
                    matrix4.SetTRS(drawPos2, Quaternion.identity, new Vector3(0.4f * num, 1f, 0.4f * num2));
                    Graphics.DrawMesh(MeshPool.plane10, matrix4, UF_Utility.qualityMaterials[TargetQuality], 0);
                }
            }
        }

        public override void PreAbsorbStack(Thing otherStack, int count)
        {
            float t = (float)count / (float)(parent.stackCount + count);
            CompUniversalFermenter comp = ((ThingWithComps)otherStack).GetComp<CompUniversalFermenter>();
            ruinedPercent = Mathf.Lerp(ruinedPercent, comp.ruinedPercent, t);
        }

        public override bool AllowStackWith(Thing other)
        {
            CompUniversalFermenter comp = ((ThingWithComps)other).GetComp<CompUniversalFermenter>();
            return Ruined == comp.Ruined;
        }

        public override void PostSplitOff(Thing piece)
        {
            ((ThingWithComps)piece).GetComp<CompUniversalFermenter>().ruinedPercent = ruinedPercent;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (CurrentProcess.usesTemperature)
            {
                stringBuilder.AppendLine(StatusInfo());
            }
            if (!Ruined)
            {
                if (CurrentProcess.usesQuality && ProgressDays >= CurrentProcess.qualityDays.awful)
                {
                    stringBuilder.AppendLine("UF_ContainsProduct".Translate(ingredientCount, CurrentProcess.maxCapacity, CurrentProcess.thingDef.label) + " (" + CurrentQuality.GetLabel().ToLower() + ")");
                }
                else if (Finished)
                {
                    stringBuilder.AppendLine("UF_ContainsProduct".Translate(ingredientCount, CurrentProcess.maxCapacity, CurrentProcess.thingDef.label));
                }
                else
                {
                    stringBuilder.AppendLine("UF_ContainsIngredient".Translate(ingredientCount, CurrentProcess.maxCapacity, SummaryAddedIngredients));
                }
            }
            if (!Empty)
            {
                if (Finished)
                {
                    stringBuilder.AppendLine("UF_Finished".Translate());
                }
                else if (parent.Map != null)
                {
                    stringBuilder.AppendLine("UF_Progress".Translate(ProgressPercent.ToStringPercent(), TimeLeft()));
                    if (CurrentSpeedFactor != 1f)
                    {
                        if (CurrentSpeedFactor < 1f)
                        {
                            stringBuilder.Append("UF_NonIdealInfluences".Translate(WhatsWrong())).Append(" ").AppendLine("UF_NonIdealSpeedFactor".Translate(CurrentSpeedFactor.ToStringPercent()));
                        }
                        else
                        {
                            stringBuilder.AppendLine("UF_NonIdealSpeedFactor".Translate(CurrentSpeedFactor.ToStringPercent()));
                        }
                    }
                }
            }
            if (CurrentProcess.usesTemperature)
            {
                stringBuilder.AppendLine(string.Concat("UF_IdealSafeProductionTemperature".Translate(), ": ", CurrentProcess.temperatureIdeal.min.ToStringTemperature("F0"), "~", CurrentProcess.temperatureIdeal.max.ToStringTemperature("F0"), " (", CurrentProcess.temperatureSafe.min.ToStringTemperature("F0"), "~", CurrentProcess.temperatureSafe.max.ToStringTemperature("F0"), ")"));
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void CompTick()
        {
            base.CompTick();
            DoTicks(1);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            DoTicks(250);
        }

        public void DoTicks(int ticks)
        {
            if (!Empty && Fueled && Powered && FlickedOn)
            {
                ProgressTicks += Mathf.RoundToInt((float)ticks * CurrentSpeedFactor);
            }
            if (Ruined)
            {
                return;
            }
            if (!Empty)
            {
                float ambientTemperature = parent.AmbientTemperature;
                if (ambientTemperature > CurrentProcess.temperatureSafe.max)
                {
                    ruinedPercent += (ambientTemperature - CurrentProcess.temperatureSafe.max) * (CurrentProcess.ruinedPerDegreePerHour / 250000f) * (float)ticks;
                }
                else if (ambientTemperature < CurrentProcess.temperatureSafe.min)
                {
                    ruinedPercent -= (ambientTemperature - CurrentProcess.temperatureSafe.min) * (CurrentProcess.ruinedPerDegreePerHour / 250000f) * (float)ticks;
                }
            }
            if (ruinedPercent >= 1f)
            {
                ruinedPercent = 1f;
                parent.BroadcastCompSignal("RuinedByTemperature");
                Reset();
            }
            else if (ruinedPercent < 0f)
            {
                ruinedPercent = 0f;
            }
        }

        public void AddIngredient(Thing ingredient)
        {
            if (!ingredientLabels.Contains(ingredient.def.label))
            {
                ingredientLabels.Add(ingredient.def.label);
            }
            CompIngredients compIngredients = ingredient.TryGetComp<CompIngredients>();
            if (compIngredients != null)
            {
                inputIngredients.AddRange(compIngredients.ingredients);
            }
            if (Mathf.Min(ingredient.stackCount, CurrentProcess.maxCapacity - ingredientCount) > 0)
            {
                AddIngredient(ingredient.stackCount);
                ingredient.Destroy();
            }
        }

        public void AddIngredient(int count)
        {
            ruinedPercent = 0f;
            if (Finished)
            {
                Log.Warning("Universal Fermenter:: Tried to add ingredient to a fermenter full of product. Colonists should take the product first.");
                return;
            }
            int num = Mathf.Min(count, CurrentProcess.maxCapacity - ingredientCount);
            if (num > 0)
            {
                ProgressTicks = Mathf.RoundToInt(GenMath.WeightedAverage(0f, num, ProgressTicks, ingredientCount));
                if (Empty)
                {
                    GraphicChange(toEmpty: false);
                }
                ingredientCount += num;
            }
        }

        public Thing TakeOutProduct()
        {
            if (!Finished && !CurrentProcess.usesQuality)
            {
                Log.Warning("Universal Fermenter: Tried to get product but it's not yet fermented.");
                return null;
            }
            if (CurrentProcess.usesQuality && (int)CurrentQuality < (int)TargetQuality)
            {
                Log.Warning("Universal Fermenter: Tried to get product but it has not reached target quality.");
                return null;
            }
            Thing thing = ThingMaker.MakeThing(CurrentProcess.thingDef);
            CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
            if (compIngredients != null && !inputIngredients.NullOrEmpty())
            {
                compIngredients.ingredients.AddRange(inputIngredients);
            }
            if (CurrentProcess.usesQuality)
            {
                thing.TryGetComp<CompQuality>()?.SetQuality(CurrentQuality, ArtGenerationContext.Colony);
            }
            thing.stackCount = Mathf.RoundToInt((float)ingredientCount * CurrentProcess.efficiency);
            Reset();
            return thing;
        }

        public void Reset()
        {
            ingredientCount = 0;
            ProgressTicks = 0;
            inputIngredients.Clear();
            ingredientLabels.Clear();
            GraphicChange(toEmpty: true);
            CurrentProcess = Props.processes[queuedProcessIndex];
        }

        public void GraphicChange(bool toEmpty)
        {
            if (CurrentProcess.graphicSuffix != null)
            {
                string text = parent.def.graphicData.texPath;
                if (!toEmpty)
                {
                    text += CurrentProcess.graphicSuffix;
                }
                TexReloader.Reload(parent, text);
            }
        }

        public string TimeLeft()
        {
            if (EstimatedTicksLeft >= 0)
            {
                return EstimatedTicksLeft.ToStringTicksToPeriod() + " left";
            }
            return "stopped";
        }

        public string WhatsWrong()
        {
            if (CurrentSpeedFactor < 1f)
            {
                List<string> list = new List<string>();
                if (CurrentTemperatureFactor < 1f)
                {
                    list.Add("UF_WeatherTemperature".Translate());
                }
                if (CurrentSunFactor < 1f)
                {
                    list.Add("UF_WeatherSunshine".Translate());
                }
                if (CurrentRainFactor < 1f)
                {
                    list.Add("UF_WeatherRain".Translate());
                }
                if (CurrentSnowFactor < 1f)
                {
                    list.Add("UF_WeatherSnow".Translate());
                }
                if (CurrentWindFactor < 1f)
                {
                    list.Add("UF_WeatherWind".Translate());
                }
                return string.Join(", ", list.ToArray());
            }
            return "nothing";
        }

        public string StatusInfo()
        {
            if (Ruined)
            {
                return "RuinedByTemperature".Translate();
            }
            float ambientTemperature = parent.AmbientTemperature;
            string text = null;
            string text2 = "Temperature".Translate() + ": " + ambientTemperature.ToStringTemperature("F0");
            if (!Empty)
            {
                if (CurrentProcess.temperatureSafe.Includes(ambientTemperature))
                {
                    text = ((!CurrentProcess.temperatureIdeal.Includes(ambientTemperature)) ? ((string)"UF_Safe".Translate()) : ((string)"UF_Ideal".Translate()));
                }
                else if (ruinedPercent > 0f)
                {
                    text = ((!(ambientTemperature < CurrentProcess.temperatureSafe.min)) ? ((string)"Overheating".Translate()) : ((string)"Freezing".Translate()));
                    text = text + " " + ruinedPercent.ToStringPercent();
                }
            }
            if (text == null)
            {
                return text2;
            }
            return text2 + " (" + text + ")";
        }
    }
}
