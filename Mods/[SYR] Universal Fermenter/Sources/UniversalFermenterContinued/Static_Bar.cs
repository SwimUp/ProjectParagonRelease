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
    public static class Static_Bar
    {
        public static readonly Vector2 Size = new Vector2(0.55f, 0.1f);

        public static readonly Color ZeroProgressColor = new Color(0.3f, 0.3f, 0.3f);

        public static readonly Color FermentedColor = new Color(0.9f, 0.85f, 0.2f);

        public static readonly Material UnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.2f, 0.2f, 0.2f));

        public static readonly Material BlackMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f));
    }
}
