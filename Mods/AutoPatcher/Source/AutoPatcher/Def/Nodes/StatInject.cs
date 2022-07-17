using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AutoPatcher
{
    public class StuffStatInject : DefPatchNode<ThingDef, Formula>
    {
        public bool skipNoStuff = true;
        public StuffProperties defaultStuffProperties;
        public bool asFactor = true;
        public List<StatModifier> defaultStats = new List<StatModifier>();
        public bool replacePreviousStat = false;
        public bool successIfAllInjected = false;
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            var tDefs = node.inputPorts[0].GetData<ThingDef>();
            var formulas = node.inputPorts[1].GetDataList<Formula>();
            var stats = formulas.SelectMany(t => t.inputs).ToList();
            stats.RemoveDuplicates();
            var outStats = formulas.Select(t=>t.output).ToList();
            outStats.RemoveDuplicates();
            foreach (var tDef in tDefs)
            {
                if (tDef.stuffProps == null)
                {
                    if (skipNoStuff)
                        continue;
                    tDef.stuffProps = defaultStuffProperties ?? new StuffProperties();
                }
                if (asFactor && tDef.stuffProps.statFactors == null)
                    tDef.stuffProps.statFactors = new List<StatModifier>();
                else if (!asFactor && tDef.stuffProps.statOffsets == null)
                    tDef.stuffProps.statOffsets = new List<StatModifier>();
                var tStats = new List<StatModifier>(stats.Count);
                var stuffMods = asFactor ? tDef.stuffProps.statFactors : tDef.stuffProps.statOffsets;
                var thingMods = tDef.statBases;
                var stuffCat = tDef.stuffProps.categories;
                foreach (var stat in stats)
                {
                    if (stuffMods.StatListContains(stat))
                    {
                        tStats.Add(stuffMods.First(t => t.stat == stat));
                        continue;
                    }
                    if (defaultStats.StatListContains(stat))
                    {
                        tStats.Add(defaultStats.First(t => t.stat == stat));
                        continue;
                    }
                    if (thingMods.StatListContains(stat))
                        tStats.Add(thingMods.First(t => t.stat == stat));
                }
                var success = new Dictionary<StatDef, bool>(outStats.Count);
                foreach (var stat in outStats)
                    success.Add(stat, false);
                foreach (var formula in formulas)
                {
                    var outStat = formula.output;
                    if (!replacePreviousStat && stuffMods.StatListContains(outStat))
                        continue;
                    if (formula.Calculate(tStats, stuffCat, out float val))
                    {
                        stuffMods.Add(new StatModifier() { stat = outStat, value = val });
                        success[outStat] = true;
                    }
                }
                if (successIfAllInjected)
                {
                    if (success.Values.All(t => t))
                        SuccessfulPorts(node)[0].AddData(tDef);
                    else
                        FailedPorts(node)[0].AddData(tDef);
                }
                else
                {
                    if (success.Values.Any(t => t))
                        SuccessfulPorts(node)[0].AddData(tDef);
                    else
                        FailedPorts(node)[0].AddData(tDef);
                }
            }
            return true;
        }
    }
}
