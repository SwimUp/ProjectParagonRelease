using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class Command_Process : Command_Action
    {
        public UF_Process processToTarget;

        public List<UF_Process> processOptions = new List<UF_Process>();

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (UF_Process process in processOptions)
                {
                    list.Add(new FloatMenuOption((process.customLabel != "") ? process.customLabel : process.thingDef.label.CapitalizeFirst(), delegate
                    {
                        ChangeProcess(processToTarget, process);
                    }, UF_Utility.GetIcon(process.thingDef, UF_Settings.singleItemIcon), Color.white));
                }
                if (UF_Settings.sortAlphabetically)
                {
                    list.SortBy((FloatMenuOption fmo) => fmo.Label);
                }
                return list;
            }
        }

        internal static void ChangeProcess(UF_Process processToTarget, UF_Process process)
        {
            foreach (Thing item in Find.Selector.SelectedObjects.OfType<Thing>())
            {
                CompUniversalFermenter compUniversalFermenter = item.TryGetComp<CompUniversalFermenter>();
                if (compUniversalFermenter != null && compUniversalFermenter.CurrentProcess == processToTarget)
                {
                    compUniversalFermenter.CurrentProcess = process;
                }
            }
        }
    }
}
