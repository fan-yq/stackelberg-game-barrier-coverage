using System.CommandLine;
using System.Text.Json;
using Airship.Deployment.SSGBarrier;
// using Airship.Deployment.SSGBarrier.AttackerOracle;
using Airship.Deployment.SSGBarrier.MMDE;
using Airship.Deployment.SSGBarrier.SGDE;
using Microsoft.Extensions.Logging;

namespace BuoyancyAircraft.Samples.Airship.SSGBarrier;

public sealed record SSGBarrierSamples2Parameters(SSGBarrierSetting Model, int PopulationDefFactor, int PopulationAttFactor) {
    readonly static SSGBarrierSetting defaultSetting = new(300.0 / 110, 2000, 50, 0.03, 0.9, 16.0 / 360 * 2 * PI, 0.03);

    public static SSGBarrierSamples2Parameters Default => new(defaultSetting, 10, 8);
}
/// <summary>
/// 自动生成场景，可以选择区域
/// </summary>
public sealed class SSGBarrierSamples2(SSGBarrierArea area, int randomSeed, ILoggerFactory factory, SSGBarrierMethod method, PointFixMode mode, bool isCrowding = false, SSGBarrierSamples2Parameters? sSGBarrierSamples2Parameters = null) {
    Random random = new(randomSeed);
    const int defaultSeed = 20260515;
    readonly ILogger logger = factory.CreateLogger<SSGBarrierSamples2>();
    SSGBarrierSamples2Parameters sSGBarrierSamples2Parameters1 = sSGBarrierSamples2Parameters ?? SSGBarrierSamples2Parameters.Default;
    SSGBarrierSetting setting => sSGBarrierSamples2Parameters1.Model;
    int populationAttFactor => sSGBarrierSamples2Parameters1.PopulationAttFactor;
    int populationDefFactor => sSGBarrierSamples2Parameters1.PopulationDefFactor;
    const int AttackerOracleInnerIteration = 100;

    public void SimpleCase(int N_airship, int N_ControlPoints, int iterations, double eps2, string? savePath = null, string? suffix = null) {
        int N_source = area.Sources.Length;
        int N_sink = area.Sinks.Length;

        string name = $"SSG_{area.Name}_NSO{N_source}_NSI{N_sink}_NAI{N_airship}_NC{N_ControlPoints}_IT{iterations}_EPS2{eps2}_{method}_crowding{isCrowding}_fix{mode}_s{randomSeed}";
        if (suffix is not null) { name += $"_{suffix}"; }
        name += ".json";

        PointGeo2D[] sources = area.Sources, sinks = area.Sinks;
        double[] sinkValues = area.SinkValues;

        SSGBarrierEnv2 env = new(area.AreaDef, area.AreaAtt, N_airship, N_ControlPoints, sources[..N_source], sinks[..N_sink], sinks, sinkValues[..N_sink], area.PointSortMode);

        int PopSizeDefender = populationDefFactor * env.N_Airship;
        int PopSizeAttacker = populationAttFactor * (env.N_Airship + 1);

        SSGResult2 res;
        SGDE4HullSetting setting1 = new() {
            Eps2 = eps2,
            IsCrowding = isCrowding,
            ProportionOfRandPBest = 1,
            DefOprType = SGDEDefOprType.Heuristic,
            AttOprType = SGDEAttOprType.Heuristic,
            UpdateBestAttacker = true,
            PointFixMode = mode
        };
        ACGAConfig aCGAConfig = ACGAConfig.Stackelberg(eps2).UseCrowding(isCrowding);
        aCGAConfig.PointFixMode = mode;
        switch (method) {
            case SSGBarrierMethod.ACDE:
                SSGOracleACDE sSGAttackerOracle = new(PopSizeDefender, PopSizeAttacker, random, env, setting, aCGAConfig, factory, eps2);
                res = sSGAttackerOracle.Resolve(iterations);
                break;
            case SSGBarrierMethod.MMDE:
                SSGOracleMMDE sSGOracleMMDE = new(random, env, setting, mode, factory);
                res = sSGOracleMMDE.Resolve(iterations);
                break;
            case SSGBarrierMethod.SSGDE:
                SSGOracleSGDE sSGOracleSGDE = new(PopSizeDefender, PopSizeAttacker, random, env, setting, factory, setting1);
                res = sSGOracleSGDE.Resolve(iterations);
                break;
            case SSGBarrierMethod.SSGDEPabestRandHalf:
                setting1.ProportionOfRandPBest = 0.5;
                goto case SSGBarrierMethod.SSGDE;
            case SSGBarrierMethod.SSGDEPureDE:
                setting1.DefOprType = SGDEDefOprType.DE;
                setting1.AttOprType = SGDEAttOprType.DE;
                goto case SSGBarrierMethod.SSGDE;
            case SSGBarrierMethod.SSGDEAttH:
                setting1.AttOprType = SGDEAttOprType.DE;
                goto case SSGBarrierMethod.SSGDE;
            case SSGBarrierMethod.SSGDEDefH:
                setting1.DefOprType = SGDEDefOprType.DE;
                goto case SSGBarrierMethod.SSGDE;
            case SSGBarrierMethod.SSGDENoAttUpdate:
                setting1.UpdateBestAttacker = false;
                goto case SSGBarrierMethod.SSGDE;

            // case SSGBarrierMethod.AttackerOracle2:
            //     // double iterations等效 = 0.5 + Sqrt(1 + 4 * 2 * PopSizeAttacker * PopSizeDefender * iterations / (20 * env.N_Airship + 20 * env.N_ControlPointOfAttacker) / AttackerOracleInnerIteration);
            //     SSGAttackerOracle2 sSGAttackerOracle2 = new(random, 20 * env.N_Airship, 20 * env.N_ControlPointOfAttacker, AttackerOracleInnerIteration, mode, env, setting, factory);
            //     res = sSGAttackerOracle2.Resolve(iterations);
            //     break;
            default:
                throw new NotImplementedException();
        }

        res.CaseName = area.Name;
        res.Method = method.ToString();
        logger.LogInformation("maxminU: {}, minmaxU: {}", res.MaxMinU, res.MinMaxU);
        if (savePath is not null) {
            Save(savePath, name, res);
        }
    }
    // 使用1个飞艇初始化导弹轨迹种群，然后测试3个飞艇总群的结果


