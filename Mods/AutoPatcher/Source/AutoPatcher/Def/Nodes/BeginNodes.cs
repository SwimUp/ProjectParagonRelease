using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using AutoPatcher.Utility;

namespace AutoPatcher
{
    public class BeginDriver : BeginNode<Type>
    {
        private bool allDrivers = false;
        private Type baseDriver;
        private List<Type> driverException = new List<Type>();
        private List<Type> driverList = new List<Type>();
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            if (allDrivers)
                node.outputPorts[0].SetData(GenTypes.AllSubclasses(baseDriver).Where(t=>!driverException.Contains(t)).ToList());
            else
                node.outputPorts[0].SetData(driverList);
            return true;
        }
    }
    public class BeginDef : BeginNode<Def>
    {
        private bool allDefs = false;
        private Type defType;
        private List<Def> defException = new List<Def>();
        private List<Def> defList = new List<Def>();
        private List<string> defExceptionName = new List<string>();
        private List<string> defListName = new List<string>();
        protected override void CreateOutputPortGroup(Node node, int group)
            => node.outputPorts.Add((IPort)Activator.CreateInstance(typeof(Port<>).MakeGenericType(defType)));
        public override void Initialize(bool fromSave = false)
        {
            base.Initialize(fromSave);
            if (fromSave)
                return;
            var allDefs = NodeUtility.getDefListInfo.MakeGenericMethod(defType).Invoke(null, null) as List<Def>;
            defListName.Do(t => defList.Add(allDefs.First(tt => tt.defName == t)));
            defExceptionName.Do(t => defException.Add(allDefs.First(tt => tt.defName == t)));
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            if (allDefs)
                node.outputPorts[0].SetData(((List<Def>)NodeUtility.getDefListInfo.MakeGenericMethod(defType).Invoke(null, null)).Where(t => !defException.Contains(t)).ToList());
            else
                node.outputPorts[0].SetData(defList);
            return true;
        }
    }
    public class BeginDef<T> : BeginNode<T> where T : Def
    {
        private bool allDefs = false;
        private List<T> defException = new List<T>();
        private List<T> defList = new List<T>();
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            if (allDefs)
                node.outputPorts[0].SetData(DefDatabase<T>.AllDefsListForReading.Where(t => !defException.Contains(t)).ToList());
            else
                node.outputPorts[0].SetData(defList);
            return true;
        }
    }
    public class BeginFormula : BeginNode<Formula>
    {
        private List<Formula> formulaList = new List<Formula>();
        private bool initialized = true;
        public override void Initialize(bool fromSave = false)
        {
            base.Initialize(fromSave);
            if (fromSave)
                return;
            foreach (var formula in formulaList)
                initialized &= formula.Initialize();
        }
        public override bool Initialize(Node node)
        {
            if (!base.Initialize(node))
                return false;
            return initialized;
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            node.outputPorts[0].SetData(formulaList);
            return true;
        }
    }
}
