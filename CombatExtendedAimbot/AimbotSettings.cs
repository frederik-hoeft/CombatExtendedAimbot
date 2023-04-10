// Decompiled with JetBrains decompiler
// Type: CombatExtendedAimbot.AimbotSettings
// Assembly: CombatExtendedAimbot, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 96207368-A754-483F-9F5D-9DD256683876
// Assembly location: G:\SteamLibrary\steamapps\workshop\content\294100\2590848610\1.4\Assemblies\CombatExtendedAimbot.dll

using Verse;

namespace CombatExtendedAimbot;

public class AimbotSettings : ModSettings
{
    public static float FireModeLargestDistance = 25f;
    public static float FireModeMediumDistance = 10f;
    public static float SingleFireAimed = 10f;
    public static float SingleFireSnap = 5f;
    public static float BurstFireAimed = 18f;
    public static float BurstFireSnap = 10f;
    public static float BurstLmgSnap = 10f;
    public static float FullNoLmgSnap = 10f;
    public static float FullLmgSnap = 18f;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref FireModeLargestDistance, "FireModeLargestDistance", 25f, false);
        Scribe_Values.Look(ref FireModeMediumDistance, "FireModeMediumDistance", 10f, false);
        Scribe_Values.Look(ref SingleFireAimed, "SingleFireAimed", 10f, false);
        Scribe_Values.Look(ref SingleFireSnap, "SingleFireSnap", 5f, false);
        Scribe_Values.Look(ref BurstFireAimed, "BurstFireAimed", 18f, false);
        Scribe_Values.Look(ref BurstFireSnap, "BurstFireSnap", 10f, false);
        Scribe_Values.Look(ref BurstLmgSnap, "BurstLmgSnap", 10f, false);
        Scribe_Values.Look(ref FullNoLmgSnap, "FullNoLmgSnap", 10f, false);
        Scribe_Values.Look(ref FullLmgSnap, "FullLmgSnap", 10f, false);
    }
}
