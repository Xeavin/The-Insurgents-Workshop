using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Formats.MenuHandBook
{
    public class Hcomg
    {
        private static readonly byte[] Magic = { 0x68, 0x63, 0x6F, 0x6D, 0x67, 0x00, 0x00, 0x00 }; //hcomg

        [JsonPropertyName("Cartographer - Required Maps")]
        public Dictionary<string, bool> CartographerMapStates { get; set; }

        [JsonPropertyName("Scrivener - Required Foe Count")]
        public ushort ScrivenerRequiredFoeCount { get; set; }

        [JsonConverter(typeof(ScrivenerConverter))]
        [JsonPropertyName("Scrivener - Custom Foe Requirements")]
        public Dictionary<string, dynamic> ScrivenerRequirements { get; set; }

        [JsonPropertyName("Clan Rank - Hunt Completion Requirements")]
        public Dictionary<string, Hunt> HuntRequirements { get; set; }

        [JsonPropertyName("Defeated Foe Figures - Bestiary Identifier")]
        public Dictionary<string, ushort> DefeatedFoeFigures { get; set; }

        [JsonPropertyName("Figure Parameters")]
        public Dictionary<string, int> FigureParameters { get; set; }

        [JsonConstructor]
        public Hcomg(Dictionary<string, bool> cartographerMapStates, ushort scrivenerRequiredFoeCount, Dictionary<string, dynamic> scrivenerRequirements, Dictionary<string, Hunt> huntRequirements, Dictionary<string, ushort> defeatedFoeFigures, Dictionary<string, int> figureParameters)
        {
            if (cartographerMapStates.Count != 384)
            {
                throw new ArgumentException("Hcomg: 'Cartographer - Required Maps' must have exactly 384 entries.");
            }

            if (scrivenerRequiredFoeCount > 512)
            {
                throw new ArgumentException("Hcomg: 'Scrivener - Required Foe Count' cannot be higher than 512.");
            }

            CartographerMapStates = cartographerMapStates;
            ScrivenerRequiredFoeCount = scrivenerRequiredFoeCount;
            ScrivenerRequirements = scrivenerRequirements;
            HuntRequirements = huntRequirements;
            DefeatedFoeFigures = defeatedFoeFigures;
            FigureParameters = figureParameters;
        }

        public Hcomg(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(8).SequenceEqual(Magic))
            {
                throw new ArgumentException("Hcomg: Unexpected magic.");
            }
            var baseOffset = (ushort)(br.BaseStream.Position + br.ReadUInt16());

            //the map count is hardcoded into the executable anyway.
            br.BaseStream.Seek(0x02, SeekOrigin.Current);
            const ushort mapCount = 384;
            var mapStates = br.ReadBytes(48); //48 = 384 / 8

            CartographerMapStates = new Dictionary<string, bool>();
            for (var i = 0; i < mapCount; i++)
            {
                var state = (mapStates[i / 8] >> i % 8 & 0x01) == 1;
                CartographerMapStates.Add($"Map {i}", state);
            }

            br.BaseStream.Seek(baseOffset, SeekOrigin.Begin);
            var huntOffset = (ushort)(br.BaseStream.Position + br.ReadUInt16());
            ScrivenerRequiredFoeCount = br.ReadUInt16();
            br.BaseStream.Seek(0x02, SeekOrigin.Current); //skip unused 2 bytes

            var scrivenerCount = br.ReadUInt16();
            ScrivenerRequirements = new Dictionary<string, dynamic>();
            for (var i = 0; i < scrivenerCount; i++)
            {
                var bestiaryId = br.ReadUInt16();
                var type = bestiaryId >> 12;

                dynamic scrivener;
                switch (type)
                {
                    case 0:
                        scrivener = new ScrivenerStory();
                        scrivener.ViewedBestiaryIdentifier = (ushort)(bestiaryId & 0x0FFF);
                        scrivener.RequiredStoryProgress = br.ReadUInt16();
                        break;
                    case 1:
                        scrivener = new ScrivenerFoe();
                        scrivener.ViewedBestiaryIdentifier = (ushort)(bestiaryId & 0x0FFF);
                        scrivener.RequiredBestiaryIdentifier = br.ReadUInt16();
                        break;
                    default:
                        throw new ArgumentException("Hcomg: 'Scrivener -> Type' is unknown.");
                }
                ScrivenerRequirements.Add($"Foe {i}", scrivener);
            }

            br.BaseStream.Seek(huntOffset, SeekOrigin.Begin);
            var defeatedFoeFiguresOffset = (ushort)(br.BaseStream.Position + br.ReadUInt16());

            var huntCount = br.ReadUInt16();
            HuntRequirements = new Dictionary<string, Hunt>();
            for (var i = 0; i < huntCount; i++)
            {
                var hunt = new Hunt
                {
                    QuestIdentifier = br.ReadByte(),
                    RequiredQuestStage = br.ReadByte()
                };
                HuntRequirements.Add($"Hunt {i}", hunt);
            }

            br.BaseStream.Seek(defeatedFoeFiguresOffset, SeekOrigin.Begin);
            var figureParametersOffset = (ushort)(br.BaseStream.Position + br.ReadUInt16());

            br.BaseStream.Seek(0x02, SeekOrigin.Current);
            DefeatedFoeFigures = new Dictionary<string, ushort>();
            for (var i = 0; i < 10; i++) //the defeated foe figure count is hardcoded into the executable anyway.
            {
                DefeatedFoeFigures.Add(Figures[i + 20], br.ReadUInt16());
            }

            br.BaseStream.Seek(figureParametersOffset, SeekOrigin.Begin);
            br.BaseStream.Seek(0x02, SeekOrigin.Current); //skip end of file offset

            //since the figure parameter count isn't always accurate, calculate from figure states instead.
            br.BaseStream.Seek(0x02, SeekOrigin.Current);
            var figureStates = br.ReadUInt32();
            FigureParameters = new Dictionary<string, int>();
            for (var i = 0; i < Figures.Count; i++)
            {
                if ((figureStates >> i & 0x01) == 1)
                {
                    FigureParameters.Add(Figures[i], br.ReadInt32());
                }
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(Magic);
            var baseOffsetPosition = bw.BaseStream.Position;
            bw.BaseStream.Seek(0x02, SeekOrigin.Current); //set base offset later
            bw.Write((ushort)CartographerMapStates.Count);

            var mapStates = new byte[48]; //48 = 384 / 8
            for (var i = 0; i < CartographerMapStates.Count; i++)
            {
                if (CartographerMapStates.ElementAt(i).Value)
                {
                    mapStates[i / 8] |= (byte)(1 << i % 8);
                }
            }
            bw.Write(mapStates);

            var baseOffset = (ushort)(bw.BaseStream.Position - baseOffsetPosition);
            var huntOffsetPosition = bw.BaseStream.Position;
            bw.BaseStream.Seek(0x02, SeekOrigin.Current); //set hunt offset later
            bw.Write(ScrivenerRequiredFoeCount);
            bw.BaseStream.Seek(0x02, SeekOrigin.Current); //skip unused 2 bytes
            bw.Write((ushort)ScrivenerRequirements.Count);
            foreach (var foe in ScrivenerRequirements.Values)
            {
                switch ((int)foe.Type)
                {
                    case 0:
                        bw.Write(foe.ViewedBestiaryIdentifier);
                        bw.Write(foe.RequiredStoryProgress);
                        break;
                    case 1:
                        bw.Write((ushort)(foe.ViewedBestiaryIdentifier | 1 << 12));
                        bw.Write(foe.RequiredBestiaryIdentifier);
                        break;
                }
            }

            var huntOffset = (ushort)(bw.BaseStream.Position - huntOffsetPosition);
            var defeatedFoeFiguresOffsetPosition = bw.BaseStream.Position;
            bw.BaseStream.Seek(0x02, SeekOrigin.Current); //set defeated foe figures offset later

            bw.Write((ushort)HuntRequirements.Count);
            foreach (var hunt in HuntRequirements.Values)
            {
                bw.Write(hunt.QuestIdentifier);
                bw.Write(hunt.RequiredQuestStage);
            }
            BinaryHelper.Align(bw, 4);

            var defeatedFoeFiguresOffset = (ushort)(bw.BaseStream.Position - defeatedFoeFiguresOffsetPosition);
            var figureParametersOffsetPosition = bw.BaseStream.Position;
            bw.BaseStream.Seek(0x02, SeekOrigin.Current); //set figure parameters offset later

            bw.Write((ushort)DefeatedFoeFigures.Count);
            foreach (var dff in DefeatedFoeFigures.Values)
            {
                bw.Write(dff);
            }

            var figureParametersOffset = (ushort)(bw.BaseStream.Position - figureParametersOffsetPosition);
            var endOfFileOffsetPosition = bw.BaseStream.Position;
            bw.BaseStream.Seek(0x02, SeekOrigin.Current); //set end of file offset later

            bw.Write((ushort)FigureParameters.Count);
            var figureStates = 0;
            foreach (var parameter in FigureParameters.Keys)
            {
                var exp = Figures.IndexOf(parameter);
                if (exp != -1)
                {
                    figureStates |= 1 << exp;
                }
            }
            bw.Write(figureStates);

            foreach (var parameter in FigureParameters.Values)
            {
                bw.Write(parameter);
            }

            var endOfFileOffset = (ushort)(bw.BaseStream.Position - endOfFileOffsetPosition); //without alignment
            BinaryHelper.Align(bw, 16);

            bw.BaseStream.Seek(baseOffsetPosition, SeekOrigin.Begin);
            bw.Write(baseOffset);
            bw.BaseStream.Seek(huntOffsetPosition, SeekOrigin.Begin);
            bw.Write(huntOffset);
            bw.BaseStream.Seek(defeatedFoeFiguresOffsetPosition, SeekOrigin.Begin);
            bw.Write(defeatedFoeFiguresOffset);
            bw.BaseStream.Seek(figureParametersOffsetPosition, SeekOrigin.Begin);
            bw.Write(figureParametersOffset);
            bw.BaseStream.Seek(endOfFileOffsetPosition, SeekOrigin.Begin);
            bw.Write(endOfFileOffset);
        }

        public class ScrivenerStory
        {
            [JsonPropertyName("Type")]
            public byte Type { get; set; }

            [JsonPropertyName("Viewed Bestiary Identifier")]
            public ushort ViewedBestiaryIdentifier { get; set; }

            [JsonPropertyName("Required Story Progress")]
            public ushort RequiredStoryProgress { get; set; }

            public ScrivenerStory()
            {
                Type = 0;
            }
        }

        public class ScrivenerFoe
        {
            [JsonPropertyName("Type")]
            public byte Type { get; set; }

            [JsonPropertyName("Viewed Bestiary Identifier")]
            public ushort ViewedBestiaryIdentifier { get; set; }

            [JsonPropertyName("Required Bestiary Identifier")]
            public ushort RequiredBestiaryIdentifier { get; set; }

            public ScrivenerFoe()
            {
                Type = 1;
            }
        }

        private class ScrivenerConverter : JsonConverter<Dictionary<string, dynamic>>
        {
            public override Dictionary<string, dynamic> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var scrivenerRequirements = new Dictionary<string, dynamic>();
                var jsonDoc = JsonDocument.ParseValue(ref reader);

                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    var typeName = property.Value.GetProperty("Type").GetByte();
                    var typeClass = entryTypeMap.GetValueOrDefault(typeName);
                    if (typeClass == null)
                    {
                        throw new ArgumentException("Hcomg: 'Scrivener -> Type' is unknown.");
                    }

                    var data = JsonSerializer.Deserialize(property.Value.ToString(), typeClass, options);
                    scrivenerRequirements.Add(property.Name, data);
                }
                return scrivenerRequirements;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, dynamic> data, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, data, options);
            }

            private readonly Dictionary<byte, Type> entryTypeMap = new()
            {
                { 0, typeof(ScrivenerStory) },
                { 1, typeof(ScrivenerFoe) }
            };
        }

        public class Hunt
        {
            [JsonPropertyName("Quest Identifier")]
            public byte QuestIdentifier { get; set; }

            [JsonPropertyName("Required Quest Stage")]
            public byte RequiredQuestStage { get; set; }
        }

        private static readonly List<string> Figures = new()
        {
            "Balthier (Assault Striker)",
            "Fran (Spellsinger)",
            "Vayne (Premier Prestidigitator)",
            "Vaan (Master Thief)",
            "Basch (Blood Dancer)",
            "Chocobo (Wayfarer)",
            "Penelo (Plunderer)",
            "Gurdy (Spendthrift)",
            "Ashe (Exemplar)",
            "Crystal (Runeweaver)",
            "Vossler (Jack-of-All-Trades)",
            "Rasler (Conqueror)",
            "Belias (High Summoner)",
            "Gabranth (Mist Walker)",
            "Migelo (Privateer)",
            "Old Dalan (Cartographer)",
            "Ba'Gamnan (Scrivener)",
            "Reks (Record Breaker)",
            "Montblanc (The Unrelenting)",
            "Mimic? (Collector)",
            "Trickster (Sharpshooter)",
            "Gilgamesh (Master Swordsman)",
            "Behemoth King (Lord of the Kings)",
            "Fafnir (Wyrmslayer)",
            "Carrot (Freshmaker)",
            "Deathgaze (Eagle Eye)",
            "Yiazmat (Hunter Extraordinaire)",
            "Hell Wyrm (Radiant Savior)",
            "Ultima (Fell Angel)",
            "Zodiark (Zodiac Knight)"
        };
    }
}