    void Save<T>(string savePath, string name, T res) {
        var saveFile = Path.Join(savePath, name);
        File.Delete(saveFile);
        var s = JsonSerializer.SerializeToUtf8Bytes(res);
        File.WriteAllBytes(saveFile, s);
        logger.LogInformation("save file: {}", saveFile);
    }

    public static Command GetCommand(ILoggerFactory factory, string dataPath) {
        Command command = new(nameof(SSGBarrierSamples2), "飞艇SSG路径覆盖样例，输入参数，输出结果文件，包含部署方案和效用值");
        Option<int> iter = new Option<int>("--iter") { DefaultValueFactory = _ => 1000 };
        Option<int> nAirship = new Option<int>("--n-airship", "-d") { DefaultValueFactory = _ => 5 };
        Option<int> nSource = new Option<int>("--n-source", "-s") { DefaultValueFactory = _ => 3 };
        Option<int> nSink = new Option<int>("--n-sink", "-k") { DefaultValueFactory = _ => 3 };
        Option<int> nControl = new Option<int>("--n-control", "-n") { DefaultValueFactory = _ => 3 };
        Option<int> seedOption = new Option<int>("--seed") { Description = "seed for generate rand distrubtion of start positions and destinations", DefaultValueFactory = _ => defaultSeed };
        Option<int> seed2Option = new Option<int>("--seed2") { Description = "seed for init SSG algorithm" };
        Option<double> oEps2 = new("--eps2") { DefaultValueFactory = _ => 0.001 };
        Option<string> caseName = new("--case") { DefaultValueFactory = _ => "Simple" };
        Option<string> methodName = new("--method") { DefaultValueFactory = _ => SSGBarrierMethod.ACDE.ToString() };
        Option<bool> crowding = new("--crowding") { DefaultValueFactory = _ => false };
        Option<string> pointFixModeOpt = new("--point-fix-mode") { DefaultValueFactory = _ => PointFixMode.Proj.ToString() };
        Option<string> modelJsonOpt = new("--model-file") { Description = $"加载系统参数文件{nameof(SSGBarrierSetting)}" };
        Option<int> attFactorOpt = new("--att-factor") { DefaultValueFactory = _ => SSGBarrierSamples2Parameters.Default.PopulationAttFactor };
        Option<int> defFactorOpt = new("--def-factor") { DefaultValueFactory = _ => SSGBarrierSamples2Parameters.Default.PopulationDefFactor };
        Option<string> suffixOpt = new("--suffix");
        command.Options.Add(iter);
        command.Options.Add(nAirship);
        command.Options.Add(nSource);
        command.Options.Add(nSink);
        command.Options.Add(nControl);
        command.Options.Add(caseName);
        command.Options.Add(oEps2);
        command.Options.Add(methodName);
        command.Options.Add(crowding);
        command.Options.Add(seedOption);
        command.Options.Add(seed2Option);
        command.Options.Add(pointFixModeOpt);
        command.Options.Add(attFactorOpt);
        command.Options.Add(defFactorOpt);
        command.Options.Add(modelJsonOpt);
        command.Options.Add(suffixOpt);

        command.SetAction(parseResult => {
            var iterations = parseResult.GetValue(iter);
            var caseName_ = parseResult.GetValue(caseName) ?? "";
            var method = Enum.Parse<SSGBarrierMethod>(parseResult.GetValue(methodName)!);
            var pointFixMode = Enum.Parse<PointFixMode>(parseResult.GetValue(pointFixModeOpt)!);
            bool isCrowding = parseResult.GetValue(crowding);
            int seed = parseResult.GetValue(seedOption);
            int seed2 = parseResult.GetValue(seed2Option);
            if (seed2 == 0) { seed2 = seed; }
            int nA = parseResult.GetValue(nAirship), nS = parseResult.GetValue(nSource), nSi = parseResult.GetValue(nSink), nC = parseResult.GetValue(nControl);
            string? suffix = parseResult.GetValue(suffixOpt);

            SSGBarrierSamples2Parameters sSGBarrierSamples2Parameters1 = SSGBarrierSamples2Parameters.Default;
            string? modelJsonFile = parseResult.GetValue(modelJsonOpt);
            if (modelJsonFile is not null) {
                var setting = JsonSerializer.Deserialize<SSGBarrierSetting>(File.ReadAllBytes(modelJsonFile)) ?? throw new ArgumentException($"failed to load {modelJsonFile}");
                sSGBarrierSamples2Parameters1 = sSGBarrierSamples2Parameters1 with { Model = setting };
            }
            sSGBarrierSamples2Parameters1 = sSGBarrierSamples2Parameters1 with {
                PopulationAttFactor = parseResult.GetValue(attFactorOpt),
                PopulationDefFactor = parseResult.GetValue(defFactorOpt),
            };
            SSGBarrierSamples2 sSGBarrierSamples2 = new(SSGBarrierArea.FromName(caseName_, nS, nSi, seed), seed2, factory, method, pointFixMode, isCrowding, sSGBarrierSamples2Parameters1);
            sSGBarrierSamples2.SimpleCase(nA, nC, iterations: iterations, eps2: parseResult.GetValue(oEps2), savePath: dataPath, suffix: suffix);
        });
        return command;
    }

