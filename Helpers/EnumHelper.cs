﻿using System;

namespace Helpers
{
    [Flags]
    public enum AugmentsEnum : ulong
    {
        None = 0x0000000000000000,
        Stability = 0x0000000000000001,
        Safety = 0x0000000000000002,
        AccuracyBoost = 0x0000000000000004,
        ShieldBoost = 0x0000000000000008,
        EvasionBoost = 0x0000000000000010,
        LastStand = 0x0000000000000020,
        Counter = 0x0000000000000040,
        CounterBoost = 0x0000000000000080,
        Spellbreaker = 0x0000000000000100,
        Brawler = 0x0000000000000200,
        Adrenaline = 0x0000000000000400,
        Focus = 0x0000000000000800,
        Lobbying = 0x0000000000001000,
        ComboBoost = 0x0000000000002000,
        ItemBoost = 0x0000000000004000,
        MedicineReverse = 0x0000000000008000,
        Weatherproof = 0x0000000000010000,
        Thievery = 0x0000000000020000,
        Saboteur = 0x0000000000040000,
        MagickLore1 = 0x0000000000080000,
        Warmage = 0x0000000000100000,
        Martyr = 0x0000000000200000,
        MagickLore2 = 0x0000000000400000,
        Headsman = 0x0000000000800000,
        MagickLore3 = 0x0000000001000000,
        TreasureHunter = 0x0000000002000000,
        MagickLore4 = 0x0000000004000000,
        DoubleExp = 0x0000000008000000,
        DoubleLp = 0x0000000010000000,
        NoExp = 0x0000000020000000,
        Spellbound = 0x0000000040000000,
        PiercingMagick = 0x0000000080000000,
        Offering = 0x0000000100000000,
        Muffle = 0x0000000200000000,
        LifeCloak = 0x0000000400000000,
        BattleLore1 = 0x0000000800000000,
        Parsimony = 0x0000001000000000,
        TreadLightly = 0x0000002000000000,
        Unused39 = 0x0000004000000000,
        Emptiness = 0x0000008000000000,
        ResistPiercingDamage = 0x0000010000000000,
        AntiLibra = 0x0000020000000000,
        BattleLore2 = 0x0000040000000000,
        BattleLore3 = 0x0000080000000000,
        BattleLore4 = 0x0000100000000000,
        BattleLore5 = 0x0000200000000000,
        BattleLore6 = 0x0000400000000000,
        BattleLore7 = 0x0000800000000000,
        Stoneskin = 0x0001000000000000,
        AttackBoost = 0x0002000000000000,
        DoubleEdged = 0x0004000000000000,
        Spellspring = 0x0008000000000000,
        ElementalShift = 0x0010000000000000,
        Celerity = 0x0020000000000000,
        Swiftcast = 0x0040000000000000,
        AttackImmunity = 0x0080000000000000,
        MagickImmunity = 0x0100000000000000,
        StatusImmunity = 0x0200000000000000,
        DamageSpikes = 0x0400000000000000,
        Suicidal = 0x0800000000000000,
        BattleLore8 = 0x1000000000000000,
        BattleLore9 = 0x2000000000000000,
        BattleLore10 = 0x4000000000000000,
        BattleLore11 = 0x8000000000000000
    }

    [Flags]
    public enum StatusEffectsEnum : uint
    {
        None = 0x00000000,
        Ko = 0x00000001,
        Stone = 0x00000002,
        Petrify = 0x00000004,
        Stop = 0x00000008,
        Sleep = 0x00000010,
        Confuse = 0x00000020,
        Doom = 0x00000040,
        Blind = 0x00000080,
        Poison = 0x00000100,
        Silence = 0x00000200,
        Sap = 0x00000400,
        Oil = 0x00000800,
        Reverse = 0x00001000,
        Disable = 0x00002000,
        Immobilize = 0x00004000,
        Slow = 0x00008000,
        Disease = 0x00010000,
        Lure = 0x00020000,
        Protect = 0x00040000,
        Shell = 0x00080000,
        Haste = 0x00100000,
        Bravery = 0x00200000,
        Faith = 0x00400000,
        Reflect = 0x00800000,
        Invisible = 0x01000000,
        Regen = 0x02000000,
        Float = 0x04000000,
        Berserk = 0x08000000,
        Bubble = 0x10000000,
        HpCritical = 0x20000000,
        Libra = 0x40000000,
        XZone = 0x80000000
    }

    [Flags]
    public enum ElementsEnum : byte
    {
        None = 0x00,
        Fire = 0x01,
        Lightning = 0x02,
        Ice = 0x04,
        Earth = 0x08,
        Water = 0x10,
        Wind = 0x20,
        Holy = 0x40,
        Dark = 0x80
    }
}