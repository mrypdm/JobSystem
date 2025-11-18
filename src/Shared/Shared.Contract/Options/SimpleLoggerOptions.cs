using Shared.Contract.Logging;

namespace Shared.Contract.Options;

/// <summary>
/// Options for <see cref="SimpleLogger"/>
/// </summary>
public class SimpleLoggerOptions
{
    /// <summary>
    /// Whether or not logging is enabled
    /// </summary>
    public Func<bool> IsEnabled { get; set; } = () => false;

    /// <summary>
    /// The output writer to use for standard output
    /// </summary>
    public Func<TextWriter> StandardOutput { get; set; } = () => Console.Out;

    /// <summary>
    /// The output writer to use for error output
    /// </summary>
    public Func<TextWriter> ErrorOutput { get; set; } = () => Console.Error;
}
