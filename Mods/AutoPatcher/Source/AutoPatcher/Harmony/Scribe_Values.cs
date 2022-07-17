using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace AutoPatcher.Harmony
{
    /*[HarmonyPatch]
    //[HarmonyPatch(typeof(Scribe_Values), nameof(Scribe_Values.Look))]
    public static class Patch_Scribe_Values_Look
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            // yield return AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look));
            // yield return AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new Type[] { typeof(MethodInfo) });
            // yield return AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new Type[] { typeof(FieldInfo) });
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var type = original.GetGenericArguments().First();
            var instructionList = instructions.ToList();
            var label = generator.DefineLabel();
            var token = instructionList.First(t => t.opcode == OpCodes.Ldtoken).operand;
            instructionList[0].labels.Add(label);
            instructionList.InsertRange(0, new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldtoken, token),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle))),
                new CodeInstruction(OpCodes.Ldarg_0, null),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Scribe_Values_Look), nameof(IsChanged))),
                new CodeInstruction(OpCodes.Brfalse, label),
                new CodeInstruction(OpCodes.Ldarg_0, null),
                new CodeInstruction(OpCodes.Ldarg_1, null),
                new CodeInstruction(OpCodes.Ldarg_2, null),
                new CodeInstruction(OpCodes.Ldarg_3, null),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Scribe_Values_Look), nameof(Scribe_Value)).MakeGenericMethod(type)),
                new CodeInstruction(OpCodes.Ret, null),
            });
            return instructionList;
        }
        private static bool IsChanged(Type type, ref object value)
        {
            return (type == typeof(MethodInfo) || type == typeof(FieldInfo)) && (value == null || typeof(MethodInfo).IsAssignableFrom(value.GetType()) || typeof(FieldInfo).IsAssignableFrom(value.GetType()));
        }
        private static void Scribe_Value<T>(ref object value, string label, T defaultValue, bool forceSave)
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var node = Scribe.loader.curXmlParent[label];
                if (node != null)
                {
                    value = ScribeExtractor.ValueFromNode(node, defaultValue);
                    if (value != null)
                        return;
                }
            }
            else if (Scribe.mode == LoadSaveMode.Saving && value == null)
            {
                if (Scribe.EnterNode(label))
                {
                    try
                    {
                        Scribe.saver.WriteAttribute("IsNull", "True");
                    }
                    finally
                    {
                        Scribe.ExitNode();
                    }
                }
                return;
            }
            var type = value?.GetType();
            if (typeof(MethodInfo).IsAssignableFrom(type))
            {
                var method = value as MethodInfo;
                Scribe_MethodInfo(ref method, label);
                value = method;
            }
            else if (typeof(FieldInfo).IsAssignableFrom(type))
            {
                var field = value as FieldInfo;
                Scribe_FieldInfo(ref field, label);
                value = field;
            }
            else
                Scribe_Unknown(ref value, label);
        }
        public static void Scribe_MethodInfo(ref MethodInfo method, string label)
        {
            string method_name;
            Type method_type;
            List<Type> method_param;
            List<Type> method_generics;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                method_name = method.Name;
                method_type = method.DeclaringType;
                method_param = method.GetParameters().Select(t => t.ParameterType).ToList();
                method_generics = method.GetGenericArguments().ToList();
                method_param = method_param.NullOrEmpty() ? null : method_param;
                method_generics = method_generics.NullOrEmpty() ? null : method_generics;
                Scribe.saver.WriteElement(label + "_Name", method_name);
                Scribe.saver.WriteElement(label + "_Type", method_type.Name);
                Scribe_Collections.Look(ref method_param, label + "_Param");
                Scribe_Collections.Look(ref method_generics, label + "_Generics");
                return;
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                method_name = ScribeExtractor.ValueFromNode<string>(Scribe.loader.curXmlParent[label + "_Name"], null);
                if (method_name == null)
                    return;
                method_type = ScribeExtractor.ValueFromNode<Type>(Scribe.loader.curXmlParent[label + "_Type"], null);
                if (method_type == null)
                    return;
                method_param = null;
                method_generics = null;
                Scribe_Collections.Look(ref method_param, label + "_Param");
                Scribe_Collections.Look(ref method_generics, label + "_Generics");
                method_param = method_param.NullOrEmpty() ? null : method_param;
                method_generics = method_generics.NullOrEmpty() ? null : method_generics;
                method = AccessTools.Method(method_type, method_name, method_param?.ToArray(), method_generics?.ToArray());
            }
        }
        public static void Scribe_FieldInfo(ref FieldInfo field, string label)
        {
            string field_name;
            Type field_type;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                field_name = field.Name;
                field_type = field.DeclaringType;
                Scribe.saver.WriteElement(label + "_Name", field_name);
                Scribe.saver.WriteElement(label + "_Type", field_type.Name);
                return;
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                field_name = ScribeExtractor.ValueFromNode<string>(Scribe.loader.curXmlParent[label + "_Name"], null);
                if (field_name == null)
                    return;
                field_type = ScribeExtractor.ValueFromNode<Type>(Scribe.loader.curXmlParent[label + "_Type"], null);
                if (field_type == null)
                    return;
                field = AccessTools.Field(field_type, field_name);
            }
        }
        public static void Scribe_Unknown(ref object obj, string label)
        {
            string obj_name;
            Type obj_type;
            List<Type> obj_param;
            List<Type> obj_generics;
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var node = Scribe.loader.curXmlParent[label + "_Name"];
                if (node == null)
                    return;
                obj_name = ScribeExtractor.ValueFromNode<string>(node, null);
                if (obj_name == null)
                    return;
                node = Scribe.loader.curXmlParent[label + "_Type"];
                if (node == null)
                    return;
                obj_type = ScribeExtractor.ValueFromNode<Type>(node, null);
                if (obj_type == null)
                    return;
                obj_param = null;
                obj_generics = null;
                node = Scribe.loader.curXmlParent[label + "_Param"];
                if (node == null)
                {
                    obj = AccessTools.Field(obj_type, obj_name);
                    return;
                }
                node = Scribe.loader.curXmlParent[label + "_Generics"];
                if (node == null)
                    return;
                Scribe_Collections.Look(ref obj_param, label + "_Param");
                Scribe_Collections.Look(ref obj_generics, label + "_Generics");
                obj_param = obj_param.NullOrEmpty() ? null : obj_param;
                obj_generics = obj_generics.NullOrEmpty() ? null : obj_generics;
                obj = AccessTools.Method(obj_type, obj_name, obj_param?.ToArray(), obj_generics?.ToArray());
            }
        }
    }*/
}
