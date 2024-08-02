using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LWM.FuelFilter
{
    [StaticConstructorOnStartup]
    public class DoHorrifyingThingsToDefDatabase
    {
        static DoHorrifyingThingsToDefDatabase()
        {
            foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (allDef.comps == null)
                {
                    continue;
                }
                foreach (CompProperties comp in allDef.comps)
                {
                    if (comp is CompProperties_Refuelable)
                    {
                        allDef.comps.Add(new CompProperties
                        {
                            compClass = typeof(Comp_FuelFilter)
                        });
                        if (allDef.inspectorTabs == null)
                        {
                            allDef.inspectorTabs = new List<Type>();
                        }
                        allDef.inspectorTabs.Add(typeof(ITab_FuelFilter));
                        if (allDef.inspectorTabsResolved == null)
                        {
                            allDef.inspectorTabsResolved = new List<InspectTabBase>();
                        }
                        allDef.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_FuelFilter)));
                        break;
                    }
                }
            }
        }
    }
}
