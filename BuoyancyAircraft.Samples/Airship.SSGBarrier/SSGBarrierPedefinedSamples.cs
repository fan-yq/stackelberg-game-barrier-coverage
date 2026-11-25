using System.CommandLine;
using System.Text.Json;
using Airship.Deployment.SSGBarrier;
// using Airship.Deployment.SSGBarrier.AttackerOracle;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace BuoyancyAircraft.Samples.Airship.SSGBarrier;

// 针对审稿人几何部署和计算近似最优的结果
public sealed class SSGBarrierPredefinedSamples(SSGBarrierArea area, int randomSeed, PointFixMode mode, ILoggerFactory factory) {
    readonly ILogger logger = factory.CreateLogger<SSGBarrierPredefinedSamples>();
    SSGBarrierSetting setting = new(300.0 / 110, 2000, 50, 0.03, 0.9, 16.0 / 360 * 2 * PI, 0.03);
    const int defaultSeed = 20260515;

    // public void Line(int N_airship, int N_ControlPoints, int iterations, string? savePath = null) {
    //     int N_source = area.Sources.Length;
    //     int N_sink = area.Sinks.Length;

    //     string name = $"PredefinedLine_{area.Name}_NSO{N_source}_NSI{N_sink}_NAI{N_airship}_NC{N_ControlPoints}_IT{iterations}_fix{mode}_s{randomSeed}.json";

    //     PointGeo2D start, end;
    //     PointGeo2D[] lines;
    //     if (area.Name.StartsWith(nameof(SSGBarrierArea.Case3))) {
    //         start = new(10, 117.9);
    //         end = new(22.8, 117.9);
    //         lines = SSGPredefinedDeployment.Line(start, end, N_airship);
    //     } else if (area.Name.StartsWith("HullArea")) {
    //         start = new(14.5, 111);
    //         end = new(21.6, 123.5);
    //         lines = SSGPredefinedDeployment.Line2(start, end, N_airship);
    //     } else {
    //         throw new NotImplementedException();
    //     }
    //     logger.LogInformation("line {} to {}", lines[0], lines[^1]);


    //     PredefinedAirshipFormation(lines, N_ControlPoints, iterations, name, savePath);
    // }

    // void PredefinedAirshipFormation(PointGeo2D[] points, int N_ControlPoints, int iterations, string name, string? savePath = null) {
    //     int N_airship = points.Length;
    //     PointGeo2D[] sources = area.Sources, sinks = area.Sinks;
    //     int N_source = area.Sources.Length;
    //     int N_sink = area.Sinks.Length;
    //     double[] sinkValues = area.SinkValues;

    //     SSGBarrierEnv2 env = new(area.AreaDef, area.AreaAtt, N_airship, N_ControlPoints, sources[..N_source], sinks[..N_sink], sinks, sinkValues[..N_sink], area.PointSortMode);

    //     int PopSizeAttacker = 20 * (env.N_Airship + 1);

    //     SSGPredefinedDeployment sSGPredefinedDeployment = new(PopSizeAttacker, mode, new(randomSeed), env, setting, factory);

    //     SSGResult2 res = sSGPredefinedDeployment.Predefined(iterations, points);

    //     res.CaseName = area.Name;
    //     res.Method = nameof(SSGPredefinedDeployment);
    //     logger.LogInformation("maxminU: {}, minmaxU: {}", res.MaxMinU, res.MinMaxU);
    //     if (savePath is not null) {
    //         Save(savePath, name, res);
    //     }
    // }

    void Save<T>(string savePath, string name, T res) {
        var saveFile = Path.Join(savePath, name);
        File.Delete(saveFile);
        var s = JsonSerializer.SerializeToUtf8Bytes(res);
        File.WriteAllBytes(saveFile, s);
        logger.LogInformation("save file: {}", saveFile);
    }

    // public static Command SSGPredefinedCommand(ILoggerFactory factory, string dataPath) {
    //     Command command1 = new("SSGPredefined") {
    //         SSGLine(factory,dataPath),SSGPredefined(factory,dataPath)
    //     };
    //     return command1;
    // }

    // static Command SSGLine(ILoggerFactory factory, string dataPath) {
    //     Option<string> caseName = new("--case") { DefaultValueFactory = _ => "Simple" };
    //     Option<int> seedOption = new Option<int>("--seed") { Description = "seed for generate rand distrubtion of start positions and destinations", DefaultValueFactory = _ => defaultSeed };
    //     Option<int> nSource = new Option<int>("--n-source", "-s") { DefaultValueFactory = _ => 3 };
    //     Option<int> nSink = new Option<int>("--n-sink", "-k") { DefaultValueFactory = _ => 3 };
    //     Option<int> nAirship = new Option<int>("--n-airship", "-d") { DefaultValueFactory = _ => 5 };
    //     Option<int> nControl = new Option<int>("--n-control", "-n") { DefaultValueFactory = _ => 3 };
    //     Option<string> pointFixModeOpt = new("--point-fix-mode") { DefaultValueFactory = _ => PointFixMode.Proj.ToString() };
    //     Option<int> iter = new Option<int>("--iter") { DefaultValueFactory = _ => 1000 };


    //     Command command = new("line", "采用预定的line部署方案计算最优的attacker") {
    //         caseName,seedOption,nSource,nSink,nAirship,nControl,pointFixModeOpt,iter
    //     };

    //     command.SetAction(parseResult => {
    //         var iterations = parseResult.GetValue(iter);

