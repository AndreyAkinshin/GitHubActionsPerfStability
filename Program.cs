using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Mathematics.OutlierDetection;

namespace GitHubActionsPerfStability;

public class ResultCsvExporter : ExporterBase
{
    protected override string FileExtension => "csv";
    protected override string FileCaption => "results";

    public override void ExportToLog(Summary summary, ILogger logger)
    {
        var os = GetOs();
        logger.WriteLine("os,benchmark,durationNs");
        foreach (var report in summary.Reports)
        {
            var benchmark = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            var measurements = report.AllMeasurements.Where(m => m.IterationStage == IterationStage.Result);
            foreach (var measurement in measurements)
            {
                var durationNs = Math.Round(measurement.GetAverageTime().Nanoseconds);
                logger.WriteLine($"{os},{benchmark},{durationNs}");
            }
        }
    }

    private static string GetOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macos";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return "freebsd";
        return "?";
    }
}

public class Config : ManualConfig
{
    public Config()
    {
        AddExporter(new ResultCsvExporter());
        AddJob(Job.Default
            .WithIterationCount(100)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithOutlierMode(OutlierMode.DontRemove));
    }
}

[Config(typeof(Config))]
public class Benchmarks
{
    private readonly byte[] data = new byte[100 * 1024 * 1024];

    [GlobalSetup]
    public void Setup()
    {
        new Random(1729).NextBytes(data);
    }

    [Benchmark]
    public double Cpu()
    {
        double pi = 0;
        for (var i = 1; i <= 500_000_000; i++)
            pi += 1.0 / i / i;
        pi = Math.Sqrt(pi * 6);
        return pi;
    }

    [Benchmark]
    public void Disk()
    {
        for (var i = 0; i < 10; i++)
        {
            var fileName = Path.GetTempFileName();
            File.WriteAllBytes(fileName, data);
            File.Delete(fileName);
        }
    }

    [Benchmark]
    public int Memory()
    {
        var random = new Random(1729);
        var sum = 0;
        for (int i = 0; i < 200_000_00; i++)
            sum += data[random.Next(data.Length)];
        return sum;
    }
}

internal class Program
{
    public static void Main() => BenchmarkRunner.Run<Benchmarks>();
}