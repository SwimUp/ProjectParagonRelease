using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using AutoPatcher.Utility;
using Verse;
using System.Reflection.Emit;

namespace AutoPatcher
{
    public class EnumerableMethodPatchNode<TargetT> : PatchNode<TargetT, EnumItemPos<TargetT>>
    {
        protected override int baseInPorts => 4;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<EnumInfo>());
        }
        public IPort EnumInfo(List<IPort> ports) => ports[3];
        public override bool Prepare(Node node)
        {
            var typeMethods = node.inputPorts[0].GetDataList<TypeMethod>();
            var methods = typeMethods.ConvertAll(t => t.method);
            var targets = node.inputPorts[2].GetDataList<SavedList<EnumItemPos<TargetT>>>();
            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                if (!NodeUtility.methodInstructionOffset.TryGetValue(method, out var methodOffsets))
                    continue;
                var enumerableOffsets = NodeUtility.enumerableMethodOffset.TryGetValue(method, null);
                for (int j = 0; j < targets[i].Count; j++)
                {
                    var target = targets[i][j];
                    var targetPos = target.pos;
                    var targetInd = target.index;
                    foreach (var offset in methodOffsets)
                        if (targetPos >= offset.pos)
                            targetPos += offset.offset;
                    if (enumerableOffsets != null)
                        foreach (var offset in enumerableOffsets.Where(t => targetInd >= t.pos))
                            if (targetInd >= offset.pos)
                                targetInd += offset.offset;
                    targets[i][j] = new EnumItemPos<TargetT>(targetInd, targetPos, target.target);
                }
            }
            node.inputPorts[2].SetData(targets);
            return base.Prepare(node);
        }
    }
    public class EnumerableMethodPatchNode<TargetT, InputAT> : EnumerableMethodPatchNode<TargetT>
    {
        protected override int baseInPorts => 5;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputAT>());
        }
        public IPort InputA(List<IPort> ports) => ports[4];
    }
    public class EnumerableMethodPatchNode<TargetT, InputAT, InputBT> : EnumerableMethodPatchNode<TargetT, InputAT>
    {
        protected override int baseInPorts => 6;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputBT>());
        }
        public IPort InputB(List<IPort> ports) => ports[5];
    }
    public class EnumerableMethodPatchNode<TargetT, InputAT, InputBT, InputCT> : EnumerableMethodPatchNode<TargetT, InputAT, InputBT>
    {
        protected override int baseInPorts => 7;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputCT>());
        }
        public IPort InputC(List<IPort> ports) => ports[6];
    }
    public class EnumerableMethodPatchNode<TargetT, InputAT, InputBT, InputCT, InputDT> : EnumerableMethodPatchNode<TargetT, InputAT, InputBT, InputCT>
    {
        protected override int baseInPorts => 8;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputDT>());
        }
        public IPort InputD(List<IPort> ports) => ports[7];
    }
}
