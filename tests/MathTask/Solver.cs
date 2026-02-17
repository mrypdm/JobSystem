using MathNet.Numerics.Statistics;

namespace MathTask;

/// <summary>
/// Metric to optimize
/// </summary>
public enum OptimizingMetric
{
    WaitTime = 1,

    QueueSize = 2
}

/// <summary>
/// Result of one experiment
/// </summary>
public record ExperimentResult(long CpuCores, long RamGb, double Metric);

/// <summary>
/// Solving result for all experiments
/// </summary>
/// <param name="CpuCores">Optimized value of CPU cores</param>
/// <param name="RamGb">Optimized value of RAM GB</param>
/// <param name="Results">All experiments results</param>
public record SolverResult(long CpuCores, long RamGb, List<ExperimentResult> Results);

/// <summary>
/// Solver for Job queue
/// </summary>
public class Solver(SimpleLogger logger, Job[] jobs)
{
    /// <summary>
    /// Optimize CPU and RAM resources for given <paramref name="mectriToOptimize"/> and <see cref="jobs"/>.
    /// Firstly, optimizing CPU then RAM
    /// </summary>
    public SolverResult Optimize(OptimizingMetric mectricToOptimize, double targetMetric,
        long initialCpuCores = 2048, long initialRamGb = 2048)
    {
        var (finalCpu, cpuHistory) = SearchInternal(
            targetMetric,
            GetMetricFunction(mectricToOptimize),
            resource => new Estimator(resource, initialRamGb),
            initialCpuCores);
        var (finalRam, ramHistory) = SearchInternal(
            targetMetric,
            GetMetricFunction(mectricToOptimize),
            resource => new Estimator(finalCpu.CpuCores, resource),
            initialRamGb);

        return new SolverResult(finalCpu.CpuCores, finalRam.RamGb, [.. cpuHistory.Union(ramHistory).Distinct()]);
    }

    /// <summary>
    /// Do experiment for taraget metric <paramref name="mectricToOptimize"/>
    /// with given resources <paramref name="cpuCores"/> and <paramref name="ramGb"/>
    /// </summary>
    public ExperimentResult DoExperiment(OptimizingMetric mectricToOptimize, long cpuCores, long ramGb)
    {
        return DoExperiment(new Estimator(cpuCores, ramGb), GetMetricFunction(mectricToOptimize));
    }

    /// <summary>
    /// Searching optimized value of <paramref name="resource"/> by <paramref name="metricFunc"/>
    /// </summary>
    private (ExperimentResult, List<ExperimentResult>) SearchInternal(
        double targetMetric,
        Func<SortedList<JobEvent, Job>, List<Metric>, double> metricFunc,
        Func<long, Estimator> estimatorFactory,
        long resource)
    {
        var previousStepWasLowerThanTarget = true;
        var step = resource / 2;
        var history = new Dictionary<long, ExperimentResult>();

        logger.WriteLine($"\tSearch started");

        while (true)
        {
            var estimator = estimatorFactory(resource);
            var result = DoExperiment(estimator, metricFunc);
            history.Add(resource, result);

            if (result.Metric > targetMetric)
            {
                if (previousStepWasLowerThanTarget || history.ContainsKey(resource + step))
                {
                    step /= 2;
                }

                resource += step;
                previousStepWasLowerThanTarget = false;
            }
            else
            {
                if (!previousStepWasLowerThanTarget || history.ContainsKey(resource - step))
                {
                    step /= 2;
                }

                resource -= step;
                previousStepWasLowerThanTarget = true;
            }

            if (step <= 0)
            {
                break;
            }
        }

        logger.WriteLine($"\tSearch stopped");

        var finalResult = history.Values
            .Where(m => targetMetric - m.Metric >= 0)
            .OrderBy(m => targetMetric - m.Metric)
            .First();

        return (finalResult, history.Values.ToList());
    }

    /// <summary>
    /// Do experiment for given <paramref name="estimator"/>
    /// </summary>
    private ExperimentResult DoExperiment(Estimator estimator, Func<SortedList<JobEvent, Job>, List<Metric>, double> metricFunc)
    {
        var (timeline, metrics) = LoadFromDisk(estimator.ToString());

        if (timeline is null || metrics is null)
        {
            logger.WriteLine($"\tRunning experiment for [{estimator}]");
            (timeline, metrics) = BuildTimeline(estimator);
            DumpToDisk(estimator.ToString(), timeline, metrics);
        }
        else
        {
            logger.WriteLine($"\tExperiment results for [{estimator}] found on disk");
        }

        var metric = metricFunc(timeline, metrics);
        logger.WriteLine($"\tMetric result for [{estimator}] is {metric}");

        return new ExperimentResult(estimator.CpuCores, estimator.RamGb, metric);
    }

