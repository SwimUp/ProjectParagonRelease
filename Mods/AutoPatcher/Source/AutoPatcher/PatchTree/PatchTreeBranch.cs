using System.Linq;
using System.Collections.Generic;
using Verse;
using AutoPatcher.Utility;

namespace AutoPatcher
{
    public class PatchTreeBranch : IExposable
    {
        public PatchTreeDef patchTree;
        public Node inputNode;
        public Node outputNode;
        public IPort inputPort;
        public IPort outputPort;
        // Input output node interface via XML
        private int inputNodeInd;
        private int outputNodeInd;
        private string inputNodeName;
        private string outputNodeName;
        // Port indexes 
        private int inputPortInd = -1;
        private int outputPortInd = -1;
        private int inputPortGroup = -1;
        private int outputPortGroup = -1;
        public PatchTreeBranch() { }
        public PatchTreeBranch(PatchTreeDef patchTree)
        {
            this.patchTree = patchTree;
        }
        public bool Initialize(PatchTreeDef patchTree, List<Node> list)
        {
            this.patchTree = patchTree;
            if (inputNode == null)
            {
                if (inputNodeName != null)
                {
                    if (!(list.FirstOrDefault(t => t.name == inputNodeName) is Node node))
                    {
                        Log.Error($"[[LC]AutoPatcher] TempError 687669");
                        return false;
                    }
                    inputNode = node;
                }
                else
                {
                    if (!(list.FirstOrDefault(t => t.index == inputNodeInd) is Node node))
                    {
                        Log.Error($"[[LC]AutoPatcher] TempError 949867");
                        return false;
                    }
                    inputNode = node;
                }
            }
            if (outputNode == null)
            {
                if (outputNodeName != null)
                {
                    if (!(list.FirstOrDefault(t => t.name == outputNodeName) is Node node))
                    {
                        Log.Error($"[[LC]AutoPatcher] TempError 687669");
                        return false;
                    }
                    outputNode = node;
                }
                else
                {
                    if (!(list.FirstOrDefault(t => t.index == outputNodeInd) is Node node))
                    {
                        Log.Error($"[[LC]AutoPatcher] TempError 949867");
                        return false;
                    }
                    outputNode = node;
                }
            }
            if (inputPort != null && outputPort != null)
                return true;

            if (inputPortInd < 0 || outputPortInd < 0)
            {
                List<IPort> inputPorts;
                List<IPort> outputPorts;
                if (inputPortInd < 0)
                {
                    inputPortGroup = inputPortGroup < 0 ? 0 : inputPortGroup;
                    inputPorts = inputNode.inputPortGroups[inputPortGroup];
                }
                else
                {
                    if (inputPortGroup < 0)
                        inputPort = inputNode.inputPorts[inputPortInd];
                    else
                        inputPort = inputNode.inputPortGroups[inputPortGroup][inputPortInd];
                    inputPorts = new List<IPort>() { inputPort };
                }
                if (outputPortInd < 0)
                {
                    outputPortGroup = outputPortGroup < 0 ? 0 : outputPortGroup;
                    outputPorts = outputNode.outputPortGroups[outputPortGroup];
                }
                else
                {
                    if (outputPortGroup < 0)
                        outputPort = outputNode.outputPorts[outputPortInd];
                    else
                        outputPort = outputNode.outputPortGroups[outputPortGroup][outputPortInd];
                    outputPorts = new List<IPort>() { outputPort };
                }
                foreach (var outPort in outputPorts)
                {
                    foreach (var inPort in inputPorts)
                    {
                        if (outPort.DataType.CanCastTo(inPort.DataType))
                        // if (inPort.DataType.IsAssignableFrom(outPort.DataType))
                        {
                            var branch = new PatchTreeBranch(patchTree)
                            {
                                inputNode = inputNode,
                                outputNode = outputNode,
                                inputPort = inPort,
                                outputPort = outPort,
                            };
                            patchTree.Branches.Add(branch);
                            branch.Initialize(patchTree, list);
                        }
                    }
                }
                patchTree.Branches.Remove(this);
            }
            else
            {
                if (inputPortGroup < 0)
                    inputPort = inputNode.inputPorts[inputPortInd];
                else
                    inputPort = inputNode.inputPortGroups[inputPortGroup][inputPortInd];
                if (outputPortGroup < 0)
                    outputPort = outputNode.outputPorts[outputPortInd];
                else
                    outputPort = outputNode.outputPortGroups[outputPortGroup][outputPortInd];
            }
            return true;
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref patchTree, "patchTree");
            Scribe_References.Look(ref inputNode, "inputNode");
            Scribe_References.Look(ref outputNode, "outputNode");
            Scribe_References.Look(ref inputPort, "inputPort");
            Scribe_References.Look(ref outputPort, "outputPort");
        }
    }
}
