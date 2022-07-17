using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace AutoPatcher
{
    public class DefPatchNode<T, TargetT> : PassNode<T, T> where T : Def
    {
        protected override int baseInPorts => 2;
        protected override int baseOutPorts => 1;
        protected override int nInPortGroups => 1;
        protected override int nOutPortGroups => 2;
        public List<IPort> SuccessfulPorts(Node node)
            => node.outputPorts.GetRange(0, baseOutPorts);
        public List<IPort> FailedPorts(Node node)
            => node.outputPorts.GetRange(baseOutPorts, baseOutPorts);
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<TargetT>());
        }
        public override bool PostPerform(Node node)
        {
            if (!base.PostPerform(node))
                return false;
            var data = node.inputPorts[0].GetDataList<T>();
            SuccessfulPorts(node)[0].GetData<T>().Do(t => data.Remove(t));
            FailedPorts(node)[0].SetData(data);
            return true;
        }
    }
}
