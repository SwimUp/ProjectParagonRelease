using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace AutoPatcher.Utility
{
    public static class Utility
    {
        public static bool IsLdloc(this CodeInstruction instruction, LocalVar local)
        {
            if (!instruction.IsLdloc(local.builder))
                return false;
            if (local.builder != null)
                return true;
            return LocalVar.OpCodeToInt[instruction.opcode] == local.index;
        }
        public static bool IsStloc(this CodeInstruction instruction, LocalVar local)
        {
            if (!instruction.IsStloc(local.builder))
                return false;
            if (local.builder != null)
                return true;
            return LocalVar.OpCodeToInt[instruction.opcode] == local.index;
        }
        public static LocalVar ToLocalVar(this CodeInstruction instruction, MethodInfo method = null)
        {
            if (!instruction.IsLdloc() && !instruction.IsStloc())
                return null;
            if (instruction.operand is LocalBuilder builder)
                return new LocalVar(builder);
            return new LocalVar(LocalVar.OpCodeToInt[instruction.opcode]) { method = method };
        }

    }
}
