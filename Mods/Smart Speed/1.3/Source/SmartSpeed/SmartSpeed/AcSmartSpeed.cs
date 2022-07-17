using SmartSpeed.Detouring;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace SmartSpeed
{
    [StaticConstructorOnStartup]
    public class AcSmartSpeed
    {
        public enum Option
        {
            Slow,
            Normal,
            Fast,
            Half,
            Ignore
        }

        public static AcSmartSpeed.Option currSetting;

        private static Assembly Assembly
        {
            get
            {
                return Assembly.GetAssembly(typeof(AcSmartSpeed));
            }
        }

        private static string AssemblyName
        {
            get
            {
                return AcSmartSpeed.Assembly.FullName.Split(new char[]
                {
                    ','
                }).First<string>();
            }
        }

        public string ModIdentifier
        {
            get
            {
                return AcSmartSpeed.AssemblyName;
            }
        }

        static AcSmartSpeed()
        {
            AcSmartSpeed.currSetting = AcSmartSpeed.Option.Normal;
            AcSmartSpeed.InitUltraFastMode();
            AcSmartSpeed.InitEventSpeedControl();
           // Log.Message(AcSmartSpeed.AssemblyName + " injected.");
        }

        private static void InitUltraFastMode()
        {
            MethodInfo source = typeof(RimWorld.TimeControls).GetMethod("DoTimeControlsGUI", BindingFlags.Static | BindingFlags.Public);
            MethodInfo dest = typeof(Detouring.TimeControls).GetMethod("DoTimeControlsGUI", BindingFlags.Static | BindingFlags.Public);
            Detour.TryDetourFromTo(source, dest);

        }

        private static void InitEventSpeedControl()
        {
            MethodInfo source = typeof(Verse.TickManager).GetProperty("TickRateMultiplier", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            MethodInfo dest = typeof(Detouring.TickManager).GetProperty("TickRateMultiplier", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            Detour.TryDetourFromTo(source, dest);

            source = typeof(Detouring.TickManager).GetMethod("NothingHappeningInGame", BindingFlags.Instance | BindingFlags.NonPublic);
            dest = typeof(Verse.TickManager).GetMethod("NothingHappeningInGame", BindingFlags.Instance | BindingFlags.NonPublic);
            Detour.TryDetourFromTo(source, dest);

        }
    }
}