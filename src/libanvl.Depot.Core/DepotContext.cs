using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;

namespace libanvl.Depot;

/// <summary>
/// Represents a private, local package cache.
/// </summary>
/// <param name="DepotRoot"></param>
/// <param name="ConfigFile"></param>
/// <param name="MergedSettings"></param>
public record DepotContext(DirectoryInfo DepotRoot, FileInfo ConfigFile, ISettings MergedSettings)
{
    /// <summary>
    /// Creates a <see cref="DepotContext"/>.
    /// </summary>
    /// <param name="depotRootPath"></param>
    /// <param name="settingsRootPath"></param>
    /// <param name="configFileName"></param>
    /// <param name="createDepotIfNotExist"></param>
    /// <param name="mergeDefaultSettings"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static DepotContext Create(string depotRootPath, string settingsRootPath, string configFileName, bool createDepotIfNotExist, bool mergeDefaultSettings)
    {
        if (Path.EndsInDirectorySeparator(configFileName))
        {
            throw new ArgumentException("Config filename must not be a directory.");
        }

        depotRootPath = Path.GetFullPath(depotRootPath);
        settingsRootPath = Path.GetFullPath(settingsRootPath);

        DirectoryInfo root = new DirectoryInfo(depotRootPath);

        if (!root.Exists)
        {
            if (!createDepotIfNotExist)
            {
                throw new DirectoryNotFoundException($"Depot root does not exist, and depot root creation was not requested.");
            }

            root.Create();
        }

        var configFile = new FileInfo(Path.Combine(settingsRootPath, configFileName));
        if (!configFile.Exists)
        {
            if (!createDepotIfNotExist)
            {
                throw new FileNotFoundException($"Depot settings file does not exist, and depot root creation was not requested.");
            }

            new Settings(settingsRootPath, configFileName).SaveToDisk();
        }

        ISettings mergedSettings = mergeDefaultSettings
            ? Settings.LoadDefaultSettings(settingsRootPath, configFileName, new XPlatMachineWideSetting())
            : Settings.LoadSpecificSettings(settingsRootPath, configFileName);

        return new DepotContext(
            DepotRoot: root,
            ConfigFile: configFile,
            MergedSettings: mergedSettings);
    }

    /// <summary>
    /// Creates a <see cref="DepotContext"/>.
    /// </summary>
    /// <param name="settingsRootPath"></param>
    /// <param name="configFileName"></param>
    /// <param name="mergeDefaultSettings"></param>
    /// <returns></returns>
    public static DepotContext Create(string settingsRootPath, string configFileName, bool mergeDefaultSettings = false) =>
        Create(Directory.GetCurrentDirectory(), settingsRootPath, configFileName, createDepotIfNotExist: false, mergeDefaultSettings);

    /// <summary>
    /// Creates a <see cref="DepotContext"/>.
    /// </summary>
    /// <param name="depotRootPath"></param>
    /// <param name="createDepotIfNotExist"></param>
    /// <param name="mergeDefaultSettings"></param>
    /// <returns></returns>
    public static DepotContext Create(string depotRootPath, bool createDepotIfNotExist = false, bool mergeDefaultSettings = false) =>
        Create(depotRootPath, depotRootPath, "depot.config", createDepotIfNotExist, mergeDefaultSettings);

    /// <summary>
    /// Creates a <see cref="DepotContext"/>.
    /// </summary>
    /// <param name="mergeDefaultSettings"></param>
    /// <returns></returns>
    public static DepotContext Create(bool mergeDefaultSettings = false) =>
        Create(Directory.GetCurrentDirectory(), createDepotIfNotExist: false, mergeDefaultSettings);
}

/// <summary>
/// Extensions for <see cref="DepotContext"/>.
/// </summary>
public static class DepotContextExtensions
{
    /// <summary>
    /// Creates a new <see cref="PackageManager"/> for the <paramref name="context"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="configuration"></param>
    public static PackageManager CreatePackageManager(this DepotContext context, PackageManagerConfiguration configuration) =>
        new(context, configuration);

    /// <summary>
    /// Gets a local <see cref="SourceRepository"/> for the <paramref name="context"/>.
    /// </summary>
    /// <param name="context"></param>
    public static SourceRepository GetDepotSourceRepository(this DepotContext context) =>
        new(new PackageSource(context.DepotRoot.FullName), Repository.Provider.GetCoreV3());

    internal static PackagePathResolver GetDepotPackagePathResolver(this DepotContext context) =>
        new(context.DepotRoot.FullName);

    internal static Task<T> GetDepotResourceAsync<T>(this DepotContext context, CancellationToken cancellationToken) where T : class, INuGetResource =>
        GetDepotSourceRepository(context).GetResourceAsync<T>(cancellationToken);

    internal static IPackageSourceProvider GetPackageSourceProvider(this ISettings settings, IEnumerable<PackageSource> additionalSources) =>
        new PackageSourceProvider(settings, additionalSources);

    internal static ISourceRepositoryProvider GetSourceRepositoryProvider(this ISettings settings, IEnumerable<PackageSource> additionalSources) =>
        new SourceRepositoryProvider(settings.GetPackageSourceProvider(additionalSources), Repository.Provider.GetCoreV3());
}
