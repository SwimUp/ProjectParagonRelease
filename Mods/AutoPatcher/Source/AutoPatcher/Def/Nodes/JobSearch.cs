using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;

namespace AutoPatcher
{
    public class JobSearch : SearchNode<Type, JobDef, TypeMethod, SavedList<ItemPos<JobDef>>>
    {
        protected bool findDeep = true;
        private List<FieldInfo> JobDefOfFieldInfo = new List<FieldInfo>();
        private Dictionary<FieldInfo, JobDef> FieldToJob = new Dictionary<FieldInfo, JobDef>();
        public override bool Prepare(Node node)
        {
            if (!base.Prepare(node))
                return false;
            var DebugLevel = node.DebugLevel;
            var DebugMessage = node.DebugMessage;
            var targets = Targets(node.inputPorts);
            FieldToJob = new Dictionary<FieldInfo, JobDef>();
            JobDefOfFieldInfo = new List<FieldInfo>();
            foreach (Type type in GenTypes.AllTypesWithAttribute<DefOf>())
                foreach (FieldInfo field in type.GetFields().Where(t => t.FieldType == typeof(JobDef)))
                    if (targets.GetData<JobDef>().FirstOrFallback(t => field.Name.Equals(t.defName)) is JobDef job)
                    {
                        JobDefOfFieldInfo.Add(field);
                        FieldToJob.Add(field, job);
                    }
            if (DebugLevel > 1)
            {
                DebugMessage.AppendLine($"Initialize stage: From [{defName}]: StatSearch method:\nFieldToStat:");
                FieldToJob.Do(t => DebugMessage.AppendLine($"{t.Key} : {t.Value} [{t.Key.DeclaringType}]"));
            }
            return true;
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            var input = node.inputPorts[0].GetData<Type>();
            var foundPorts = FoundPorts(node);
            var ambiguousPorts = AmbiguousPorts(node);
            foreach (Type type in input)
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.IsAbstract)
                        continue;
                    if (SearchMethod(method, out var searchResults))
                    {
                        if (mergeAmbiguous || searchResults.Any(t => t.target != null))
                        {
                            ResultA(foundPorts).AddData((TypeMethod)(type, null, method));
                            if (mergeAmbiguous)
                                ResultB(foundPorts).AddData(searchResults);
                            else
                                ResultB(foundPorts).AddData(searchResults.Where(t => t.target != null).ToList());
                        }
                        if (!mergeAmbiguous && searchResults.Any(t => t.target == null))
                        {
                            ResultA(ambiguousPorts).AddData((TypeMethod)(type, null, method));
                            ResultB(ambiguousPorts).AddData(searchResults.Where(t => t.target == null).ToList());
                        }
                    }
                }
                if (findDeep)
                    foreach (Type nType in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance))
                        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(nType))
                        {
                            if (method.IsAbstract)
                                continue;
                            if (SearchMethod(method, out var searchResults))
                            {
                                if (mergeAmbiguous || searchResults.Any(t => t.target != null))
                                {
                                    ResultA(foundPorts).AddData((TypeMethod)(type, nType, method));
                                    if (mergeAmbiguous)
                                        ResultB(foundPorts).AddData(searchResults);
                                    else
                                        ResultB(foundPorts).AddData(searchResults.Where(t => t.target != null).ToList());
                                }
                                if (!mergeAmbiguous && searchResults.Any(t => t.target == null))
                                {
                                    ResultA(ambiguousPorts).AddData((TypeMethod)(type, nType, method));
                                    ResultB(ambiguousPorts).AddData(searchResults.Where(t => t.target == null).ToList());
                                }
                            }
                        }
            }
            var typeList = ResultA(foundPorts).GetData<TypeMethod>().Select(t => t.type).ToList();
            var jobList = ResultB(foundPorts).GetData<List<ItemPos<JobDef>>>().SelectMany(t => t.Select(tt => tt.target)).ToList();
            typeList.RemoveDuplicates();
            jobList.RemoveDuplicates();
            foundPorts[0].SetData(typeList);
            foundPorts[1].SetData(jobList);
            typeList = ResultA(ambiguousPorts).GetData<(Type type, Type, MethodInfo)>().Select(t => t.type).ToList();
            typeList.RemoveDuplicates();
            ambiguousPorts[0].SetData(typeList);
            return true;
        }
        public virtual bool SearchMethod(MethodInfo searchMethod, out List<ItemPos<JobDef>> Results)
        {
            Results = new List<ItemPos<JobDef>>();
            bool foundResult = false;
            List<CodeInstruction> instructionList;
            try { instructionList = PatchProcessor.GetCurrentInstructions(searchMethod); }
            catch { instructionList = PatchProcessor.GetOriginalInstructions(searchMethod); }
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                // Ambiguous method call
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && 
                    instruction.operand is MethodInfo calledMethod)
                {
                    if (calledMethod.ReturnType == typeof(JobDef))
                    {
                        foundResult = true;
                        Results.Add((ItemPos<JobDef>)(i, null));
                    }
                }
                // StatDefOf call
                if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo jobField &&
                    jobField.FieldType == typeof(JobDef) && JobDefOfFieldInfo.Contains(jobField))
                {
                    Results.Add((ItemPos<JobDef>)(i, FieldToJob[jobField]));
                    foundResult = true;
                }
                if (!FindAll && foundResult)
                    return foundResult;
            }
            return foundResult;
        }
    }
}
