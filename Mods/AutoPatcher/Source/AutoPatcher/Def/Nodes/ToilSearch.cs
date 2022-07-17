using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using Verse.AI;
using System.Collections;
using AutoPatcher.Utility;

namespace AutoPatcher
{
    public class JobDriverSearchToil : SearchNode<Type, TypeMethod, TypeMethod,
        SavedList<EnumItemPos<SavedList<MethodInfo>>>,
        //SavedList<(int pos, int ToilIndex, List<MethodInfo> actions)>,
        SavedList<EnumItemInfo>, SavedList<MethodInfo>, EnumInfo>
    {
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            var input = node.inputPorts[0].GetData<Type>();
            // Action targets
            var targets = node.inputPorts[1].GetDataList<TypeMethod>();
            var foundPorts = FoundPorts(node);
            var targetMethods = targets.ConvertAll(t => t.method);
            var ToilGenerators = new List<(MethodInfo generator, List<MethodInfo> actions)>();
            // Search for Toil generators
            foreach (Type type in input)
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type).Where(t => t.ReturnType == typeof(Toil)))
                    if (SearchToilGenerator(method, targetMethods, out List<MethodInfo> ActionsFound))
                        ToilGenerators.Add((method, ActionsFound));
            // Search for Toils in MakeNewToils
            foreach (Type type in input)
            {
                var MakeNewToils = AccessTools.Method(type, "MakeNewToils");
                foreach (Type nType in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (nType.GetInterfaces()?.Any(t => t == typeof(IEnumerator<Toil>)) == true)
                    {
                        var interfaceMap = nType.GetInterfaceMap(typeof(IEnumerator));
                        var MoveNext = AccessTools.Method(nType, "MoveNext");
                        var get_Current = nType.GetInterfaceMap(typeof(IEnumerator<Toil>)).TargetMethods.First();
                        var Current = GetFieldInfo(get_Current);
                        var enumInfo = new EnumInfo(Current, null);
                        if (SearchMoveNext(MoveNext, targetMethods, ToilGenerators, ref enumInfo, out var SearchResults, out var toilInfo))
                        {
                            foundPorts[0].AddData(type);
                            ResultA(foundPorts).AddData(new TypeMethod(type, nType, MoveNext));
                            ResultB(foundPorts).AddData(SearchResults);
                            ResultC(foundPorts).AddData(toilInfo);
                            var actions = SearchResults.SelectMany(t => t.target).ToList();
                            actions.RemoveDuplicates();
                            ResultD(foundPorts).AddData(actions);
                            NodeUtility.enumerableItems.SetOrAdd(MoveNext, toilInfo);
                            ResultE(foundPorts).AddData(enumInfo);
                        }
                        break;
                    }
                }
            }
            var foundActions = ResultB(foundPorts).GetData<List<EnumItemPos<SavedList<MethodInfo>>>>().SelectMany(t => t.SelectMany(tt => tt.target)).ToList();
            foundActions.RemoveDuplicates();
            foundPorts[1].SetData(targets.Where(t => foundActions.Contains(t.method)).ToList());
            return true;
        }
        private static FieldInfo GetFieldInfo(MethodInfo getterMethod)
        {
            List<CodeInstruction> instructionList;
            try { instructionList = PatchProcessor.GetCurrentInstructions(getterMethod); }
            catch { instructionList = PatchProcessor.GetOriginalInstructions(getterMethod); }
            var length = instructionList.Count;
            for (int i = 0; i < length; i++)
            {
                CodeInstruction instruction = instructionList[length - 1 - i];
                if (instruction.opcode == OpCodes.Ldfld && instructionList[length - 2 - i].IsLdarg(0))
                    return instruction.operand as FieldInfo;
            }
            return null;
        }
        private static bool IsBaseMethod(MethodInfo finalMethod, MethodInfo baseMethod)
        {
            MethodInfo currMethod = finalMethod;
            MethodInfo prevMethod = null;
            while (currMethod != prevMethod)
            {
                if (currMethod == baseMethod)
                    return true;
                prevMethod = currMethod;
                currMethod = currMethod.GetBaseDefinition();
            }
            return false;
        }
        private bool SearchToilGenerator(MethodInfo searchMethod, List<MethodInfo> ActionList, out List<MethodInfo> ActionsFound)
        {
            ActionsFound = new List<MethodInfo>();
            bool foundResult = false;
            List<CodeInstruction> instructionList;
            try { instructionList = PatchProcessor.GetCurrentInstructions(searchMethod); }
            catch { instructionList = PatchProcessor.GetOriginalInstructions(searchMethod); }
            foreach (var instruction in instructionList)
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Ldftn) &&
                    instruction.operand is MethodInfo calledMethod && ActionList.Any(t => IsBaseMethod(t, calledMethod)))
                {
                    foundResult = true;
                    ActionsFound.Add(calledMethod);
                    if (!FindAll)
                        return true;
                }
            return foundResult;
        }
        public bool SearchMoveNext(MethodInfo searchMethod, List<MethodInfo> ActionList, List<(MethodInfo generator, List<MethodInfo> actions)> ToilGenerators, ref EnumInfo enumInfo,
            out List<EnumItemPos<SavedList<MethodInfo>>> Results, out List<EnumItemInfo> toilInfo)
        {

            Results = new List<EnumItemPos<SavedList<MethodInfo>>>();
            toilInfo = new List<EnumItemInfo>();
            bool foundResult = false;
            bool foundSwitch = false;

            var toilLabelPos = new List<(Label label, int pos)>();
            var instructionList = PatchProcessor.GetCurrentInstructions(searchMethod);
            for (int i = 0; i < instructionList.Count; i++)
            {
                var instruction = instructionList[i];
                // Get stateInfo: assume the first int field loaded is it.
                if (instruction.IsLdarg(0))
                {
                    var nextInstruction = instructionList[i + 1];
                    if (nextInstruction.opcode == OpCodes.Ldfld && nextInstruction.operand is FieldInfo field && field.FieldType == typeof(int))
                    {
                        enumInfo.State = field;
                        if (instructionList[i + 2].IsStloc())
                            enumInfo.localState = instructionList[i + 2].ToLocalVar(searchMethod);
                        continue;
                    }
                }
                // Get the toil labels and index
                if (instruction.opcode == OpCodes.Switch && loadsState(instructionList[i - 1], enumInfo))
                {
                    foundSwitch = true;
                    var toilLabels = (Label[])instruction.operand;
                    for (int j = i + 1; j < instructionList.Count; j++)
                    {
                        var currInstruction = instructionList[j];
                        if (currInstruction.labels.NullOrEmpty())
                            continue;
                        bool found = false;
                        foreach (var label in currInstruction.labels)
                        {
                            if (toilLabels.Contains(label))
                            {
                                found = true;
                                toilLabelPos.Add((label, j));
                            }
                        }
                        if (found && toilLabelPos.Count == toilLabels.Length)
                            break;
                    }
                    break;
                }
                // Search branches
                if (instruction.Branches(out var label2) && loadsState(instructionList[i - 1], enumInfo))
                {
                    for (int j = i + 1; j < instructionList.Count; j++)
                    {
                        var currInstruction = instructionList[j];
                        if (!currInstruction.labels.NullOrEmpty() && currInstruction.labels.Contains(label2.Value))
                        {
                            toilLabelPos.Add((label2.Value, j));
                            break;
                        }
                    }
                    continue;
                }
                // Assamptuion: first return or leave is default
                if (instruction.opcode == OpCodes.Ret || instruction.opcode == OpCodes.Leave || instruction.opcode == OpCodes.Leave_S)
                    break;
            }
            // Did not find Current
            if (enumInfo.Current == null || toilLabelPos.EnumerableNullOrEmpty())
            {
                Log.Error($"[[LC]AutoPatcher]: TempError 8765499: {searchMethod.DeclaringType.DeclaringType} : {searchMethod.DeclaringType} : {searchMethod}\n" +
                    $"{enumInfo.Current} : {toilLabelPos.Count}");
                return false;
            }
            // No solution for method without Switch
            if (!foundSwitch)
                return false;
            // Determine Toil code range
            foreach (var toilLabel in toilLabelPos)
            {
                var pos = toilLabel.pos;
                var instruction = instructionList[pos];
                if (instruction.opcode == OpCodes.Br_S || instruction.opcode == OpCodes.Br)
                {
                    var label = (Label)instruction.operand;
                    for (int j = 0; j < instructionList.Count; j++)
                        if (instructionList[j].labels.Contains(label))
                        {
                            pos = j;
                            break;
                        }
                }
                var endPos = instructionList.Count - 1;
                foreach (var otherToilLabel in toilLabelPos)
                {
                    if (toilLabel.label == otherToilLabel.label)
                        continue;
                    var otherPos = otherToilLabel.pos;
                    if (otherPos > pos && otherPos < endPos)
                    {
                        endPos = otherPos - 1;
                        continue;
                    }
                    var otherInstruction = instructionList[otherPos];
                    if (otherInstruction.opcode == OpCodes.Br_S || otherInstruction.opcode == OpCodes.Br)
                    {
                        var label = (Label)otherInstruction.operand;
                        for (int j = 0; j < instructionList.Count; j++)
                            if (instructionList[j].labels.Contains(label))
                            {
                                otherPos = j;
                                break;
                            }
                        if (otherPos > pos && otherPos < endPos)
                            endPos = otherPos - 1;
                    }
                }
                toilInfo.Add(new EnumItemInfo(toilLabel.label, pos, endPos));
            }
            // Search each toil
            for (int toilIndex = 0; toilIndex < toilInfo.Count; toilIndex++)
            {
                var info = toilInfo[toilIndex];
                bool flagFound = false;
                var ActionsFound = new List<MethodInfo>();
                for (int i = info.startPos; i < info.endPos + 1; i++)
                {
                    var instruction = instructionList[i];
                    // Action being saved into Toil
                    if (instruction.Is(OpCodes.Newobj, Action_ctor))
                    {
                        var prevInstruction = instructionList[i - 1];
                        if (prevInstruction.opcode == OpCodes.Ldftn && prevInstruction.operand is MethodInfo method &&
                            ActionList.Contains(method))
                        {
                            flagFound = true;
                            ActionsFound.Add(method);
                            continue;
                        }
                    }
                    // Action inside a ToilGenerator
                    if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                        instruction.operand is MethodInfo calledMethod && ToilGenerators.Any(t => t.generator == calledMethod))
                    {
                        flagFound = true;
                        ActionsFound.AddRange(ToilGenerators.First(t => t.generator == calledMethod).actions);
                        continue;
                    }
                    // Storing the Toil
                    if (flagFound && instruction.StoresField(enumInfo.Current))
                    {
                        foundResult = true;
                        Results.Add(new EnumItemPos<SavedList<MethodInfo>>(toilIndex, i, (SavedList<MethodInfo>)ActionsFound));
                        ActionsFound = new List<MethodInfo>();
                        flagFound = false;
                        continue;
                    }
                    // End of Toil
                    /*if (instruction.opcode == OpCodes.Ret)
                    {
                        toilInfo.Add(new EnumItemInfo(labelPos.label, labelPos.pos, i));
                        toilIndex++;
                        break;
                    }*/
                }
            }
