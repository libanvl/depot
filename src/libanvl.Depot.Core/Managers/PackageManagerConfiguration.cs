using libanvl.Depot.Logging;
using NuGet.Common;
using NuGet.Configuration;

namespace libanvl.Depot;

/// <summary>
/// Configuration for a <see cref="PackageManager"/>.
/// </summary>
/// <param name="Logger"></param>
/// <param name="CredentialProviderOptions"></param>
/// <param name="AdditionalSources"></param>
public record PackageManagerConfiguration(ILogger Logger, CredentialProviderOptions CredentialProviderOptions, IEnumerable<PackageSource> AdditionalSources)
{
    /// <summary>
    /// Creates an instance of <see cref="PackageManagerConfiguration"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="credentialProviderOptions"></param>
    /// <param name="additionalSources"></param>
    /// <returns></returns>
    public static PackageManagerConfiguration Create(ILogger? logger = null, CredentialProviderOptions credentialProviderOptions = default, string[]? additionalSources = null)
    {
        return new PackageManagerConfiguration(
            CredentialProviderOptions: credentialProviderOptions,
            Logger: logger ?? ConsoleLogger.Minimal,
            AdditionalSources: additionalSources is null
                ? Enumerable.Empty<PackageSource>()
                : additionalSources.Select(p => new PackageSource(p))
        );
    }
}
