using NuGet.Configuration;

namespace libanvl.Depot;

internal class DefaultExtensionLocator : IExtensionLocator
{
    private static readonly string ExtensionsRootPath =
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            "NuGet",
            "Commands");

    private static readonly string CredentialProvidersRootPath =
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            "NuGet",
            "CredentialProviders");

    private static DefaultExtensionLocator? _instance;

    public static DefaultExtensionLocator Instance => _instance ??= new DefaultExtensionLocator();

    public IEnumerable<string> FindExtensions() =>
        EnumerateAssemblies(
            ExtensionsRootPath,
            GetPathsFromEnviromentVariable(EnvironmentVariables.ExtensionsPath),
            "*.dll");

    public IEnumerable<string> FindCredentialProviders() =>
        EnumerateAssemblies(
            CredentialProvidersRootPath,
            GetPathsFromEnviromentVariable(EnvironmentVariables.CredentialProvidersPath),
            "CredentialProvider*.exe");

    private static IEnumerable<string> EnumerateAssemblies(string globalRootDirectory, IEnumerable<string> customPaths, string assemblyPattern)
    {
        return customPaths
            .Append(globalRootDirectory)
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, assemblyPattern, SearchOption.AllDirectories));
    }

    private static IEnumerable<string> GetPathsFromEnviromentVariable(string variable)
    {
        var paths = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(paths))
        {
            return Enumerable.Empty<string>();
        }

        paths = Environment.ExpandEnvironmentVariables(paths);
        return paths.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }

    public static class EnvironmentVariables
    {
        public static readonly string ExtensionsPath = "NUGET_EXTENSIONS_PATH";
        public static readonly string CredentialProvidersPath = "NUGET_CREDENTIALPROVIDERS_PATH";
    }
}
