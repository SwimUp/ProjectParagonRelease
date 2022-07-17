using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using AutoPatcher.Utility;

namespace AutoPatcher
{
    public class MethodPatchNode<TargetT> : PatchNode<TargetT, ItemPos<TargetT>>
    {
        public override bool Prepare(Node node)
        {
            var typeMethods = node.inputPorts[0].GetDataList<TypeMethod>();
            var methods = typeMethods.ConvertAll(t => t.method);
            var targets = node.inputPorts[2].GetDataList<SavedList<ItemPos<TargetT>>>();
            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                if (!NodeUtility.methodInstructionOffset.TryGetValue(method, out var offsets))
                    continue;
                for (int j = 0; j < targets[i].Count; j++)
                {
                    var target = targets[i][j];
                    var targetPos = target.pos;
                    foreach (var offset in offsets)
                        if (targetPos >= offset.pos)
                            targetPos += offset.offset;
                    targets[i][j] = new ItemPos<TargetT>(targetPos, target.target);
                }
            }
            node.inputPorts[2].SetData(targets);
            return base.Prepare(node);
        }
    }
    public class MethodPatchNode<TargetT, InputAT> : MethodPatchNode<TargetT>
    {
        protected override int baseInPorts => 4;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputAT>());
        }
        public IPort InputA(List<IPort> ports) => ports[3];
    }
    public class MethodPatchNode<TargetT, InputAT, InputBT> : MethodPatchNode<TargetT, InputAT>
    {
        protected override int baseInPorts => 5;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputBT>());
        }
        public IPort InputB(List<IPort> ports) => ports[4];
    }
    public class MethodPatchNode<TargetT, InputAT, InputBT, InputCT> : MethodPatchNode<TargetT, InputAT, InputBT>
    {
        protected override int baseInPorts => 6;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputCT>());
        }
        public IPort InputC(List<IPort> ports) => ports[5];
    }
    public class MethodPatchNode<TargetT, InputAT, InputBT, InputCT, InputDT> : MethodPatchNode<TargetT, InputAT, InputBT, InputCT>
    {
        protected override int baseInPorts => 7;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputDT>());
        }
        public IPort InputD(List<IPort> ports) => ports[6];
    }
}