#if DEBUG
            if (foundResult)
            {
                var test = new System.Text.StringBuilder($"Test 1.1: {searchMethod.DeclaringType} : {searchMethod}\n");
                for (int i = 0; i < toilInfo.Count; i++)
                {
                    var info = toilInfo[i];
                    test.AppendLine($"[{i}] {info.startPos} : {info.endPos}");
                }
                test.AppendLine();
                for (int i = 0; i < instructionList.Count; i++)
                {
                    var ins = instructionList[i];
                    var str = "";
                    if (ins.operand is Label[] labels)
                    {
                        foreach (var label in labels)
                            for (int j = 0; j < instructionList.Count; j++)
                                if (instructionList[j].labels.Contains(label))
                                {
                                    str += $", {j}";
                                    break;
                                }
                    }
                    else if (ins.operand is Label label)
                        for (int j = 0; j < instructionList.Count; j++)
                            if (instructionList[j].labels.Contains(label))
                            {
                                str = $"{j}";
                                break;
                            }
                    test.AppendLine($"[{i}/{ins.labels.Count}] {ins.opcode} : {ins.operand} : [{ins.operand?.GetType()}] : {str}");
                }
                Log.Message(test.ToString());
            }
#endif
            return foundResult;
            bool loadsState(CodeInstruction instruction, EnumInfo info)
                => instruction.LoadsField(info.State) || (info.localState != null && instruction.IsLdloc(info.localState));
        }
        private static readonly ConstructorInfo Action_ctor = AccessTools.Constructor(typeof(Action), new[] { typeof(object), typeof(IntPtr) });
        /*private static readonly List<FieldInfo> ActionFields = new List<FieldInfo>()
        {
            AccessTools.Field(typeof(Toil), nameof(Toil.preInitActions)),
            AccessTools.Field(typeof(Toil), nameof(Toil.initAction)),
            AccessTools.Field(typeof(Toil), nameof(Toil.preTickActions)),
            AccessTools.Field(typeof(Toil), nameof(Toil.tickAction)),
            AccessTools.Field(typeof(Toil), nameof(Toil.finishActions)),
        };
        private static readonly List<MethodInfo> AddActionMethods = new List<MethodInfo>()
        {
            AccessTools.Method(typeof(Toil), nameof(Toil.AddPreInitAction)),
            AccessTools.Method(typeof(Toil), nameof(Toil.AddPreTickAction)),
            AccessTools.Method(typeof(Toil), nameof(Toil.AddFinishAction)),
        };*/
    }
}
