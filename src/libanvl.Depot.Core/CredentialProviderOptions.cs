using NuGet.Configuration;

namespace libanvl.Depot;

/// <summary>
/// Options for Credential Provider handling.
/// </summary>
public readonly struct CredentialProviderOptions
{
    /// <summary>
    /// An instance of <see cref="CredentialProviderOptions"/> that allows interactive prompts.
    /// </summary>
    public static CredentialProviderOptions EnableInteractive { get; } = new CredentialProviderOptions()
    {
        Enable = true,
        AllowInteractive = true,
        OverrideDefaultCredentials = true
    };

    /// <summary>
    /// An instance of <see cref="CredentialProviderOptions"/> that does not allow interactive prompts.
    /// </summary>
    public static CredentialProviderOptions EnableNonInteractive { get; } = new CredentialProviderOptions()
    {
        Enable = true,
        AllowInteractive = false,
        OverrideDefaultCredentials = true
    };

    /// <summary>
    /// Whether to enable credential providers.
    /// </summary>
    public bool Enable { get; init; }

    /// <summary>
    /// Whether credential providers should override default credentials.
    /// </summary>
    public bool OverrideDefaultCredentials { get; init; }

    /// <summary>
    /// Whether interactive prompts are allowed.
    /// </summary>
    public bool AllowInteractive { get; init; }

    /// <summary>
    /// The <see cref="IExtensionLocator"/> to use to find credential providers.
    /// </summary>
    public IExtensionLocator ExtensionLocator { get; init; } = DefaultExtensionLocator.Instance;
}
