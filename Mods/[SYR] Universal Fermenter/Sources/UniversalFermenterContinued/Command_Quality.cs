using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class Command_Quality : Command_Action
    {
        public QualityCategory qualityToTarget;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (QualityCategory quality in Enum.GetValues(typeof(QualityCategory)))
                {
                    list.Add(new FloatMenuOption(quality.GetLabel(), delegate
                    {
                        ChangeQuality(qualityToTarget, quality);
                    }, (Texture2D)UF_Utility.qualityMaterials[quality].mainTexture, Color.white));
                }
                return list;
            }
        }

        internal static void ChangeQuality(QualityCategory qualityToTarget, QualityCategory quality)
        {
            foreach (Thing item in Find.Selector.SelectedObjects.OfType<Thing>())
            {
                CompUniversalFermenter compUniversalFermenter = item.TryGetComp<CompUniversalFermenter>();
                if (compUniversalFermenter != null && compUniversalFermenter.CurrentProcess.usesQuality && compUniversalFermenter.TargetQuality == qualityToTarget)
                {
                    compUniversalFermenter.TargetQuality = quality;
                }
            }
        }
    }
}
