using Formats;
using Formats.Ard;
using Formats.Battlepack;
using Formats.Ebp;
using Formats.MenuHandBook;
using System;
using System.Collections.Generic;

namespace Resources
{
    public class JsonFile
    {
        public string Name { get; set; }
        public Type Class { get; set; }

        public JsonFile(string filename, Type classname)
        {
            Name = filename;
            Class = classname;
        }

        public static readonly List<JsonFile> FileList = new()
        {
            new("(.*).mrp($)", typeof(Mrp)),
            new("(.*)battle_pack.bin.dir\\\\section_000.bin($)", typeof(WeaponStances)),
            new("(.*)battle_pack.bin.dir\\\\section_003.bin($)", typeof(MpRegeneration)),
            new("(.*)battle_pack.bin.dir\\\\section_005.bin($)", typeof(EquipmentCategories)),
            new("(.*)battle_pack.bin.dir\\\\section_006.bin($)", typeof(ChainLevels)),
            new("(.*)battle_pack.bin.dir\\\\section_007.bin($)", typeof(Gambits)),
            new("(.*)battle_pack.bin.dir\\\\section_008.bin($)", typeof(DefaultPartyMemberGambits)),
            new("(.*)battle_pack.bin.dir\\\\section_009.bin($)", typeof(PartyMemberLevelGrowth)),
            new("(.*)battle_pack.bin.dir\\\\section_010.bin($)", typeof(ActionGroups)),
            new("(.*)battle_pack.bin.dir\\\\section_011.bin($)", typeof(MagickCategories)),
            new("(.*)battle_pack.bin.dir\\\\section_012.bin($)", typeof(LicenseNodes)),
            new("(.*)battle_pack.bin.dir\\\\section_013.bin($)", typeof(EquipmentAndAttributes)),
            new("(.*)battle_pack.bin.dir\\\\section_014.bin($)", typeof(Actions)),
            new("(.*)battle_pack.bin.dir\\\\section_015.bin($)", typeof(StatusEffects)),
            new("(.*)battle_pack.bin.dir\\\\section_016.bin($)", typeof(PartyMembers)),
            new("(.*)battle_pack.bin.dir\\\\section_017.bin($)", typeof(BattleMenuCategories)),
            new("(.*)battle_pack.bin.dir\\\\section_018.bin($)", typeof(Items)),
            new("(.*)battle_pack.bin.dir\\\\section_026.bin($)", typeof(Mist)),
            new("(.*)battle_pack.bin.dir\\\\section_028.bin($)", typeof(Prices)),
            new("(.*)battle_pack.bin.dir\\\\section_029.bin($)", typeof(Magicks)),
            new("(.*)battle_pack.bin.dir\\\\section_030.bin($)", typeof(Technicks)),
            new("(.*)battle_pack.bin.dir\\\\section_031.bin($)", typeof(Concurrences)),
            new("(.*)battle_pack.bin.dir\\\\section_032.bin($)", typeof(Loot)),
            new("(.*)battle_pack.bin.dir\\\\section_033.bin($)", typeof(Maps)),
            new("(.*)battle_pack.bin.dir\\\\section_034.bin($)", typeof(TeleportLocations)),
            new("(.*)battle_pack.bin.dir\\\\section_035.bin($)", typeof(KeyItems)),
            new("(.*)battle_pack.bin.dir\\\\section_037.bin($)", typeof(Packages)),
            new("(.*)battle_pack.bin.dir\\\\section_038.bin($)", typeof(Rewards)),
            new("(.*)battle_pack.bin.dir\\\\section_039.bin($)", typeof(Shops)),
            new("(.*)battle_pack.bin.dir\\\\section_041.bin($)", typeof(Elements)),
            new("(.*)battle_pack.bin.dir\\\\section_042.bin($)", typeof(InitialInventory)),
            new("(.*)battle_pack.bin.dir\\\\section_057.bin($)", typeof(BazaarGoods)),
            new("(.*)battle_pack.bin.dir\\\\section_058.bin($)", typeof(Augments)),
            new("(.*)battle_pack.bin.dir\\\\section_059.bin($)", typeof(StoryPointAdditionsExtendedInfo)),
            new("(.*)battle_pack.bin.dir\\\\section_060.bin($)", typeof(StoryPointAdditionsBasicInfo)),
            new("(.*)battle_pack.bin.dir\\\\section_068.bin($)", typeof(LocationMovementBehaviour)),
            new("(.*)battle_pack.bin.dir\\\\section_069.bin($)", typeof(Movies)),
            new("(.*)battle_pack.bin.dir\\\\section_070.bin($)", typeof(LicenseBoard)),
            new("(.*)board_([1-9]|1[0-2]).bin($)", typeof(LicenseBoard)),
            new("(.*).ebp.dir\\\\section_004.bin($)", typeof(NavigationIcons)),
            new("(.*).ebp.dir\\\\section_(008|010).bin($)", typeof(CameraMappings)),
            new("(.*).ebp.dir\\\\section_009.bin($)", typeof(Cameras)),
            new("(.*).ebp.dir\\\\section_012.bin($)", typeof(Formats.Ebp.Models)),
            new("(.*).ebp.dir\\\\section_016.bin($)", typeof(Positions)),
            new("(.*).ard.dir\\\\section_001.bin($)", typeof(Formats.Ard.Models)),
            new("(.*).ard.dir\\\\section_002.bin($)", typeof(Classes)),
            new("(.*).ard.dir\\\\section_003.bin($)", typeof(AiScripts)),
            new("(.*).ard.dir\\\\section_004.bin($)", typeof(Units)),
            new("(.*).ard.dir\\\\section_(007|008).bin($)", typeof(Stats)),
            new("(.*).ard.dir\\\\section_009.bin($)", typeof(SpecialActionAnimations)),
            new("(.*)menuhandbook_knowledge.bin($)", typeof(Hctgf)),
            new("(.*)menuhandbook_monster.bin($)", typeof(Hctgf)),
            new("(.*)menuhandbook_person.bin($)", typeof(Hctgf)),
            new("(.*)menuhandbook_story.bin($)", typeof(Hctgf)),
            new("(.*)menuhandbook_tutorial.bin($)", typeof(Hctgf)),
            new("(.*)menuhandbook_world.bin($)", typeof(Hctgf)),
            new("(.*)menuhandbook.bin($)", typeof(Hcomg))
        };
    }
}
