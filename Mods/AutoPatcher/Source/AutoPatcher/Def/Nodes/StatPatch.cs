using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using AutoPatcher.Utility;

namespace AutoPatcher
{
    // ReplaceStat
    public class ReplaceStat : MethodPatchNode<StatDef>
    {
        private Dictionary<StatDef, StatDef> replaceStat;
        private Dictionary<StatDef, FieldInfo> replaceStatField;
        private static Dictionary<StatDef, FieldInfo> replaceStatFieldStatic;
        protected static bool successfull = true;
        private static List<ItemPos<StatDef>> Targets;
        public override void Initialize(bool fromSave = false)
        {
            if (!replaceStatField.EnumerableNullOrEmpty())
                return;
            if (!replaceStat.EnumerableNullOrEmpty())
            {
                List<StatDef> statOut = replaceStat.Values.ToList();
                statOut.RemoveDuplicates();
                replaceStatField = replaceStat.Keys.ToDictionary(t => t, t => null as FieldInfo);
                foreach (Type type in GenTypes.AllTypesWithAttribute<DefOf>())
                    foreach (FieldInfo field in type.GetFields().Where(t => t.FieldType == typeof(StatDef)))
                        if (statOut.FirstOrFallback(t => field.Name.Equals(t.defName)) is StatDef stat && !replaceStatField.ContainsKey(stat))
                            foreach (KeyValuePair<StatDef, StatDef> pair in replaceStat.Where(t => t.Value == stat))
                                replaceStatField[pair.Key] = field;
                return;
            }
            Log.Error($"[[LC]AutoPatcher]: TempError 451356");
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(ReplaceStat), "Transpiler"));
            var TypeMethods = node.inputPorts[0].GetDataList<TypeMethod>();
            var targetCI = TargetPos(node.inputPorts).GetDataList<List<ItemPos<StatDef>>>();
            replaceStatFieldStatic = replaceStatField;
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Targets = targetCI[i];
                harmony.Patch(TypeMethods[i].method, transpiler: transpiler);
                if (successfull)
                    SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
            }
            return true;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            successfull = false;
            List<CodeInstruction> instuctionList = instructions.ToList();
            foreach ((int pos, StatDef stat) target in Targets)
                if (replaceStatFieldStatic.ContainsKey(target.stat))
                {
                    successfull = true;
                    instuctionList[target.pos].operand = replaceStatFieldStatic[target.stat];
                }
            return instuctionList;
        }
    }
    // HarmonyStat
    public class HarmonyStat : MethodPatchNode<StatDef>
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
            var PositionsList = TargetPos(node.inputPorts).GetDataList<List<ItemPos<StatDef>>>();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                var positions = PositionsList[i];
                if (Helper_Prepare(type, ntype, method, positions))
                {
                    harmony.Patch(method, Prefix, Postfix, Transpiler, Finalizer);
                    if (successfull)
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                }
            }
            return true;
        }
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, List<ItemPos<StatDef>> Positions)
            => PrepareMethod?.Invoke(null, new object[] { type, ntype, method, Positions }).ChangeType<bool>() ?? true;
    }
    // AP_StatPatch
    public class AP_StatPatch : MethodPatchNode<StatDef>
    {
        private Type HelperType;
        private MethodInfo HelperPrepare;
        private MethodInfo HelperTranspile;
        private static MethodInfo HelperTranspileStatic;
        private static List<ItemPos<StatDef>> Targets;
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
            var thisTranspiler = new HarmonyMethod(AccessTools.Method(typeof(AP_StatPatch), "Transpiler"));
            var TypeMethods = node.inputPorts[0].GetDataList<TypeMethod>();
            var targetCI = TargetPos(node.inputPorts).GetDataList<List<ItemPos<StatDef>>>();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                Targets = targetCI[i];
                List<StatDef> stats = Targets.ConvertAll(t => t.target);
                if (HelperPrepare == null || Helper_Prepare(type, ntype, method, stats))
                {
                    harmony.Patch(method, transpiler: thisTranspiler);
                    if (successfull)
                    {
                        NodeUtility.RegisterOffset(method, Offsets);
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                    }
                }
            }
            return true;
        }
        private static List<(int pos, int offset)> Offsets;
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            Offsets = new List<(int, int)>();
            Targets.SortBy(t => t.pos);
            successfull = false;
            foreach (var target in Targets)
            {
                int pos = target.pos;
                foreach (var offset in Offsets)
                    if (pos >= offset.pos)
                        pos += offset.offset;
                if (Helper_Transpile(ref instructionList, generator, pos, target.target, out var offsets))
                {
                    successfull = true;
                    if (!offsets.NullOrEmpty())
                        Offsets.AddRange(offsets);
                }
            }
            if (!successfull)
                return null;
            return instructionList;
        }
        public static bool Helper_Transpile(ref List<CodeInstruction> instructions, ILGenerator generator, int pos, StatDef stat, out List<(int pos, int offset)> CIOffsets)
        {
            var param = new object[] { instructions, generator, pos, stat, null };
            var res = (bool)HelperTranspileStatic.Invoke(null, param);
            CIOffsets = (List<(int, int)>)param[4];
            return res;
        }
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, List<StatDef> stats)
            => (bool)HelperPrepare.Invoke(null, new object[] { type, ntype, method, stats });
    }
}
