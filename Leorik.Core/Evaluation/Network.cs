using System.Text;

namespace Leorik.Core
{
    public class Network
    {
        struct Header
        {
            public string Magic;
            public ushort Version;
            public ushort Flags;
            public byte Padding;
            public byte Arch;
            public byte Activation;
            public ushort HiddenSize;
            public byte InputBuckets;
            public byte OutputBuckets;
            public byte NameLen;
            public string Name;

            public Header(ushort hiddenSize)
            {
                HiddenSize = hiddenSize;
            }

            internal void Read(BinaryReader reader)
            {
                //Read header
                Magic = new string(reader.ReadChars(4));
                if (Magic != "CBNF")
                    throw new Exception("Invalid magic bytes in network header");

                Version = reader.ReadUInt16();
                if(Version != 1)
                    throw new Exception($"Unsupported Header Version. Expected 1 but got {Version}");

                Flags = reader.ReadUInt16();
                Padding = reader.ReadByte();
                Arch = reader.ReadByte();
                Activation = reader.ReadByte();
                HiddenSize = reader.ReadUInt16();
                InputBuckets = reader.ReadByte();
                OutputBuckets = reader.ReadByte();
                NameLen = reader.ReadByte();
                Name = new string(reader.ReadChars(NameLen));
                reader.BaseStream.Position += 48 - NameLen;
            }
        }
        // current arch: (768->768)x2->1, ClippedReLU
        // perspective
        //const int ArchId = 1;
        //using Activation = activation::ClippedReLU<255>;
        //const uint Layer1Size = 768;
        //const int Scale = 400;
        //const int Q = 255 * 64;

        public static Network Default { get; private set; }
        public static void InitDefaultNetwork(string filePath)
        {
            Default = new Network(filePath);
        }

        const uint InputSize = 768;
        public short[] FeatureWeights; //new short[InputSize * Layer1Size];
        public short[] FeatureBiases; //new short[Layer1Size];
        public short[] OutputWeights; //new short[Layer1Size * 2];
        public short OutputBias;

        private Header _header;
        public uint Layer1Size => _header.HiddenSize;
        public uint FeatureWeightsCount => Layer1Size * InputSize;
        public uint FeatureBiasesCount => Layer1Size;

        public Network(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    //_header = default;
                    //_header.Read(reader);
                    _header = new Header(128);

                    FeatureWeights = new short[InputSize * Layer1Size];
                    for (int i = 0; i < FeatureWeights.Length; i++)
                        FeatureWeights[i] = reader.ReadInt16();

                    FeatureBiases = new short[Layer1Size];
                    for (int i = 0; i < FeatureBiases.Length; i++)
                        FeatureBiases[i] = reader.ReadInt16();

                    OutputWeights = new short[Layer1Size * 2];
                    for (int i = 0; i < OutputWeights.Length; i++)
                        OutputWeights[i] = reader.ReadInt16();

                    OutputBias = reader.ReadInt16();

                    //remaining data should be padding
                    //Debug.Assert(reader.BaseStream.Length - reader.BaseStream.Position < 64);
                    //while (reader.BaseStream.Position < reader.BaseStream.Length)
                    //    Debug.Assert(reader.ReadByte() == 0);
                }
            }
        }
    }
}
