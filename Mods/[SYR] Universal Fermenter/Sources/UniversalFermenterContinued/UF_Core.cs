using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class UF_Core : Mod
    {
        public static UF_Settings settings;

        public static FieldInfo cachedGraphic = typeof(MinifiedThing).GetField("cachedGraphic", BindingFlags.Instance | BindingFlags.NonPublic);

        public UF_Core(ModContentPack content)
            : base(content)
        {
            settings = GetSettings<UF_Settings>();
        }

        public override string SettingsCategory()
        {
            return "UF_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.CheckboxLabeled("UF_ShowProcessIcon".Translate(), ref UF_Settings.showProcessIconGlobal, "UF_ShowProcessIconTooltip".Translate());
            listing_Standard.Gap();
            listing_Standard.Label("UF_ProcessIconSize".Translate() + ": " + UF_Settings.processIconSize.ToStringByStyle(ToStringStyle.PercentZero), -1f, "UF_ProcessIconSizeTooltip".Translate());
            UF_Settings.processIconSize = listing_Standard.Slider(GenMath.RoundTo(UF_Settings.processIconSize, 0.05f), 0.2f, 1f);
            listing_Standard.CheckboxLabeled("UF_SingleItemIcon".Translate(), ref UF_Settings.singleItemIcon, "UF_SingleItemIconTooltip".Translate());
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("UF_SortAlphabetically".Translate(), ref UF_Settings.sortAlphabetically, "UF_SortAlphabeticallyTooltip".Translate());
            listing_Standard.GapLine(30f);
            listing_Standard.CheckboxLabeled("UF_ShowCurrentQualityIcon".Translate(), ref UF_Settings.showCurrentQualityIcon, "UF_ShowCurrentQualityIconTooltip".Translate());
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("UF_ShowTargetQualityIcon".Translate(), ref UF_Settings.showTargetQualityIcon, "UF_ShowTargetQualityTooltip".Translate());
            listing_Standard.GapLine(30f);
            Rect rect = listing_Standard.GetRect(30f);
            TooltipHandler.TipRegion(rect, "UF_ReplaceVanillaBarrelsTooltip".Translate());
            if (Widgets.ButtonText(rect, "UF_ReplaceVanillaBarrels".Translate()))
            {
                ReplaceVanillaBarrels();
            }
            listing_Standard.GapLine(30f);
            Rect rect2 = listing_Standard.GetRect(30f);
            TooltipHandler.TipRegion(rect2, "UF_DefaultSettingsTooltip".Translate());
            if (Widgets.ButtonText(rect2, "UF_DefaultSettings".Translate()))
            {
                UF_Settings.showProcessIconGlobal = true;
                UF_Settings.processIconSize = 0.6f;
                UF_Settings.singleItemIcon = true;
                UF_Settings.sortAlphabetically = false;
                UF_Settings.showCurrentQualityIcon = true;
                UF_Settings.showTargetQualityIcon = false;
            }
            listing_Standard.End();
            settings.Write();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            UF_Utility.RecacheAll();
        }

        public void ReplaceVanillaBarrels()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            foreach (Map map in Find.Maps)
            {
                foreach (Thing item in map.listerThings.ThingsOfDef(ThingDefOf.FermentingBarrel).ToList())
                {
                    bool flag = false;
                    float num = 0f;
                    int stackCount = 0;
                    IntVec3 position = item.Position;
                    ThingDef stuff = ((item.Stuff == null) ? ThingDefOf.WoodLog : item.Stuff);
                    if (item is Building_FermentingBarrel building_FermentingBarrel)
                    {
                        flag = building_FermentingBarrel.SpaceLeftForWort < 25;
                        if (flag)
                        {
                            num = building_FermentingBarrel.Progress;
                            stackCount = 25 - building_FermentingBarrel.SpaceLeftForWort;
                        }
                    }
                    Thing thing = ThingMaker.MakeThing(UF_DefOf.UniversalFermenter, stuff);
                    GenSpawn.Spawn(thing, position, map);
                    if (flag)
                    {
                        CompUniversalFermenter compUniversalFermenter = thing.TryGetComp<CompUniversalFermenter>();
                        compUniversalFermenter.CurrentProcess = compUniversalFermenter.Props.processes.First((UF_Process p) => p.thingDef == ThingDefOf.Beer);
                        Thing thing2 = ThingMaker.MakeThing(ThingDefOf.Wort);
                        thing2.stackCount = stackCount;
                        compUniversalFermenter.AddIngredient(thing2);
                        compUniversalFermenter.ProgressTicks = (int)(360000f * num);
                    }
                }
                foreach (Thing item2 in from t in map.listerThings.ThingsOfDef(ThingDefOf.MinifiedThing)
                                        where t.GetInnerIfMinified().def == ThingDefOf.FermentingBarrel
                                        select t)
                {
                    MinifiedThing minifiedThing = item2 as MinifiedThing;
                    ThingDef stuff2 = ((minifiedThing.InnerThing.Stuff == null) ? ThingDefOf.WoodLog : minifiedThing.InnerThing.Stuff);
                    minifiedThing.InnerThing = null;
                    Thing thing4 = (minifiedThing.InnerThing = ThingMaker.MakeThing(UF_DefOf.UniversalFermenter, stuff2));
                    cachedGraphic.SetValue(minifiedThing, null);
                }
            }
        }
    }
}
