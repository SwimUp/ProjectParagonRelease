using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class QualityDays
    {
        public float awful;

        public float poor;

        public float normal;

        public float good;

        public float excellent;

        public float masterwork;

        public float legendary;

        public QualityDays()
        {
        }

        public QualityDays(float awful, float poor, float normal, float good, float excellent, float masterwork, float legendary)
        {
            this.awful = awful;
            this.poor = poor;
            this.normal = normal;
            this.good = good;
            this.excellent = excellent;
            this.masterwork = masterwork;
            this.legendary = legendary;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("UF: QualityDays configured incorrectly");
                return;
            }
            string[] array = xmlRoot.FirstChild.Value.TrimStart('(').TrimEnd(')').Split(',');
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            awful = Convert.ToSingle(array[0], invariantCulture);
            poor = Convert.ToSingle(array[1], invariantCulture);
            normal = Convert.ToSingle(array[2], invariantCulture);
            good = Convert.ToSingle(array[3], invariantCulture);
            excellent = Convert.ToSingle(array[4], invariantCulture);
            masterwork = Convert.ToSingle(array[5], invariantCulture);
            legendary = Convert.ToSingle(array[6], invariantCulture);
        }
    }
}