    public static Command CrossCompareCmd(ILoggerFactory factory, string dataPath) {
        ILogger logger = factory.CreateLogger(nameof(CrossCompareCmd));
        Command command = new(nameof(SSGCrossCompare),
            "输入SSG路径覆盖结果文件，交叉比较效用值，用于分析不同算法和参数设置优劣");

        // 改成接收通配符/文件路径（可以有多个）
        Argument<string[]> arguments = new("patterns") {
            Description = "文件路径或通配符，例如 data/*.json（需自行加引号以防被 shell 提前展开）"
        };
        Option<bool> onlyBestOption = new("--only-best") { DefaultValueFactory = _ => false };
        Option<double> oEps2 = new("--eps") { DefaultValueFactory = _ => 0.001 };

        command.Arguments.Add(arguments);
        command.Options.Add(onlyBestOption);
        command.Options.Add(oEps2);

        command.SetAction(async parseResult => {
            var onlyBest = parseResult.GetValue(onlyBestOption);
            var eps = parseResult.GetValue(oEps2);
            var patterns = parseResult.GetRequiredValue(arguments);

            // 展开通配符 -> 去重 -> 排序
            var files = ExpandPatterns(patterns);
            if (files.Length == 0) {
                logger.LogError("没有匹配到任何文件。");
                return;
            } else {
                foreach (var file in files) {
                    logger.LogInformation($"文件：{file}");
                }
            }

            var res = await SSGCrossCompare.Evaluate(files, onlyBest, eps);
            string onlybestTag = onlyBest ? "onlybest" : "all";
            var saveFile = Path.Join(dataPath,
                $"SSG-compare-{res.CaseName ?? "unknown"}-{files.Length}-{onlybestTag}.json");

            var s = JsonSerializer.SerializeToUtf8Bytes(res);
            File.WriteAllBytes(saveFile, s);

            logger.LogInformation($"匹配 {files.Length} 个文件，结果已写入 {saveFile}");
        });

        return command;
    }
    // public static Command SSGCompareWithFixed(ILoggerFactory factory, TextWriter writer) {
    //     ILogger logger = factory.CreateLogger(nameof(SSGCompareWithFixed));
    //     Command command = new(nameof(SSGCompareWithFixed),
    //         "输入SSG路径覆盖结果文件，与固定的attacker player方法比较，用于分析不同算法和参数设置优劣");

    //     // 改成接收通配符/文件路径（可以有多个）
    //     Argument<string[]> arguments = new("patterns") {
    //         Description = "文件路径或通配符，例如 data/*.json（需自行加引号以防被 shell 提前展开）"
    //     };
    //     Option<double> oEps2 = new("--eps") { DefaultValueFactory = _ => 0.001 };

