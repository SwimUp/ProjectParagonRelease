namespace AutoPatcher
{
    public abstract class BeginNode<T> : NodeDef where T : class
    {
        protected override int baseOutPorts => 1;
        protected override int nOutPortGroups => 1;
        protected override void CreateOutputPortGroup(Node node, int group)
            => node.outputPorts.Add(new Port<T>());
    }
}
