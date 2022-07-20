using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SmartSpeed.Detouring
{
    public static class Detour
    {
        private static readonly List<string> detoured = new List<string>();

        private static readonly List<string> destinations = new List<string>();

        public unsafe static bool TryDetourFromTo(MethodInfo source, MethodInfo destination)
        {
            if (source == null)
            {
                Log.Error("Source MethodInfo is null: Detour");
                return false;
            }
            if (destination == null)
            {
                Log.Error("Destination MethodInfo is null: Detour");
                return false;
            }
            string item = string.Concat(new string[]
            {
                source.DeclaringType.FullName,
                ".",
                source.Name,
                " @ 0x",
                source.MethodHandle.GetFunctionPointer().ToString("X" + IntPtr.Size * 2)
            });
            string item2 = string.Concat(new string[]
            {
                destination.DeclaringType.FullName,
                ".",
                destination.Name,
                " @ 0x",
                destination.MethodHandle.GetFunctionPointer().ToString("X" + IntPtr.Size * 2)
            });
            Detour.detoured.Add(item);
            Detour.destinations.Add(item2);
            if (IntPtr.Size == 8)
            {
                // 64-bit systems use 64-bit absolute address and jumps
                // 12 byte destructive

                // Get function pointers
                var Source_Base = source.MethodHandle.GetFunctionPointer().ToInt64();
                var Destination_Base = destination.MethodHandle.GetFunctionPointer().ToInt64();

                // Native source address
                var Pointer_Raw_Source = (byte*)Source_Base;

                // Pointer to insert jump address into native code
                var Pointer_Raw_Address = (long*)(Pointer_Raw_Source + 0x02);

                // Insert 64-bit absolute jump into native code (address in rax)
                // mov rax, immediate64
                // jmp [rax]
                *(Pointer_Raw_Source + 0x00) = 0x48;
                *(Pointer_Raw_Source + 0x01) = 0xB8;
                *Pointer_Raw_Address = Destination_Base; // ( Pointer_Raw_Source + 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 )
                *(Pointer_Raw_Source + 0x0A) = 0xFF;
                *(Pointer_Raw_Source + 0x0B) = 0xE0;
            }
            else
            {
                // 32-bit systems use 32-bit relative offset and jump
                // 5 byte destructive

                // Get function pointers
                var Source_Base = source.MethodHandle.GetFunctionPointer().ToInt32();
                var Destination_Base = destination.MethodHandle.GetFunctionPointer().ToInt32();

                // Native source address
                var Pointer_Raw_Source = (byte*)Source_Base;

                // Pointer to insert jump address into native code
                var Pointer_Raw_Address = (int*)(Pointer_Raw_Source + 1);

                // Jump offset (less instruction size)
                var offset = Destination_Base - Source_Base - 5;

                // Insert 32-bit relative jump into native code
                *Pointer_Raw_Source = 0xE9;
                *Pointer_Raw_Address = offset;
            }
            return true;
        }
    }
}
