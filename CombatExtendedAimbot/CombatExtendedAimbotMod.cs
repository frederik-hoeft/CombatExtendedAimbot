// Decompiled with JetBrains decompiler
// Type: CombatExtendedAimbot.CombatExtendedAimbotMod
// Assembly: CombatExtendedAimbot, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 96207368-A754-483F-9F5D-9DD256683876
// Assembly location: G:\SteamLibrary\steamapps\workshop\content\294100\2590848610\1.4\Assemblies\CombatExtendedAimbot.dll

using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtendedAimbot;

public class CombatExtendedAimbotMod : Mod
{
    private static AimbotSettings? _settings;

    public CombatExtendedAimbotMod(ModContentPack content) : base(content)
    {
        _settings = GetSettings<AimbotSettings>();
        new Harmony("bustedbunny.CombatExtendedAimbot").PatchAll();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new();
        listingStandard.Begin(inRect);
        listingStandard.End();
        base.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory() => "Combat Extended Aimbot";
}
