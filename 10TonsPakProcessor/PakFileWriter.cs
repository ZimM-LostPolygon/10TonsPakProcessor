using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TenTonsPakProcessor {
    internal class PakFileWriter {
        private readonly List<TenTonsPakFile> _files = new List<TenTonsPakFile>();

        public PakFileWriter() {
        }

        public void AddFile(string pakName, string path) {
            _files.Add(new TenTonsPakFile(pakName, path, new FileInfo(path).Length));
        }

        public void WritePak(Stream outputStream) {
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.ASCII);

            // Write magic header
            writer.Write('P');
            writer.Write('A');
            writer.Write('K');
            writer.Write((byte) 0);
            writer.Write('V');
            writer.Write('1');
            writer.Write('1');
            writer.Write((byte) 0);

            // Write content table position placeholder
            long contentTablePositionPosition = writer.BaseStream.Position;
            writer.Write((uint)0);

            // Write pak size placeholder
            long pakSizePositionPosition = writer.BaseStream.Position;
            writer.Write((uint)0);

            // Write files while remembering their offset
            uint[] filesOffsets = new uint[_files.Count];
            for (int i = 0; i < _files.Count; i++) {
                filesOffsets[i] = (uint) writer.BaseStream.Position;
                TenTonsPakFile pakFile = _files[i];
                byte[] fileBytes = File.ReadAllBytes(pakFile.Path);
                writer.Write(fileBytes);
            }

            // Write content table
            long contentTablePosition = writer.BaseStream.Position;
            writer.Write((uint)_files.Count);

            for (int i = 0; i < _files.Count; i++) {
                TenTonsPakFile pakFile = _files[i];
                byte[] pakNameBytes = Encoding.ASCII.GetBytes(pakFile.PakName);
                writer.Write(pakNameBytes);
                writer.Write((byte) 0);

                writer.Write((uint) filesOffsets[i]);
                writer.Write((uint) pakFile.Size);

                // Write unknown magic
                writer.Write((byte) 0xFF);
                writer.Write((byte) 0x26);
                writer.Write((byte) 0xE2);
                writer.Write((byte) 0x50);

                writer.Write((byte) 0x20);
                writer.Write((byte) 0x00);
                writer.Write((byte) 0x00);
                writer.Write((byte) 0x00);
            }

            long pakSize = writer.BaseStream.Position;

            // Write final content table position
            writer.Seek((int) contentTablePositionPosition, SeekOrigin.Begin);
            writer.Write((uint) contentTablePosition);

            // Write final pak size
            writer.Seek((int) pakSizePositionPosition, SeekOrigin.Begin);
            writer.Write((uint) pakSize);
        }

        private struct TenTonsPakFile {
            public readonly string PakName;
            public readonly string Path;
            public readonly long Size;

            public TenTonsPakFile(string pakName, string path, long size) {
                PakName = pakName.Replace('\\', '/');
                Path = path;
                Size = size;
            }
        }
    }
}