using System.Text.RegularExpressions;
using Minizon.Catalog.Domain.Products;

namespace Minizon.ArchitectureTests;

/// <summary>
/// Layer rules from ADR 0003, item 6. Changing <see cref="AllowedReferences"/> is an architectural decision (ADR required).
/// </summary>
public partial class LayerDependencyTests
{
    // Layer -> layers of the same service it may reference directly.
    private static readonly Dictionary<string, string[]> AllowedReferences = new(StringComparer.Ordinal)
    {
        ["Domain"] = [],
        ["Application"] = ["Domain"],
        ["Infrastructure"] = ["Application"],
        ["Api"] = ["Application", "Infrastructure"],
    };

    [GeneratedRegex(@"^Minizon\.(?<service>[A-Za-z0-9]+)\.(?<layer>[A-Za-z0-9]+)$")]
    private static partial Regex ProjectNamePattern();

    [Fact]
    public void ServiceProjects_AreFound()
    {
        Assert.NotEmpty(RepositoryLayout.ServiceProjects());
    }

    [Fact]
    public void ServiceProjects_FollowNamingConvention()
    {
        var violations = new List<string>();

        foreach (var project in RepositoryLayout.ServiceProjects())
        {
            var match = ProjectNamePattern().Match(project.Name);
            if (!match.Success)
            {
                violations.Add($"{project.Name}: name must be Minizon.<Service>.<Layer>.");
                continue;
            }

            var service = match.Groups["service"].Value;
            var layer = match.Groups["layer"].Value;

            if (service != project.ServiceFolder)
            {
                violations.Add($"{project.Name}: service '{service}' does not match folder '{project.ServiceFolder}'.");
            }

            if (!AllowedReferences.ContainsKey(layer))
            {
                violations.Add($"{project.Name}: unknown layer '{layer}', expected one of {string.Join(", ", AllowedReferences.Keys)}.");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ServiceProjects_ReferenceOnlyAllowedLayersOfOwnService()
    {
        var violations = new List<string>();

        foreach (var project in RepositoryLayout.ServiceProjects())
        {
            var match = ProjectNamePattern().Match(project.Name);
            if (!match.Success || !AllowedReferences.TryGetValue(match.Groups["layer"].Value, out var allowedLayers))
            {
                violations.Add($"{project.Name}: cannot determine the layer, see the naming convention test.");
                continue;
            }

            var allowed = allowedLayers
                .Select(layer => $"Minizon.{project.ServiceFolder}.{layer}")
                .ToHashSet(StringComparer.Ordinal);

            foreach (var reference in project.ProjectReferences.Where(reference => !allowed.Contains(reference)))
            {
                violations.Add($"{project.Name}: reference to '{reference}' is not allowed.");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void DomainProjects_HaveNoPackageOrFrameworkReferencesAndUsePlainSdk()
    {
        var domainProjects = RepositoryLayout.ServiceProjects()
            .Where(project => project.Name.EndsWith(".Domain", StringComparison.Ordinal))
            .ToList();
        var violations = new List<string>();

        Assert.NotEmpty(domainProjects);

        foreach (var project in domainProjects)
        {
            violations.AddRange(project.PackageReferences
                .Select(package => $"{project.Name}: PackageReference '{package}' is not allowed."));
            violations.AddRange(project.FrameworkReferences
                .Select(framework => $"{project.Name}: FrameworkReference '{framework}' is not allowed."));

            if (project.Sdk != "Microsoft.NET.Sdk")
            {
                violations.Add($"{project.Name}: Sdk must be 'Microsoft.NET.Sdk', found '{project.Sdk}'.");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void CatalogDomainAssembly_ReferencesOnlyPlatformAssemblies()
    {
        var assembly = typeof(Product).Assembly;

        var foreign = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !IsPlatformAssembly(name))
            .ToList();

        Assert.True(
            foreign.Count == 0,
            $"{assembly.GetName().Name}: references non-platform assemblies: {string.Join(", ", foreign)}.");
    }

    private static bool IsPlatformAssembly(string name) =>
        name is "System" or "netstandard" or "mscorlib" || name.StartsWith("System.", StringComparison.Ordinal);
}
