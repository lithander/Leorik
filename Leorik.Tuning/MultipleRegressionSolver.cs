using Leorik.Core;

namespace Leorik.Tuning
{
    internal class MultipleRegressionSolver
    {
        struct Subset
        {
            public List<Data> Positions;
            public float[] CFeatures;
        }

        private float[] _cPhase;
        private List<Subset> _subsets = new();

        public IEnumerable<float[]> Features => _subsets.Select(subset => subset.CFeatures);
        public IEnumerable<List<Data>> Positions => _subsets.Select(subset => subset.Positions);


        public MultipleRegressionSolver(float[] cPhase)
        {
            _cPhase = (float[])cPhase.Clone();
        }

        public MultipleRegressionSolver(string fileName)
        {
            ReadFromFile(fileName);
        }

        internal void AddSubset(List<Data> data, float[] cFeatures)
        {
            _subsets.Add(new Subset { Positions = data, CFeatures = cFeatures });
        }

        internal void WriteToFile(string fileName)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                WriteArray(writer, _cPhase);
                writer.Write(_subsets.Count);
                foreach (Subset value in _subsets)
                    WriteSubset(writer, value);
            }
        }

        private static void WriteSubset(BinaryWriter writer, Subset value)
        {
            writer.Write(value.Positions.Count);
            foreach (Data data in value.Positions)
            {
                writer.Write(Notation.GetFen(data.Position));
                writer.Write(data.Result);
            }
            WriteArray(writer, value.CFeatures);
        }

        private static void WriteArray(BinaryWriter writer, float[] values)
        {
            writer.Write(values.Length);
            foreach (float value in values)
                writer.Write(value);
        }

        internal void ReadFromFile(string fileName)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                _cPhase = ReadArray(reader);
                _subsets.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    _subsets.Add(ReadSubset(reader));
            }
        }

        private static Subset ReadSubset(BinaryReader reader)
        {
            Subset result = new Subset();
            result.Positions = new List<Data>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Data data = new Data();
                data.Position = Notation.GetBoardState(reader.ReadString());
                data.Result = reader.ReadSByte();
                result.Positions.Add(data);
            }
            result.CFeatures = ReadArray(reader);
            return result;
        }

        private static float[] ReadArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            var result = new float[length];
            for (int i = 0; i < length; i++)
                result[i] = reader.ReadSingle();
            return result;
        }
    }
}
