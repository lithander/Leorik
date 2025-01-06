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

        public static void InitEmptyNetwork(int layer1Size)
        {
            Default = new Network(layer1Size);
        }

        public static void LoadDefaultNetwork(string filePath, int layer1Size)
        {
            Default = new Network(filePath, layer1Size);
        }

        public static bool LoadDefaultNetwork()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] files = Directory.GetFiles(currentDirectory, $"{DefaultLayer1Size}HL*.nnue");
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
            LoadDefaultNetwork(files[0], DefaultLayer1Size);
            return true;
        }

        public const int DefaultLayer1Size = 256;

        public const int InputSize = 768;

        public int Layer1Size;
        public short[] FeatureWeights; //new short[InputSize * Layer1Size];
        public short[] FeatureBiases; //new short[Layer1Size];
        public short[] OutputWeights; //new short[Layer1Size * 2];
        public short OutputBias;

        public Network(int layer1Size)
        {
            Layer1Size = layer1Size;
            FeatureWeights = new short[InputSize * Layer1Size];
            FeatureBiases = new short[Layer1Size];
            OutputWeights = new short[Layer1Size * 2];
            OutputBias = 0;
        }

        public Network(string filePath, int layer1Size) : this(layer1Size)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    for (int i = 0; i < FeatureWeights.Length; i++)
                        FeatureWeights[i] = reader.ReadInt16();

                    for (int i = 0; i < FeatureBiases.Length; i++)
                        FeatureBiases[i] = reader.ReadInt16();

                    for (int i = 0; i < OutputWeights.Length; i++)
                        OutputWeights[i] = reader.ReadInt16();

                    OutputBias = reader.ReadInt16();
                }
            }
        }
    }
}
