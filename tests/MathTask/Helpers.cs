using MathNet.Numerics.Distributions;

namespace MathTask;

public static class Helpers
{
    /// <summary>
    /// Saves <paramref name="metrics"/> to CSV file at <paramref name="path"/>
    /// </summary>
    public static void CreateCsv(this IEnumerable<Metric> metrics, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        using var writer = new StreamWriter(path);
        writer.WriteLine("time;cpu;ram;run;total");
        foreach (var metric in metrics)
        {
            writer.WriteLine($"{metric.Time.TotalMilliseconds};{metric.CpuUsage};{metric.RamUsage};{metric.RunningJobs};{metric.TotalJobs}");
        }
    }

    /// <summary>
    /// Saves <paramref name="jobEvents"/> to CSV file at <paramref name="path"/>
    /// </summary>
    public static void CreateCsv(this IEnumerable<KeyValuePair<JobEvent, Job>> jobEvents, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        using var writer = new StreamWriter(path);
        writer.WriteLine("time;type;job_id");
        foreach (var (jobEvent, job) in jobEvents)
        {
            writer.WriteLine($"{jobEvent.Time.TotalMilliseconds};{jobEvent.Type};{job.Id}");
        }
    }

    /// <summary>
    /// Saves <paramref name="jobs"/> to CSV file at <paramref name="path"/>
    /// </summary>
    public static void CreateCsv(this IEnumerable<Job> jobs, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        using var writer = new StreamWriter(path);
        writer.WriteLine("id;created_at;timeout;cpu;ram");
        foreach (var job in jobs)
        {
            writer.WriteLine(job.ToString());
        }
    }

    /// <summary>
    /// Normalzie <paramref name="value"/>
    /// </summary>
    /// <returns>
    /// <paramref name="min"/> if <paramref name="value"/> is less than <paramref name="min"/>,
    /// <paramref name="max"/> if <paramref name="value"/> is greater than <paramref name="max"/>,
    /// <paramref name="value"/> otherwise <br/>
    /// </returns>
    public static double Normalize(this double value,
        double min = double.NegativeInfinity, double max = double.PositiveInfinity)
    {
        return value < min
            ? min
            : value > max
                ? max
                : value;
    }

    /// <summary>
    /// Reads samples from CSV files in directory at <paramref name="path"/>
    /// </summary>
    public static IEnumerable<(int Id, Job[] Jobs)> ReadSamples(string path)
    {
        foreach (var file in Directory.GetFiles(path, "*.csv"))
        {
            var id = int.Parse(Path.GetFileNameWithoutExtension(file));
            yield return (id, File.ReadAllLines(file).Skip(1).Select(Job.Parse).ToArray());
        }
    }

    /// <summary>
    /// Generate samples
    /// </summary>
    public static IEnumerable<(int Id, Job[] Jobs)> GenerateSamples(
        int samplesCount,
        int minJobsPerSample, int maxJobsPerSample,
        int meanHotspotTime, int deltaHotspotTime,
        int minJobTimeout, int maxJobTimeout,
        double meanJobUsage, double deltaJobUsage)
    {
        var jobsCreationGenerator = new Normal(meanHotspotTime, deltaHotspotTime);
        var jobsCountGenerator = new ContinuousUniform(minJobsPerSample, maxJobsPerSample);
        var jobTimeoutGenerator = new ContinuousUniform(minJobTimeout, maxJobTimeout);
        var jobUsageGenerator = new Normal(meanJobUsage, deltaJobUsage);

        while (samplesCount-- != 0)
        {
            var jobsCount = (int)jobsCountGenerator.Sample();

            var jobs = jobsCreationGenerator.Samples()
                .Take(jobsCount)
                .OrderBy(c => c)
                .Select((c, i) => new Job(
                    Id: i,
                    CreatedAt: TimeSpan.FromSeconds(Normalize(c, 0, 23 * 3600 - 1)),
                    Timeout: TimeSpan.FromSeconds(jobTimeoutGenerator.Sample()),
                    CpuUsage: (long)(jobUsageGenerator.Sample().Normalize(0.1) * 100),
                    RamUsage: (long)(jobUsageGenerator.Sample().Normalize(0.1) * 1024 * 1024 * 1024)));

            yield return (samplesCount, jobs.ToArray());
        }
    }
}
