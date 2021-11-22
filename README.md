[![.NET 6](https://github.com/libanvl/depot/actions/workflows/dotnet.yml/badge.svg)](https://github.com/libanvl/depot/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/libanvl/depot/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/libanvl/depot/actions/workflows/codeql-analysis.yml)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/libanvl.depot?label=libanvl.depot)](https://www.nuget.org/packages/libanvl.depot/)

# libanvl.depot

Manage private, local NuGet package caches

## Insipiration

> &ldquo;Immature poets imitate; mature poets steal&rdquo; _- [T.S.Eliot](https://nancyprager.wordpress.com/2007/05/08/good-poets-borrow-great-poets-steal/)_ 

This library _borrows_ heavily from:

* [Revisiting the NuGet v3 Libraries](https://martinbjorkstrom.com/posts/2018-09-19-revisiting-nuget-client-libraries) by [MARTIN BJÖRKSTRÖM](https://martinbjorkstrom.com/)
* [NugetManager.cs](https://gist.github.com/cpyfferoen/74092a74b165e85aed5ca1d51973b9d2) by [cpyfferoen](https://github.com/cpyfferoen)


## Requirements

[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)

## Releases

* NuGet packages are available on [NuGet.org](https://www.nuget.org/packages/libanvl.depot)
  * Embedded debug symbols
  * Source Link enabled
* NuGet packages from CI builds are available on the [libanvl GitHub feed](https://github.com/libanvl/depot/packages/)

## Features

- [X] Search packages from configured feeds
- [X] Supports Credential Providers
- [X] Supports package credentials in config files
- [X] Install packages to your private cache
- [X] Delete packages from your private cache
- [X] Each depot has a private config file
- [X] Merge depot config with NuGet default settings
- [ ] Upgrade packages in place
- [ ] Manage the depot private config file

## Examples

```csharp
```

## Credential Providers

* [Azure Artifacts Credential Provider](https://github.com/microsoft/artifacts-credprovider)