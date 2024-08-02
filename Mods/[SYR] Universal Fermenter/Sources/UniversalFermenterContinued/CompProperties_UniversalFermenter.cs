using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public class CompProperties_UniversalFermenter : CompProperties
    {
        public List<UF_Process> products = new List<UF_Process>();

        public List<UF_Process> processes = new List<UF_Process>();

        public bool showProductIcon = true;

        public Vector2 barOffset = new Vector2(0f, 0.25f);

        public Vector2 barScale = new Vector2(1f, 1f);

        public Vector2 productIconSize = new Vector2(1f, 1f);

        public CompProperties_UniversalFermenter()
        {
            compClass = typeof(CompUniversalFermenter);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            for (int i = 0; i < processes.Count; i++)
            {
                processes[i].ResolveReferences();
            }
        }
    }
}
