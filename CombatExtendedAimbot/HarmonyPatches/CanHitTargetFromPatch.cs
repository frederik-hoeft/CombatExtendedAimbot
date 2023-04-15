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
            SwitchFireMode(distance, compFireModes, __instance.VerbPropsCE);
        }

        if (compFireModes.AvailableAimModes.Count <= 2)
        {
            return;
        }

        SwitchAimMode(distance, compFireModes, __instance.VerbPropsCE);
    }

    private static readonly IReadOnlyDictionary<string, int> _animalPriorities = new Dictionary<string, int>()
    {
        { "HP", 5 },
        { "AP-I", 4 },
        { "FMJ", 3 },
        { "AP", 2 },
        { "Sabot", 1 },
    };

    private static readonly IReadOnlyDictionary<string, int> _buildingPriorities = new Dictionary<string, int>()
    {
        { "AP-I", 5 },
        { "Sabot", 4 },
        { "AP", 3 },
        { "FMJ", 2 },
        { "HP", 1 },
    };

    private static readonly IReadOnlyDictionary<string, int> _mechPriorities = new Dictionary<string, int>()
    {
        { "HEAT", 7 },
        { "AP-I", 6 },
        { "HEDP", 6 },
        { "AP-HE", 5 },
        { "flame", 5 },
        { "Sabot", 4 },
        { "HE", 3 },
        { "AP", 3 },
        { "Frag", 2 },
        { "FMJ", 2 },
        { "buck", 2 },
        { "HP", 1 },
        { "bean", 1 },
    };

    /*
    conc. with {damage: 20.1, penetration: 0.3015, explosive penetration: 6.633, extras: []}
    ion with {damage: 18.08333, penetration: 0.27125, explosive penetration: 5.9675, extras: []}
    buck with {damage: 8.882353, penetration: 0.1332353, explosive penetration: 2.931177, extras: []}
    Sabot with {damage: 34.42857, penetration: 0.5164285, explosive penetration: 11.36143, extras: []}
    stone with {damage: 10.71429, penetration: 0.1607143, explosive penetration: 3.535714, extras: []}
    HE with {damage: 190.3784, penetration: 2.855675, explosive penetration: 62.82486, extras: []}
    plasma with {damage: 7.333333, penetration: 0.11, explosive penetration: 2.42, extras: []}
    FMJ with {damage: 19.03158, penetration: 0.2854736, explosive penetration: 6.280419, extras: []}
    AP with {damage: 18.40909, penetration: 0.2761365, explosive penetration: 6.075002, extras: []}
    AP-I with {damage: 24.77049, penetration: 0.3715574, explosive penetration: 8.174262, extras: []}
    AP-HE with {damage: 43.34921, penetration: 0.650238, explosive penetration: 14.30524, extras: []}
    HP with {damage: 17.83333, penetration: 0.2675, explosive penetration: 5.885, extras: []}
    HEDP with {damage: 26.8, penetration: 0.402, explosive penetration: 8.844, extras: []}
    bean with {damage: 7.363636, penetration: 0.1104545, explosive penetration: 2.43, extras: []}
    Airburst with {damage: 38.85714, penetration: 0.5828571, explosive penetration: 12.82286, extras: []}
    HEAT with {damage: 276.85, penetration: 4.152749, explosive penetration: 91.36051, extras: []}
    AMP with {damage: 323.3333, penetration: 4.85, explosive penetration: 106.7, extras: []}
    boulder with {damage: 250, penetration: 3.75, explosive penetration: 82.5, extras: []}
    steel with {damage: 9.5, penetration: 0.1425, explosive penetration: 3.135, extras: []}
    plasteel with {damage: 8.6, penetration: 0.129, explosive penetration: 2.838, extras: []}
    venom with {damage: 8.666667, penetration: 0.13, explosive penetration: 2.86, extras: []}
    flame with {damage: 3, penetration: 0.045, explosive penetration: 0.9900001, extras: []}
    APP with {damage: 15.33333, penetration: 0.23, explosive penetration: 5.06, extras: []}
    Frag with {damage: 114.75, penetration: 1.72125, explosive penetration: 37.8675, extras: []}
    Tomahawk Missile with {damage: 5653, penetration: 84.795, explosive penetration: 1865.49, extras: []}
    Cluster Munition with {damage: 44, penetration: 0.66, explosive penetration: 14.52, extras: []}
    HE-PG with {damage: 248.5, penetration: 3.7275, explosive penetration: 82.00501, extras: []}
    Cluster-PG with {damage: 44, penetration: 0.66, explosive penetration: 14.52, extras: []}
    EMP-PG with {damage: 225, penetration: 3.375, explosive penetration: 74.25, extras: []}
    Incendiary-PG with {damage: 5, penetration: 0.075, explosive penetration: 1.65, extras: []}
    SMK-PG with {damage: 0, penetration: 0, explosive penetration: -0.33, extras: []}
    Foam-PG with {damage: 500399.5, penetration: 6, explosive penetration: 131.835, extras: []}
    AP-Slug with {damage: 26, penetration: 0.39, explosive penetration: 8.58, extras: []}
    */

    private static readonly IReadOnlyDictionary<string, int> _humanPriorities = new Dictionary<string, int>()
    {
        { "AP-I", 5 },
        { "Sabot", 4 },
        { "AP", 3 },
        { "FMJ", 2 },
        { "HP", 1 },
    };

    private static bool tmp = true;

    private static void Test()
    {
        if (!tmp)
        {
            return;
        }
        List<(string, float, float, float, string[])> types = DefDatabase<AmmoSetDef>.AllDefs
            .SelectMany(x => x?.ammoTypes)
            .Where(x => x != null)
            .GroupBy(x => x.ammo?.ammoClass?.labelShort)
            .Where(x => x?.Key != null)
            .Select(x => Flatten(x.Key!, x.ToArray()))
            .ToList();

        foreach ((string name, float avgPen, float avgExPen, float avgDmg, string[] extras) in types)
        {
            Log.Message($"{name} with {{damage: {avgDmg}, penetration: {avgPen}, explosive penetration: {avgExPen}, extras: [{string.Join(", ", extras)}]}}");
        }
        tmp = false;
    }

    private static (string, float, float, float, string[]) Flatten(string name, AmmoLink?[] links)
    {
        int ctr = 0;
        (float avgPen, float avgExPen, float avgDmg) = (0f, 0f, 0f);
        List<string> extraDamages = new();
        foreach (AmmoLink? link in links)
        {
            if (link?.projectile?.projectile is not null)
            {
                ProjectileProperties projectile = link.projectile.projectile;
                ctr++;
                avgPen += TryGetArmorPen(projectile);
                avgExPen += TryGetExArmorPen(projectile);
                avgDmg += TryGetDamage(projectile);
                if (projectile.extraDamages?.Count is > 0)
                {
                    extraDamages.Add(string.Join(", ", projectile.extraDamages.Select(dmg => $"(chance: {dmg?.chance}, ap pen: {dmg?.armorPenetration}, amount: {dmg?.amount})")));
                }
            }
        }
        avgPen /= ctr;
        avgExPen /= ctr;
        avgDmg /= ctr;
        return (name, avgPen, avgExPen, avgDmg, extraDamages.ToArray());
    }

    private static float TryGetArmorPen(ProjectileProperties projectile)
    {
        try
        {
            return projectile.GetArmorPenetration(1);
        }
        catch (Exception ex) { }
        return 0f;
    }

    private static float TryGetExArmorPen(ProjectileProperties projectile)
    {
        try
        {
            return projectile.GetExplosionArmorPenetration();
        }
        catch (Exception ex) { }
        return 0;
    }

    private static float TryGetDamage(ProjectileProperties projectile)
    {
        try
        {
            return projectile.GetDamageAmount(1);
        }
        catch (Exception ex) { }
        return 0;
    }

    private static IReadOnlyDictionary<string, int> PriorityListBasedOnTarget(LocalTargetInfo target)
    {
        if (target.Thing is Building)
        {
            return _buildingPriorities;
        }
        else if (target.Pawn?.RaceProps is not null)
        {
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

    private static void PrepareForTarget(Pawn shooterPawn, ThingWithComps primary, LocalTargetInfo target)
    {
        CompAmmoUser ammoComp = ThingCompUtility.TryGetComp<CompAmmoUser>(primary);
        if (ammoComp is not null)
        {
            IReadOnlyDictionary<string, int> priorities = PriorityListBasedOnTarget(target);
            AmmoDefPriority current = new(ammoComp.CurrentAmmo, _animalPriorities.GetValueOrDefault(ammoComp.CurrentAmmo.ammoClass.labelShort, 0));
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
                .Select(ammo => new AmmoDefPriority(ammo, _animalPriorities.GetValueOrDefault(ammo.ammoClass.labelShort, -1)))
                .OrderByDescending(ammo => ammo.Priority)
                .FirstOrDefault();
            if (optimalAmmoType.AmmoDef != null && optimalAmmoType.Priority > current.Priority)
            {
                Log.Message($"{shooterPawn.Name} determined that {optimalAmmoType.AmmoDef.ammoClass.labelShort} is more suitable than {current.AmmoDef.ammoClass.labelShort} for shooting {target.Label} ({target.Thing.GetType()}) and is now reloading ...");
                ammoComp.SelectedAmmo = optimalAmmoType.AmmoDef;
                ammoComp.TryStartReload();
                Test();
            }
        }
    }

    private readonly record struct AmmoDefPriority(AmmoDef AmmoDef, int Priority);

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
