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
        samplesLength: 24 * 60 * 60,
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

var waitTimeResults = await samples.Optimize(OptimizingMetric.WaitTime, TargetWaitTime, ResultsDir);
var queueSizeResults = await samples.Optimize(OptimizingMetric.QueueSize, TargetQueueSize, ResultsDir);

using var logger = new SimpleLogger($"{ResultsDir}/common.log");

var (waitTimeMeanBestCpu, waitTimeMeanBestRam) = waitTimeResults
    .DumpTotalResults(OptimizingMetric.WaitTime.ToString(), TargetWaitTime, logger);
var (queueSizeMeanBestCpu, queueSizeMeanBestRam) = queueSizeResults
    .DumpTotalResults(OptimizingMetric.QueueSize.ToString(), TargetQueueSize, logger);

var bestCpu = Math.Max(waitTimeMeanBestCpu, queueSizeMeanBestCpu);
var bestRam = Math.Max(waitTimeMeanBestRam, queueSizeMeanBestRam);
logger.WriteLine($"\nFinal optimization results is CPU={bestCpu} and RAM={bestRam}\n");

var finalWaitTimeResults = await samples.CheckResources(bestCpu, bestRam, OptimizingMetric.WaitTime, ResultsDir);
var finalQueueSizeResults = await samples.CheckResources(bestCpu, bestRam, OptimizingMetric.QueueSize, ResultsDir);

for (var i = 0; i < samples.Length; ++i)
{
    var finalWaitTimeResult = finalWaitTimeResults[i];
    var finalQueueResult = finalQueueSizeResults[i];
    logger.WriteLine($"Final results for sample {i} are "
        + $"{OptimizingMetric.WaitTime}={finalWaitTimeResult.Metric} ({finalWaitTimeResult.Metric <= TargetWaitTime})"
        + $" and {OptimizingMetric.QueueSize}={finalQueueResult.Metric} ({finalQueueResult.Metric <= TargetQueueSize})");
}
