using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmartSpeed.Detouring
{
    public static class TimeControls
    {
      

    private static readonly string[] SpeedSounds = new string[]
    {
            "Clock_Stop",
            "Clock_Normal",
            "Clock_Fast",
            "Clock_Superfast",
            "Clock_Superfast"
    };

    private static readonly TimeSpeed[] CachedTimeSpeedValues = (TimeSpeed[])Enum.GetValues(typeof(TimeSpeed));

    private static void PlaySoundOf(TimeSpeed speed)
    {
        SoundStarter.PlayOneShotOnCamera(SoundDef.Named(TimeControls.SpeedSounds[(int)speed].ToString()), null);
    }

    public static void DoConfigGUI(Rect timeRect)
    {
            if (Mouse.IsOver(timeRect) && (Event.current.button == 1))
                if (Event.current.type == EventType.MouseDown)
                {
                    var floatOptionList = new List<FloatMenuOption>();

                    floatOptionList.Add(new FloatMenuOption(string.Format("Slow".Translate() + " {0}", AcSmartSpeed.currSetting == AcSmartSpeed.Option.Slow ? "V" : ""), delegate { AcSmartSpeed.currSetting = AcSmartSpeed.Option.Slow; }));
                    floatOptionList.Add(new FloatMenuOption(string.Format("Normal".Translate() + " {0}", AcSmartSpeed.currSetting == AcSmartSpeed.Option.Normal ? "V" : ""), delegate { AcSmartSpeed.currSetting = AcSmartSpeed.Option.Normal; }));
                    floatOptionList.Add(new FloatMenuOption(string.Format("Fast".Translate() + " {0}", AcSmartSpeed.currSetting == AcSmartSpeed.Option.Fast ? "V" : ""), delegate { AcSmartSpeed.currSetting = AcSmartSpeed.Option.Fast; }));
                    floatOptionList.Add(new FloatMenuOption(string.Format("Half".Translate() + " {0}", AcSmartSpeed.currSetting == AcSmartSpeed.Option.Half ? "V" : ""), delegate { AcSmartSpeed.currSetting = AcSmartSpeed.Option.Half; }));
                    floatOptionList.Add(new FloatMenuOption(string.Format("Ignore".Translate() + " {0}", AcSmartSpeed.currSetting == AcSmartSpeed.Option.Ignore ? "V" : ""), delegate { AcSmartSpeed.currSetting = AcSmartSpeed.Option.Ignore; }));
                    var window = new FloatMenu(floatOptionList, "EventSpeed".Translate());
                    Find.WindowStack.Add(window);

                    // use event so it doesn't bubble through
                    Event.current.Use();
                }
     }


        public static void DoTimeControlsGUI(Rect timerRect)
        {
            DoConfigGUI(timerRect);

            timerRect.x -= 14f;
            var tickManager = Find.TickManager;
            GUI.BeginGroup(timerRect);
            var rect = new Rect(0f, 0f, 28f, 24f);
            for (var i = 0; i < CachedTimeSpeedValues.Length; i++)
            {
                var timeSpeed = CachedTimeSpeedValues[i];

                if (Widgets.ButtonImage(rect, TexButton._SpeedButtonTextures[(int)timeSpeed]))
                {
                    if (timeSpeed == TimeSpeed.Paused)
                        tickManager.TogglePaused();
                    else
                        tickManager.CurTimeSpeed = timeSpeed;
                    PlaySoundOf(tickManager.CurTimeSpeed);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls, KnowledgeAmount.SpecificInteraction);
                }
                if (tickManager.CurTimeSpeed == timeSpeed)
                    GUI.DrawTexture(rect, TexUI.HighlightTex);
                rect.x += rect.width;
            }

            if (Find.TickManager.slower.ForcedNormalSpeed)
                Widgets.DrawLineHorizontal(rect.width * 2f, rect.height / 2f, rect.width * 3f);
            GUI.EndGroup();
            GenUI.AbsorbClicksInRect(timerRect);
            UIHighlighter.HighlightOpportunity(timerRect, "TimeControls");

            if (Event.current.type == EventType.KeyDown)
            {
                if (KeyBindingDefOf.TogglePause.KeyDownEvent)
                {
                    Find.TickManager.TogglePaused();
                    PlaySoundOf(Find.TickManager.CurTimeSpeed);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Pause,
                        KnowledgeAmount.SpecificInteraction);
                    Event.current.Use();
                }
                if (KeyBindingDefOf.TimeSpeed_Normal.KeyDownEvent)
                {
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    PlaySoundOf(Find.TickManager.CurTimeSpeed);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls,
                        KnowledgeAmount.SpecificInteraction);
                    Event.current.Use();
                }
                if (KeyBindingDefOf.TimeSpeed_Fast.KeyDownEvent)
                {
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Fast;
                    PlaySoundOf(Find.TickManager.CurTimeSpeed);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls,
                        KnowledgeAmount.SpecificInteraction);
                    Event.current.Use();
                }
                if (KeyBindingDefOf.TimeSpeed_Superfast.KeyDownEvent)
                {
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
                    PlaySoundOf(Find.TickManager.CurTimeSpeed);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls,
                        KnowledgeAmount.SpecificInteraction);
                    Event.current.Use();
                }
                if (KeyBindingDefOf.TimeSpeed_Ultrafast.KeyDownEvent)
                {
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                    PlaySoundOf(Find.TickManager.CurTimeSpeed);
                    Event.current.Use();
                }
                if (Prefs.DevMode)
                    if (KeyBindingDefOf.Dev_TickOnce.KeyDownEvent && (tickManager.CurTimeSpeed == TimeSpeed.Paused))
                    {
                        tickManager.DoSingleTick();
                        SoundDef.Named(SpeedSounds[0]).PlayOneShotOnCamera();
                    }
            }
        }





    }
}


