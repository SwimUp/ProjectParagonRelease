namespace AutoPatcher
{
    /// <summary>
    /// Pass node type
    /// </summary>
    /// <typeparam name="inT"></typeparam>
    /// <typeparam name="outT"></typeparam>
    public class PassNode<inT,outT> : NodeDef
    {
        protected override int baseInPorts => 1;
        protected override int baseOutPorts => 1;
        protected override int nInPortGroups => 1;
        protected override int nOutPortGroups => 1;
        protected override void CreateInputPortGroup(Node node, int group)
            => node.inputPorts.Add(new Port<inT>());
        protected override void CreateOutputPortGroup(Node node, int group)
            => node.outputPorts.Add(new Port<outT>());
    }
}
