namespace Allotment.Utils
{
    public interface IFileSystem
    {
        Task AppendAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default);
        void CreateFileDirectory(FileInfo fileInfo);
        bool Exists(string? path);
        Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);
    }

    public class FileSystem : IFileSystem
    {
        public async Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await File.ReadAllLinesAsync(path, cancellationToken);
        }

        public void CreateFileDirectory(FileInfo fileInfo)
        {
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
        }

        public bool Exists(string? path)
        {
            return File.Exists(path);
        }

        public async Task AppendAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default(CancellationToken))
        {
            await File.AppendAllTextAsync(path, contents, cancellationToken);
        }

    }
}
