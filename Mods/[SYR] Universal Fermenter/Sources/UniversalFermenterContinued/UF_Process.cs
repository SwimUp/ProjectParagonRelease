using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class UF_Process
    {
        public int uniqueID;

        public ThingDef thingDef;

        public ThingFilter ingredientFilter = new ThingFilter();

        public bool usesTemperature = true;

        public FloatRange temperatureSafe = new FloatRange(-1f, 32f);

        public FloatRange temperatureIdeal = new FloatRange(7f, 32f);

        public float ruinedPerDegreePerHour = 2.5f;

        public float speedBelowSafe = 0.1f;

        public float speedAboveSafe = 1f;

        public float processDays = 6f;

        public int maxCapacity = 25;

        public float efficiency = 1f;

        public FloatRange sunFactor = new FloatRange(1f, 1f);

        public FloatRange rainFactor = new FloatRange(1f, 1f);

        public FloatRange snowFactor = new FloatRange(1f, 1f);

        public FloatRange windFactor = new FloatRange(1f, 1f);

        public string graphicSuffix;

        public bool usesQuality;

        public QualityDays qualityDays = new QualityDays(1f, 0f, 0f, 0f, 0f, 0f, 0f);

        public bool colorCoded;

        public Color color = new Color(1f, 1f, 1f);

        public string customLabel = "";

        public void ResolveReferences()
        {
            ingredientFilter.ResolveReferences();
        }
    }
}
