using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace AutoPatcher
{
    public abstract class SearchNode<InOutT, TargetT> : PassNode<InOutT, InOutT>
    {
        protected override int baseInPorts => 2;
        protected override int baseOutPorts => 2;
        protected override int nInPortGroups => 1;
        protected override int nOutPortGroups => 3;
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<TargetT>());
        }
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<TargetT>());
        }
        protected bool mergeAmbiguous = false;
        protected bool FindAll = true;
        public List<IPort> FoundPorts(Node node)
            => node.outputPorts.GetRange(0, baseOutPorts);
        public List<IPort> FailedPorts(Node node)
            => node.outputPorts.GetRange(baseOutPorts, baseOutPorts);
        public List<IPort> AmbiguousPorts(Node node)
            => node.outputPorts.GetRange(2 * baseOutPorts, baseOutPorts);
        public IPort Targets(List<IPort> ports)  => ports[1];
        public override bool PostPerform(Node node)
        {
            if (!base.PostPerform(node))
                return false;
            var data = node.inputPorts[0].GetDataList<InOutT>();
            FoundPorts(node)[0].GetData<InOutT>().Do(t => data.Remove(t));
            AmbiguousPorts(node)[0].GetData<InOutT>().Do(t => data.Remove(t));
            FailedPorts(node)[0].SetData(data);
            return true;
        }
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT> : SearchNode<InOutT, TargetT>
    {
        protected override int baseOutPorts => 3;
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultAT>());
        }
        public IPort ResultA(List<IPort> ports) => ports[2];
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT, ResultBT> : SearchNode<InOutT, TargetT, ResultAT>
    {
        protected override int baseOutPorts => 4;
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultBT>());
        }
        public IPort ResultB(List<IPort> ports) => ports[3];
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT, ResultBT, ResultCT> : SearchNode<InOutT, TargetT, ResultAT, ResultBT>
    {
        protected override int baseOutPorts => 5;
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultCT>());
        }
        public IPort ResultC(List<IPort> ports) => ports[4];
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT, ResultBT, ResultCT, ResultDT> : SearchNode<InOutT, TargetT, ResultAT, ResultBT, ResultCT>
    {
        protected override int baseOutPorts => 6;
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultDT>());
        }
        public IPort ResultD(List<IPort> ports) => ports[5];
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT, ResultBT, ResultCT, ResultDT, ResultET> : SearchNode<InOutT, TargetT, ResultAT, ResultBT, ResultCT, ResultDT>
    {
        protected override int baseOutPorts => 7;
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultET>());
        }
        public IPort ResultE(List<IPort> ports) => ports[6];
    }
}
