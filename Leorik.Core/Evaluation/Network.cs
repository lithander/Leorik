using System.Reflection.PortableExecutable;
using System.Text;

namespace Leorik.Core
{
    public class Network
    {
        public static Network Default { get; private set; }

        public static void InitEmptyNetwork()
        {
            Default = new Network(0, 1, 1);
        }

        public static void LoadDefaultNetwork(string filePath, int layer1Size, int inputBuckets, int outputBuckets)
        {
            Default = new Network(filePath, layer1Size, inputBuckets, outputBuckets);
        }

        public static bool LoadDefaultNetwork()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //Example 384HL-S-1MKB-8PCB-5061M-FRCv1.nnue
            string[] files = Directory.GetFiles(currentDirectory, $"*HL-S-*MKB-*PCB-*.nnue");
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

            int layer1Size = Parse(fileName, "HL");
            int inputBuckets = Parse(fileName, "MKB");
            int outputBuckets = Parse(fileName, "PCB");
            LoadDefaultNetwork(files[0], layer1Size, inputBuckets, outputBuckets);
            return true;

            int Parse(string fileName, string label)
            {
                int end = fileName.IndexOf(label);
                int start = fileName.LastIndexOf('-', end) + 1;
                return int.Parse(fileName.Substring(start, end - start));
            }
        }

        public const int Scale = 400;
        public const short QA = 255;
        public const short QB = 64;
        public const int InputSize = 768;

        public int Layer1Size;
        public int InputBuckets;
        public int OutputBuckets;
        public short[] FeatureWeights; //[InputSize * Layer1Size];
        public short[] FeatureBiases; //[Layer1Size];
        public short[] OutputWeights; //[Layer1Size * 2 * OutputBuckets];
        public short[] OutputBiases; //[OutputBuckets]

        public Network(int layer1Size, int inputBuckets, int outputBuckets)
        {
            Layer1Size = layer1Size;
            InputBuckets = inputBuckets;
            OutputBuckets = outputBuckets;
            FeatureWeights = new short[InputSize * Layer1Size];
            FeatureBiases = new short[Layer1Size];
            OutputWeights = new short[Layer1Size * 2 * OutputBuckets];
            OutputBiases = new short[OutputBuckets];
        }

        public Network(string filePath, int layer1Size, int inputBuckets, int outputBuckets) : this(Math.Max(16, layer1Size), inputBuckets, outputBuckets)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    reader.Read(FeatureWeights, InputSize, layer1Size, Layer1Size);
                    reader.Read(FeatureBiases, 1, layer1Size, Layer1Size);
                    reader.Read(OutputWeights, 2 * OutputBuckets, layer1Size, Layer1Size);
                    reader.Read(OutputBiases, OutputBuckets, 1, 1);
                }
            }
        }

        public int GetMaterialBucket(int pieceCount)
        {
            int DivCeil(int a, int b) => (a + b - 1) / b;
            int divisor = DivCeil(32, OutputBuckets);
            return (pieceCount - 2) / divisor;
        }
    }

    public static class ReaderExtension
    {
        public static void Read(this BinaryReader reader, short[] target, int blockCount, int blockSize, int stride)
        {
            for (int i = 0; i < blockCount; i++)
                for (int j = 0; j < blockSize; j++)
                    target[i * stride + j] = reader.ReadInt16();
        }
    }
}
