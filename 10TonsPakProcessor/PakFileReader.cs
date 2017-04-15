using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TenTonsPakProcessor {
    internal class PakFileReader : IDisposable {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly List<PakFileItem> _files = new List<PakFileItem>();

        public IReadOnlyList<PakFileItem> Files => _files;

        public PakFileReader(Stream stream) {
            _stream = stream;
            _reader = new BinaryReader(stream);
            ReadHeaders();
        }

        public void Dispose() {
            _reader?.Close();
            _stream?.Close();
        }

        public byte[] GetData(PakFileItem file) {
            _stream.Seek(file.Offset, SeekOrigin.Begin);

            byte[] data = new byte[file.Size];
            _stream.Read(data, 0, data.Length);
            return data;
        }

        private void ReadHeaders() {
            // Skip magic (8 bytes)
            _stream.Seek(8, SeekOrigin.Begin);

            // Read the offset to file list
            uint fileListOffset = _reader.ReadUInt32();

            // Seek to the beginning of file list
            _stream.Seek(fileListOffset, SeekOrigin.Begin);

            // Skip number of files (4 bytes)
            _stream.Seek(4, SeekOrigin.Current);
            do {
                string fileName = ReadCString(_stream);
                if (fileName == null)
                    break;

                uint offset = _reader.ReadUInt32();
                uint size = _reader.ReadUInt32();

                // Skip unknown data (8 bytes)
                _stream.Seek(8, SeekOrigin.Current);

                _files.Add(new PakFileItem(fileName, offset, size));
            } while (true);
        }

        // Reads a C-style 0-terminated string from Stream
        private static string ReadCString(Stream stream) {
            List<byte> stringData = new List<byte>(16);
            do {
                int currentByte = stream.ReadByte();
                if (currentByte == -1) {
                    return null;
                }
                if (currentByte == 0) {
                    break;
                }

                stringData.Add((byte) currentByte);
            } while (true);

            return Encoding.ASCII.GetString(stringData.ToArray());
        }
    }
}