using ScottPlot;

namespace MathTask;

/// <summary>
/// Methods for plotting
/// </summary>
public static class Graphs
{
    public const int Width = 1280;

    public const int Height = 960;

    /// <summary>
    /// Plot samples: jobs timeout and jobs resources
    /// </summary>
    public static void PlotSamples(this IEnumerable<(int Id, Job[] Jobs)> samples, string basePath)
    {
        Directory.CreateDirectory(basePath);

        foreach (var (id, jobs) in samples)
        {
            var mulitplot = new Multiplot();
            mulitplot.AddPlots(3);

            var cpuHistogram = jobs
                .GroupBy(m => m.CpuUsage / 100.0)
                .Select(m => (Cpu: m.Key, Count: m.LongCount()))
                .OrderBy(m => m.Cpu)
                .ToArray();

            var ramHistogram = jobs
                .GroupBy(m => Math.Round(m.RamUsage / 1073741824.0, 2))
                .Select(m => (Ram: m.Key, Count: m.LongCount()))
                .OrderBy(m => m.Ram)
                .ToArray();

            Plot(mulitplot.Subplots.GetPlot(0), "Timeout, minutes",
                (
                    null,
                    jobs.Select(j => j.Id).ToArray(),
                    jobs.Select(j => j.Timeout.TotalMinutes).ToArray()
                ));

            Plot(mulitplot.Subplots.GetPlot(1), "Job resources",
                (
                    "CPU, Cores",
                    jobs.Select(j => j.Id).ToArray(),
                    jobs.Select(j => j.CpuUsage / 100.0).ToArray()
                ),
                (
                    "RAM, GB",
                    jobs.Select(j => j.Id).ToArray(),
                    jobs.Select(j => Math.Round(j.RamUsage / 1073741824.0)).ToArray()
                ));

            Plot(mulitplot.Subplots.GetPlot(2), "Resources distribution",
                (
                    "CPU Distribution",
                    cpuHistogram.Select(j => j.Cpu).ToArray(),
                    cpuHistogram.Select(j => j.Count).ToArray()
                ),
                (
                    "RAM Distribution",
                    ramHistogram.Select(j => j.Ram).ToArray(),
                    ramHistogram.Select(j => j.Count).ToArray()
                ));

            mulitplot.SavePng($"{basePath}/{id}/Jobs.png", Width, Height);
        }
    }

    /// <summary>
    /// Plot mean values by samples: jobs timeout and resources
    /// </summary>
    /// <param name="samples"></param>
    /// <param name="basePath"></param>
    public static void PlotMeanValuesBySamples(this IEnumerable<(int Id, Job[] Jobs)> samples, string basePath)
    {
        Directory.CreateDirectory(basePath);

        var multiplot = new Multiplot();
        multiplot.AddPlots(2);

        var meanSamples = samples.Select(m =>
        (
            m.Id,
            MeanRamUsage: m.Jobs.Average(m => m.RamUsage),
            MeanCpuUsage: m.Jobs.Average(m => m.CpuUsage),
            MeanTimeout: m.Jobs.Average(m => m.Timeout.TotalMinutes)
        )).ToArray();

        Plot(multiplot.Subplots.GetPlot(0), "Mean jobs resources",
            (
                "Timeout, minutes",
                meanSamples.Select(m => m.Id).ToArray(),
                meanSamples.Select(m => m.MeanTimeout).ToArray()
            ),
            (
                "RAM, GB",
                meanSamples.Select(m => m.Id).ToArray(),
                meanSamples.Select(m => m.MeanRamUsage / 1073741824.0).ToArray()
            ),
            (
                "CPU, Cores",
                meanSamples.Select(m => m.Id).ToArray(),
                meanSamples.Select(m => m.MeanCpuUsage / 100.0).ToArray()
            )
        );

        Plot(multiplot.Subplots.GetPlot(1), "Jobs count",
            (
                null,
                samples.Select(m => m.Id).ToArray(),
                samples.Select(m => m.Jobs.Length).ToArray()
            )
        );
        multiplot.SavePng($"{basePath}/Mean samples values.png", Width, Height);
    }

    /// <summary>
    /// Plot results of experiments
    /// </summary>
    public static void PlotExperimentsResults(this IEnumerable<KeyValuePair<long, SolverResult>> results,
        string experimentName, double targetValue, double bestCpu, double bestRam, string basePath)
    {
        Directory.CreateDirectory(basePath);

        var multiplot = new Multiplot();
        multiplot.AddPlots(2);

        var byCpu = results.Select(m => (
            Id: m.Key,
            Values: m.Value.Results
                .GroupBy(m => m.RamGb)
                .Single(m => m.Count() != 1)
                .Select(m => (m.CpuCores, m.Metric))
                .OrderBy(m => m.CpuCores)
                .ToArray()
        )).ToArray();
        Plot(multiplot.Subplots.GetPlot(0), $"{experimentName} by CPU, Cores",
            byCpu
                .Select(m => (
                    $"Sample {m.Id}",
                    m.Values.Select(v => v.CpuCores).ToArray(),
                    m.Values.Select(v => v.Metric).ToArray()
                ))
                .ToArray());

        var byRam = results.Select(m => (
            Id: m.Key,
            Values: m.Value.Results
                .GroupBy(m => m.CpuCores)
                .Single(m => m.Count() != 1)
                .Select(m => (m.RamGb, m.Metric))
                .OrderBy(m => m.RamGb)
                .ToArray()
        )).ToArray();
        Plot(multiplot.Subplots.GetPlot(1), $"{experimentName} by RAM, GB",
            byRam
                .Select(m => (
                    $"Sample {m.Id}",
                    m.Values.Select(v => v.RamGb).ToArray(),
                    m.Values.Select(v => v.Metric).ToArray()
                ))
                .ToArray());

        for (var i = 0; i < 2; ++i)
        {
            var targetLine = multiplot.Subplots.GetPlot(i).Add.HorizontalLine(targetValue);
            targetLine.LinePattern = LinePattern.Dashed;
            targetLine.Color = Colors.Black;
        }

        foreach (var sample in results)
        {
            var cpuResultLine = multiplot.Subplots.GetPlot(0).Add.VerticalLine(sample.Value.CpuCores);
            cpuResultLine.LinePattern = LinePattern.Dotted;
            cpuResultLine.Color = Colors.Black;

            var ramResultLine = multiplot.Subplots.GetPlot(1).Add.VerticalLine(sample.Value.RamGb);
            ramResultLine.LinePattern = LinePattern.Dotted;
            ramResultLine.Color = Colors.Black;
        }

        var bestCpuLine = multiplot.Subplots.GetPlot(0).Add.VerticalLine(bestCpu);
        bestCpuLine.LinePattern = LinePattern.Dashed;
        bestCpuLine.Color = Colors.Red;

        var bestRamLine = multiplot.Subplots.GetPlot(1).Add.VerticalLine(bestRam);
        bestRamLine.LinePattern = LinePattern.Dashed;
        bestRamLine.Color = Colors.Red;

        multiplot.SavePng($"{basePath}/{experimentName}.png", Width, Height);
    }

    /// <summary>
    /// Create simple XY-plot
    /// </summary>
    public static void Plot<TX, TY>(Plot plot, string title, params (string name, TX[] x, TY[] y)[] data)
    {
        plot.Title(title);
        plot.Legend.Alignment = Alignment.UpperRight;
        foreach (var (name, x, y) in data)
        {
            var scatter = plot.Add.Scatter(x, y);

            if (name is not null)
            {
                scatter.LegendText = name;
            }
        }
    }
}
