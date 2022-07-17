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
    public class StatSearch : SearchNode<Type, StatDef, TypeMethod, SavedList<ItemPos<StatDef>>>
    {
        protected bool findPawn = false;
        protected bool findDeep = true;
        private List<FieldInfo> StatDefOfFieldInfo = new List<FieldInfo>();
        private Dictionary<FieldInfo, StatDef> FieldToStat = new Dictionary<FieldInfo, StatDef>();
        public override bool Prepare(Node node)
        {
            if (!base.Prepare(node))
                return false;
            var DebugLevel = node.DebugLevel;
            var DebugMessage = node.DebugMessage;
            var targets = Targets(node.inputPorts);
            FieldToStat = new Dictionary<FieldInfo, StatDef>();
            StatDefOfFieldInfo = new List<FieldInfo>();
            foreach (Type type in GenTypes.AllTypesWithAttribute<DefOf>())
                foreach (FieldInfo field in type.GetFields().Where(t => t.FieldType == typeof(StatDef)))
                    if (targets.GetData<StatDef>().FirstOrFallback(t => field.Name.Equals(t.defName)) is StatDef stat)
                    {
                        StatDefOfFieldInfo.Add(field);
                        FieldToStat.Add(field, stat);
                    }
            if (DebugLevel > 1)
            {
                DebugMessage.AppendLine($"Initialize stage: From [{defName}]: StatSearch method:\nFieldToStat:");
                FieldToStat.Do(t => DebugMessage.AppendLine($"{t.Key} : {t.Value} [{t.Key.DeclaringType}]"));
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
                    if (SearchMethod(method, out var searchResults, out List<CodeInstruction> PawnIns))
                    {
                        /*if (findPawn)
                            PawnInstructions.Add(PawnIns);*/
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
                            if (SearchMethod(method, out var searchResults, out List<CodeInstruction> PawnIns))
                            {
                                /*if (findPawn)
                                    PawnInstructions.Add(PawnIns);*/
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
            var statList = ResultB(foundPorts).GetData<List<ItemPos<StatDef>>>().SelectMany(t => t.Select(tt => tt.target)).ToList();
            typeList.RemoveDuplicates();
            statList.RemoveDuplicates();
            foundPorts[0].SetData(typeList);
            foundPorts[1].SetData(statList);
            typeList = ResultA(ambiguousPorts).GetData<TypeMethod>().Select(t => t.type).ToList();
            typeList.RemoveDuplicates();
            ambiguousPorts[0].SetData(typeList);
            return true;
        }
        public virtual bool SearchMethod(MethodInfo searchMethod, out List<ItemPos<StatDef>> Results, out List<CodeInstruction> PawnIns)
        {
            Results = new List<ItemPos<StatDef>>();
            PawnIns = new List<CodeInstruction>();
            bool foundResult = false;
            bool foundPawn = false;
            List<CodeInstruction> instructionList;
            try { instructionList = PatchProcessor.GetCurrentInstructions(searchMethod); }
            catch { instructionList = PatchProcessor.GetOriginalInstructions(searchMethod); }
            List<int?> ParamList = searchMethod.GetParameters()?.Where(t => t.ParameterType.IsAssignableFrom(typeof(Pawn)))?.Select(t=>new int?(t.Position))?.ToList() ?? new List<int?>();
            List<LocalBuilder> LocalList = new List<LocalBuilder>();
            if (!ParamList.NullOrEmpty())
                foundPawn = true;
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo pawnField &&
                    pawnField.FieldType.IsAssignableFrom(typeof(Pawn)))
                {
                    foundPawn = true;
                    for (int j = 1; j < i; j++)
                        if (instructionList[i - j].IsLdarg(0))
                        {
                            PawnIns = new List<CodeInstruction>(instructionList.GetRange(i - j, j + 1));
                            break;
                        }
                }
                if (ParamList.Any(t => instruction.IsLdarg(t)))
                    PawnIns = new List<CodeInstruction>() { instruction };
                if (instruction.IsStloc() && instruction.operand is LocalBuilder local && !LocalList.Contains(local) && local.LocalType.IsAssignableFrom(typeof(Pawn)))
                    LocalList.Add(local);
                if (LocalList.Any(t => instruction.IsLdloc(t)))
                    PawnIns = new List<CodeInstruction>() { instruction };
                // Ambiguous method call
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && 
                    instruction.operand is MethodInfo calledMethod)
                {
                    if (calledMethod.ReturnType == typeof(StatDef))
                    {
                        foundResult = true;
                        Results.Add((ItemPos<StatDef>)(i, null));
                    }
                    else if (calledMethod.ReturnType.IsAssignableFrom(typeof(Pawn)))
                    {
                        foundPawn = true;
                        if (calledMethod.IsStatic)
                            PawnIns = new List<CodeInstruction>() { instruction };
                        else
                        {
                            for (int j = 1; j < i; j++)
                                if (instructionList[i - j].IsLdarg(0))
                                {
                                    PawnIns = new List<CodeInstruction>(instructionList.GetRange(i - j, j + 1));
                                    break;
                                }
                        }
                    }
                }
                // StatDefOf call
                if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo statField &&
                    statField.FieldType == typeof(StatDef) && StatDefOfFieldInfo.Contains(statField))
                {
                    Results.Add((ItemPos<StatDef>)(i, FieldToStat[statField]));
                    foundResult = true;
                }
                if (!FindAll && foundResult && (!findPawn || foundPawn))
                    return foundResult;
            }
            return foundResult;
        }
    }
}
