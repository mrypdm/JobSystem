namespace MathTask;

/// <summary>
/// Simple logger to Console and file
/// </summary>
public sealed class SimpleLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly string _header;

    public SimpleLogger(string dumpsPath)
    {
        _header = string.Empty;
        BasePath = dumpsPath;
        Directory.CreateDirectory(BasePath);

        _writer = new($"{BasePath}/log.txt");
    }

    public SimpleLogger(string dumpsPath, int sampleId)
    {
        _header = $"[Sample {sampleId}] ";
        BasePath = $"{dumpsPath}/{sampleId}";

        Directory.CreateDirectory(BasePath);
        _writer = new($"{BasePath}/log.txt");
    }

    /// <summary>
    /// Base directory for files
    /// </summary>
    public string BasePath { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Dispose();
    }

    /// <summary>
    /// Writes line to <see cref="Console"/> and to file
    /// </summary>
    /// <param name="line"></param>
    public void WriteLine(string line)
    {
        Console.WriteLine($"{_header}{line}");
        _writer.WriteLine(line);
    }
}
