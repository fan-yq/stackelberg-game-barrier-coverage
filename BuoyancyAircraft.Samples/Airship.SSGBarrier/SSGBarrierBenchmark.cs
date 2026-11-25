using System.CommandLine;
using Airship.Deployment.SSGBarrier;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuoyancyAircraft.Samples.Airship.SSGBarrier;


public class SSGBarrierBenchmark {
    const int seed = 20260515, iterations = 1000;
    readonly ILoggerFactory factory = NullLoggerFactory.Instance;
    readonly bool isCrowding = false;
    readonly PointFixMode mode = PointFixMode.Proj;
    [Benchmark]
    [Arguments(SSGBarrierMethod.ACDE, 3)]
    [Arguments(SSGBarrierMethod.ACDE, 6)]
    [Arguments(SSGBarrierMethod.ACDE, 12)]
    [Arguments(SSGBarrierMethod.SSGDE, 3)]
    [Arguments(SSGBarrierMethod.SSGDE, 6)]
    [Arguments(SSGBarrierMethod.SSGDE, 12)]
    [Arguments(SSGBarrierMethod.SSGDEPureDE, 3)]
    [Arguments(SSGBarrierMethod.SSGDEPureDE, 6)]
    [Arguments(SSGBarrierMethod.SSGDEPureDE, 12)]
    public void Run(SSGBarrierMethod method, int nAirship) {
        SSGBarrierSamples2 sSGBarrierSamples2 = new(SSGBarrierArea.HullArea4(3, 3, seed), seed, factory, method, mode, isCrowding);
        sSGBarrierSamples2.SimpleCase(nAirship, 3, iterations: iterations, eps2: 0, savePath: null);
    }

    public static Command GetCommand() {
        Command command1 = new(nameof(SSGBarrierBenchmark), "SSG barrier benchmark");
        command1.SetAction(_ => {
            var config = ManualConfig.CreateMinimumViable()
            .AddJob(
            // Job.Default
            Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance)
            .WithLaunchCount(1)
            .WithWarmupCount(2)
            .WithIterationCount(5))
            .WithArtifactsPath(".");
            BenchmarkRunner.Run<SSGBarrierBenchmark>(config);
        });
        return command1;
    }
}