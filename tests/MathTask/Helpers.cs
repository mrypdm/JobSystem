using System.Collections.Concurrent;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace MathTask;

/// <summary>
/// Functions for doing something helpful
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Saves <paramref name="results"/> to CSV file at <paramref name="path"/>
    /// </summary>
    public static void CreateCsv(this IEnumerable<ExperimentResult> results, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        using var writer = new StreamWriter(path);
        writer.WriteLine("cpu;ram;metric");
        foreach (var result in results)
        {
            writer.WriteLine($"{result.CpuCores};{result.RamGb};{result.Metric}");
        }
    }

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
    /// Loads metrics events from CSV file at <paramref name="path"/>
    /// </summary>
    public static IEnumerable<Metric> ReadMetrics(string path)
    {
        return File.ReadAllLines(path).Skip(1).Select(line =>
        {
            var parts = line.Split(';');
            return new Metric(
                TimeSpan.FromMilliseconds(double.Parse(parts[0])),
                long.Parse(parts[1]),
                long.Parse(parts[2]),
                long.Parse(parts[3]),
                long.Parse(parts[4])
            );
        });
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
    /// Loads jobs events from CSV file at <paramref name="path"/>
    /// </summary>
    public static IEnumerable<KeyValuePair<JobEvent, Job>> ReadEvents(string path)
    {
        return File.ReadAllLines(path).Skip(1).Select(line =>
        {
            var parts = line.Split(';');
            return new KeyValuePair<JobEvent, Job>(
                new JobEvent(Enum.Parse<JobEventType>(parts[1]), TimeSpan.FromMilliseconds(double.Parse(parts[0]))),
                new Job(long.Parse(parts[2]), default, default, default, default)
            );
        });
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
        int meanJobsPerSample, int deltaJobsPerSample,
        int minJobTimeout, int maxJobTimeout,
        double meanJobCpuUsage, double deltaJobCpuUsage,
        double meanJobRamUsage, double deltaJobRamUsage)
    {
        var jobsCreationGenerator = new ContinuousUniform(0, 24 * 60 * 60);
        var jobsCountGenerator = new Normal(meanJobsPerSample, deltaJobsPerSample);
        var jobTimeoutGenerator = new ContinuousUniform(minJobTimeout, maxJobTimeout);
        var jobCpuGenerator = new Normal(meanJobCpuUsage, deltaJobCpuUsage);
        var jobRamGenerator = new Normal(meanJobRamUsage, deltaJobRamUsage);

        for (var sampleNumber = 0; sampleNumber < samplesCount; ++sampleNumber)
        {
            var jobsCount = (int)jobsCountGenerator.Sample();

            var jobs = jobsCreationGenerator.Samples()
                .Take(jobsCount)
                .OrderBy(c => c)
                .Select((jobCreationTime, i) => new Job(
                    Id: i,
                    CreatedAt: TimeSpan.FromSeconds(jobCreationTime),
                    Timeout: TimeSpan.FromSeconds(jobTimeoutGenerator.Sample()),
                    CpuUsage: (long)(jobCpuGenerator.Sample().Normalize(0) * 100),
                    RamUsage: (long)(jobRamGenerator.Sample().Normalize(0) * 1024 * 1024 * 1024)));

            yield return (sampleNumber, jobs.ToArray());
        }
    }

    /// <summary>
    /// Converts enumerable to sorted list
    /// </summary>
    public static SortedList<TKey, TValue> ToSortedList<TData, TKey, TValue>(
        this IEnumerable<TData> source,
        Func<TData, TKey> keySelector,
        Func<TData, TValue> valueSelector)
    {
        var sortedList = new SortedList<TKey, TValue>();
        foreach (var elem in source)
        {
            sortedList.Add(keySelector(elem), valueSelector(elem));
        }

        return sortedList;
    }

    /// <summary>
    /// Optimize <paramref name="samples"/> resources by <paramref name="metricToOptimize"/> and <paramref name="targetMetric"/>.
    /// Saves results to <paramref name="dumpsPath"/>
    /// </summary>
    public static async Task<ConcurrentDictionary<long, SolverResult>> Optimize(
        this IEnumerable<(int Id, Job[] Jobs)> samples,
        OptimizingMetric metricToOptimize,
        double targetMetric,
        string dumpsPath)
    {
        var results = new ConcurrentDictionary<long, SolverResult>();

        await Parallel.ForEachAsync(samples, async (sample, _) =>
        {
            using var logger = new SimpleLogger($"{dumpsPath}/{sample.Id}/{metricToOptimize}.log", $"[Sample {sample.Id}] ");
            var solver = new Solver(logger, sample.Jobs);

            logger.WriteLine($"Running optimization by {metricToOptimize} [target is {targetMetric}] with {sample.Jobs.Length} jobs");
            var solverResult = solver.Optimize(metricToOptimize, targetMetric);
            solverResult.Results.CreateCsv($"{logger.BasePath}/{metricToOptimize}.csv");
            logger.WriteLine($"Optimization by {metricToOptimize} is CPU={solverResult.CpuCores} and RAM={solverResult.RamGb}");

            results.TryAdd(sample.Id, solverResult);
        });

        return results;
    }

    /// <summary>
    /// Check <paramref name="metricToOptimize"/> by <paramref name="cpuCores"/> and <paramref name="ramGb"/> over all <paramref name="samples"/>
    /// Saves results to <paramref name="dumpsPath"/>
    /// </summary>
    public static async Task<ConcurrentDictionary<long, ExperimentResult>> CheckResources(
        this IEnumerable<(int Id, Job[] Jobs)> samples,
        long cpuCores, long ramGb,
        OptimizingMetric metricToOptimize,
        string dumpsPath)
    {
        var results = new ConcurrentDictionary<long, ExperimentResult>();

        await Parallel.ForEachAsync(samples, async (sample, _) =>
        {
            using var logger = new SimpleLogger($"{dumpsPath}/{sample.Id}/{metricToOptimize}.log", $"[Sample {sample.Id}] ", append: true);
            var solver = new Solver(logger, sample.Jobs);

            logger.WriteLine($"Checking CPU and RAM values for {metricToOptimize}");
            var checkResult = solver.DoExperiment(metricToOptimize, cpuCores, ramGb);

            results.TryAdd(sample.Id, checkResult);
        });

        return results;
    }

    /// <summary>
    /// Writes total results to log
    /// </summary>
    public static (long MeanBestCpuCores, long MeanBestRamGb) DumpTotalResults(
        this ConcurrentDictionary<long, SolverResult> results,
        string experimentName,
        double targetMetric,
        SimpleLogger logger)
    {
        logger.WriteLine($"{experimentName} results");
        foreach (var (sampleId, solverResult) in results)
        {
            var metric = solverResult.Results
                .Single(m => m.CpuCores == solverResult.CpuCores && m.RamGb == solverResult.RamGb)
                .Metric;
            logger.WriteLine($"\tSample {sampleId} best result is {metric} with CPU={solverResult.CpuCores} and RAM={solverResult.RamGb}");
        }

        var percBestCpu = (long)Math.Ceiling(results.Select(m => (double)m.Value.CpuCores).Percentile(90));
        var percBestRam = (long)Math.Ceiling(results.Select(m => (double)m.Value.RamGb).Percentile(90));
        logger.WriteLine($"\tBest result (90%) is CPU={percBestCpu} and RAM={percBestRam}");

        results.PlotExperimentsResults(experimentName, targetMetric, percBestCpu, percBestRam, logger.BasePath);

        return (percBestCpu, percBestRam);
    }
}
