using System.Text;

namespace Leorik.Core
{
    public class Network
    {
        // current arch: (768->768)x2->1, ClippedReLU
        // perspective
        //const int ArchId = 1;
        //using Activation = activation::ClippedReLU<255>;
        //const uint Layer1Size = 768;
        //const int Scale = 400;
        //const int Q = 255 * 64;

        public static Network Default { get; private set; }

        public static void LoadDefaultNetwork(string filePath)
        {
            Default = new Network(filePath);
        }

        public static bool LoadDefaultNetwork()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string[] files = Directory.GetFiles(currentDirectory, $"{Layer1Size}HL*.nnue");
            if (files.Length > 1)
                Console.WriteLine("Warning: Multiple network files found!");
            if (files.Length == 0)
            {
                Console.WriteLine("Error: No network file found!");
                Console.WriteLine($"Current Directory: {currentDirectory}");
                return false;
            }
            string fileName = Path.GetFileName(files[0]);
            Console.WriteLine($"Loading NNUE weights from {fileName}!");
            LoadDefaultNetwork(files[0]);
            return true;
        }


        public const int InputSize = 768;
        public const int Layer1Size = 256;

        public short[] FeatureWeights; //new short[InputSize * Layer1Size];
        public short[] FeatureBiases; //new short[Layer1Size];
        public short[] OutputWeights; //new short[Layer1Size * 2];
        public short OutputBias;

        public Network(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
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
