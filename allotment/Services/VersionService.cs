using System.Reflection;

namespace Allotment.Services;

public interface IVersionService
{
    string CommitHash { get; }
}

public class VersionService : IVersionService
{
    public string CommitHash { get; }

    public VersionService()
    {
        var infoVersion = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? string.Empty;

        // The .NET SDK embeds SourceRevisionId as "1.0.0+{commitHash}"
        var plusIndex = infoVersion.IndexOf('+');
        CommitHash = plusIndex >= 0 ? infoVersion[(plusIndex + 1)..] : "dev";
    }
}
