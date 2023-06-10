using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Leorik.Core
{
    public class NNUE_ParseException : Exception
    {
        public NNUE_ParseException(string? message) : base("nnue file data error: " + message)
        {
        }
    }

    public static class NNUE_Layout
    {
        public const int weight_scale = 6;
        public const int Traits = 1024;
        public const int InputDimensions = 64 * (11 * 64) / 2;
        public const int Dims = 8;
    }

    class FeatureTransform
    {
        readonly short[] Biases = new short[NNUE_Layout.Traits];
        readonly short[] Weights = new short[NNUE_Layout.Traits * NNUE_Layout.InputDimensions];
        readonly int[] PSQT = new int[NNUE_Layout.Dims * NNUE_Layout.InputDimensions];


        public FeatureTransform(BinaryReader reader) {
            uint hash = reader.ReadUInt32();
            uint hash_expect = 0x7f234cb8u ^ (NNUE_Layout.Traits * 2);
            if (hash != hash_expect) throw new NNUE_ParseException("nnue featuretransform hashcode missmatch");

            Buffer.BlockCopy(reader.ReadBytes(Biases.Length * sizeof(short)), 0, Biases, 0, Biases.Length * sizeof(short));
            Buffer.BlockCopy(reader.ReadBytes(Weights.Length * sizeof(short)), 0, Weights, 0, Weights.Length * sizeof(short));
            Buffer.BlockCopy(reader.ReadBytes(PSQT.Length * sizeof(int)), 0, PSQT, 0, PSQT.Length * sizeof(int));
        }
    }

    class NetworkShape
    {
        public int InputDimensions { get; }
        public int PaddedInputDimensions { get; }
        public int OutputDimensions { get; }
        
        public int WeightCount { get; } //int8_t
        public int BiasCount { get; } //int32_t

        public NetworkShape(int InDims, int OutDims)
        {
            InputDimensions = InDims;
            PaddedInputDimensions = (InDims + 32 - 1) / 32 * 32; //Upwards round towards 32
            OutputDimensions = OutDims;

            WeightCount = OutputDimensions * PaddedInputDimensions;
            BiasCount = OutputDimensions;
        }

        public void deserialize(BinaryReader reader, int[] biases, sbyte[] weights)
        {
            if (biases.Length != BiasCount) throw new NNUE_ParseException("bias count array missmatch");
            if (weights.Length != WeightCount) throw new NNUE_ParseException("weight count array missmatch");
            Buffer.BlockCopy(reader.ReadBytes(BiasCount * sizeof(uint)), 0, biases, 0, BiasCount * sizeof(uint));
            Buffer.BlockCopy(reader.ReadBytes(WeightCount * sizeof(sbyte)), 0, weights, 0, WeightCount * sizeof(sbyte));
        }

        // Hash value embedded in the evaluation file
        public uint get_hash_value(uint prevHash) {
            uint hashValue = 0xCC03DAE4u;
            hashValue += (uint)OutputDimensions;
            hashValue ^= prevHash >> 1;
            hashValue ^= prevHash << 31;
            return hashValue;
        }
    }

    class Network
    {
        static NetworkShape Layer0 = new NetworkShape(1024, 16);
        static NetworkShape Layer1 = new NetworkShape(30, 32);
        static NetworkShape Layer2 = new NetworkShape(32, 1);

        readonly sbyte[] w0 = new sbyte[Layer0.WeightCount]; readonly int[] b0 = new int[Layer0.BiasCount];
        readonly sbyte[] w1 = new sbyte[Layer1.WeightCount]; readonly int[] b1 = new int[Layer1.BiasCount];
        readonly sbyte[] w2 = new sbyte[Layer2.WeightCount]; readonly int[] b2 = new int[Layer2.BiasCount]; //b2 is just int really

        public Network(BinaryReader reader)
        {
            uint hash_expected = reader.ReadUInt32();
            uint hash = 0xEC42E90Du;
            hash ^= NNUE_Layout.Traits * 2;
            hash = Layer0.get_hash_value(hash);
            hash += 0x538D24C7u; //consolidated clipping layers
            hash = Layer1.get_hash_value(hash);
            hash += 0x538D24C7u; //consolidated clipping layers
            hash = Layer2.get_hash_value(hash);

            if (hash_expected != hash) throw new NNUE_ParseException("nnue network hashcode missmatch");

            Layer0.deserialize(reader, b0, w0);
            Layer1.deserialize(reader, b1, w1);
            Layer2.deserialize(reader, b2, w2); 
        }
        
        public int Propagate(ReadOnlySpan<short> acc, Color SideToMove)
        {
            Span<byte> input = stackalloc byte[NNUE_Layout.Traits];

            //Pointer declarations - make code more readable and compiled away offsets in inner loops
            Span<byte> own_half = input.Slice(SideToMove == Color.White ? 0 : NNUE_Layout.Traits / 2);
            Span<byte> opp_half = input.Slice(SideToMove == Color.White ? NNUE_Layout.Traits / 2 : 0);

            int own_offset = SideToMove == Color.White ? 0 : 1024;
            int opp_offset = SideToMove == Color.White ? 1024 : 0;

            ReadOnlySpan<short> own1 = acc.Slice(own_offset + 0);    //+ 0;
            ReadOnlySpan<short> own2 = acc.Slice(own_offset + 512);  //+ 512;
            ReadOnlySpan<short> opp1 = acc.Slice(opp_offset + 0);    //+ 1024;
            ReadOnlySpan<short> opp2 = acc.Slice(opp_offset + 512);  //+ 1536;

            //Featuretransform
            for (int j = 0; j < 512; ++j)
            {
                short sum0 = Math.Max((short)0, Math.Min((short)127, own1[j]));
                short sum1 = Math.Max((short)0, Math.Min((short)127, own2[j]));
                short sum3 = Math.Max((short)0, Math.Min((short)127, opp1[j]));
                short sum4 = Math.Max((short)0, Math.Min((short)127, opp2[j]));
                own_half[j] = (byte)(sum0 * sum1 / 128);
                opp_half[j] = (byte)(sum3 * sum4 / 128);
            }

            Span<byte> buffer = stackalloc byte[30]; //32 for simd version
            Span<byte> input0 = buffer.Slice(0);
            Span<byte> input1 = buffer.Slice(15);
            for (int i = 0; i < 15; ++i)
            {
                ReadOnlySpan<sbyte> pos_ptr = w0.AsSpan(i * Layer0.PaddedInputDimensions);
                int sum = b0[i];
                for (int j = 0; j < Layer0.InputDimensions; ++j)
                {
                    sum += pos_ptr[j] * input[j];
                }
                input0[i] = (byte)(Math.Max(0L, Math.Min(127L, (((long)sum * sum) >> (2 * NNUE_Layout.weight_scale)) / 128)));
                input1[i] = (byte)(Math.Max(0,  Math.Min(127, sum >> NNUE_Layout.weight_scale)));
            }

            //Material
            ReadOnlySpan<sbyte> mat_ptr = w0.AsSpan(15 * Layer0.PaddedInputDimensions);
            int material = b0[15];
			for (int j = 0; j < Layer0.InputDimensions; ++j) {
				material += mat_ptr[j] * input[j];
			}
            material = (material* 600 * 16) / (127 * (1 << NNUE_Layout.weight_scale)); //Scaling

			//Positional
			int positional = b2[0];
			for (int i = 0; i< 32; ++i) {
				int offset = i * Layer1.PaddedInputDimensions;
                int sum = b1[i];
				for (int j = 0; j < Layer1.InputDimensions; ++j) {
					sum += w1[offset + j] * buffer[j];
                }
                positional += w2[i] * (byte)(Math.Max(0, Math.Min(127, sum >> NNUE_Layout.weight_scale)));
			}
			return material + positional;
        }
    }

    public static class NNUE
    {
        public static Stream DefaultNet(string net_name_sha256 = "dabb1ed23026")
        {
            string filename = $"nn-{net_name_sha256}.nnue";
            if (File.Exists(filename)) return File.OpenRead(filename);

            Console.WriteLine("Downloading..." + filename);
            new WebClient().DownloadFile($"https://tests.stockfishchess.org/api/nn/{filename}", filename);
            return File.OpenRead(filename);
        }

        public static string Description { get; private set; }

        readonly static FeatureTransform Features;
        readonly static Network[] Network = new Network[NNUE_Layout.Dims];

        static NNUE()
        {
            BinaryReader reader = new BinaryReader(DefaultNet());
            
            uint vers = reader.ReadUInt32();
            uint hash = reader.ReadUInt32();
            uint size = reader.ReadUInt32();
            if (hash != 470822642) throw new NNUE_ParseException("hashcode missmatch");
            if (size > 1024) throw new NNUE_ParseException("description string size");

            Description = Encoding.ASCII.GetString(reader.ReadBytes((int)size));
            Features = new FeatureTransform(reader);

            for(int i = 0; i < NNUE_Layout.Dims; i++)
            {
                Network[i] = new Network(reader);
            }
        }

        public static int evaluate(int piece_popcount, Color SideToMove)
        {
            int bucket = (piece_popcount - 1) / 4;
            int own = SideToMove == Color.White ? 0 : 8;
            int opp = SideToMove == Color.White ? 8 : 0;

            int material = (accumulator.psqt[bucket + own] - accumulator.psqt[bucket + opp]) >> 1;
            int positional = Network[bucket].Propagate(accumulator.acc.AsSpan(), SideToMove);

            return material + positional;
        }


        struct Accumulator
        {
            public Accumulator() { }
            public short[] acc = new short[2 * NNUE_Layout.Traits];
            public int[] psqt = new int[2 * NNUE_Layout.Dims];
        };

        static Accumulator accumulator = new Accumulator();


        public static void AddPiece(Piece piece, int squareIndex)
        {
            //Todo - update accumulator, lower half = us, upper half = them
        }

        public static void RemovePiece(Piece piece, int squareIndex)
        {
            //Todo - update accumulator, lower half = us, upper half = them
        }

        public static void Update(BoardState board, ref EvalTerm nnue, ref Move move)
        {
            //Todo - resolve this to enumeration of add/removePiece calls
        }
    }
}
