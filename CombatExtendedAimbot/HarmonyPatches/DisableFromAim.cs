// Decompiled with JetBrains decompiler
// Type: CombatExtendedAimbot.HarmonyPatches.DisableFromAim
// Assembly: CombatExtendedAimbot, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 96207368-A754-483F-9F5D-9DD256683876
// Assembly location: G:\SteamLibrary\steamapps\workshop\content\294100\2590848610\1.4\Assemblies\CombatExtendedAimbot.dll

using CombatExtended;
using HarmonyLib;
using Verse;

namespace CombatExtendedAimbot.HarmonyPatches;

[HarmonyPatch(typeof(CompFireModes), "ToggleAimMode")]
public static class DisableFromAim
{
    public static void Postfix(CompFireModes __instance)
    {
        Pawn casterPawn = __instance.CasterPawn;
        if (casterPawn?.IsColonist is true)
        {
            PawnAimBotTracker.DisableAimBot(casterPawn);
        }
    }
}
