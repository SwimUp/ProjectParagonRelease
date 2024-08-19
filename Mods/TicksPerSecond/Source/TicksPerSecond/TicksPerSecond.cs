using System;
using System.Reflection;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace TicksPerSecond
{
    [StaticConstructorOnStartup]
    static class TicksPerSecond
    {
        static TicksPerSecond()
        {
            Harmony harmony = new Harmony("rimworld.sparr.tickspersecond");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

	public class Settings : ModSettings
	{
		public int tpsUpdateInterval = 1;
        private string tpsUpdateIntervalBuffer = "1";


        public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref tpsUpdateInterval, "tpsUpdateInterval", 1);
			tpsUpdateIntervalBuffer = tpsUpdateInterval.ToString();
		}

		public void DoWindowContents(Rect inRect)
		{
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(new Rect(inRect.x, inRect.y, 400, inRect.height));

			Widgets.Label(listing_Standard.GetRect(30f), "Ticks Per Second Update Interval in seconds (default: 1):");
			Widgets.IntEntry(listing_Standard.GetRect(40f), ref tpsUpdateInterval, ref tpsUpdateIntervalBuffer, 1);
            if (tpsUpdateInterval <= 0)
            {
                tpsUpdateInterval = 1;
                tpsUpdateIntervalBuffer = "1";
            }

			listing_Standard.End();
		}
	}

    class TicksPerSecondMod : Mod
    {
        public static Settings Settings;
        public TicksPerSecondMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Ticks Per Second";

        }

    }

    [HarmonyPatch(typeof(RimWorld.GlobalControlsUtility), "DoTimespeedControls")]
    static class GlobalControlsUtility_DoTimespeedControls
    {
        private const double TicksPerSecondMultiplier = 1.0 / TimeSpan.TicksPerSecond;
        private static long PreviousTime;
        private static long PreviousTick = -1;
        private static float PreviousTickRateMultiplier = -1;
        private static string ToDisplay = "TPS: 0";

        static void Postfix(float leftX, float width, ref float curBaseY)
        {
            float trm = Current.Game.tickManager.TickRateMultiplier;
            long td = DateTime.Now.Ticks - PreviousTime;
            if (trm != PreviousTickRateMultiplier || td >= TimeSpan.TicksPerSecond * TicksPerSecondMod.Settings.tpsUpdateInterval)
            {
                long tick = Current.Game.tickManager.TicksGame;
                if (PreviousTick < -1)
                    PreviousTick = tick;
                int target = (int)Math.Round((trm == 0f) ? 0f : (60f * trm));
                ToDisplay = $"TPS: {(tick - PreviousTick) / (td * TicksPerSecondMultiplier):0} ({target})";
                PreviousTime = DateTime.Now.Ticks;
                PreviousTick = tick;
                PreviousTickRateMultiplier = trm;
            }

            Rect rect = new Rect(leftX - 20f, curBaseY - 26f, width + 20f - 7f, 26f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, ToDisplay);
            Text.Anchor = TextAnchor.UpperLeft;
            curBaseY -= 26f;
        }
    }
}