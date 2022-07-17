using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;

namespace AutoPatcher
{
    public class ActionSearch : SearchNode<Type, TypeMethod, TypeMethod, SavedList<ItemPos<MethodInfo>>>
    {
        protected bool findDeep = true;
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (node.fromSave)
                return true;
            var input = node.inputPorts[0].GetDataList<Type>();
            var output = new List<Type>();
            var targets = node.inputPorts[1].GetDataList<TypeMethod>();
            var foundPorts = FoundPorts(node);
            foreach (var target in targets.ToList())
                if (target.method.ReturnType == typeof(void))
                {
                    if (input.Contains(target.type))
                        output.AddDistinct(target.type);
                    ResultA(foundPorts).AddData(target);
                    ResultB(foundPorts).AddData(new List<ItemPos<MethodInfo>>() { (ItemPos<MethodInfo>)(-1, target.method) });
                }
            var targetMethods = targets.ConvertAll(t => t.method);
            foreach (Type type in input)
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.IsAbstract)
                        continue;
                    if (SearchMethod(method, targetMethods, out var searchResults))
                    {
                        output.AddDistinct(type);
                        ResultA(foundPorts).AddData((TypeMethod)(type, null, method));
                        ResultB(foundPorts).AddData((SavedList<ItemPos<MethodInfo>>)searchResults);
                    }
                }
                if (findDeep)
                    foreach (Type nType in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance))
                        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(nType))
                        {
                            if (method.IsAbstract)
                                continue;
                            if (SearchMethod(method, targetMethods, out var searchResults))
                            {
                                output.AddDistinct(type);
                                ResultA(foundPorts).AddData((TypeMethod)(type, nType, method));
                                ResultB(foundPorts).AddData((SavedList<ItemPos<MethodInfo>>)searchResults);
                            }
                        }
            }
            var actions = ResultB(foundPorts).GetData<List<ItemPos<MethodInfo>>>().SelectMany(t => t.Select(tt => tt.target)).ToList();
            actions.RemoveDuplicates();
            foundPorts[0].SetData(output);
            foundPorts[1].SetData(targets.Where(t => actions.Contains(t.method)).ToList());
            return true;
        }
        public virtual bool SearchMethod(MethodInfo searchMethod, List<MethodInfo> targets, out List<ItemPos<MethodInfo>> Results)
        {
            Results = new List<ItemPos<MethodInfo>>();
            List<CodeInstruction> instructionList;
            try { instructionList = PatchProcessor.GetCurrentInstructions(searchMethod); }
            catch { instructionList = PatchProcessor.GetOriginalInstructions(searchMethod); }
            bool foundResult = false;
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                    instruction.operand is MethodInfo calledMethod && targets.Exists(t => IsBaseMethod(t, calledMethod)))
                {
                    foundResult = true;
                    Results.Add((ItemPos<MethodInfo>)(i, calledMethod));
                    if (!FindAll)
                        return true;
                }
            }
            return foundResult;
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
    }
}
