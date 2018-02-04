using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TenTonsPakProcessor {
    internal class Program {
        private static void Main(string[] args) {
            Console.WriteLine("10tons .pak processor by ZimM" + Environment.NewLine);
            if (args.Length == 0) {
                ShowUsageHelp();
                return;
            }

            try {
                string action = args[0].ToLowerInvariant();
                if (action == "-pack") {
                    PackPak(args[1], args[2]);
                } else if (action == "-unpack") {
                    UnpackPak(args[1]);
                } else {
                    ShowUsageHelp();
                }
            } catch (Exception e) {
                Console.WriteLine($"Error while executing operation: {e}");
                Environment.ExitCode = 1;
            }
        }

        private static void ShowUsageHelp() {
            Console.WriteLine(
@"Usage:
    10TonsPakProcessor -unpack someFileToUnpack.pak
    10TonsPakProcessor -pack <inputDirectory> newPakFile.pak"
            );
            Console.ReadLine();
            Environment.ExitCode = 1;
        }

        private static void PackPak(string inputDirectory, string pakPath) {
            Console.Write("Retrieving files list... ");
            string[] files = Directory.GetFiles(inputDirectory, "*", SearchOption.AllDirectories);
            inputDirectory = Path.GetFullPath(inputDirectory + Path.DirectorySeparatorChar);
            Tuple<string, string>[] relativeFiles =
                files
                .Select(file => new Tuple<string, string>(file, MakeRelativePath(inputDirectory, Path.GetFullPath(file)))).ToArray();
            relativeFiles = relativeFiles.OrderBy(tuple => tuple.Item1).ToArray();
            Console.WriteLine("{0} files to be packed.", files.Length);

            PakFileWriter pakFileWriter = new PakFileWriter();
            foreach (Tuple<string, string> t in relativeFiles) {
                pakFileWriter.AddFile(t.Item2, t.Item1);
            }

            using (FileStream outputStream = new FileStream(pakPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)) {
                pakFileWriter.WritePak(outputStream);
            }
        }

        private static void UnpackPak(string pakPath) {
            FileStream stream = new FileStream(pakPath, FileMode.Open, FileAccess.Read);
            PakFileReader pakFileReader = new PakFileReader(stream);

            IReadOnlyList<PakFileItem> files = pakFileReader.Files;
            Console.WriteLine("Unpacking {0} files...", files.Count);

            foreach (PakFileItem file in files) {
                Console.WriteLine("Unpacking '{0}' ({1} bytes)", file.Name, file.Size);

                string dirPath = Path.GetDirectoryName(file.Name);
                if (!String.IsNullOrWhiteSpace(dirPath) &&
                    !Directory.Exists(dirPath)) {
                    Directory.CreateDirectory(dirPath);
                }

                // Retrieving the data from .pak
                byte[] data = pakFileReader.GetData(file);

                // Saving the data
                File.WriteAllBytes(file.Name, data);
            }
        }

        private static string MakeRelativePath(string fromPath, string toPath) {
            if (string.IsNullOrEmpty(fromPath))
                throw new ArgumentNullException(nameof(fromPath));

            if (string.IsNullOrEmpty(toPath))
                throw new ArgumentNullException(nameof(toPath));

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
                return toPath;

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase)) {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}
