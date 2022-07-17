using System.Collections.Generic;

namespace AutoPatcher
{
    public abstract class EndNode<T> : NodeDef
    {
        public override bool SaveInPort => true;
        protected override int baseInPorts => 1;
        protected override int nInPortGroups => 1;
        protected override void CreateInputPortGroup(Node node, int group)
            => node.inputPorts.Add(new Port<T>());
        protected IPort BaseInput(List<IPort> ports) => ports[0];
        public override bool Perform(Node node) => true;
    }
    public abstract class EndNode<T,InputAT> : EndNode<T>
    {
        protected override int baseInPorts => 2;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputAT>());
        }
        protected IPort InputA(List<IPort> ports) => ports[1];
    }
    public abstract class EndNode<T, InputAT, InputBT> : EndNode<T, InputAT>
    {
        protected override int baseInPorts => 3;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputBT>());
        }
        protected IPort InputB(List<IPort> ports) => ports[2];
    }
    public abstract class EndNode<T, InputAT, InputBT, InputCT> : EndNode<T, InputAT, InputBT>
    {
        protected override int baseInPorts => 4;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputCT>());
        }
        protected IPort InputC(List<IPort> ports) => ports[3];
    }
    public abstract class EndNode<T, InputAT, InputBT, InputCT, InputDT> : EndNode<T, InputAT, InputBT, InputCT>
    {
        protected override int baseInPorts => 5;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputDT>());
        }
        protected IPort InputD(List<IPort> ports) => ports[4];
    }
}
