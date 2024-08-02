using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class UF_Settings : ModSettings
    {
        public static bool showProcessIconGlobal = true;

        public static float processIconSize = 0.6f;

        public static bool showCurrentQualityIcon = true;

        public static bool showTargetQualityIcon = false;

        public static bool singleItemIcon = true;

        public static bool sortAlphabetically = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref showProcessIconGlobal, "UF_showProcessIconGlobal", defaultValue: true, forceSave: true);
            Scribe_Values.Look(ref processIconSize, "UF_processIconSize", 0.6f, forceSave: true);
            Scribe_Values.Look(ref showCurrentQualityIcon, "UF_showCurrentQualityIcon", defaultValue: true, forceSave: true);
            Scribe_Values.Look(ref showTargetQualityIcon, "UF_showTargetQualityIcon", defaultValue: false, forceSave: true);
            Scribe_Values.Look(ref singleItemIcon, "UF_singleItemIcon", defaultValue: true, forceSave: true);
            Scribe_Values.Look(ref sortAlphabetically, "UF_sortAlphabetically", defaultValue: false, forceSave: true);
        }
    }
}
