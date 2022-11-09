using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Leorik.Tablebase
{
    public class EndgameFiles
    {
        Dictionary<string, MemoryMappedViewAccessor> _wdlFiles = new Dictionary<string, MemoryMappedViewAccessor>();
        Dictionary<string, MemoryMappedViewAccessor> _dtzFiles = new Dictionary<string, MemoryMappedViewAccessor>();

        public MemoryMappedViewAccessor GetDTZ(string egClass) => _dtzFiles[egClass];
        public MemoryMappedViewAccessor GeWDL(string egClass) => _wdlFiles[egClass];

        public void AddEndgames(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(path);

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                string extension = Path.GetExtension(file);
                if (extension == null)
                    continue;

                switch (extension.ToLowerInvariant())
                {
                    case ".rtbw":
                        AddWDL(file);
                        break;
                    case ".rtbz":
                        AddDTZ(file);
                        break;
                }
            }
        }

        private void AddDTZ(string filePath)
        {
            string egClass = Path.GetFileNameWithoutExtension(filePath);
            _dtzFiles[egClass] = Open(filePath);
            Console.WriteLine($"_dtzFiles[{egClass}] = {filePath}");
        }

        private void AddWDL(string filePath)
        {
            string egClass = Path.GetFileNameWithoutExtension(filePath);
            _wdlFiles[egClass] = Open(filePath);
            Console.WriteLine($"_wdlFiles[{egClass}] = {filePath}");
        }

        public static MemoryMappedViewAccessor Open(string path)
        {
            //var file = MemoryMappedFile.CreateFromFile(path);
            var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var file = MemoryMappedFile.CreateFromFile(
                stream, //include a readonly shared stream
                null, //not mapping to a name
                0L, //use the file's actual size
                MemoryMappedFileAccess.Read,
                HandleInheritability.None,
                false //close the provided in stream when done
            );
            return file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        }
    }
}
