// Decompiled with JetBrains decompiler
// Type: CombatExtendedAimbot.HarmonyPatches.TurnAimBotBackOn
// Assembly: CombatExtendedAimbot, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 96207368-A754-483F-9F5D-9DD256683876
// Assembly location: G:\SteamLibrary\steamapps\workshop\content\294100\2590848610\1.4\Assemblies\CombatExtendedAimbot.dll

using CombatExtended;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatExtendedAimbot.HarmonyPatches;

[HarmonyPatch(typeof(CompFireModes), "GenerateGizmos")]
public static class TurnAimBotBackOn
{
    public static IEnumerable<Command> Postfix(IEnumerable<Command> __result, CompFireModes __instance)
    {
        foreach (Command result in __result)
        {
            yield return result;
        }

        Pawn pawn = __instance.CasterPawn;
        if (pawn?.IsColonist is not true)
        {
            yield break;
        }
        if (!PawnAimBotTracker.Get(pawn).AimBotStatus)
        {
            yield return new Command_Action
            {
                action = () => PawnAimBotTracker.EnableAimBot(pawn),
                defaultLabel = ("CEA_TurnAimbotOn").Translate(),
                defaultDesc = ("CEA_TurnAimbotOnDesc").Translate(),
                icon = ContentFinder<Texture2D>.Get("EnableAimbotGizmo")
            };
        }
    }
}
