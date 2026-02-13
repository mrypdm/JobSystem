namespace MathTask;

/// <summary>
/// Job for execution
/// </summary>
public record Job(long Id, TimeSpan CreatedAt, TimeSpan Timeout, long CpuUsage, long RamUsage)
{
    public override string ToString()
    {
        return $"{Id};{CreatedAt.TotalMilliseconds};{Timeout.TotalMilliseconds};{CpuUsage};{RamUsage}";
    }

    public static Job Parse(string str)
    {
        var parts = str.Split(';');
        return new Job(
            Id: long.Parse(parts[0]),
            CreatedAt: TimeSpan.FromMilliseconds(double.Parse(parts[1])),
            Timeout: TimeSpan.FromMilliseconds(double.Parse(parts[2])),
            CpuUsage: long.Parse(parts[3]),
            RamUsage: long.Parse(parts[4]));
    }
}

/// <summary>
/// Types of Job event
/// </summary>
public enum JobEventType
{
    /// <summary>
    /// Event of finishing Job
    /// </summary>
    Finish = 0,

    /// <summary>
    /// Event of creating Job
    /// </summary>
    Create = 1,

    /// <summary>
    /// Event of starting Job
    /// </summary>
    Start = 2,
}

/// <summary>
/// Event in Job lifetime
/// </summary>
public record JobEvent(JobEventType Type, TimeSpan Time) : IComparable<JobEvent>
{
    /// <summary>
    /// Job creation event
    /// </summary>
    public static JobEvent Create(TimeSpan time)
    {
        return new JobEvent(JobEventType.Create, time);
    }

    /// <summary>
    /// Job start event
    /// </summary>
    public static JobEvent Start(TimeSpan time)
    {
        return new JobEvent(JobEventType.Start, time);
    }

    /// <summary>
    /// Job finish event
    /// </summary>
    public static JobEvent Finish(TimeSpan time)
    {
        return new JobEvent(JobEventType.Finish, time);
    }

    /// <inheritdoc />
    public int CompareTo(JobEvent other)
    {
        var timeCompare = Time.CompareTo(other.Time);
        if (timeCompare != 0)
        {
            return timeCompare;
        }

        if (Type != other.Type)
        {
            return Type - other.Type;
        }

        return -11; // events are same time and same time; consider that `this` is earlier
    }
}
