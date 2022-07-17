using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AutoPatcher.Harmony
{
    [HarmonyPatch(typeof(GenTypes))]
    [HarmonyPatch("GetTypeInAnyAssemblyRaw")]
    public static class Patch_BackCompatibility_GetBackCompatibleType
    {
        public static void Postfix(ref Type __result, string typeName)
        {
            if (__result == null && typeName.Contains('`'))
            {
                try
                {
                    __result = GetGenericType(typeName);
                }
                catch
                {
                    Log.ErrorOnce($"[[LC]AutoPatcher]TempError 4987654: Could not get generic type for: {typeName}", typeName.GetHashCode());
                }
            }
        }
        private static Type GetGenericType(string str)
        {
            if (!str.Contains('['))
                return AccessTools.TypeByName(str);
            Type type = null;
            var i1 = str.IndexOf('[');
            var i2 = str.LastIndexOf(']');
            var genericType = AccessTools.TypeByName(str.Substring(0, i1));
            var paramTypes = GetParameters(str.Substring(i1 + 1, i2 - i1 - 1)).Select(t => GetGenericType(t)).ToArray();
            type = genericType.MakeGenericType(paramTypes);
            return type;
        }
        private static IEnumerable<string> GetParameters(string str)
        {
            int layer = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '[')
                    layer++;
                else if (str[i] == ']')
                    layer--;
                else if (layer == 0 && str[i] == ',')
                {
                    yield return str.Substring(0, i);
                    str = str.Remove(0, i + 1);
                    i = 0;
                }
            }
            yield return str;
        }
    }
}
