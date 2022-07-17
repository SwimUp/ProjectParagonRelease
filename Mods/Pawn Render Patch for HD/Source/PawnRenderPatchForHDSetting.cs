using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
//using SettingsHelper;





namespace PawnRenderPatchForHD
{


    public class PawnRenderPatchForHDSetting : ModSettings
    {
        public static bool DebugLog = false;

        public static float ZoomValue = 12;




        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref DebugLog, "DebugLog", false);


            Scribe_Values.Look(ref ZoomValue, "ZoomValue", 12f);

        }

    }
    public class PawnRenderPatchForHDMod : Mod
    {
        public static PawnRenderPatchForHDSetting settings;

        public PawnRenderPatchForHDMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<PawnRenderPatchForHDSetting>();
        }


        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            Rect newRect = new Rect(inRect.x, inRect.y, inRect.width/2, inRect.height);
            listingStandard.Begin(newRect);

            listingStandard.Gap(6);
            listingStandard.Label("current Zoom Target Value : " + PawnRenderPatchForHDSetting.ZoomValue,-1,"Higher Value means active in far range\nDefault = 12");
            PawnRenderPatchForHDSetting.ZoomValue = listingStandard.Slider(PawnRenderPatchForHDSetting.ZoomValue, 5f, 70f);
            listingStandard.Label("If you are using mod \"Camera+\", Recomanded Value is 12~20.\nIf you don't use, recomanded value is 12~13. \nToo High value may have bad performance");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            //return "Quality Of Building";
            return "Pawn Render Patch For HD";
        }

       


    }
    //LoadedModManager.GetMod<QualityOFBuildingMod>().GetSettings<ExampleSettings>().exampleBool
}