using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.Resolver;
using NuGet.Versioning;
using System.Runtime.CompilerServices;

namespace libanvl.Depot;

/// <summary>
/// Manages packages for a <see cref="DepotContext"/>.
/// </summary>
public class PackageManager
{
    private readonly DepotContext _depotContext;
    private readonly CredentialProviderOptions _credentialProviderOptions;
    private readonly ILogger _logger;
    private readonly ISourceRepositoryProvider _sourceRepositoryProvider;
    private readonly PackagePathResolver _depotPathResolver;

    /// <summary>
    /// Creates an instance of <see cref="PackageManager"/>.
    /// </summary>
    /// <param name="depotContext"></param>
    /// <param name="logger"></param>
    /// <param name="credentialProviderOptions"></param>
    /// <param name="additionalSources"></param>
    public PackageManager(
        DepotContext depotContext,
        ILogger logger,
        CredentialProviderOptions credentialProviderOptions,
        IEnumerable<PackageSource> additionalSources)
    {
        _depotContext = depotContext;
        _credentialProviderOptions = credentialProviderOptions;
        _logger = logger;
        _sourceRepositoryProvider = depotContext.MergedSettings.GetSourceRepositoryProvider(additionalSources);
        _depotPathResolver = depotContext.GetDepotPackagePathResolver();

        if (_credentialProviderOptions.Enable)
        {
            PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders = credentialProviderOptions.OverrideDefaultCredentials;

            HttpHandlerResourceV3.CredentialService = new Lazy<ICredentialService>(() =>
                new CredentialService(
                    new AsyncLazy<IEnumerable<ICredentialProvider>>(GetCredentialProvidersAsync),
                    nonInteractive: !_credentialProviderOptions.AllowInteractive,
                    handlesDefaultCredentials: PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders));
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="PackageManager"/>.
    /// </summary>
    /// <param name="depotContext"></param>
    /// <param name="configuration"></param>
    public PackageManager(DepotContext depotContext, PackageManagerConfiguration configuration)
        : this(
              depotContext,
              configuration.Logger,
              configuration.CredentialProviderOptions,
              configuration.AdditionalSources)
    {
    }

    /// <summary>
    /// Gets the installed packages.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task<IEnumerable<LocalPackageInfo>> GetInstalledAsync(CancellationToken cancellationToken)
    {
        var findResource = await _depotContext.GetDepotResourceAsync<FindLocalPackagesResource>(cancellationToken);
        return findResource.GetPackages(_logger, cancellationToken);
    }

    /// <summary>
    /// Finds installed packaged by package id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    public async Task<IEnumerable<LocalPackageInfo>> FindByIdAsync(string id, CancellationToken cancellationToken)
    {
        var findResource = await _depotContext.GetDepotResourceAsync<FindLocalPackagesResource>(cancellationToken);
        return findResource.FindPackagesById(id, _logger, cancellationToken);
    }

    /// <summary>
    /// Deletes an installed package.
    /// </summary>
    /// <param name="packageIdentity"></param>
    /// <returns><c>true</c> if the package was deleted, <c>false</c> otherwise.</returns>
    public bool Delete(PackageIdentity packageIdentity)
    {
        var packageDirectory = _depotPathResolver.GetInstalledPath(packageIdentity);

        _logger.LogVerbose($"Resolved package directory: {packageDirectory}");

        if (!Directory.Exists(packageDirectory))
        {
            _logger.LogWarning("Package directory not found");
            return false;
        }

        return DeleteDirectory(packageDirectory);
    }

    /// <exception cref="FatalProtocolException" />
    public ConfiguredCancelableAsyncEnumerable<IPackageSearchMetadata> SearchAsync(
        string searchTerm,
        bool includePrerelease = false,
        bool includeDelisted = false,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var searchFilter = new SearchFilter(includePrerelease)
        {
            IncludeDelisted = includeDelisted
        };

        return SearchAsync(searchTerm, searchFilter, skip, take).WithCancellation(cancellationToken);
    }

    /// <exception cref="FatalProtocolException" />
    public async IAsyncEnumerable<IPackageSearchMetadata> SearchAsync(
        string searchTerm,
        SearchFilter searchFilter,
        int skip = 0,
        int take = 30,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var feed in _sourceRepositoryProvider.GetRepositories())
        {
            var searchResource = await feed.GetResourceAsync<PackageSearchResource>(cancellationToken);
            foreach (var psm in await searchResource.SearchAsync(searchTerm, searchFilter, skip, take, _logger, cancellationToken))
            {
                yield return psm;
            }
        }
    }

    /// <exception cref="FatalProtocolException" />
    public Task<IReadOnlyList<PackageReaderBase>> InstallAsync(string packageId, string packageVersion, string targetFramework, CancellationToken cancellationToken) =>
        InstallAsync(new PackageIdentity(packageId, NuGetVersion.Parse(packageVersion)), NuGetFramework.ParseFolder(targetFramework), cancellationToken);

    /// <exception cref="FatalProtocolException" />
    public async Task<IReadOnlyList<PackageReaderBase>> InstallAsync(PackageIdentity packageIdentity, NuGetFramework targetFramework, CancellationToken cancellationToken)
    {
        using var cacheContext = new SourceCacheContext();

        var repositories = _sourceRepositoryProvider.GetRepositories();
        var availablePackages = await GetPackageDependencyInfosAsync(packageIdentity, targetFramework, cacheContext, repositories, cancellationToken);

        var resolverContext = new PackageResolverContext(
            DependencyBehavior.Lowest,
            new[] { packageIdentity.Id },
            Enumerable.Empty<string>(),
            Enumerable.Empty<PackageReference>(),
            Enumerable.Empty<PackageIdentity>(),
            availablePackages,
            repositories.Select(s => s.PackageSource),
            _logger);

        var packageResolver = new PackageResolver();
        var packageExtractionContext = new PackageExtractionContext(
            PackageSaveMode.Defaultv3,
            XmlDocFileSaveMode.None,
            ClientPolicyContext.GetClientPolicy(_depotContext.MergedSettings, _logger),
            _logger);

        var packagesToExtract = packageResolver.Resolve(resolverContext, cancellationToken)
            .Select(pi => availablePackages
                .Single(sdpi => PackageIdentityComparer.Default.Equals(pi, sdpi)));

        var packageDownloadContext = new PackageDownloadContext(cacheContext);

        var packageReaders = new List<PackageReaderBase>();

        foreach (var packageToExtract in packagesToExtract)
        {
            var installedPath = _depotPathResolver.GetInstalledPath(packageToExtract);
            if (installedPath is null)
            {
                var downloadResource = await packageToExtract.Source.GetResourceAsync<DownloadResource>(cancellationToken);
                var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                    packageToExtract,
                    packageDownloadContext,
                    SettingsUtility.GetGlobalPackagesFolder(_depotContext.MergedSettings),
                    _logger,
                    cancellationToken);

                await PackageExtractor.ExtractPackageAsync(
                    downloadResult.PackageSource,
                    downloadResult.PackageStream,
                    _depotPathResolver,
                    packageExtractionContext,
                    cancellationToken);

                packageReaders.Add(downloadResult.PackageReader);
            }
            else
            {
                packageReaders.Add(new PackageFolderReader(installedPath));
            }

        }

        return packageReaders;
    }

