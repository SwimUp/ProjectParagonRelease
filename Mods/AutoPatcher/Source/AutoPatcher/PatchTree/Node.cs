using System.Text;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System;

namespace AutoPatcher
{
    /// <summary>
    /// Basic node object
    /// </summary>
    public class Node : IExposable, ILoadReferenceable, IEquatable<Node>
    {
        /// <summary>
        /// Global index for quick index creation
        /// </summary>
        public static int count = 0;
        // Node driver and port interfaces
        public NodeDef nodeDef;
        public PatchTreeDef patchTree;
        public List<IPort> inputPorts;
        public List<IPort> outputPorts;
        public List<List<IPort>> inputPortGroups;
        public List<List<IPort>> outputPortGroups;
        // Sepcific index and name of the node
        public int index;
        public string name;
        public bool fromSave = false;
        // Index for how many branches are merging into this node
        public int current = 0;
        public int merging = 0;
        // Debug functions
        public int DebugLevel = 0;
        private StringBuilder debugMessage;
        public StringBuilder DebugMessage
        {
            get
            {
                if (debugMessage == null)
                    debugMessage = new StringBuilder($"[[LC]AutoPatcher] Debug Node : {this} : DebugLevel = {DebugLevel}\n");
                return debugMessage;
            }
        }
        public Node()
        {
            index = count;
            count++;
        }
        public Node(int index, string name)
        {
            this.index = index;
            this.name = name;
        }
        // Shortcut methods to the driver's methods
        public bool Initialize(PatchTreeDef patchTree)
        {
            this.patchTree = patchTree;
            return nodeDef.Initialize(this);
        }
        public bool Prepare() => nodeDef.Prepare(this);
        public bool Perform() => nodeDef.Perform(this);
        public bool PostPerform() => nodeDef.PostPerform(this);
        public bool Pass(IEnumerable<PatchTreeBranch> branches) => nodeDef.Pass(this, branches);
        public bool Finish() => nodeDef.Finish(this);
        public bool End() => nodeDef.End(this);
        public override string ToString()
            => $"[{name ?? (nodeDef.ToString() + index)}]";
        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // if (!nodeDef.SaveInPort)
                    inputPorts.Do(t => t.Clear());
                // if (!nodeDef.SaveOutPort)
                    outputPorts.Do(t => t.Clear());
            }
            Scribe_Defs.Look(ref patchTree, "patchTree");
            Scribe_Values.Look(ref index, "index");
            Scribe_Values.Look(ref name, "name");
            Scribe_Collections.Look(ref inputPorts, "inputPorts", LookMode.Deep);
            Scribe_Collections.Look(ref outputPorts, "outputPorts", LookMode.Deep);
        }

        public string GetUniqueLoadID()
            => patchTree.defName + "_Node" + index;
        public override int GetHashCode() => GetUniqueLoadID().GetHashCode();
        public bool Equals(Node other) => GetUniqueLoadID() == other.GetUniqueLoadID();
    }
}
