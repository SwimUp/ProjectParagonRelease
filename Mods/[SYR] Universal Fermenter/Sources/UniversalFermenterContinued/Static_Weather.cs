using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    [StaticConstructorOnStartup]
    public static class Static_Weather
    {
        public static readonly FloatRange SunGlowRange = new FloatRange(0f, 1f);

        public static readonly FloatRange SnowRateRange = new FloatRange(0f, 1.2f);

        public static readonly FloatRange RainRateRange = new FloatRange(0f, 1f);

        public static readonly FloatRange WindSpeedRange = new FloatRange(0f, 3f);
    }
}