    private async Task<IReadOnlySet<SourcePackageDependencyInfo>> GetPackageDependencyInfosAsync(
        PackageIdentity packageIdentity,
        NuGetFramework framework,
        SourceCacheContext cacheContext,
        IEnumerable<SourceRepository> repositories,
        CancellationToken cancellationToken)
    {
        var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
        await ResolveImpl(packageIdentity, availablePackages);

        async Task ResolveImpl(PackageIdentity _packageIdentity, ISet<SourcePackageDependencyInfo> _availablePackages)
        {
            if (_availablePackages.Contains(_packageIdentity))
            {
                return;
            }

            foreach (var repo in repositories)
            {
                var diResource = await repo.GetResourceAsync<DependencyInfoResource>();
                var di = await diResource.ResolvePackage(
                    _packageIdentity,
                    framework,
                    cacheContext,
                    _logger,
                    cancellationToken);

                if (di is null)
                {
                    continue;
                }

                _availablePackages.Add(di);

                foreach (var dep in di.Dependencies)
                {
                    await ResolveImpl(
                        new PackageIdentity(dep.Id, dep.VersionRange.MinVersion),
                        _availablePackages);
                }
            }
        }

        return availablePackages;

    }

    private async Task<IEnumerable<ICredentialProvider>> GetCredentialProvidersAsync()
    {
        var providers = new List<ICredentialProvider>();

        var securePluginProviders = await new SecurePluginCredentialProviderBuilder(
                PluginManager.Instance,
                _credentialProviderOptions.AllowInteractive,
                _logger)
            .BuildAllAsync();

        providers.AddRange(securePluginProviders);

        var pluginProviders = new PluginCredentialProviderBuilder(
                _credentialProviderOptions.ExtensionLocator,
                _depotContext.MergedSettings,
                _logger)
            .BuildAll("Detailed");

        providers.AddRange(pluginProviders);

        if (pluginProviders.Any() || securePluginProviders.Any())
        {
            if (_credentialProviderOptions.OverrideDefaultCredentials)
            {
                providers.Add(new DefaultNetworkCredentialsCredentialProvider());
            }
        }

        return providers;
    }

    private bool DeleteDirectory(string packageDirectory)
    {
        var directory = new DirectoryInfo(packageDirectory) { Attributes = FileAttributes.Normal };

        foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            info.Attributes = FileAttributes.Normal;
        }

        try
        {
            directory.Delete(recursive: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }
}
