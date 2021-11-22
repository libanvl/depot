using NuGet.Common;

namespace libanvl.Depot.Logging;

/// <summary>
/// A NuGet logger that accepts deletgates for the log functions.
/// </summary>
public class DelegateLogger : LoggerBase, ILogger
{
    private readonly Action<ILogMessage> _log;
    private readonly Func<ILogMessage, Task> _logAsync;

    /// <summary>
    /// Creates an instance of <see cref="DelegateLogger"/>
    /// </summary>
    /// <param name="log"></param>
    /// <param name="logAsync"></param>
    public DelegateLogger(Action<ILogMessage> log, Func<ILogMessage, Task> logAsync)
    {
        _log = log;
        _logAsync = logAsync;
    }

    /// <inheritdoc/>
    public override void Log(ILogMessage message) => _log(message);

    /// <inheritdoc/>
    public override Task LogAsync(ILogMessage message) => _logAsync(message);
}
