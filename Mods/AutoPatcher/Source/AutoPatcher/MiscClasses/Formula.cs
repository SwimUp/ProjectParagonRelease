using org.mariuszgromada.math.mxparser;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace AutoPatcher
{
    public class Formula : IExposable
    {
        public List<StatDef> inputs = new List<StatDef>();
        public StatDef output;
        public List<StuffCategoryDef> stuffCategoryDefs = new List<StuffCategoryDef>();
        public string expression;
        public List<string> arguments = new List<string>();
        private Expression expr;
        private List<Argument> args = new List<Argument>();
        public virtual bool Initialize()
        {
            try
            {
                foreach (var stat in inputs)
                    args.Add(new Argument(stat.defName));
                foreach (var arg in arguments)
                    args.Add(new Argument(arg));
                if (args.NullOrEmpty())
                    expr = new Expression(expression);
                else
                    expr = new Expression(expression, args.ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }
        public virtual bool Calculate(List<StatModifier> stats, List<StuffCategoryDef> stuffCategories, out float value)
        {
            value = 0f;
            if (!stuffCategoryDefs.NullOrEmpty())
                if (!stuffCategoryDefs.Any(t => stuffCategories.Contains(t)))
                    return false;
            if (inputs.Any(t => !stats.StatListContains(t)))
                return false;
            foreach (var arg in args)
                if (stats.FirstOrFallback(t => t.stat.defName == arg.getArgumentName()) is StatModifier modifier)
                    arg.setArgumentValue(modifier.value);
            value = (float)expr.calculate();
            return true;
        }
        public void ExposeData()
        {
            Scribe_Collections.Look(ref inputs, "inputs", LookMode.Def);
            Scribe_Defs.Look(ref output, "output");
            Scribe_Collections.Look(ref stuffCategoryDefs, "stuffCategoryDefs", LookMode.Def);
            Scribe_Values.Look(ref expression, "expression");
            Scribe_Collections.Look(ref arguments, "arguments", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                Initialize();
        }
    }
}
