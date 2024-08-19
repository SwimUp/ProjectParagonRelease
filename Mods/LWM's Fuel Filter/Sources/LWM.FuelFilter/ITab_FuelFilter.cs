using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace LWM.FuelFilter
{
    public class ITab_FuelFilter : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(300f, 480f);

        private ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();

        public override bool IsVisible
        {
            get
            {
                ThingWithComps thingWithComps = base.SelObject as ThingWithComps;
                if (thingWithComps != null && thingWithComps.Faction != null && thingWithComps.Faction != Faction.OfPlayer)
                {
                    return false;
                }
                ThingFilter thingFilter = thingWithComps.GetComp<CompRefuelable>()?.Props.fuelFilter;
                if (thingFilter == null || thingFilter.AllowedDefCount < 2)
                {
                    return false;
                }
                return thingWithComps.GetComp<Comp_FuelFilter>() != null;
            }
        }

        protected CompRefuelable GetCompR => (base.SelObject as ThingWithComps)?.GetComp<CompRefuelable>();

        protected Comp_FuelFilter GetCompFF => (base.SelObject as ThingWithComps)?.GetComp<Comp_FuelFilter>();

        public ITab_FuelFilter()
        {
            size = WinSize;
            labelKey = "LWM.FuelFilter.FuelLabel";
        }

        protected override void FillTab()
        {
            Rect position = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            GUI.BeginGroup(position);
            ThingFilter fuelFilter = GetCompR.Props.fuelFilter;
            ThingFilter filter = GetCompFF.filter;
            if (filter != null && fuelFilter != null)
            {
                ThingFilterUI.DoThingFilterConfigWindow(new Rect(0f, 20f, position.width, position.height - 20f), thingFilterState, filter, fuelFilter, 8, (IEnumerable<ThingDef>)null, (IEnumerable<SpecialThingFilterDef>)null, false, false, false, (List<ThingDef>)null, (Map)null);
            }
            else
            {
                Log.Warning("FuelFilter - had null filter??");
            }
            GUI.EndGroup();
        }
    }
}
