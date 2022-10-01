using Helpers;
using Resources;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TheInsurgentsWorkshop
{
    public class Program
    {
        private static void Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine(fvi.ProductName + " v" + fvi.ProductVersion + " by " + fvi.CompanyName + "\n");

            try
            {
                var sourceDir = Path.Combine(AppContext.BaseDirectory, "files\\source");
                var unpackedDir = Path.Combine(AppContext.BaseDirectory, "files\\unpacked");
                var newpackDir = Path.Combine(AppContext.BaseDirectory, "files\\newpack");
                Directory.CreateDirectory(sourceDir);
                Directory.CreateDirectory(unpackedDir);
                Directory.CreateDirectory(newpackDir);

                if (!PackHelper.CheckRequirements())
                {
                    throw new ArgumentException("Error: One or more requirements are missing.");
                }

                DisplayOptions();
                while (true)
                {
                    var success = true;
                    var inputLine = Console.ReadLine();
                    switch (inputLine)
                    {
                        case "0":
                            {
                                Environment.Exit(0);
                                break;
                            }
                        case "1":
                            {
                                foreach (var pack in PackFile.FileList)
                                {
                                    var regex = new Regex(pack.Name);
                                    var filePaths = Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                                        .Where(i => regex.IsMatch(i))
                                        .ToList();

                                    foreach (var path in filePaths)
                                    {
                                        var outputPath = path + ".dir";
                                        switch (pack.Type)
                                        {
                                            case PackType.Battelepack:
                                                PackHelper.UnpackBattlepack(path, outputPath);
                                                break;
                                            case PackType.Otherpack:
                                                PackHelper.UnpackOtherpack(path, outputPath);
                                                break;
                                            case PackType.Ebp:
                                                PackHelper.UnpackEbp(path, outputPath, pack.SectionCount);
                                                break;
                                            case PackType.Ard:
                                                PackHelper.UnpackArd(path, outputPath, pack.SectionCount);
                                                break;
                                            case PackType.Himgd:
                                                PackHelper.UnpackHimgd(path, outputPath);
                                                break;
                                        }
                                    }
                                }
                                break;
                            }
                        case "2":
                            {
                                foreach (var pack in Enumerable.Reverse(PackFile.FileList))
                                {
                                    var regex = new Regex(pack.Name);
                                    var dirPaths = Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                                        .Where(i => regex.IsMatch(i))
                                        .ToList();

                                    foreach (var path in dirPaths)
                                    {
                                        var outputPath = path.Replace("source", "newpack");
                                        var inputPath = outputPath + ".dir";

                                        switch (pack.Type)
                                        {
                                            case PackType.Battelepack:
                                                PackHelper.PackBattlepack(inputPath, outputPath, pack.SectionCount);
                                                break;
                                            case PackType.Otherpack:
                                                PackHelper.PackOtherpack(inputPath, outputPath, pack.SectionCount);
                                                break;
                                            case PackType.Ebp:
                                                PackHelper.PackEbp(inputPath, outputPath, pack.SectionCount);
                                                break;
                                            case PackType.Ard:
                                                PackHelper.PackArd(inputPath, outputPath, pack.SectionCount);
                                                break;
                                            case PackType.Himgd:
                                                PackHelper.PackHimgd(inputPath, outputPath);
                                                break;
                                        }
                                    }
                                }
                                break;
                            }
                        case "3":
                            {
                                foreach (var file in JsonFile.FileList)
                                {
                                    var regex = new Regex(file.Name);
                                    var filePaths = Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                                        .Where(i => regex.IsMatch(i))
                                        .ToList();

                                    foreach (var path in filePaths)
                                    {
                                        var outputPath = Path.ChangeExtension(path.Replace("source", "unpacked"), ".json");
                                        PackHelper.UnpackJson(path, outputPath, file.Class);
                                    }
                                }

                                foreach (var file in OtherFile.FileList)
                                {
                                    var regex = new Regex(file.Name);
                                    var filePaths = Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                                        .Where(i => regex.IsMatch(i))
                                        .ToList();

                                    foreach (var path in filePaths)
                                    {
                                        var outputPath = Path.ChangeExtension(path.Replace("source", "unpacked"), file.Extension);
                                        switch (file.Type)
                                        {
                                            case OtherType.Script:
                                                PackHelper.UnpackScript(path, outputPath);
                                                break;
                                            case OtherType.Text:
                                                PackHelper.UnpackText(path, outputPath);
                                                break;
                                            case OtherType.Tim2:
                                                PackHelper.UnpackTm2(path, outputPath);
                                                break;
                                        }
                                    }
                                }
                                break;
                            }
                        case "4":
                            {
                                foreach (var file in Enumerable.Reverse(JsonFile.FileList))
                                {
                                    var regex = new Regex(file.Name);
                                    var filePaths = Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                                        .Where(i => regex.IsMatch(i))
                                        .ToList();

                                    foreach (var path in filePaths)
                                    {
                                        var inputPath = Path.ChangeExtension(path.Replace("source", "unpacked"), ".json");
                                        var outputPath = path.Replace("source", "newpack");
                                        PackHelper.PackJson(inputPath, outputPath, file.Class);
                                    }
                                }

                                foreach (var file in Enumerable.Reverse(OtherFile.FileList))
                                {
                                    var regex = new Regex(file.Name);
                                    var filePaths = Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                                        .Where(i => regex.IsMatch(i))
                                        .ToList();

                                    foreach (var path in filePaths)
                                    {
                                        var inputPath = Path.ChangeExtension(path.Replace("source", "unpacked"), file.Extension);
                                        var outputPath = path.Replace("source", "newpack");
                                        switch (file.Type)
                                        {
                                            case OtherType.Script:
                                                PackHelper.PackScript(inputPath, outputPath);
                                                break;
                                            case OtherType.Text:
                                                PackHelper.PackText(inputPath, outputPath);
                                                break;
                                            case OtherType.Tim2:
                                                PackHelper.PackTm2(inputPath, outputPath);
                                                break;
                                        }
                                    }
                                }
                                break;
                            }
                        default:
                            Console.Clear();
                            DisplayOptions();
                            success = false;
                            break;
                    }

                    if (success)
                    {
                        Console.WriteLine("\nSucceeded!");
                        Console.Write("\nSelect an option: ");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{GetFullExceptionMessage(e)}");
                Console.Write("\nPress any key to exit ...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private static void DisplayOptions()
        {
            Console.WriteLine("Choose an option:");
            Console.WriteLine("0) Exit Application");
            Console.WriteLine("1) Unpack Packages");
            Console.WriteLine("2) Pack Packages");
            Console.WriteLine("3) Unpack Files");
            Console.WriteLine("4) Pack Files");
            Console.Write("\nSelect an option: ");
        }

        public static string GetFullExceptionMessage(Exception exception)
        {
            return exception.InnerException == null
                ? exception.Message
                : exception.Message + " --> " + GetFullExceptionMessage(exception.InnerException);
        }
    }
}
