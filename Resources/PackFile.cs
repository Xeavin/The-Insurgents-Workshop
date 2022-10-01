using System.Collections.Generic;

namespace Resources
{
    public class PackFile
    {
        public string Name { get; set; }
        public PackType Type { get; set; }
        public uint SectionCount { get; set; }

        public PackFile(string name, PackType type, uint sectionCount)
        {
            Name = name;
            Type = type;
            SectionCount = sectionCount;
        }

        public static readonly List<PackFile> FileList = new()
        {
            new("(.*)battle_pack.bin($)", PackType.Battelepack, 71 ),
            new("(.*)battle_pack.bin.dir\\\\section_061.bin($)", PackType.Battelepack, 15 ),
            new("(.*)clutpack_ys.bin($)", PackType.Otherpack, 4),
            new("(.*)fontpack_fs.bin($)", PackType.Otherpack, 7),
            new("(.*)fontpack_it.bin($)", PackType.Otherpack, 6),
            new("(.*)mrppack_ys.bin($)", PackType.Otherpack, 23),
            new("(.*)tex2pack_ys.bin($)", PackType.Otherpack, 11),
            new("(.*)texpack_ys.bin($)", PackType.Otherpack, 5),
            new("(.*).ebp($)", PackType.Ebp, 20),
            new("(.*).ard($)", PackType.Ard, 10),
            new("(.*)menuhandbook_knowledge(00[2-9]|0[12][0-9]).dat($)", PackType.Himgd, 0),
            new("(.*)menuhandbook_monster(00[4-9]|0[1-9][0-9]|[12][0-9][2]).dat($)", PackType.Himgd, 0),
            new("(.*)menuhandbook_person(00[1-9]|0[12][0-9]).dat($)", PackType.Himgd, 0),
            new("(.*)menuhandbook_story(00[1-9]|0[1-9][0-9]).dat($)", PackType.Himgd, 0),
            new("(.*)menuhandbook_tutorial(00[1-9]|0[1-4][0-9]).dat($)", PackType.Himgd, 0),
            new("(.*)menuhandbook_world(00[1-9]|0[1-4][0-9]).dat($)", PackType.Himgd, 0)
        };
    }

    public enum PackType
    {
        Battelepack = 0,
        Otherpack = 1,
        Ebp = 2,
        Ard = 3,
        Himgd = 4
    }
}
