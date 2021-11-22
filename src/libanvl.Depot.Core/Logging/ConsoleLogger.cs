using NuGet.Common;

namespace libanvl.Depot.Logging;

/// <summary>
/// Logs NuGet messages to the Console.
/// </summary>
public class ConsoleLogger : DelegateLogger
{
    private static ConsoleLogger? _debug;
    private static ConsoleLogger? _minimal;

    /// <summary>
    /// Creates an instance of <see cref="ConsoleLogger"/>.
    /// </summary>
    public ConsoleLogger()
        : base(
            m => Console.Error.WriteLine(FormatMessage(m)),
            m => Console.Error.WriteLineAsync(FormatMessage(m)))
    {
    }

    /// <summary>
    /// An instance of <see cref="ConsoleLogger"/> with level <see cref="LogLevel.Debug"/>.
    /// </summary>
    public static ILogger Debug { get; } = _debug ??= new ConsoleLogger { VerbosityLevel = LogLevel.Debug };

    /// <summary>
    /// An instance of <see cref="ConsoleLogger"/> with level <see cref="LogLevel.Minimal"/>.
    /// </summary>
    public static ILogger Minimal { get; } = _minimal ??= new ConsoleLogger { VerbosityLevel = LogLevel.Minimal };

    private static string FormatMessage(ILogMessage m) => $"[{m.Level}{(m.Level == LogLevel.Warning ? $"({m.WarningLevel})" : "")}] {m.FormatWithCode()}";
}
