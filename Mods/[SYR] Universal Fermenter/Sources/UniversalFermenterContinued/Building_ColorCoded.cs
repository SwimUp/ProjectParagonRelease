using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class Building_ColorCoded : Building
    {
        public override Color DrawColorTwo
        {
            get
            {
                CompUniversalFermenter compUniversalFermenter = this.TryGetComp<CompUniversalFermenter>();
                if (compUniversalFermenter != null && compUniversalFermenter.CurrentProcess.colorCoded)
                {
                    return compUniversalFermenter.CurrentProcess.color;
                }
                return DrawColor;
            }
        }
    }
}
