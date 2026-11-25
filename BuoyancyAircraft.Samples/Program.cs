using System.Diagnostics;
using BuoyancyAircraft.Samples.Cmd;
using BuoyancyAircraft.Samples.ConfigDef;

using Microsoft.Extensions.Logging;

// SysHighPrior.SetHighPriority();
var config = ConfigLoader.Load();
var config1 = ConfigDefinition.Create(config);

Stopwatch stopwatch = new();
stopwatch.Start();

#if DEBUG && DETAIL
using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
#else
using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
#endif

CommandLineApp commandLineApp = new(factory, config1);
var command = commandLineApp.GetRootCommand();
var res = command.Parse(args);
int flag = res.Invoke();



Console.WriteLine($"full time: {stopwatch.Elapsed}");
return flag;