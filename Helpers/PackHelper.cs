using Formats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace Helpers
{
    public class PackHelper
    {
        private static readonly Dictionary<string, byte[]> MagicExtensionMap = new()
        {
            [".cl2"] = new byte[] { 0x43, 0x4C, 0x54, 0x32 },
            [".mrp"] = new byte[] { 0x4D, 0x52, 0x50, 0x00 },
            [".tm2"] = new byte[] { 0x54, 0x49, 0x4D, 0x32 }
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true,
            Converters =
                {
                    new ByteArrayJsonConverter(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
        };

        public static void UnpackBattlepack(string filePath, string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            using var br = new BinaryReader(File.Open(filePath, FileMode.Open));
            var sectionOffsets = new List<uint>();
            var sectionCount = br.ReadUInt32() + 1; //+1 because of end of file offset

            for (var i = 0; i < sectionCount; i++)
            {
                sectionOffsets.Add(br.ReadUInt32());
            }

            br.BaseStream.Position = sectionOffsets[0]; //skip to first section
            for (var i = 0; i < sectionCount - 1; i++)
            {
                var length = sectionOffsets[i + 1] - sectionOffsets[i];
                if (length == 0)
                {
                    continue;
                }

                var data = br.ReadBytes((int)length);
                var sectionPath = Path.Combine(outputPath, $"section_{i:000}.bin");
                using var bw = new BinaryWriter(File.Open(sectionPath, FileMode.Create));
                bw.Write(data);
            }
        }

        public static void PackBattlepack(string inputPath, string outputPath, uint sectionCount)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using var bw = new BinaryWriter(File.Open(outputPath, FileMode.Create));
            bw.Write(sectionCount);

            //reserve space for section offsets and end of file offset
            bw.BaseStream.Seek((sectionCount + 1) * 4, SeekOrigin.Current); //+1 because the last section is the end of file offset

            //save section offsets, read section files and write data to the new battlepack.
            var sectionOffsets = new List<uint>();
            for (var i = 0; i < sectionCount; i++)
            {
                BinaryHelper.Align(bw, 16);
                sectionOffsets.Add((uint)bw.BaseStream.Position);
                var sectionPath = Path.Combine(inputPath, $"section_{i:000}.bin");
                var fileContent = Array.Empty<byte>();
                if (File.Exists(sectionPath))
                {
                    fileContent = File.ReadAllBytes(sectionPath);
                }
                bw.Write(fileContent);
            }
            sectionOffsets.Add((uint)bw.BaseStream.Position); //end of file offset (without alignment)
            BinaryHelper.Align(bw, 16);

            //write section offsets and end of file offset into the header
            bw.BaseStream.Seek(0x04, SeekOrigin.Begin);
            foreach (var offset in sectionOffsets)
            {
                bw.Write(offset);
            }
        }

        public static void UnpackOtherpack(string filePath, string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            using var br = new BinaryReader(File.Open(filePath, FileMode.Open));
            var sectionOffsets = new List<uint>();
            while (true)
            {
                var offset = br.ReadUInt32();
                if (offset == 0xFFFFFFFF)
                {
                    break;
                }
                sectionOffsets.Add(offset);
            }

            sectionOffsets.Add((uint)br.BaseStream.Length); //add end of file offset
            br.BaseStream.Seek(sectionOffsets[0], SeekOrigin.Begin); //skip to section 0
            for (var i = 0; i < sectionOffsets.Count - 1; i++)
            {
                var length = sectionOffsets[i + 1] - sectionOffsets[i];
                if (length == 0)
                {
                    continue;
                }

                var data = br.ReadBytes((int)length);

                //set extension based on magic
                var magic = data.Take(4);
                var extension = MagicExtensionMap.FirstOrDefault(j => j.Value.SequenceEqual(magic)).Key ?? ".bin";
                var output = Path.Combine(outputPath, $"section_{i:000}{extension}");
                using var bw = new BinaryWriter(File.Open(output, FileMode.Create));
                bw.Write(data);
            }
        }

        public static void PackOtherpack(string inputPath, string outputPath, uint sectionCount)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using var bw = new BinaryWriter(File.Open(outputPath, FileMode.Create));

            //reserve space for section offsets
            bw.BaseStream.Seek(sectionCount * 0x04, SeekOrigin.Begin);
            bw.Write(0xFFFFFFFF); //end of file indicator
            BinaryHelper.Align(bw, 16, true);

            if (!Directory.Exists(inputPath))
            {
                return;
            }

            //save section offsets, read section files and write data to the new pack.
            var sectionOffsets = new List<uint>();
            for (var i = 0; i < sectionCount; i++)
            {
                sectionOffsets.Add((uint)bw.BaseStream.Position);
                var sectionPath = Directory.EnumerateFiles(inputPath, "*.*", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(j => Path.GetFileNameWithoutExtension(j).Equals($"section_{i:000}"));

                var fileContent = Array.Empty<byte>();
                if (File.Exists(sectionPath))
                {
                    fileContent = File.ReadAllBytes(sectionPath);
                }

                bw.Write(fileContent);
                BinaryHelper.Align(bw, 16);
            }

            //write section offsets into the header
            bw.BaseStream.Seek(0x00, SeekOrigin.Begin);
            foreach (var offset in sectionOffsets)
            {
                bw.Write(offset);
            }
        }

        public static void UnpackEbp(string filePath, string outputPath, uint sectionCount)
        {
            Directory.CreateDirectory(outputPath);

            using var br = new BinaryReader(File.Open(filePath, FileMode.Open));
            var magic = new byte[] { 0x45, 0x42, 0x50, 0x32 }; //EBP2
            if (!br.ReadBytes(4).SequenceEqual(magic))
            {
                throw new ArgumentException("Ebp: Unexpected magic.");
            }

            //read section offsets
            br.BaseStream.Seek(0x10, SeekOrigin.Begin);
            var sections = new uint[sectionCount];
            for (var i = 0; i < sections.Length; i++)
            {
                sections[i] = br.ReadUInt32();
            }

            var indices = new int[sections.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }
            Array.Sort(sections, indices);

            for (var i = 0; i < sections.Length; i++)
            {
                if (sections[i] == 0)
                {
                    continue;
                }

                var length = i < sections.Length - 1 ? sections[i + 1] - sections[i] : (uint)br.BaseStream.Length - sections[i];
                if (length == 0)
                {
                    continue;
                }

                br.BaseStream.Seek(sections[i], SeekOrigin.Begin);
                var data = br.ReadBytes((int)length);
                var extension = indices[i] switch
                {
                    6 => ".tm2",
                    19 => ".ard",
                    _ => ".bin"
                };

                var output = Path.Combine(outputPath, $"section_{indices[i]:000}{extension}");
                using var bw = new BinaryWriter(File.Open(output, FileMode.Create));
                bw.Write(data);
            }
        }

        public static void PackEbp(string inputPath, string outputPath, uint sectionCount)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using var bw = new BinaryWriter(File.Open(outputPath, FileMode.Create));
            var magic = new byte[] { 0x45, 0x42, 0x50, 0x32 }; //EBP2
            bw.Write(magic);

            //reserve space for section offsets
            bw.BaseStream.Seek(0x80, SeekOrigin.Begin); //8 for magic, 8 unused, 20*4 for 20 section offsets, 32 unused (alignment).

            //save section offsets, read section files and write data to the new ebp.
            var sectionOffsets = new uint[sectionCount];
            for (var i = 0; i < sectionCount; i++)
            {
                var extension = i == 19 ? ".ard" : ".bin";
                var filePath = Path.Combine(inputPath, $"section_{i:000}{extension}");
                if (File.Exists(filePath))
                {
                    var fileData = File.ReadAllBytes(filePath);
                    if (fileData.Length != 0)
                    {
                        sectionOffsets[i] = (uint)bw.BaseStream.Position;
                        bw.Write(fileData);
                    }
                }
                BinaryHelper.Align(bw, 16);
            }

            //write section offsets into the header
            bw.BaseStream.Seek(0x10, SeekOrigin.Begin);
            foreach (var offset in sectionOffsets)
            {
                bw.Write(offset);
            }
        }

        public static void UnpackArd(string filePath, string outputPath, uint sectionCount)
        {
            Directory.CreateDirectory(outputPath);

            using var br = new BinaryReader(File.Open(filePath, FileMode.Open));
            var magic = new byte[] { 0x46, 0x46, 0x31, 0x32, 0x41, 0x52, 0x30, 0x33 }; //FF12AR03
            if (!br.ReadBytes(8).SequenceEqual(magic))
            {
                throw new ArgumentException("Ard: Unexpected magic.");
            }

            //read section offsets
            var sections = new uint[sectionCount];
            for (var i = 0; i < sections.Length; i++)
            {
                sections[i] = br.ReadUInt32();
            }

            var indices = new int[sectionCount];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }
            Array.Sort(sections, indices);

            for (var i = 0; i < sections.Length; i++)
            {
                if (sections[i] == 0)
                {
                    continue;
                }

                var length = i < sections.Length - 1 ? sections[i + 1] - sections[i] : (uint)br.BaseStream.Length - sections[i];
                if (length == 0)
                {
                    continue;
                }

                br.BaseStream.Seek(sections[i], SeekOrigin.Begin);
                var data = br.ReadBytes((int)length);

                var output = Path.Combine(outputPath, $"section_{indices[i]:000}.bin");
                using var bw = new BinaryWriter(File.Open(output, FileMode.Create));
                bw.Write(data);
            }
        }

        public static void PackArd(string inputPath, string outputPath, uint sectionCount)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using var bw = new BinaryWriter(File.Open(outputPath, FileMode.Create));
            var magic = new byte[] { 0x46, 0x46, 0x31, 0x32, 0x41, 0x52, 0x30, 0x33 }; //FF12AR03
            bw.Write(magic);

            //reserve space for section offsets
            bw.BaseStream.Seek(0x30, SeekOrigin.Begin); //8 for magic, 10*4 for 10 section offsets.

            //save section offsets, read section files and write data to the new file.
            var sectionOffsets = new uint[sectionCount];
            for (var i = 0; i < sectionOffsets.Length; i++)
            {
                if (i != 1) //handle section 1 later
                {
                    var filePath = Path.Combine(inputPath, $"section_{i:000}.bin");
                    if (File.Exists(filePath))
                    {
                        var fileData = File.ReadAllBytes(filePath);
                        if (fileData.Length != 0)
                        {
                            sectionOffsets[i] = (uint)bw.BaseStream.Position;
                            bw.Write(fileData);
                        }
                    }
                    BinaryHelper.Align(bw, 16);
                }
            }

            //check if section 1 exists and if it is empty or not.
            var modelPath = Path.Combine(inputPath, "section_001.bin");
            if (File.Exists(modelPath))
            {
                var modelData = File.ReadAllBytes(modelPath);
                if (modelData.Length != 0)
                {
                    sectionOffsets[1] = (uint)bw.BaseStream.Position;
                    bw.Write(modelData);
                }
            }

            //write section offsets into the header
            bw.BaseStream.Seek(0x08, SeekOrigin.Begin);
            foreach (var offset in sectionOffsets)
            {
                bw.Write(offset);
            }
        }

        public static void UnpackHimgd(string filePath, string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            using var br = new BinaryReader(File.Open(filePath, FileMode.Open));
            var magic = new[] { 'h', 'i', 'm', 'g', 'd', '\0', '\0', '\0' };
            if (!br.ReadChars(8).SequenceEqual(magic))
            {
                throw new ArgumentException("Himgd: Unexpected magic.");
            }

            br.BaseStream.Seek(0x02, SeekOrigin.Current); //skip index
            var sectionCount = br.ReadUInt16();

            //read section offsets
            var sections = new uint[sectionCount];
            for (var i = 0; i < sections.Length; i++)
            {
                sections[i] = br.ReadUInt32();
            }

            var indices = new int[sections.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }
            Array.Sort(sections, indices);

            for (var i = 0; i < sections.Length; i++)
            {
                if (sections[i] != 0)
                {
                    var length = i < sections.Length - 1 ? sections[i + 1] - sections[i] : (uint)br.BaseStream.Length - sections[i];
                    if (length > 0)
                    {
                        br.BaseStream.Seek(sections[i], SeekOrigin.Begin);
                        var data = br.ReadBytes((int)length);

                        var output = Path.Combine(outputPath, $"section_{indices[i]:000}.tm2");
                        using var bw = new BinaryWriter(File.Open(output, FileMode.Create));
                        bw.Write(data);
                    }
                }
            }
        }

        public static void PackHimgd(string inputPath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using var bw = new BinaryWriter(File.Open(outputPath, FileMode.Create));
            var magic = new[] { 'h', 'i', 'm', 'g', 'd', '\0', '\0', '\0' };
            bw.Write(magic);
            bw.Write((ushort)0x00); //empty index as it is impossible to determine with only this file.

            if (!Directory.Exists(inputPath))
            {
                return;
            }

            //get and write section count
            var regex = new Regex("section_(\\d{3}).tm2"); //section_000.tm2 -> section_999.tm2
            var sectionPathList = Directory.EnumerateFiles(inputPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(i => regex.IsMatch(i))
                .ToList();

            bw.Write((ushort)sectionPathList.Count);

            //reserve space for section offsets
            bw.BaseStream.Seek(sectionPathList.Count * 0x04, SeekOrigin.Current);
            BinaryHelper.Align(bw, 16);

            //save section offsets, read section files and write data to the new file.
            var sectionOffsets = new List<uint>();
            foreach (var sectionPath in sectionPathList)
            {
                sectionOffsets.Add((uint)bw.BaseStream.Position);
                var fileContent = Array.Empty<byte>();
                if (File.Exists(sectionPath))
                {
                    fileContent = File.ReadAllBytes(sectionPath);
                }

                bw.Write(fileContent);
                BinaryHelper.Align(bw, 16);
            }

            //write section offsets into the header.
            bw.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            foreach (var offset in sectionOffsets)
            {
                bw.Write(offset);
            }
        }

        public static void UnpackJson(string inputPath, string outputPath, Type fileType)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var data = Activator.CreateInstance(fileType, inputPath);
            var content = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(outputPath, content);
        }

        public static void PackJson(string inputPath, string outputPath, Type fileType)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var content = File.ReadAllText(inputPath);
            dynamic data = JsonSerializer.Deserialize(content, fileType, JsonOptions);
            data.WriteToBinary(outputPath);
        }

        public static void UnpackText(string inputPath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var lang = GetLanguageByPath(outputPath);

            var process = new Process();
            process.StartInfo.FileName = "bin\\ff12-text.exe";
            process.StartInfo.Arguments = $"-m=bin\\ext\\texttags.txt -t={lang} -u \"{inputPath}\" \"{outputPath}\"";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit();

            if (process.ExitCode > 0)
            {
                throw new ArgumentException($"{process.StandardError.ReadToEnd()}");
            }
        }

        public static void PackText(string inputPath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var lang = GetLanguageByPath(outputPath);

            var process = new Process();
            process.StartInfo.FileName = "bin\\ff12-text.exe";
            process.StartInfo.Arguments = $"-m=bin\\ext\\texttags.txt -t={lang} -p \"{inputPath}\" \"{outputPath}\"";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit();

            if (process.ExitCode > 0)
            {
                throw new ArgumentException($"{process.StandardError.ReadToEnd()}");
            }
        }

        public static void UnpackScript(string inputPath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var process = new Process();
            process.StartInfo.FileName = "bin\\ff12-script.exe";
            process.StartInfo.Arguments = $"-vd=bin\\ext\\variables.txt -fd=bin\\ext\\functions.txt -dc \"{inputPath}\" \"{outputPath}\"";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit();

            if (process.ExitCode > 0)
            {
                throw new ArgumentException($"{process.StandardError.ReadToEnd()}");
            }
        }

        public static void PackScript(string inputPath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var process = new Process();
            process.StartInfo.FileName = "bin\\ff12-script.exe";
            process.StartInfo.Arguments = $"-vd=bin\\ext\\variables.txt -fd=bin\\ext\\functions.txt -cc \"{inputPath}\" \"{outputPath}\"";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit();

            if (process.ExitCode > 0)
            {
                throw new ArgumentException($"{process.StandardError.ReadToEnd()}");
            }
        }

        public static void UnpackTm2(string inputPath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var tim2 = new Tim2(inputPath);
            tim2.WriteSingleLayerBinary(outputPath, JsonOptions);
        }

        public static void PackTm2(string inputPath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var json = File.ReadAllText(Path.Combine(inputPath, "info.json"));
            var tim2 = JsonSerializer.Deserialize<Tim2>(json, JsonOptions);
            tim2.WriteMultiLayerBinary(inputPath, outputPath);
        }

        public static bool CheckRequirements()
        {
            var binDir = Path.Combine(AppContext.BaseDirectory, "bin");
            var extDir = Path.Combine(binDir, "ext");
            Directory.CreateDirectory(binDir);
            Directory.CreateDirectory(extDir);

            var extFilePaths = new List<string>
            {
                Path.Combine(extDir, "functions.txt"),
                Path.Combine(extDir, "variables.txt"),
                Path.Combine(extDir, "texttags.txt")
            };

            foreach (var path in extFilePaths.Where(path => !File.Exists(path)))
            {
                File.Create(path);
            }

            var scriptToolPath = Path.Combine(binDir, "ff12-script.exe");
            var textToolPath = Path.Combine(binDir, "ff12-text.exe");
            return File.Exists(scriptToolPath) && File.Exists(textToolPath);
        }

        private static string GetLanguageByPath(string path)
        {
            return path switch
            {
                { } a when a.Contains("\\in\\") => "jp",
                { } b when b.Contains("\\kr\\") => "kr",
                { } c when c.Contains("\\cn\\") => "cs",
                { } d when d.Contains("\\ch\\") => "ct",
                _ => "en"
            };
        }
    }
}
