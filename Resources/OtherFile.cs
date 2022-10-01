using System.Collections.Generic;

namespace Resources
{
    public class OtherFile
    {
        public string Name { get; set; }
        public OtherType Type { get; set; }
        public string Extension { get; set; }

        public OtherFile(string name, OtherType type, string extension)
        {
            Name = name;
            Type = type;
            Extension = extension;
        }

        public static readonly List<OtherFile> FileList = new()
        {
            new("(.*).ebp.dir\\\\section_000.bin($)", OtherType.Script, ".c" ),
            new("(.*).ebp.dir\\\\section_(002|003).bin($)", OtherType.Text, ".txt" ),
            new("(.*)battle_pack.bin.dir\\\\section_002.bin($)", OtherType.Text, ".txt" ),
            new("(.*)battle_pack.bin.dir\\\\section_061.bin.dir\\\\section_(00[0-9]|01[0-4]).bin($)", OtherType.Text, ".txt" ),
            new("(.*)npcdic.bin($)", OtherType.Text, ".txt" ),
            new("(.*)planmapname.bin($)", OtherType.Text, ".txt" ),
            new("(.*)menuhandbook_knowledge(000|001).dat($)", OtherType.Text, ".txt"),
            new("(.*)menuhandbook_monster(00[0-3]).dat($)", OtherType.Text, ".txt"),
            new("(.*)menuhandbook_person000.dat($)", OtherType.Text, ".txt"),
            new("(.*)menuhandbook_story000.dat($)", OtherType.Text, ".txt"),
            new("(.*)menuhandbook_tutorial000.dat($)", OtherType.Text, ".txt"),
            new("(.*)menuhandbook_world000.dat($)", OtherType.Text, ".txt"),
            new("(.*)menuquest.lst($)", OtherType.Text, ".txt" ),
            new("(.*)menuquest(0[0-9]|1[0-5]).bin($)", OtherType.Text, ".txt" ),
            new("(.*)menumap_guide(0[0-9]|1[0-5]).bin($)", OtherType.Text, ".txt" ),
            new("(.*)help_action.bin($)", OtherType.Text, ".txt" ),
            new("(.*)help_menu.bin($)", OtherType.Text, ".txt" ),
            new("(.*)help_penelosdiary.bin($)", OtherType.Text, ".txt" ),
            new("(.*)key_signal.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_ability.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_action.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_common.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_daijinamono.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_eventitem.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_gambit.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_license.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_sobicategory.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_status.bin($)", OtherType.Text, ".txt" ),
            new("(.*)listhelp_targetchip.bin($)", OtherType.Text, ".txt" ),
            new("(.*)menu(0[0-5]).bin($)", OtherType.Text, ".txt" ),
            new("(.*)menu_command.bin($)", OtherType.Text, ".txt" ),
            new("(.*)menu_command_template.bin($)", OtherType.Text, ".txt" ),
            new("(.*)menu_expansion.bin($)", OtherType.Text, ".txt" ),
            new("(.*)menu_mapname.bin($)", OtherType.Text, ".txt" ),
            new("(.*)menu_message.bin($)", OtherType.Text, ".txt" ),
            new("(.*)penelosdiary.bin($)", OtherType.Text, ".txt" ),
            new("(.*)questtitle.bin($)", OtherType.Text, ".txt" ),
            new("(.*).tm2($)", OtherType.Tim2, ".tm2.dir" )
        };
    }

    public enum OtherType
    {
        Script = 0,
        Text = 1,
        Tim2 = 2
    }
}
