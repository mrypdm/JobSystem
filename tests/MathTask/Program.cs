using MathTask;

const string SamplesDir = "samples";
const string ResultsDir = "results";

const int SamplesCount = 30;

Directory.CreateDirectory(SamplesDir);
Directory.CreateDirectory(ResultsDir);

long[] CpuContraints = [ // Cores
    128,
    192,
    288,
    256,
    384,
    576,
    864,
    1024,
];

long[] RamContraints = [ // GB
    128,
    192,
    256,
    512,
    1024,
];

IEnumerable<(int Id, Job[] Jobs)> samples = null;
if (Directory.GetFiles(SamplesDir).Length == SamplesCount)
{
    Console.WriteLine("Rading samples from disk...");
    samples = Helpers.ReadSamples(SamplesDir);
}
else
{
    Console.WriteLine("Generating samples...");
    samples = Helpers.GenerateSamples(
        samplesCount: SamplesCount,
        minJobsPerSample: 30_000,
        maxJobsPerSample: 50_000,
        meanHotspotTime: 14 * 3600,
        deltaHotspotTime: 4 * 3600,
        minJobTimeout: 60,
        maxJobTimeout: 3600,
        meanJobUsage: 0.5,
        deltaJobUsage: 0.7);

    foreach (var sample in samples)
    {
        sample.Jobs.CreateCsv($"{SamplesDir}/{sample.Id}.csv");
    }
}

var allTests = from Cpu in CpuContraints
               from Ram in RamContraints
               from Sample in samples
               select (Cpu, Ram, Sample);

Parallel.ForEach(allTests, testData =>
{
    Console.WriteLine(
        $"Running test for sample {testData.Sample.Id} with {testData.Sample.Jobs.Length} jobs "
        + $"and {testData.Cpu} CPU Cores and {testData.Ram} GB RAM");

    var estimator = new Estimator(testData.Cpu * 100L, testData.Ram * 1024 * 1024 * 1024L);
    var solver = new Solver(estimator, testData.Sample.Jobs);
    var result = solver.CreateTimeline();

    result.Events.CreateCsv($"{ResultsDir}/{testData.Sample.Id}/{testData.Cpu}_{testData.Ram}.timeline.csv");
    result.Metrics.CreateCsv($"{ResultsDir}/{testData.Sample.Id}/{testData.Cpu}_{testData.Ram}.metrics.csv");

    Console.WriteLine(
        $"Ending test for sample {testData.Sample.Id} with {testData.Sample.Jobs.Length} jobs "
        + $"and {testData.Cpu} CPU Cores and {testData.Ram} GB RAM");
});
