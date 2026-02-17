using System.Collections.Concurrent;
using MathTask;

if (Environment.CurrentDirectory.Replace("\\", "/").Contains("bin/Debug/net"))
{
    Environment.CurrentDirectory = Path.GetFullPath($"{Environment.CurrentDirectory}/../../..");
}

const string SamplesDir = "samples";
const string ResultsDir = "results";
const int SamplesCount = 10;
const double TargetWaitTime = 0.2;
const double TargetQueueSize = 0.35;

(int Id, Job[] Jobs)[] samples = null;
if (Directory.GetFiles(SamplesDir).Length == SamplesCount)
{
    Console.WriteLine("Rading samples from disk...");
    samples = [.. Helpers.ReadSamples(SamplesDir).OrderBy(m => m.Id)];
}
else
{
    Console.WriteLine("Generating samples...");
    samples = [.. Helpers.GenerateSamples(
        samplesCount: SamplesCount,
        meanJobsPerSample: 40_000,
        deltaJobsPerSample: 5_000,
        minJobTimeout: 60,
        maxJobTimeout: 3600,
        meanJobCpuUsage: 1.5,
        deltaJobCpuUsage: 0.5,
        meanJobRamUsage: 2,
        deltaJobRamUsage: 1)];

    foreach (var sample in samples)
    {
        sample.Jobs.CreateCsv($"{SamplesDir}/{sample.Id}.csv");
    }

    samples.PlotSamples(ResultsDir);
    samples.PlotMeanValuesBySamples(ResultsDir);
}

ConcurrentDictionary<long, SolverResult> waitTimeResults = new();
ConcurrentDictionary<long, SolverResult> queueSizeResults = new();

Parallel.ForEach(samples, sample =>
{
    using var logger = new SimpleLogger(ResultsDir, sample.Id);
    var solver = new Solver(logger, sample.Jobs);

    logger.WriteLine($"Running optimization by wait time [target is {TargetWaitTime}] with {sample.Jobs.Length} jobs");
    var resourcesForWaitTime = solver.Optimize(OptimizingMetric.WaitTime, TargetWaitTime);
    resourcesForWaitTime.Results.CreateCsv($"{logger.BasePath}/wait-time-results.csv");
    logger.WriteLine($"Optimization by wait time is CPU={resourcesForWaitTime.CpuCores} and RAM={resourcesForWaitTime.RamGb}");

    logger.WriteLine($"Running optimization by queue size [target is {TargetQueueSize}] with {sample.Jobs.Length} jobs");
    var resourcesForQueueSize = solver.Optimize(OptimizingMetric.Queue, TargetQueueSize);
    resourcesForQueueSize.Results.CreateCsv($"{logger.BasePath}/queue-size-results.csv");
    logger.WriteLine($"Optimization by queue size is CPU={resourcesForQueueSize.CpuCores} and RAM={resourcesForQueueSize.RamGb}");

    var finalCpuCores = Math.Max(resourcesForWaitTime.CpuCores, resourcesForQueueSize.CpuCores);
    var finalRamGb = Math.Max(resourcesForWaitTime.RamGb, resourcesForQueueSize.RamGb);
    var finalWaitTimeResult = solver.DoExperiment(OptimizingMetric.WaitTime, finalCpuCores, finalRamGb);
    var finalQueueResult = solver.DoExperiment(OptimizingMetric.Queue, finalCpuCores, finalRamGb);

    logger.WriteLine(
        $"Optimization by all metrics is CPU={finalCpuCores} and RAM={finalRamGb} "
        + $"with results wait time {finalWaitTimeResult.Metric} and queue size {finalQueueResult.Metric}");

    waitTimeResults.TryAdd(sample.Id, resourcesForWaitTime);
    queueSizeResults.TryAdd(sample.Id, resourcesForQueueSize);
});

using var logger = new SimpleLogger(ResultsDir);

var (waitTimeMeanBestCpu, waitTimeMeanBestRam) = waitTimeResults.DumpTotalResults("Wait time", TargetWaitTime, logger);
var (queueSizeMeanBestCpu, queueSizeMeanBestRam) = queueSizeResults.DumpTotalResults("Queue size", TargetQueueSize, logger);

var bestCpu = Math.Max(waitTimeMeanBestCpu, queueSizeMeanBestCpu);
var bestRam = Math.Max(waitTimeMeanBestRam, queueSizeMeanBestRam);
logger.WriteLine($"Final optimization results is CPU={bestCpu} and RAM={bestRam}");
