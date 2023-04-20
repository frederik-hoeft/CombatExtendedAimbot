// Decompiled with JetBrains decompiler
// Type: CombatExtendedAimbot.HarmonyPatches.CanHitTargetFromPatch
// Assembly: CombatExtendedAimbot, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 96207368-A754-483F-9F5D-9DD256683876
// Assembly location: G:\SteamLibrary\steamapps\workshop\content\294100\2590848610\1.4\Assemblies\CombatExtendedAimbot.dll

using CombatExtended;
using CombatExtended.AI;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;
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
        if (__instance.ShooterPawn?.equipment?.Primary is null || !__result || !__instance.ShooterPawn.RaceProps.Humanlike || !PawnAimBotTracker.Get(__instance.ShooterPawn).AimBotStatus || __instance.ShooterPawn.Faction != Faction.OfPlayer)
        {
            return;
        }

        Pawn shooterPawn = __instance.ShooterPawn;
        ThingWithComps primary = shooterPawn.equipment.Primary;
        CompFireModes? compFireModes = ThingCompUtility.TryGetComp<CompFireModes>(primary);
        if (compFireModes == null)
        {
            return;
        }

        // don't waste ammo on non-dangerous things or training dummies
        if (targ.Thing?.HostileTo(shooterPawn) is not true)
        {
            Debug.Log($"Chilling the f*** out on non-threatening object '{targ.Label}'!");
            compFireModes.CurrentAimMode = AimMode.AimedShot;
            compFireModes.CurrentFireMode = compFireModes.AvailableFireModes.OrderByDescending(x => (int)x).First();
            PrepareForTarget(shooterPawn, primary, targ);
            return;
        }
        
        int distance = IntVec3Utility.ManhattanDistanceFlat(root, targ.Cell);
        if (compFireModes.AvailableFireModes.Count > 1)
        {
            FireModeDistance fireModeDistance = SwitchFireMode(distance, compFireModes, __instance.VerbPropsCE);
            if (fireModeDistance is FireModeDistance.Large)
            {
                PrepareForTarget(shooterPawn, primary, targ);
            }
        }

        if (compFireModes.AvailableAimModes.Count <= 2)
        {
            return;
        }

        SwitchAimMode(distance, compFireModes, __instance.VerbPropsCE);
    }

    private static readonly IReadOnlyDictionary<string, int> _animalPriorities = new Dictionary<string, int>()
    {
        { "Airburst", 7 },
        { "HP", 7 },
        { "buck", 7 },
        { "HE", 6 },
        { "ion", 6 },
        { "HEDP", 6 },
        { "FMJ", 6 },
        { "AP-HE", 5 },
        { "plasma", 5 },
        { "AP-I", 5 },
        { "flame", 5 },
        { "AP", 4 },
        { "AP-Slug", 4 },
        { "HEAT", 3 },
        { "Sabot", 1 },
        { "bean", 1 },
    };

    private static readonly IReadOnlyDictionary<string, int> _buildingPriorities = new Dictionary<string, int>()
    {
        { "HEAT", 7 },
        { "ion", 7 },
        { "HEDP", 6 },
        { "plasma", 6 },
        { "AP-HE", 6 },
        { "AP-I", 5 },
        { "flame", 5 },
        { "Sabot", 5 },
        { "AP", 4 },
        { "AP-Slug", 4 },
        { "HE", 3 },
        { "buck", 3 },
        { "Airburst", 2 },
        { "FMJ", 2 },
        { "HP", 1 },
        { "bean", 1 },
    };

    private static readonly IReadOnlyDictionary<string, int> _mechPriorities = new Dictionary<string, int>()
    {
        { "flame", 7 },
        { "HEAT", 7 },
        { "ion", 7 },
        { "HEDP", 6 },
        { "plasma", 6 },
        { "AP-I", 6 },
        { "AP-HE", 5 },
        { "Sabot", 5 },
        { "AP", 4 },
        { "AP-Slug", 4 },
        { "HE", 3 },
        { "buck", 3 },
        { "Airburst", 2 },
        { "FMJ", 2 },
        { "HP", 1 },
        { "bean", 1 },
    };

    private static readonly IReadOnlyDictionary<string, int> _humanPriorities = new Dictionary<string, int>()
    {
        { "Airburst", 8 },
        { "flame", 7 },
        { "HE", 7 },
        { "ion", 7 },
        { "HEDP", 6 },
        { "plasma", 6 },
        { "AP-HE", 6 },
        { "AP-I", 5 },
        { "HEAT", 5 },
        { "AP", 4 },
        { "AP-Slug", 4 },
        { "Sabot", 3 },
        { "buck", 3 },
        { "FMJ", 2 },
        { "HP", 1 },
        { "bean", 1 },
    };

    private static IReadOnlyDictionary<string, int> PriorityListBasedOnTarget(LocalTargetInfo target, int shooterHash)
    {
        if (target.Thing is Building)
        {
            AddOrUpdate(_pawnTargetCache, shooterHash, target.Thing);
            return _buildingPriorities;
        }
        else if (target.Pawn?.RaceProps is not null)
        {
            AddOrUpdate(_pawnTargetCache, shooterHash, target.Pawn);
            RaceProperties race = target.Pawn.RaceProps;
            if (race.Animal)
            {
                return _animalPriorities;
            }
            if (race.Humanlike || race.Insect)
            {
                return _humanPriorities;
            }
            if (race.IsMechanoid)
            {
                return _mechPriorities;
            }
        }
        return _buildingPriorities;
    }

    private static readonly Dictionary<int, object> _pawnTargetCache = new();

    private static void PrepareForTarget(Pawn shooterPawn, ThingWithComps primary, LocalTargetInfo target)
    {
        int shooterHash = shooterPawn.GetHashCode() ^ primary.GetHashCode();
        if (_pawnTargetCache.TryGetValue(shooterHash, out object? targetObject))
        {
            if (targetObject is Building && targetObject == target.Thing)
            {
                return;
            }
            if (targetObject is Pawn && targetObject == target.Pawn)
            {
                return;
            }
        }
        CompAmmoUser ammoComp = ThingCompUtility.TryGetComp<CompAmmoUser>(primary);
        if (ammoComp is not null)
        {
            IReadOnlyDictionary<string, int> priorities = PriorityListBasedOnTarget(target, shooterHash);
            AmmoDefPriority current = new(ammoComp.CurrentAmmo, priorities.GetValueOrDefault(ammoComp.CurrentAmmo.ammoClass.labelShort, 0));
            List<AmmoDef> availableAmmo = new();
            List<AmmoLink> compatibleAmmoTypes = ammoComp.Props.ammoSet.ammoTypes;
            foreach (AmmoLink link in compatibleAmmoTypes)
            {
                if (ammoComp.CompInventory?.ammoList?.Any(item => item.def == link.ammo) is true)
                {
                    availableAmmo.Add(link.ammo);
                }
            }
            Log.Message($"{shooterPawn.Name} has the following ammo types available: {string.Join(", ", availableAmmo.Select(ammo => ammo.ammoClass.labelShort))} of {string.Join(", ", compatibleAmmoTypes.Select(ammo => ammo.ammo.ammoClass.labelShort))}");
            AmmoDefPriority optimalAmmoType = availableAmmo
                .Where(ammo => ammo != current.AmmoDef)
                .Select(ammo => new AmmoDefPriority(ammo, priorities.GetValueOrDefault(ammo.ammoClass.labelShort, -1)))
                .OrderByDescending(ammo => ammo.Priority)
                .FirstOrDefault();
            if (optimalAmmoType.AmmoDef != null && optimalAmmoType.Priority > current.Priority)
            {
                Log.Message($"{shooterPawn.Name} determined that {optimalAmmoType.AmmoDef.ammoClass.labelShort} is more suitable than {current.AmmoDef.ammoClass.labelShort} for shooting {target.Label} ({target.Thing.GetType()}) and is now reloading ...");
                ammoComp.SelectedAmmo = optimalAmmoType.AmmoDef;
                ammoComp.TryStartReload();
            }
        }
    }

    private static void AddOrUpdate<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (!dictionary.TryAdd(key, value))
        {
            dictionary[key] = value;
        }
    }

    private readonly record struct AmmoDefPriority(AmmoDef AmmoDef, int Priority);

    private static FireModeDistance SwitchFireMode(int distance, CompFireModes fireMods, VerbPropertiesCE verb)
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
        return context.Distance;
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
