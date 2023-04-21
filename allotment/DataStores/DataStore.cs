using Allotment.Utils;

namespace Allotment.DataStores
{
    public abstract class DataStore
    {
        private readonly string _fileName;
        private readonly IFileSystem _fileSystem;

        public DataStore(string fileName, IFileSystem fileSystem )
        {
            _fileName = fileName;
            _fileSystem = fileSystem;
            var fi = new FileInfo(Path.Combine(BaseDir, fileName));
            fileSystem.CreateFileDirectory( fi );
        }

        public string BaseDir => $"/data/";

        public string GetFilename(DateOnly ?date = null)
        {
            if(date == null)
            {
                date = DateOnly.FromDateTime(DateTime.Now);
            }

            var replacements = new[]
            {
                ("$date", $"{date:dd-MM-yyyy}"),
            };

            string fileName = _fileName;
            foreach(var replacement in replacements)
            {
                fileName = fileName.Replace(replacement.Item1, replacement.Item2);
            }

            return Path.Combine(BaseDir, fileName);
        } 
    }
}