    //     command.Arguments.Add(arguments);
    //     command.Options.Add(oEps2);

    //     command.SetAction(async parseResult => {
    //         var eps = parseResult.GetValue(oEps2);
    //         var patterns = parseResult.GetRequiredValue(arguments);

    //         // 展开通配符 -> 去重 -> 排序
    //         var files = ExpandPatterns(patterns);
    //         if (files.Length == 0) {
    //             logger.LogError("没有匹配到任何文件。");
    //             return;
    //         } else {
    //             foreach (var file in files) {
    //                 logger.LogInformation($"文件：{file}");
    //             }
    //         }

    //         var res = SSGCrossCompare.ComparedWithFixedEpsBestDefender(files, 1000, eps, defaultSeed, factory).ToArray();
    //         writer.WriteLine($"file,original maxminUd,eps-best minUd");
    //         foreach (var r in res) {
    //             writer.WriteLine($"{r.File},{r.MaxMinUd},{r.FixedEpsBestMinUd}");
    //         }
    //         writer.WriteLine($"mean: {Mean(res.Select(r => r.MaxMinUd))}, std: {StdMSE(res.Select(r => r.MaxMinUd))}, near-optimal mean: {Mean(res.Select(r => r.FixedEpsBestMinUd))}, near-optimal std: {StdMSE(res.Select(r => r.FixedEpsBestMinUd))}");
    //     });

    //     return command;
    // }

    public static Command SSGPrintLaunchSitesTargets(TextWriter writer) {
        Option<string> caseName = new("--case") { DefaultValueFactory = _ => "Simple" };
        Option<int> nSource = new Option<int>("--n-source", "-s") { DefaultValueFactory = _ => 3 };
        Option<int> nSink = new Option<int>("--n-sink", "-k") { DefaultValueFactory = _ => 3 };
        Option<int> seedOption = new Option<int>("--seed") { Description = "seed for generate rand distrubtion of start positions and destinations", DefaultValueFactory = _ => defaultSeed };

        Command command = new(nameof(SSGPrintLaunchSitesTargets), "打印飞艇SSG路径覆盖样例的位置信息") { caseName, nSource, nSink, seedOption };

        command.SetAction(parseResult => {
            var caseName_ = parseResult.GetValue(caseName) ?? "";
            int seed = parseResult.GetValue(seedOption);
            int nS = parseResult.GetValue(nSource), nSi = parseResult.GetValue(nSink);

            var c = SSGBarrierArea.FromName(caseName_, nS, nSi, seed);

            writer.WriteLine($"id: {caseName_}, {seed}");

            writer.WriteLine("launch sites: ");
            foreach (var p in c.Sources) {
                writer.WriteLine($"lat: {p.Lat}, lon: {p.Lon}");
            }

            writer.WriteLine("targets: ");
            foreach (var p in c.Sinks) {
                writer.WriteLine($"lat: {p.Lat}, lon: {p.Lon}");
            }
        });

        return command;
    }

    static double StdMSE(IEnumerable<double> v) {
        int n = v.Count();
        double mean = v.Sum() / n;
        double mse = v.Sum(a => (a - mean) * (a - mean)) / (n - 1);
        return Sqrt(mse);
    }
    static double Mean(IEnumerable<double> v) {
        int n = v.Count();
        double mean = v.Sum() / n;
        return mean;
    }

    private static string[] ExpandPatterns(string[] patterns) {
        // 若字符串本身是存在的文件，直接保留（避免把文件名里的 * 当通配符）
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in patterns) {
            if (File.Exists(p)) {
                result.Add(Path.GetFullPath(p));
                continue;
            }

            // 判断是否含通配符
            bool hasWildcard = p.IndexOfAny(new[] { '*', '?' }) >= 0;

            if (!hasWildcard) {
                // 可能是目录，展开为目录下所有文件
                if (Directory.Exists(p)) {
                    foreach (var f in Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                        result.Add(Path.GetFullPath(f));
                    continue;
                }
                Console.Error.WriteLine($"警告：找不到文件或目录：{p}");
                continue;
            }

            // 分离目录与文件名模式，支持 "dir/*.json" 及 "*.json"
            string? dir = Path.GetDirectoryName(p);
            string pattern = Path.GetFileName(p);
            if (string.IsNullOrEmpty(dir)) dir = ".";

            if (!Directory.Exists(dir)) {
                Console.Error.WriteLine($"警告：目录不存在：{dir}");
                continue;
            }

            foreach (var f in Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly))
                result.Add(Path.GetFullPath(f));
        }

        var arr = result.ToArray();
        Array.Sort(arr, StringComparer.OrdinalIgnoreCase);
        return arr;
    }
}