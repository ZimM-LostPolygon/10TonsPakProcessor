namespace TenTonsPakProcessor {
    internal class PakFileItem {
        public string Name { get; }
        public uint Offset { get; }
        public uint Size { get; }

        public PakFileItem(string name, uint offset, uint size) {
            Name = name;
            Offset = offset;
            Size = size;
        }
    }
}