    /// <summary>
    /// Save experiment results to disk
    /// </summary>
    private void DumpToDisk(string experimentName, SortedList<JobEvent, Job> timeline, List<Metric> metrics)
    {
        timeline.CreateCsv(GetTimelineFile(experimentName));
        metrics.CreateCsv(GetMetricsFile(experimentName));
    }

    /// <summary>
    /// Load experiment results from disk
    /// </summary>
    private (SortedList<JobEvent, Job>, List<Metric>) LoadFromDisk(string experimentName)
    {
        var timelineFile = GetTimelineFile(experimentName);
        var metricsFile = GetMetricsFile(experimentName);

        if (!File.Exists(timelineFile) || !File.Exists(metricsFile))
        {
            return (null, null);
        }

        var events = Helpers.ReadEvents(timelineFile).ToSortedList(m => m.Key, m => m.Value);
        var metrics = Helpers.ReadMetrics(metricsFile).ToList();
        return (events, metrics);
    }

    /// <summary>
    /// Building timeline and metrics using given <paramref name="estimator"/>
    /// </summary>
    private (SortedList<JobEvent, Job> Events, List<Metric> Metrics) BuildTimeline(Estimator estimator)
    {
        var jobEvents = jobs.ToSortedList(m => JobEvent.Create(m.CreatedAt), m => m);
        var waitForStart = new Queue<Job>();
        var metrics = new List<Metric>();

        for (var index = 0; index != jobEvents.Count; ++index)
        {
            var (jobEvent, currentJob) = jobEvents.ElementAt(index);
            var time = jobEvent.Time;

            if (jobEvent.Type == JobEventType.Finish)
            {
                estimator.FinishJob(currentJob);
            }

            if (jobEvent.Type == JobEventType.Create)
            {
                estimator.CreateJob();
                waitForStart.Enqueue(currentJob);
            }

            // We do not prioritize tasks based on resource consumption because the current implementation of
            // task distribution is a simple message queue using Kafka
            if (waitForStart.TryPeek(out var jobToRun) && estimator.CanRunJob(jobToRun))
            {
                _ = waitForStart.Dequeue();
                jobEvents.Add(JobEvent.Start(time), jobToRun);
                jobEvents.Add(JobEvent.Finish(time + jobToRun.Timeout), jobToRun);
                estimator.StartJob(jobToRun);
            }

            metrics.Add(estimator.GetStat(time));
        }

        return (jobEvents, metrics);
    }

    /// <summary>
    /// Get metric function for optimization based on <paramref name="mectricToOptimize"/>
    /// </summary>
    private Func<SortedList<JobEvent, Job>, List<Metric>, double> GetMetricFunction(OptimizingMetric mectricToOptimize)
    {
        return mectricToOptimize == OptimizingMetric.WaitTime
            ? (timeline, _) => WaitTimePercentile(timeline)
            : (_, metrics) => QueuePercentile(metrics);
    }

    /// <summary>
    /// Metric of queue occupation percentile
    /// </summary>
    private static double QueuePercentile(List<Metric> metrics)
    {
        return metrics.Select(m => 1 - (double)m.RunningJobs / m.TotalJobs).Percentile(90);
    }

    /// <summary>
    /// Metric of waiting time percinitile
    /// </summary>
    private static double WaitTimePercentile(SortedList<JobEvent, Job> timeline)
    {
        return timeline
            .Select(m => new
            {
                Time = m.Key.Time,
                Type = m.Key.Type,
                JobId = m.Value.Id,
            })
            .GroupBy(m => m.JobId)
            .Select(g =>
            {
                var created = g.Single(m => m.Type == JobEventType.Create).Time;
                var started = g.Single(m => m.Type == JobEventType.Start).Time;
                var finished = g.Single(m => m.Type == JobEventType.Finish).Time;
                return (started - created).TotalMilliseconds / (finished - started).TotalMilliseconds;
            })
            .Percentile(90);
    }

    /// <summary>
    /// Get filepath to timeline results
    /// </summary>
    private string GetTimelineFile(string experimentName)
    {
        return $"{logger.BasePath}/{experimentName}.timeline.csv";
    }

    /// <summary>
    /// Get filepath to metrics results
    /// </summary>
    private string GetMetricsFile(string experimentName)
    {
        return $"{logger.BasePath}/{experimentName}.metrics.csv";
    }
}
