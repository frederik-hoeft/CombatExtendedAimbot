// Decompiled with JetBrains decompiler
// Type: CombatExtendedAimbot.HarmonyPatches.CanHitTargetFromPatch
// Assembly: CombatExtendedAimbot, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 96207368-A754-483F-9F5D-9DD256683876
// Assembly location: G:\SteamLibrary\steamapps\workshop\content\294100\2590848610\1.4\Assemblies\CombatExtendedAimbot.dll

using CombatExtended;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace CombatExtendedAimbot.HarmonyPatches;

[HarmonyPatch(typeof(Verb_ShootCE), "CanHitTargetFrom")]
public static class CanHitTargetFromPatch
{
    private readonly record struct FireModeContext(FireModeDistance Distance, bool IsMachineGun, bool CanSingleFire, bool CanBurstFire, bool CanAutoFire)
    {
        public static FireModeContext FromAvailable(List<FireMode> availableFireModes, int burstShotCount, int distance) => new()
        {
            Distance = distance switch
            {
                _ when distance > (double)AimbotSettings.FireModeLargestDistance => FireModeDistance.Large,
                _ when distance > (double)AimbotSettings.FireModeMediumDistance => FireModeDistance.Medium,
                _ => FireModeDistance.Close
            },
            IsMachineGun = burstShotCount > 6,
            CanSingleFire = availableFireModes.Contains(FireMode.SingleFire),
            CanBurstFire = availableFireModes.Contains(FireMode.BurstFire),
            CanAutoFire = availableFireModes.Contains(FireMode.AutoFire)
        };
    }

    private enum FireModeDistance
    {
        Large,
        Medium,
        Close
    }

    private const object? __ = null;

    public static void Postfix(Verb_ShootCE __instance, bool __result, IntVec3 root, LocalTargetInfo targ)
    {
        if (__instance.ShooterPawn == null || !__result || !__instance.ShooterPawn.RaceProps.Humanlike || !PawnAimBotTracker.Get(__instance.ShooterPawn).AimBotStatus || __instance.ShooterPawn.Faction != Faction.OfPlayer)
        {
            return;
        }

        Pawn shooterPawn = __instance.ShooterPawn;
        CompFireModes? compFireModes;
        if (shooterPawn == null)
        {
            compFireModes = null;
        }
        else
        {
            Pawn_EquipmentTracker equipment = shooterPawn.equipment;
            if (equipment == null)
            {
                compFireModes = null;
            }
            else
            {
                ThingWithComps primary = equipment.Primary;
                compFireModes = primary != null ? ThingCompUtility.TryGetComp<CompFireModes>(primary) : null;
            }
        }
        CompFireModes? fireMods = compFireModes;
        if (fireMods == null)
        {
            return;
        }
        
        Log.Message(targ.Thing?.Label ?? "no label :(");
        int distance = IntVec3Utility.ManhattanDistanceFlat(root, targ.Cell);
        if (fireMods.AvailableFireModes.Count > 1)
        {
            SwitchFireMode(distance, fireMods, __instance.VerbPropsCE);
        }

        if (fireMods.AvailableAimModes.Count <= 2)
        {
            return;
        }

        SwitchAimMode(distance, fireMods, __instance.VerbPropsCE);
    }

    public static void SwitchFireMode(int distance, CompFireModes fireMods, VerbPropertiesCE verb)
    {
        FireModeContext context = FireModeContext.FromAvailable(fireMods.AvailableFireModes, verb.burstShotCount, distance);

        FireMode? mode = context switch
        {
            { Distance: FireModeDistance.Large, IsMachineGun: false, CanSingleFire: true } => FireMode.SingleFire,
            { Distance: FireModeDistance.Large, CanBurstFire: true } => FireMode.BurstFire,
            { Distance: FireModeDistance.Medium, IsMachineGun: false, CanBurstFire: true } => FireMode.BurstFire,
            { Distance: FireModeDistance.Medium, CanAutoFire: true } => FireMode.AutoFire,
            { Distance: FireModeDistance.Close, CanAutoFire: true } => FireMode.AutoFire,
            _ => null
        };

        if (mode is not null)
        {
            fireMods.CurrentFireMode = mode.Value;
        }
    }

    private static void SwitchAimMode(int distance, CompFireModes fireMods, VerbPropertiesCE verb)
    {
        bool DistanceAbove(float exclusiveLimit) => distance > (double)exclusiveLimit;

        fireMods.CurrentAimMode = fireMods.CurrentFireMode switch
        {
            FireMode.SingleFire => __ switch
            {
                _ when DistanceAbove(AimbotSettings.SingleFireAimed) => AimMode.AimedShot,
                _ when DistanceAbove(AimbotSettings.SingleFireSnap) => AimMode.Snapshot,
                _ => AimMode.SuppressFire
            },
            FireMode.BurstFire => fireMods.Props.aimedBurstShotCount switch
            {
                3 when DistanceAbove(AimbotSettings.BurstFireAimed) => AimMode.AimedShot,
                3 when DistanceAbove(AimbotSettings.BurstFireSnap) => AimMode.Snapshot,
                not 3 when DistanceAbove(AimbotSettings.BurstLmgSnap) => AimMode.Snapshot,
                _ => AimMode.SuppressFire,
            },
            _ => verb.burstShotCount switch
            {
                < 7 when DistanceAbove(AimbotSettings.FullNoLmgSnap) => AimMode.Snapshot,
                < 7 => AimMode.SuppressFire,
                not < 7 when DistanceAbove(AimbotSettings.FullLmgSnap) => AimMode.Snapshot,
                _ => AimMode.SuppressFire
            }
        };
    }
}
