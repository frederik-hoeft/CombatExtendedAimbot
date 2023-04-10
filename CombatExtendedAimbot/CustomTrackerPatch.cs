// Decompiled with JetBrains decompiler
// Type: CombatExtendedAimbot.CustomTrackerPatch
// Assembly: CombatExtendedAimbot, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 96207368-A754-483F-9F5D-9DD256683876
// Assembly location: G:\SteamLibrary\steamapps\workshop\content\294100\2590848610\1.4\Assemblies\CombatExtendedAimbot.dll

using HarmonyLib;
using System;
using Verse;

namespace CombatExtendedAimbot;

[HarmonyPatch(typeof(Pawn), "ExposeData")]
public class CustomTrackerPatch
{
    public static void Postfix(Pawn __instance)
    {
        if (__instance.IsColonist)
        {
            PawnAimBotTracker pawnAimBotTracker = PawnAimBotTracker.Get(__instance);
            Scribe_Deep.Look(ref pawnAimBotTracker, "Pawn_AimbotTracker", Array.Empty<object>());
            PawnAimBotTracker.Trackers[__instance] = pawnAimBotTracker;
        }
    }
}
