using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using AutoPatcher.Utility;
using Verse.AI;
using System.Text;

namespace AutoPatcher
{
    // HarmonyStat
    public class HarmonyToil : EnumerableMethodPatchNode<SavedList<MethodInfo>, SavedList<EnumItemInfo>>
    {
        private MethodInfo PrepareMethod;
        private HarmonyMethod Transpiler;
        private HarmonyMethod Prefix;
        private HarmonyMethod Postfix;
        private HarmonyMethod Finalizer;
        private Type PatcherType;
        private string PrepareMethodName;
        private string TranspilerName;
        private string PrefixName;
        private string PostfixName;
        private string FinalizerName;
        public static bool successfull = true;
        public override void Initialize(bool fromSave = false)
        {
            base.Initialize(fromSave);
            PrepareMethod = PatcherType == null ? AccessTools.Method(PrepareMethodName) : AccessTools.Method(PatcherType, PrepareMethodName ?? "Prepare");
            var method = PatcherType == null ? AccessTools.Method(TranspilerName) : AccessTools.Method(PatcherType, TranspilerName ?? "Transpiler");
            if (method != null)
                Transpiler = new HarmonyMethod(method);
            method = PatcherType == null ? AccessTools.Method(PrefixName) : AccessTools.Method(PatcherType, PrefixName ?? "Prefix");
            if (method != null)
                Prefix = new HarmonyMethod(method);
            method = PatcherType == null ? AccessTools.Method(PostfixName) : AccessTools.Method(PatcherType, PostfixName ?? "Postfix");
            if (method != null)
                Postfix = new HarmonyMethod(method);
            method = PatcherType == null ? AccessTools.Method(FinalizerName) : AccessTools.Method(PatcherType, FinalizerName ?? "Finalizer");
            if (method != null)
                Finalizer = new HarmonyMethod(method);
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            var TypeMethods = node.inputPorts[0].GetDataList<TypeMethod>();
            var targetCI = TargetPos(node.inputPorts).GetDataList<List<EnumItemPos<SavedList<MethodInfo>>>>();
            var toilInfoList = InputA(node.inputPorts).GetDataList<List<EnumItemInfo>>();
            var enumInfoList = EnumInfo(node.inputPorts).GetDataList<EnumInfo>();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                method = AccessTools.Method(type, "MakeNewToils");
                var positions = targetCI[i];
                var actions = positions.SelectMany(t => t.target).ToList();
                var toilInfo = toilInfoList[i];
                var enumInfo = enumInfoList[i];
                actions.RemoveDuplicates();
                if (Helper_Prepare(type, ntype, method, enumInfo, actions, toilInfo))
                {
                    harmony.Patch(method, Prefix, Postfix, Transpiler, Finalizer);
                    if (successfull)
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                }
            }
            return true;
        }
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, EnumInfo enumInfo, List<MethodInfo> actions, List<EnumItemInfo> toilInfo)
        {
            if (PrepareMethod == null)
                return true;
            var param = PrepareMethod.GetParameters();
            var inp3 = param[3];
            var inp5 = param[5];
            var inp5_Type = inp5.ParameterType.GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.First();
            if (!enumInfo.TryCastTo(inp3.ParameterType, out var cenumInfo))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 468735468: {PrepareMethod.DeclaringType} : {PrepareMethod}\n" +
                    $"{enumInfo.GetType()} -> {inp3.ParameterType}");
                return false;
            }
            if (!toilInfo.TryCastTo<EnumItemInfo>(inp5_Type, out var ctoilInfo))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 879466879: {PrepareMethod.DeclaringType} : {PrepareMethod}\n" +
                    $"{toilInfo.GetType()} -> {inp5.ParameterType}");
                return false;
            }
            var res = (bool)PrepareMethod.Invoke(null, new object[] { type, ntype, method, cenumInfo, actions, ctoilInfo });
            return res;
        }
    }
    // AP_ToilPatch
    public class AP_ToilPatch : EnumerableMethodPatchNode<SavedList<MethodInfo>, SavedList<EnumItemInfo>>
    {
        private Type HelperType;
        private MethodInfo HelperPrepare;
        private MethodInfo HelperTranspile;
        private static MethodInfo HelperTranspileStatic;
        private static List<EnumItemPos<SavedList<MethodInfo>>> Targets;
        protected static bool successfull = true;
        public override void Initialize(bool fromSave = false)
        {
            HelperPrepare = AccessTools.Method(HelperType, "Prepare");
            HelperTranspile = AccessTools.Method(HelperType, "Transpile");
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            HelperTranspileStatic = HelperTranspile;
            var thisTranspiler = new HarmonyMethod(AccessTools.Method(typeof(AP_ToilPatch), "Transpiler"));
            var TypeMethods = node.inputPorts[0].GetDataList<TypeMethod>();
            var targetCI = TargetPos(node.inputPorts).GetDataList<List<EnumItemPos<SavedList<MethodInfo>>>>();
            var toilInfoList = InputA(node.inputPorts).GetDataList<List<EnumItemInfo>>();
            var enumInfoList = EnumInfo(node.inputPorts).GetDataList<EnumInfo>();
            DebugLevel = node.DebugLevel;
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                Targets = targetCI[i];
                List<MethodInfo> actions = Targets.SelectMany(t => t.target).ToList();
                var toilInfo = toilInfoList[i];
                var enumInfo = enumInfoList[i];
                switchField = enumInfo.State;
                actions.RemoveDuplicates();
                if (Helper_Prepare(type, ntype, method, enumInfo, actions, toilInfo))
                {
                    currMethod = method;
                    harmony.Patch(method, transpiler: thisTranspiler);
                    if (successfull)
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                }
            }
            return true;
        }
        private static MethodInfo currMethod;
        private static List<(int pos, int offset)> Offsets;
        private static List<(Label label, int pos, int length)> NewItems;
        private static FieldInfo switchField;
        private static int DebugLevel;
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var Original = (MethodInfo)original;
            List<CodeInstruction> instructionList = instructions.ToList();
            Offsets = new List<(int, int)>();
            NewItems = new List<(Label, int, int)>();
            Targets.SortBy(t => t.pos);
            successfull = false;
            foreach (var target in Targets)
            {
                int pos = target.pos;
                foreach (var offset in Offsets)
                    if (pos >= offset.pos)
                        pos += offset.offset;
                if (Helper_Transpile(ref instructionList, generator, pos, target.target, out var offsets, out var newItems))
                {
                    successfull = true;
                    if (!offsets.NullOrEmpty())
                        Offsets.AddRange(offsets);
                    if (!newItems.NullOrEmpty())
                        NewItems.AddRange(newItems);
                }
            }
            NewItems.SortBy(t => t.pos);
            NodeUtility.RegisterOffset(Original, Offsets);
            NodeUtility.AddEnumerableItem(Original, NewItems);
            if (successfull && NodeUtility.enumerableItems.TryGetValue(Original, out var items))
            {
                for (int i = 0; i < instructionList.Count; i++)
                    if (instructionList[i].opcode == OpCodes.Switch)
                    {
                        instructionList[i].operand = items.Select(t => t.label).ToArray();
                        break;
                    }
                int offset = 0;
                foreach (var item in items)
                {
                    if (NewItems.Any(t => item.startPos == t.pos))
                        continue;
                    if (NewItems.Any(t => item.startPos > t.pos))
                    {
                        offset++;
                        NewItems.RemoveAll(t => item.startPos > t.pos);
                    }
                    for (int i = item.startPos; i <= item.endPos; i++)
                    {
                        var ins = instructionList[i];
                        var ins2 = instructionList[i - 1];
                        if (ins.StoresField(switchField) && ins2.LoadsConstant())
                        {
                            if (ins2.LoadsConstant(-1))
                                continue;
                            if (ins2.opcode == OpCodes.Ldc_I4_S || ins2.opcode == OpCodes.Ldc_I4)
                            {
                                int val = (sbyte)ins2.operand;
                                if (val < 0)
                                    continue;
                                instructionList[i - 1] = new CodeInstruction(OpCodes.Ldc_I4, val + offset);
                            }
                            else
                                for (int l = 0; l < 9; l++)
                                    if (ins2.LoadsConstant(l))
                                    {
                                        instructionList[i - 1] = new CodeInstruction(OpCodes.Ldc_I4, l + offset);
                                        break;
                                    }
                        }
                    }
                }
            }
            if (!successfull)
                return null;
            if (DebugLevel > 4)
            {
                var test = new StringBuilder($"[[LC]AutoPatcher] Debug ToilPatch Node : DebugLevel = {DebugLevel}\n{Original.DeclaringType} : {Original}\n");
                items = NodeUtility.enumerableItems[Original];
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    test.AppendLine($"[{i}] {item.startPos} : {item.endPos}");
                }
                test.AppendLine();
                foreach (var target in Targets)
                {
                    test.Append($"{target.index} : {target.pos} :");
                    foreach (var item in target.target)
                        test.Append($", {item}");
                    test.AppendLine();
                }
                test.AppendLine();
                foreach (var offset in Offsets)
                {
                    test.AppendLine($"{offset.pos} : {offset.offset}");
                }
                test.AppendLine();
                foreach (var item in NewItems)
                {
                    test.AppendLine($"{item.pos} : {item.length}");
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
            return instructionList;
        }
        public static bool Helper_Transpile(ref List<CodeInstruction> instructions, ILGenerator generator, int pos, List<MethodInfo> actions, out List<(int pos, int offset)> CIOffsets, out List<(Label,int,int)> newItems)
        {
            var param = new object[] { instructions, generator, pos, actions, null, null };
            var res = (bool)HelperTranspileStatic.Invoke(null, param);
            CIOffsets = (List<(int,int)>)param[4];
            var param5 = param[5];
            if (!param5.TryCastTo(out newItems))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 468735468: {HelperTranspileStatic.DeclaringType} : {HelperTranspileStatic}\n" +
                    $"{param5.GetType()} -> {typeof(List<(Label, int, int)>)}");
                return false;
            }
            newItems = (List<(Label,int,int)>)param[5];
            return res;
        }
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, EnumInfo enumInfo, List<MethodInfo> actions, List<EnumItemInfo> toilInfo)
        {
            if (HelperPrepare == null)
                return true;
            var param = HelperPrepare.GetParameters();
            var inp3 = param[3];
            var inp5 = param[5];
            var inp5_Type = inp5.ParameterType.GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.First();
            if (!enumInfo.TryCastTo(inp3.ParameterType, out var cenumInfo))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 468735468: {HelperPrepare.DeclaringType} : {HelperPrepare}\n" +
                    $"{enumInfo.GetType()} -> {inp3.ParameterType}");
                return false;
            }
            if (!toilInfo.TryCastTo<EnumItemInfo>(inp5_Type, out var ctoilInfo))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 879466879: {HelperPrepare.DeclaringType} : {HelperPrepare}\n" +
                    $"{toilInfo.GetType()} -> {inp5.ParameterType}");
                return false;
            }
            var res = (bool)HelperPrepare.Invoke(null, new object[] { type, ntype, method, cenumInfo, actions, ctoilInfo });
            return res;
        }
    }
}
