// Decompiled with JetBrains decompiler
// Type: CombatExtendedAimbot.PawnAimBotTracker
// Assembly: CombatExtendedAimbot, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 96207368-A754-483F-9F5D-9DD256683876
// Assembly location: G:\SteamLibrary\steamapps\workshop\content\294100\2590848610\1.4\Assemblies\CombatExtendedAimbot.dll

using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtendedAimbot;

public class PawnAimBotTracker : IExposable
{
    public static readonly Dictionary<Pawn, PawnAimBotTracker> Trackers = new();
    public bool AimBotStatus;

    [Obsolete("Public to fix try fix load errors :P")]
    public PawnAimBotTracker() => AimBotStatus = true;

    public static PawnAimBotTracker Get(Pawn pawn)
    {
        Trackers.TryGetValue(pawn, out PawnAimBotTracker pawnAimBotTracker1);
        if (pawnAimBotTracker1 != null)
        {
            return pawnAimBotTracker1;
        }

#pragma warning disable CS0618 // Type or member is obsolete
        PawnAimBotTracker pawnAimBotTracker2 = new();
#pragma warning restore CS0618 // Type or member is obsolete
        GenCollection.SetOrAdd(Trackers, pawn, pawnAimBotTracker2);
        return pawnAimBotTracker2;
    }

    public static void EnableAimBot(Pawn pawn)
    {
        if (!pawn.IsColonist)
        {
            return;
        }

        Get(pawn).AimBotStatus = true;
    }

    public static void DisableAimBot(Pawn pawn)
    {
        if (!pawn.IsColonist)
        {
            return;
        }

        Get(pawn).AimBotStatus = false;
    }

    public void ExposeData() => Scribe_Values.Look(ref AimBotStatus, "AimbotEnabled", false, false);
}
