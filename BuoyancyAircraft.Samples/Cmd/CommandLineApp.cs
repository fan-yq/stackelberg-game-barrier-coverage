using System.CommandLine;
using BuoyancyAircraft.Samples.Airship.SSGBarrier;
using BuoyancyAircraft.Samples.ConfigDef;
using Microsoft.Extensions.Logging;

namespace BuoyancyAircraft.Samples.Cmd;

public sealed class CommandLineApp(ILoggerFactory factory, ConfigDefinition config) {
    public RootCommand GetRootCommand() {
        RootCommand rootCommand = new("samples") {
            // SSG
            SSGBarrierSamples2.GetCommand(factory, Path.Combine(config.OutputPath, "SSG")),
            SSGBarrierSamples2.CrossCompareCmd(factory, Path.Combine(config.OutputPath, "SSG")),
            // SSGBarrierSamples2.SSGCompareWithFixed(factory, Console.Out),
            SSGBarrierSamples2.SSGPrintLaunchSitesTargets(Console.Out),
            SSGBarrierPredefinedSamples.SSGProbSurvWithS(factory,Console.Out),
            // SSGBarrierPredefinedSamples.SSGPredefinedCommand(factory,Path.Combine(config.OutputPath, "SSG")),

            MinimaxBenchSamples.MinimaxBench(factory, Path.Combine(config.OutputPath, "SSG")),
            SSGBarrierBenchmark.GetCommand(),

        };
        return rootCommand;
    }
}