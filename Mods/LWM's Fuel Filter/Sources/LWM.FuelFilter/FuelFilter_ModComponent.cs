using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LWM.FuelFilter
{
    public class FuelFilter_ModComponent : Mod
    {
        public FuelFilter_ModComponent(ModContentPack content)
            : base(content)
        {
            try
            {
                new Harmony("net.littlewhitemouse.fuelfilter").PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Log.Error("LWM's FuelFilter :: Caught exception: " + ex);
            }
        }
    }
}
