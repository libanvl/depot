using libanvl.Depot;
using libanvl.Depot.Logging;
using NuGet.Frameworks;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace libanvl;

public class Program
{
    public static Task Main(string[] args)
    {
        return BuildCommandLine()
            .UseDefaults()
            .UseExceptionHandler((ex, ic) => Console.Error.WriteLine(ConsoleColor.Red, ex.GetBaseException().Message), -1)
            .Build()
            .InvokeAsync(args);
    }

    public static CommandLineBuilder BuildCommandLine()
    {
        var depotRootArgument = new Argument<DirectoryInfo>("depot-root", "The root directory of the package depot");
        depotRootArgument.SetDefaultValueFactory(() => new DirectoryInfo(Directory.GetCurrentDirectory()));
        depotRootArgument.LegalFilePathsOnly();

        var createDepotOption = new Option<bool>("--create-depot", "Create the depot if it does not exist");
        createDepotOption.AddAlias("-c");

        var mergeDefaultSettingsOption = new Option<bool>("--merge-default-settings", "Merge default settings with depot settings");
        mergeDefaultSettingsOption.AddAlias("-m");

        var configCommand = new Command("config", "Get the configuration file path")
        {
            Handler = CommandHandler.Create(ConfigHandler)
        };

        var listCommand = new Command("list", "list all installed packages")
        {
            Handler = CommandHandler.Create(ListPackagesHandlerAsync)
        };

        var searchInstallCommand = new Command("install")
        {
            new Argument<string>("search-term")
        };
        searchInstallCommand.Handler = CommandHandler.Create(SearchInstallHandlerAsync);

        var searchDeleteCommand = new Command("delete")
        {
            new Argument<string>("package-id")
        };
        searchDeleteCommand.Handler = CommandHandler.Create(SearchDeleteHandlerAsync);

        var rootCommand = new RootCommand("libanvl Depot Tool")
        {
            depotRootArgument,
            createDepotOption,
            mergeDefaultSettingsOption,
            configCommand,
            listCommand,
            searchInstallCommand,
            searchDeleteCommand,
        };

        return new CommandLineBuilder(rootCommand);
    }

    internal static void ConfigHandler(DepotArguments depotArguments)
    {
        var context = depotArguments.GetDepotContext();
        Console.Out.WriteLine(context.ConfigFile.FullName);
    }

    internal static async Task ListPackagesHandlerAsync(DepotArguments depotArguments, CancellationToken cancellationToken)
    {
        var context = depotArguments.GetDepotContext();
        var configuration = PackageManagerConfiguration.Create(ConsoleLogger.Debug);
        var pm = context.CreatePackageManager(configuration);

        Console.Error.WriteHeader("Installed Packages".PadCenter(30), '-', ConsoleColor.White, ConsoleColor.DarkGreen);

        var packages = await pm.GetInstalledAsync(cancellationToken);

        foreach (var info in packages)
        {
            Console.Error.Write(ConsoleColor.Blue, info.Identity.Id);
            Console.Error.WriteLine(ConsoleColor.White, $"\t {info.Identity.Version.ToFullString()}");
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2016:Forward the 'CancellationToken' parameter to methods", Justification = "<Pending>")]
    internal static async Task SearchInstallHandlerAsync(DepotArguments depotArguments, string searchTerm, CancellationToken cancellationToken)
    {
        var context = depotArguments.GetDepotContext();
        var configuration = PackageManagerConfiguration.Create(ConsoleLogger.Debug, CredentialProviderOptions.EnableInteractive);
        var pm = context.CreatePackageManager(configuration);

        Console.Error.WriteHeader($"Search Results: {searchTerm}".PadCenter(30), '-', ConsoleColor.White, ConsoleColor.DarkGreen);

        await foreach (var info in pm.SearchAsync(searchTerm, includePrerelease: true, take: 50).WithCancellation(cancellationToken))
        {
            Console.WriteLine(info.Identity.Id);
            Console.WriteLine(info.Identity.Version);

            Console.Out.Write(ConsoleColor.Yellow, "Install ('c' to cancel)? >");
            cancellationToken.ThrowIfCancellationRequested();

            var consoleKey = Console.ReadKey();
            if (consoleKey.KeyChar == 'c')
            {
                return;
            }

            if (consoleKey.KeyChar == 'y')
            {
                cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine();
                await pm.InstallAsync(info.Identity, NuGetFramework.Parse("net6.0"), cancellationToken);
            }

            Console.WriteLine();
        }
    }

    internal static async Task SearchDeleteHandlerAsync(DepotArguments depotArguments, string packageId, CancellationToken cancellationToken)
    {
        var context = depotArguments.GetDepotContext();
        var configuration = PackageManagerConfiguration.Create(ConsoleLogger.Debug);
        var pm = context.CreatePackageManager(configuration);

        Console.Error.WriteHeader($"Search Results: {packageId}".PadCenter(30), '-', ConsoleColor.White, ConsoleColor.DarkMagenta);
        
        foreach (var info in await pm.FindByIdAsync(packageId, cancellationToken))
        {
            Console.WriteLine(info.Identity.Id);
            Console.WriteLine(info.Identity.Version);

            Console.Out.Write(ConsoleColor.DarkMagenta, "Delete ('c' to cancel)? >");
            cancellationToken.ThrowIfCancellationRequested();

            var consoleKey = Console.ReadKey();
            if (consoleKey.KeyChar == 'c')
            {
                return;
            }

            if (consoleKey.KeyChar == 'y')
            {
                cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine();
                if (pm.Delete(info.Identity))
                {
                    Console.Out.WriteInverted("...Deleted");
                }
                else
                {
                    Console.Out.Write(ConsoleColor.White, "Failed to delete package!", ConsoleColor.Red);
                }

            }

            Console.WriteLine();
        }
    }
}
