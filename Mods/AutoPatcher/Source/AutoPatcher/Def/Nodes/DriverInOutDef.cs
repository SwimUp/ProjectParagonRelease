using System;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoPatcher
{
    // XML interface of generic type is prefered
    public class DriverIn_DefOut<T> : PassNode<Type, T> where T : Def
    {
        protected bool includeDerived = true;
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            var input = node.inputPorts[0].GetData<Type>();
            var output = DefDatabase<T>.AllDefs;
            if (includeDerived)
                output = output.Where(t => input.Any(tt => tt.IsAssignableFrom(DefToDriver(t))));
            else
                output = output.Where(t => input.Contains(DefToDriver(t)));
            node.outputPorts[0].SetData(output.ToList());
            return true;
        }
        protected virtual Type DefToDriver(T def)
            => null;
    }
    public class DefIn_DriverOut<T> : PassNode<T, Type> where T : Def
    {
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            var input = node.inputPorts[0].GetData<T>();
            var output = input.Select(t => DefToDriver(t));
            node.outputPorts[0].SetData(output.ToList());
            return true;
        }
        protected virtual Type DefToDriver(T def)
            => null;
    }
    // Workaround until generic XML interface is coded
    public class JobDriverInDefOut : DriverIn_DefOut<JobDef>
    {
        protected override Type DefToDriver(JobDef def)
            => def.driverClass;
    }
    public class WorkGiverInDefOut : DriverIn_DefOut<WorkGiverDef>
    {
        protected override Type DefToDriver(WorkGiverDef def)
            => def.giverClass;
    }
    public class StatWorkerInDefOut : DriverIn_DefOut<StatDef>
    {
        protected override Type DefToDriver(StatDef def)
            => def.workerClass;
    }
    public class JobDefInDriverOut : DefIn_DriverOut<JobDef>
    {
        protected override Type DefToDriver(JobDef def)
            => def.driverClass;
    }
    public class WorkGiverDefInGiverOut : DefIn_DriverOut<WorkGiverDef>
    {
        protected override Type DefToDriver(WorkGiverDef def)
            => def.giverClass;
    }
    public class StatDefInWorkerOut : DefIn_DriverOut<StatDef>
    {
        protected override Type DefToDriver(StatDef def)
            => def.workerClass;
    }
}
