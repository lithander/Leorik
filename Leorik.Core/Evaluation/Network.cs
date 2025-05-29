using System.Reflection.PortableExecutable;
using System.Text;

namespace Leorik.Core
{
    public class Network
    {
        public static Network Default { get; private set; }

        public static void InitEmptyNetwork()
        {
            Default = new Network(0, 1);
        }

        public static void LoadDefaultNetwork(string filePath, int layer1Size, int materialBuckets)
        {
            Default = new Network(filePath, layer1Size, materialBuckets);
        }

        public static bool LoadDefaultNetwork()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] files = Directory.GetFiles(currentDirectory, $"*HL*-S-*MB-*.nnue");
            if (files.Length > 1)
                Console.WriteLine("Warning: Multiple network files found!");
            if (files.Length == 0)
            {
                Console.WriteLine("Error: No network file found!");
                Console.WriteLine($"Current Directory: {currentDirectory}");
                return false;
            }
            string fileName = Path.GetFileName(files[0]);
            int layer1Size = int.Parse(fileName.Substring(0, fileName.IndexOf("HL")));
            int end = fileName.IndexOf("MB");
            int start = fileName.LastIndexOf('-', end) + 1;
            int materialBuckets = int.Parse(fileName.Substring(start, end - start));
            Console.WriteLine($"Loading NNUE weights from {fileName}!");
            LoadDefaultNetwork(files[0], layer1Size, materialBuckets);
            return true;
        }

        public const int Scale = 400;
        public const short QA = 255;
        public const short QB = 64;
        public const int InputSize = 768;

        public int Layer1Size;
        public int MaterialBuckets;
        public short[] FeatureWeights; //new short[InputSize * Layer1Size];
        public short[] FeatureBiases; //new short[Layer1Size];
        public short[] OutputWeights; //new short[Layer1Size * 2 * MaterialBuckets];
        public short[] OutputBiases; //new short[MaterialBuckets]

        public Network(int layer1Size, int materialBuckets)
        {
            Layer1Size = layer1Size;
            MaterialBuckets = materialBuckets;
            FeatureWeights = new short[InputSize * Layer1Size];
            FeatureBiases = new short[Layer1Size];
            OutputWeights = new short[Layer1Size * 2 * MaterialBuckets];
            OutputBiases = new short[MaterialBuckets];
        }

        public Network(string filePath, int layer1Size, int materialBuckets) : this(Math.Max(16, layer1Size), materialBuckets)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    reader.Read(FeatureWeights, InputSize, layer1Size, Layer1Size);
                    reader.Read(FeatureBiases, 1, layer1Size, Layer1Size);
                    reader.Read(OutputWeights, 2 * MaterialBuckets, layer1Size, Layer1Size);
                    reader.Read(OutputBiases, MaterialBuckets, 1, 1);
                }
            }
        }

        public int GetMaterialBucket(int pieceCount)
        {
            int DivCeil(int a, int b) => (a + b - 1) / b;
            int divisor = DivCeil(32, MaterialBuckets);
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
