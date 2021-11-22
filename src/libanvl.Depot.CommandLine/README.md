[![.NET 6](https://github.com/libanvl/depot/actions/workflows/dotnet.yml/badge.svg)](https://github.com/libanvl/depot/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/libanvl/depot/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/libanvl/depot/actions/workflows/codeql-analysis.yml)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/libanvl.depot.commandline?label=libanvl.depot.commandline)](https://www.nuget.org/packages/libanvl.depot.commandline/)

# libanvl.depot.commandline

command line wrapper for libanvl.depot.core

## Requirements

[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)

## Releases

* NuGet packages are available on [NuGet.org](https://www.nuget.org/packages/libanvl.depot)
  * Embedded debug symbols
  * Source Link enabled
* NuGet packages from CI builds are available on the [libanvl GitHub feed](https://github.com/libanvl/depot/packages/)

## Install

Depot tool can be installed as a dotnet tool:

```
dotnet tool install --tool-path c:\dotnet-tools --prerelease libanvl.depot.commandline
```

Run it using `depot.exe`

## Examples

```
```

## Credential Providers

* [Azure Artifacts Credential Provider](https://github.com/microsoft/artifacts-credprovider)