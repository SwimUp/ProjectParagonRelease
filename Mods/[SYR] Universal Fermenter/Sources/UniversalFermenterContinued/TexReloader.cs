using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UniversalFermenter
{
    public static class TexReloader
    {
        public static void Reload(Thing t, string texPath)
        {
            Graphic value = GraphicDatabase.Get(t.def.graphicData.graphicClass, texPath, ShaderDatabase.LoadShader(t.def.graphicData.shaderType.shaderPath), t.def.graphicData.drawSize, t.DrawColor, t.DrawColorTwo);
            typeof(Thing).GetField("graphicInt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(t, value);
            if (t.Map != null)
            {
                t.Map.mapDrawer.MapMeshDirty(t.Position, 1);
            }
        }
    }
}
