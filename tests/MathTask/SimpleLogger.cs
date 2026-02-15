namespace MathTask;

/// <summary>
/// Simple logger to Console and file
/// </summary>
public sealed class SimpleLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly int _sampleId;

    public SimpleLogger(string dumpsPath, int sampleId)
    {
        BasePath = $"{dumpsPath}/{sampleId}";
        Directory.CreateDirectory(BasePath);

        _sampleId = sampleId;
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
        Console.WriteLine($"[Sample {_sampleId}] {line}");
        _writer.WriteLine(line);
    }
}
