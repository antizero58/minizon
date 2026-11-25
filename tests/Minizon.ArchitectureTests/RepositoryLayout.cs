using System.Xml.Linq;

namespace Minizon.ArchitectureTests;

/// <summary>
/// Locates the repository root (the directory with Minizon.slnx) and the service projects under src/Services.
/// </summary>
internal static class RepositoryLayout
{
    private const string SolutionFileName = "Minizon.slnx";

    public static DirectoryInfo Root { get; } = FindRoot();

    public static DirectoryInfo ServicesDirectory => new(Path.Combine(Root.FullName, "src", "Services"));

    /// <summary>All csproj files under src/Services/&lt;Service&gt;/, recursively, excluding bin and obj.</summary>
    public static IReadOnlyList<ServiceProject> ServiceProjects()
    {
        if (!ServicesDirectory.Exists)
        {
            return [];
        }

        return ServicesDirectory
            .EnumerateDirectories()
            .SelectMany(service => service
                .EnumerateFiles("*.csproj", SearchOption.AllDirectories)
                .Where(file => !IsBuildOutput(file))
                .Select(file => new ServiceProject(service.Name, file)))
            .OrderBy(project => project.File.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsBuildOutput(FileInfo file)
    {
        var relative = Path.GetRelativePath(ServicesDirectory.FullName, file.FullName);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => segment is "bin" or "obj");
    }

    private static DirectoryInfo FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory;
            }
        }

        throw new InvalidOperationException(
            $"Repository root not found: no {SolutionFileName} in '{AppContext.BaseDirectory}' or any parent directory.");
    }
}

/// <summary>A project of a service: its service folder name and parsed csproj.</summary>
internal sealed class ServiceProject
{
    public ServiceProject(string serviceFolder, FileInfo file)
    {
        ServiceFolder = serviceFolder;
        File = file;
        Name = Path.GetFileNameWithoutExtension(file.Name);
        Document = XDocument.Load(file.FullName);
    }

    public string ServiceFolder { get; }

    public FileInfo File { get; }

    public string Name { get; }

    public XDocument Document { get; }

    public string? Sdk => Document.Root?.Attribute("Sdk")?.Value;

    /// <summary>Normalized absolute paths of direct ProjectReference items.</summary>
    public IReadOnlyList<string> ProjectReferences => Includes("ProjectReference")
        .Select(include => Path.GetFullPath(
            include.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar),
            File.Directory!.FullName))
        .ToList();

    public IReadOnlyList<string> PackageReferences => Includes("PackageReference");

    public IReadOnlyList<string> FrameworkReferences => Includes("FrameworkReference");

    public override string ToString() => Name;

    private List<string> Includes(string itemName) => Document
        .Descendants()
        .Where(element => element.Name.LocalName == itemName)
        .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
        .ToList();
}