    //         var caseName_ = parseResult.GetValue(caseName) ?? "";
    //         int seed = parseResult.GetValue(seedOption);
    //         int nA = parseResult.GetValue(nAirship), nS = parseResult.GetValue(nSource), nSi = parseResult.GetValue(nSink), nC = parseResult.GetValue(nControl);
    //         var pointFixMode = Enum.Parse<PointFixMode>(parseResult.GetValue(pointFixModeOpt)!);

    //         SSGBarrierPredefinedSamples sSGBarrierPredefinedSamples = new(SSGBarrierArea.FromName(caseName_, nS, nSi, seed), seed, pointFixMode, factory);

    //         sSGBarrierPredefinedSamples.Line(nA, nC, iterations, savePath: dataPath);
    //     });

    //     return command;
    // }
    // static Command SSGPredefined(ILoggerFactory factory, string dataPath) {
    //     Option<int> defOpt = new("--def-index");
    //     Option<string> fileOpt = new("--file", "-f");
    //     Option<double> oEps2 = new("--eps2") { DefaultValueFactory = _ => 0 };
    //     Option<int> seedOption = new Option<int>("--seed") { DefaultValueFactory = _ => defaultSeed };
    //     Option<string> pointFixModeOpt = new("--point-fix-mode") { DefaultValueFactory = _ => PointFixMode.Proj.ToString() };
    //     Option<int> iter = new Option<int>("--iter") { DefaultValueFactory = _ => 1000 };


    //     Command command = new("predefined", "选择SSG文件和index，计算该index部署方案下的最优路径。如果eps2=0，使用--def-index") {
    //         fileOpt,defOpt,seedOption,pointFixModeOpt,iter,oEps2
    //     };

    //     command.SetAction(parseResult => {
    //         var logger = factory.CreateLogger(nameof(SSGPredefined));

    //         var iterations = parseResult.GetValue(iter);

    //         var files = parseResult.GetRequiredValue(fileOpt);
    //         var defIndex1 = parseResult.GetValue(defOpt);
    //         var res = SSGCrossCompare.LoadData2([files])[0];
    //         var env = res.Env;
    //         var pointFixMode = Enum.Parse<PointFixMode>(parseResult.GetValue(pointFixModeOpt)!);
    //         var eps2 = parseResult.GetValue(oEps2);

    //         List<int> defIndices = [];

    //         if (eps2 == 0) {
    //             defIndices.Add(defIndex1);
    //         } else {
    //             defIndices = ACGAResult.EpsBestDefenderIndices(eps2, Matrix<double>.Build.DenseOfRowArrays(res.UMatrix));
    //         }

    //         foreach (var defIndex in defIndices) {
    //             string name = $"PredefinedCase_defIndex{defIndex}_{Path.GetFileNameWithoutExtension(files)}.json";
    //             logger.LogInformation("process def-index: {}", defIndex);

    //             SSGBarrierArea sSGBarrierArea = new(res.CaseName ?? "unknown", env.MissleArea, env.DeployArea, env.Sources, env.Sinks, PointGeo2DSortMode.Lat);

    //             SSGBarrierPredefinedSamples sSGBarrierPredefinedSamples = new(sSGBarrierArea, parseResult.GetValue(seedOption), pointFixMode, factory);

    //             if (res.AttackerStrategies[0].Paths.Length != env.N_ControlPointOfAttacker + 2) {
    //                 throw new ArgumentException("invalid format of result");
    //             }

    //             sSGBarrierPredefinedSamples.PredefinedAirshipFormation(res.AirshipStrategies[defIndex].Deployments, env.N_ControlPointOfAttacker, iterations, name, savePath: dataPath);
    //         }

    //     });

    //     return command;
    // }

    public static Command SSGProbSurvWithS(ILoggerFactory factory, TextWriter writer) {
        Option<string> fileOpt = new("--file", "-f");
        Option<int> attOpt = new("--att-index");
        Option<int> defOpt = new("--def-index");
        Command command = new(nameof(SSGProbSurvWithS), "选择SSG文件和index，计算该index路径的效用值随路径推进的变化") {
            fileOpt,attOpt,defOpt
        };

        command.SetAction(async parseResult => {
            var files = parseResult.GetRequiredValue(fileOpt);
            var attIndex = parseResult.GetRequiredValue(attOpt);
            var defIndex = parseResult.GetRequiredValue(defOpt);
            var res = SSGCrossCompare.LoadData2([files])[0];
            SSGBarrierUtility sSGBarrierUtility = new(res.Env, res.Setting);
            var range = Generate.LinearSpaced(50, 0, 1);
            var (Ua, Ud) = sSGBarrierUtility.U(res.AirshipStrategies[defIndex].ToMixdStrategy(), res.AttackerStrategies[attIndex]);
            var sprob = range.Select(s => sSGBarrierUtility.ProbSurvWithS(res.AirshipStrategies[defIndex].ToMixdStrategy(), res.AttackerStrategies[attIndex], s));
            if (Ud != res.UMatrix[defIndex][attIndex]) {
                factory.CreateLogger<SSGBarrierSamples2>().LogWarning("caculated Ud ({}) is not equal to stored Ud ({})", Ud, res.UMatrix[defIndex][attIndex]);
            }
            writer.WriteLine($"{defIndex}/{attIndex}: {Ud} [{WriteArray(sprob)}]");
        });
        return command;
    }
}