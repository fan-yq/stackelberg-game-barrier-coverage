using System.CommandLine;
using System.Text.Json;
using Airship.Deployment.SSGBarrier;
using Airship.Deployment.SSGBarrier.MMDE;
using Airship.Deployment.SSGBarrier.SGDE;
using Microsoft.Extensions.Logging;

namespace BuoyancyAircraft.Samples.Airship.SSGBarrier;

public sealed record MinimaxResult {
    public string ProblemName { get; set; } = "unknown";
    public string MethodName { get; set; } = "unknown";
    public int Iterations { get; init; }
    public int Repetitions { get; init; }
    public required double[] MaxMin { get; init; }
    public required double[] MinMax { get; init; }
    public double[][] BestDefenders { get; init; } = [];
    public double[][] BestAttackers { get; init; } = [];
    public double? MSEValue { get; init; }
    public double[]? AllMSEValues { get; init; }
    public double? MeanMSE { get; init; }
    public double? MedianMSE { get; init; }
    public double? StdMSE { get; init; }
    public double[][] OptimalX { get; init; } = [];
    public double[][] OptimalY { get; init; } = [];
    public double? OptimalValue { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
public sealed partial class MinimaxBenchSamples {
    static MinimaxResult Optimize(MinimaxProblem problem, MinimaxMethod method, ILogger logger, int iterations, Random? random = null) {
        int PDef = 50, PAtt = 50;
        random ??= new(20260512);
        bool parallel_ = problem.DimDef > 10 ? true : false;
        double eps2 = 0, F = 0.5, CR = 0.9;

        ACGAResult res;
        var objective = problem.ObjectiveFnGame();

        SGDEBoxedSetting setting = new() {
            Eps2 = eps2,
            ProportionOfRandPBest = 1,
            IsCrowding = false,
            OprType = SGDEBoxedType.Reflection,
            UpdateBestAttacker = true,
        };
        ACGAConfig aCGAConfig = ACGAConfig.Stackelberg(eps2);
        switch (method) {
            case MinimaxMethod.ACGA:
                ACGABoxed aCGABoxed = new(aCGAConfig, problem.DimDef, problem.DimAtt, PDef, PAtt, objective, problem.xl, problem.xu, problem.sl, problem.su, random, logger, parallel_) {
                    F = F,
                    CR = CR
                };
                res = aCGABoxed.Optimize(iterations);
                break;
            case MinimaxMethod.PCGA:
                aCGAConfig = aCGAConfig.UseCAMode(CAMode.PCGA);
                goto case MinimaxMethod.ACGA;
            case MinimaxMethod.ACGACrowding:
                aCGAConfig = aCGAConfig.UseCrowding(true);
                iterations = iterations * PAtt / 10; // 按照U的有效计算次数
                PAtt = 10;
                goto case MinimaxMethod.ACGA;


            case MinimaxMethod.SGDE:
                SGDEBoxed sGDEBoxed = new(problem.DimDef, problem.DimAtt, PDef, PAtt, objective, problem.xl, problem.xu, problem.sl, problem.su, random, logger, parallel_, setting) {
                    F = F,
                    CR = CR
                };
                res = sGDEBoxed.Optimize(iterations);
                break;
            case MinimaxMethod.SGDECrowding:
                setting.IsCrowding = true;
                iterations = iterations * PAtt / 10; // 按照U的有效计算次数
                PAtt = 10;
                goto case MinimaxMethod.SGDE;
            case MinimaxMethod.SGDEPabestRandHalf:
                setting.ProportionOfRandPBest = 0.5;
                goto case MinimaxMethod.SGDE;
            case MinimaxMethod.SGDEPabestDisable:
                setting.ProportionOfRandPBest = 0;
                goto case MinimaxMethod.SGDE;
            case MinimaxMethod.SGDEClamp:
                setting.OprType = SGDEBoxedType.DEClamp;
                goto case MinimaxMethod.SGDE;
            case MinimaxMethod.SGDENoAttUpdate:
                setting.UpdateBestAttacker = false;
                goto case MinimaxMethod.SGDE;
            case MinimaxMethod.SGDEAttPop1:
                iterations = iterations * PAtt; // 按照U的有效计算次数
                PAtt = 1;
                goto case MinimaxMethod.SGDE;
            case MinimaxMethod.MMDE:
                MMDEBoxed mMDEBoxed = new(problem.DimDef, problem.DimAtt, PDef, (int)(PDef * 1.8), Max((int)(PDef * 0.2), 1), objective, problem.xl, problem.xu, problem.sl, problem.su, random, logger, parallel_) {
                    F = F,
                    CR = CR
                };
                iterations = iterations * PDef * PAtt / (2 * PDef); // 按照U的有效计算次数
                var res1 = mMDEBoxed.Optimize(iterations);
                res = MMDE.MMDEPopToACGAResult(res1, objective, parallel_);
                break;
            default:
                goto case MinimaxMethod.ACGA;
        }
        double eps = 0.001;
        double maxMin = res.MaxMinU();
        double minMax = res.MinMaxU();
        logger.LogInformation("max min: {} min max: {}", maxMin, minMax);
        var bestAttackerIndices = res.BestAttackerIndices(eps);
        var bestDefenderIndices = res.BestDefenderIndices(eps);

        // Collect defender solutions
        var bestDefenders = bestDefenderIndices.Select(i => res.PopDefender[i].ToArray()).ToArray();
        var bestAttackers = bestAttackerIndices.Select(j => res.PopAttacker[j].ToArray()).ToArray();

        foreach (var i in bestDefenderIndices) {
            logger.LogTrace("Def {}: {}", i, WriteArrayG3(res.PopDefender[i]));
        }
        foreach (var j in bestAttackerIndices) {
            logger.LogTrace("Att {}: {}", j, WriteArrayG3(res.PopAttacker[j]));
        }

        // Calculate and log MSE for best defender solutions
        double? mse = null;
        if (problem.optimalX != null && problem.optimalX.Length > 0) {
            bestDefenderIndices = res.BestDefenderIndices(0);
            var xBest = res.PopDefender[bestDefenderIndices[0]];
            mse = problem.CalculateMSE(xBest);
            logger.LogInformation("MSE between Xbest and optimal X: {}", mse);

            logger.LogInformation("Optimal solutions ({} found): {}", problem.optimalX.Length,
                string.Join("; ", problem.optimalX.Select((x, i) =>
                $"Optimal {i}: X={WriteArrayG3(x)}, Y={WriteArrayG3(problem.optimalY![i])}, Value={problem.OptimalValue() ?? 0:0.0000}")));
        } else {
            logger.LogWarning("No optimal solution defined for this problem");
        }

        var optimalValue = problem.OptimalValue();
        if (optimalValue is not null) {
            if (maxMin < optimalValue || minMax < optimalValue) {
                logger.LogWarning("[{}] found better than optimal value ({}), max min: {}, min max: {}", method, optimalValue, maxMin, minMax);
            }
        }

        return new MinimaxResult {
            MethodName = method.ToString(),
            MaxMin = [maxMin],
            MinMax = [minMax],
            BestDefenders = bestDefenders,
            BestAttackers = bestAttackers,
            MSEValue = mse,
            Repetitions = 1,
            OptimalX = problem.optimalX ?? Array.Empty<double[]>(),
            OptimalY = problem.optimalY ?? Array.Empty<double[]>(),
            OptimalValue = problem.OptimalValue(),
            Iterations = iterations
        };
    }



    // Add this method to MinimaxBenchSamples class
    public static void RunAllCasesAndMethods(ILogger logger, int iterations, int repetitions = 30, string outputDir = "results") {
        var problems = new Dictionary<string, MinimaxProblem> {
            ["F1"] = F1(),
            ["F2"] = F2(),
            ["F3"] = F3(),
            ["F4"] = F4(),
            ["F5"] = F5(),
            ["F6"] = F6(),
            ["F7"] = F7(),
            ["F8"] = F8(),
            ["F9"] = F9(),
            ["F10"] = F10(),
            ["F11"] = F11(),
            ["F12"] = F12()
        };
        // MinimaxMethod[] excludes = [MinimaxMethod.SGDECrowding]; // F7 太慢
        MinimaxMethod[] excludes = []; // F7 太慢

        var methods = Enum.GetValues<MinimaxMethod>();
        methods = methods.Where(m => !excludes.Contains(m)).ToArray();
        var results = new List<MinimaxResult>();

        // Create output directory
        Directory.CreateDirectory(outputDir);
        int seed0 = 20260616;

        foreach (var (problemName, problem) in problems) {
            foreach (var method in methods) {
                logger.LogInformation("Running {Problem} with {Method} for {Repetitions} repetitions...", problemName, method, repetitions);

                try {
                    var allMSEs = new List<double>();
                    var allBestDefenders = new List<double[]>();
                    var allBestAttackers = new List<double[]>();
                    List<double> allMaxMin = [], allMinMax = [];
                    int validRuns = 0;

                    for (int rep = 0; rep < repetitions; rep++) {
                        logger.LogDebug("  Repetition {Rep}/{Repetitions}", rep + 1, repetitions);

                        var result = Optimize(problem, method, logger, iterations, new(seed0++));

                        allMaxMin.Add(result.MaxMin[0]);
                        allMinMax.Add(result.MinMax[0]);
                        allBestDefenders.AddRange(result.BestDefenders);
                        allBestAttackers.AddRange(result.BestAttackers);

                        if (result.MSEValue.HasValue && !double.IsNaN(result.MSEValue.Value) && !double.IsInfinity(result.MSEValue.Value)) {
                            allMSEs.Add(result.MSEValue.Value);
                            validRuns++;
                        }
                    }

                    double meanMSE = 0, medianMSE = 0, stdMSE = 0;
                    if (validRuns == 0) {
                        logger.LogWarning("No valid runs for {Problem} with {Method}", problemName, method);
                        // continue;
                    } else {
                        // Calculate statistics
                        var sortedMSEs = allMSEs.OrderBy(m => m).ToList();
                        meanMSE = allMSEs.Average();
                        medianMSE = sortedMSEs.Count % 2 == 0
                           ? (sortedMSEs[sortedMSEs.Count / 2 - 1] + sortedMSEs[sortedMSEs.Count / 2]) / 2
                           : sortedMSEs[sortedMSEs.Count / 2];
                        stdMSE = Math.Sqrt(allMSEs.Sum(m => Math.Pow(m - meanMSE, 2)) / allMSEs.Count);
                    }


                    var finalResult = new MinimaxResult {
                        ProblemName = problemName,
                        MethodName = method.ToString(),
                        Iterations = iterations,
                        Repetitions = repetitions,
                        MaxMin = allMaxMin.ToArray(),
                        MinMax = allMinMax.ToArray(),
                        BestDefenders = allBestDefenders.Distinct(new DoubleArrayComparer()).ToArray(),
                        BestAttackers = allBestAttackers.Distinct(new DoubleArrayComparer()).ToArray(),
                        AllMSEValues = allMSEs.ToArray(),
                        MeanMSE = meanMSE,
                        MedianMSE = medianMSE,
                        StdMSE = stdMSE,
                        MSEValue = meanMSE, // Keep for backward compatibility
                        OptimalX = problem.optimalX ?? [],
                        OptimalY = problem.optimalY ?? [],
                        OptimalValue = problem.OptimalValue()
                    };

                    results.Add(finalResult);

                    // Save individual result
                    // string json = JsonSerializer.Serialize(finalResult);
                    // string fileName = $"{problemName}_{method}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    // File.WriteAllText(Path.Combine(outputDir, fileName), json);

                    logger.LogInformation("Completed {Problem} with {Method}. Mean MSE: {MeanMSE:F6}, Median MSE: {MedianMSE:F6}, Std MSE: {StdMSE:F6}",
                        problemName, method, meanMSE, medianMSE, stdMSE);
                } catch (Exception ex) {
                    logger.LogError(ex, "Error running {Problem} with {Method}", problemName, method);
                }
            }
        }

        // Save summary results
        var summaryOptions = new JsonSerializerOptions { WriteIndented = true };
        string summaryJson = JsonSerializer.Serialize(results, summaryOptions);
        File.WriteAllText(Path.Combine(outputDir, $"summary_{DateTime.Now:yyyyMMdd_HHmmss}.json"), summaryJson);

        logger.LogInformation("All results saved to {OutputDir}", outputDir);
    }

    // Helper class for distinct arrays
    public class DoubleArrayComparer : IEqualityComparer<double[]> {
        public bool Equals(double[]? x, double[]? y) {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.Length != y.Length) return false;
            return x.SequenceEqual(y);
        }

        public int GetHashCode(double[] obj) {
            if (obj is null) return 0;
            return obj.Aggregate(17, (hash, val) => hash * 31 + val.GetHashCode());
        }
    }

    public static Command MinimaxBench(ILoggerFactory factory, string outputDir) {
        Option<string> c = new("--case", "-c") {
            DefaultValueFactory = _ => "F1"
        };
        Option<string> methodOption = new("--method", "-m") {
            DefaultValueFactory = _ => MinimaxMethod.ACGA.ToString()
        };
        Option<int> iterationOption = new("--iterations") {
            DefaultValueFactory = _ => 100
        };
        Option<int> repeatOption = new("--repeat") {
            DefaultValueFactory = _ => 100
        };
        Option<bool> allOption = new("--all") {
            Description = "if all = true, run all methods and testcases from F1 to F11",
            DefaultValueFactory = _ => false,
        };
        Command command1 = new(nameof(MinimaxBenchSamples), "不同SSG方法和参数设置下在标准测试集的性能") {
            c,methodOption,iterationOption,allOption,repeatOption
        };

        command1.SetAction(parseResult => {
            var ca = parseResult.GetValue(c) ?? "";
            var method = parseResult.GetValue(methodOption) ?? nameof(MinimaxMethod.ACGA);
            var all = parseResult.GetValue(allOption);
            var m = Enum.Parse<MinimaxMethod>(method);
            var iterations = parseResult.GetValue(iterationOption);
            var repeat = parseResult.GetValue(repeatOption);

            var logger = factory.CreateLogger<MinimaxBenchSamples>();
            if (all) {
                RunAllCasesAndMethods(logger, iterations, repeat, outputDir);
            } else {
                var res = Optimize(GetProblem(ca), m, logger, iterations);
                res.ProblemName = ca;
                string json = JsonSerializer.Serialize(res);
                string fileName = $"{ca}_{method}.json";
                File.WriteAllText(Path.Combine(outputDir, fileName), json);
            }
        });

        return command1;
    }
}