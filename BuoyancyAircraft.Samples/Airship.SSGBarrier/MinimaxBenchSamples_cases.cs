using Airship.Deployment.SSGBarrier;
using MathNet.Numerics.LinearAlgebra;

namespace BuoyancyAircraft.Samples.Airship.SSGBarrier;

public enum MinimaxMethod {
    ACGA, SGDE, MMDE, SGDECrowding, PCGA, ACGACrowding,
    SGDEPabestRandHalf, SGDEPabestDisable, SGDEClamp, SGDEAttPop1,
    SGDENoAttUpdate
}
public sealed partial class MinimaxBenchSamples {
    delegate double MinimaxObjectiveFn(Vector<double> x, Vector<double> s);
    sealed record MinimaxProblem(MinimaxObjectiveFn Fn, double[] xl, double[] xu, double[] sl, double[] su, double[][]? optimalX, double[][]? optimalY) {
        public MinimaxProblem(MinimaxObjectiveFn Fn, double[] xl, double[] xu, double[] sl, double[] su, double[]? optimalX = null, double[]? optimalY = null)
        : this(Fn, xl, xu, sl, su, optimalX is null ? null : [optimalX], optimalY is null ? null : [optimalY]) { }
        public int DimAtt => sl.Length;
        public int DimDef => xl.Length;
        public ObjectiveFunctionGame ObjectiveFnGame() {
            return (x, y) => -Fn(x, y);
        }

        public double? OptimalValue() {
            if (optimalX is not null && optimalX.Length > 0 && optimalY is not null && optimalY.Length > 0) {
                // Return the value for the first optimal solution
                return -Fn(Vector<double>.Build.Dense(optimalX[0]), Vector<double>.Build.Dense(optimalY[0]));
            }
            return null;
        }

        // Calculate MSE between Xbest and optimal X solutions
        public double CalculateMSE(IList<double> xBest) {
            if (optimalX == null || optimalX.Length == 0) {
                return double.NaN; // No optimal solution available
            }

            // Find the optimal X that is closest to xBest
            double minMSE = double.MaxValue;
            foreach (var xOpt in optimalX) {
                double mse = 0;
                for (int j = 0; j < DimDef; j++) {
                    double diff = xBest[j] - xOpt[j];
                    mse += diff * diff;
                }
                mse /= DimDef; // Average over dimensions
                if (mse < minMSE) {
                    minMSE = mse;
                }
            }
            return minMSE;
        }
    }


    static MinimaxProblem F1() {
        double f1(Vector<double> x, Vector<double> s) {
            double x1 = x[0], s1 = s[0];
            return (x1 - 5) * (x1 - 5) - (s1 - 5) * (s1 - 5);
        }
        return new(f1, [0], [10], [0], [10], [5], [5]);
    }

    static MinimaxProblem F2() {
        double f(Vector<double> x, Vector<double> s) {
            double xVal = x[0], sVal = s[0];
            double term1 = 3 - 0.2 * xVal + 0.3 * sVal;
            double term2 = 3 + 0.2 * xVal - 0.1 * sVal;
            return Math.Min(term1, term2);
        }
        return new(f, new double[] { 0 }, new double[] { 10 }, new double[] { 0 }, new double[] { 10 }, [0], [0]);
    }

    static MinimaxProblem F3() {
        double f(Vector<double> x, Vector<double> s) {
            double xVal = x[0], sVal = s[0];
            double numerator = Math.Sin(xVal - sVal);
            double denominator = Math.Sqrt(xVal * xVal + sVal * sVal);
            // 避免分母为0（实际优化中概率极低，此处做防御）
            if (denominator < 1e-9) return 0;
            return numerator / denominator;
        }
        return new(f, new double[] { 0 }, new double[] { 10 }, new double[] { 0 }, new double[] { 10 }, [10], [2.125683]);
    }

    static MinimaxProblem F4() {
        double f(Vector<double> x, Vector<double> s) {
            double xVal = x[0], sVal = s[0];
            double r = Math.Sqrt(xVal * xVal + sVal * sVal);
            double numerator = Math.Cos(r);
            double denominator = r + 10;
            return numerator / denominator;
        }
        return new(f, new double[] { 0 }, new double[] { 10 }, new double[] { 0 }, new double[] { 10 }, [[7.04424633375], [7.04424633375]], [[10], [0]]); // 0 or 10
    }

    static MinimaxProblem F5() {
        double f(Vector<double> x, Vector<double> s) {
            double x1 = x[0], x2 = x[1];
            double s1 = s[0], s2 = s[1];
            return 100 * Math.Pow(x2 - x1 * x1, 2) + Math.Pow(1 - x1, 2)
                   - s1 * (x1 + x2 * x2) - s2 * (x1 * x1 + x2);
        }
        return new(
            f,
            new double[] { -0.5, 0 },   // xl: x1下限, x2下限
            new double[] { 0.5, 1 },    // xu: x1上限, x2上限
            new double[] { 0, 0 },      // sl: s1下限, s2下限
            new double[] { 10, 10 },     // su: s1上限, s2上限
            [0.5, 0.25],
            [0, 0]
        );
    }

    static MinimaxProblem F6() {
        double f(Vector<double> x, Vector<double> s) {
            double x1 = x[0], x2 = x[1];
            double s1 = s[0], s2 = s[1];
            return 100 * Math.Pow(x1 - 2, 2) + Math.Pow(x2 - 1, 2)
                   + s1 * (x1 * x1 - x2) + s2 * (x1 + x2 - 2);
        }
        return new(
            f,
            new double[] { -1, -1 },    // xl: x1下限, x2下限
            new double[] { 3, 3 },      // xu: x1上限, x2上限
            new double[] { 0, 0 },      // sl: s1下限, s2下限
            new double[] { 10, 10 },     // su: s1上限, s2上限
            [1, 1],
            [0, 0] // any
        );
    }

    static MinimaxProblem F7() {
        const int Dim = 20;
        double f(Vector<double> x, Vector<double> s) {
            double sumX = 0, sumS = 0;
            for (int d = 0; d < Dim; d++) {
                sumX += Math.Pow(x[d] - 5, 2);
                sumS += Math.Pow(s[d] - 5, 2);
            }
            return sumX - sumS;
        }
        // 生成20维全0/全10数组
        double[] zeros = new double[Dim], tens = new double[Dim], fives = new double[Dim];
        Array.Fill(zeros, 0);
        Array.Fill(tens, 10);
        Array.Fill(fives, 5);
        return new(f, zeros, tens, zeros, tens, fives, fives);
    }

    static MinimaxProblem F8() {
        const int Dim = 20;
        double f(Vector<double> x, Vector<double> s) {
            double total = 0;
            for (int d = 0; d < Dim; d++) {
                double xd = x[d], sd = s[d];
                double term1 = 3 - 0.2 * xd + 0.3 * sd;
                double term2 = 3 + 0.2 * xd - 0.1 * sd;
                total += Math.Min(term1, term2);
            }
            return total;
        }
        double[] zeros = new double[Dim], tens = new double[Dim];
        Array.Fill(zeros, 0);
        Array.Fill(tens, 10);
        return new(f, zeros, tens, zeros, tens, zeros, zeros);
    }

    static MinimaxProblem F9() {
        double f(Vector<double> x, Vector<double> s) {
            double x1 = x[0], x2 = x[1], x3 = x[2];
            double s1 = s[0], s2 = s[1], s3 = s[2];
            return -(x1 - 1) * s1 - (x2 - 2) * s2 - (x3 - 1) * s3
                   + 2 * x1 * x1 + 3 * x2 * x2 + x3 * x3
                   - s1 * s1 - s2 * s2 - s3 * s3;
        }
        return new(
            f,
            new double[] { -5, -5, -5 },   // xl: x1~x3下限
            new double[] { 5, 5, 5 },      // xu: x1~x3上限
            new double[] { -1, -1, -1 },   // sl: s1~s3下限
            new double[] { 1, 1, 1 },      // su: s1~s3上限
            [0.111111111, 0.15384615, 0.2], [0.4444444, 0.9230769, 0.4]
        );
    }

    static MinimaxProblem F10() {
        double f(Vector<double> x, Vector<double> s) {
            double x1 = x[0], x2 = x[1], x3 = x[2], x4 = x[3];
            double s1 = s[0], s2 = s[1], s3 = s[2];
            double polyPart = s1 * (x1 * x1 - x2 + x3 - x4 + 2)
                            + s2 * (-x1 + 2 * x2 * x2 - x3 * x3 + 2 * x4 + 1)
                            + s3 * (2 * x1 - x2 + 2 * x3 - x4 * x4 + 5);
            double quadPart = 5 * x1 * x1 + 4 * x2 * x2 + 3 * x3 * x3 + 2 * x4 * x4;
            double scenePart = -(s1 * s1 + s2 * s2 + s3 * s3);
            return polyPart + quadPart + scenePart;
        }
        return new(
            f,
            new double[] { -5, -5, -5, -5 },   // xl: x1~x4下限
            new double[] { 5, 5, 5, 5 },       // xu: x1~x4上限
            new double[] { -2, -2, -2 },       // sl: s1~s3下限
            new double[] { 2, 2, 2 },           // su: s1~s3上限
            [-0.23156024, 0.2228109, -0.67552026, -0.0837693], [0.6195291, 0.3534917, 1.4780054]
        );
    }

    static MinimaxProblem F11() {
        const int Dim = 5;
        double f(Vector<double> x, Vector<double> s) {
            double x1 = x[0], x2 = x[1], x3 = x[2], x4 = x[3], x5 = x[4];
            double s1 = s[0], s2 = s[1], s3 = s[2], s4 = s[3], s5 = s[4];

            // 固定项：乘积与平方
            double fixedTerm = 2 * x1 * x5 + 3 * x4 * x2 + x5 * x3 + 5 * x4 * x4 + 5 * x5 * x5;
            // 场景交互项
            double sceneInteract = -x4 * (s4 - s5 - 5) + x5 * (s4 - s5 + 3);
            // 求和项：Σ[s_d(x_d²-1)]
            double sumSceneVar = 0;
            for (int d = 0; d < Dim; d++) sumSceneVar += s[d] * (x[d] * x[d] - 1);
            // 场景平方和：Σs_d²
            double sumSceneSq = s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4 + s5 * s5;

            return fixedTerm + sceneInteract + sumSceneVar - sumSceneSq;
        }
        double[] negFive = new double[Dim], five = new double[Dim];
        double[] negThree = new double[Dim], three = new double[Dim];
        Array.Fill(negFive, -5);
        Array.Fill(five, 5);
        Array.Fill(negThree, -3);
        Array.Fill(three, 3);
        return new(f, negFive, five, negThree, three,
        [1.425207, 1.66122949, 1.2585294, -0.97441266, -0.73485080], [0.5156094, 0.8798417, 0.2919482, 0.1197809, -0.1197809]);
    }

    static MinimaxProblem F12() {
        const int Dim = 28;
        double f(Vector<double> x, Vector<double> s) {
            double total = 0;
            for (int d = 0; d < Dim; d++) {
                double xd = x[d], sd = s[d];
                // Rastrigin项：x部分 + s部分
                double rastriginX = xd * xd - 10 * Math.Cos(2 * Math.PI * xd) + 10;
                double rastriginS = sd * sd - 10 * Math.Cos(2 * Math.PI * sd) + 10;
                total += rastriginX + rastriginS;
            }
            return total;
        }
        double[] negFive = new double[Dim], five = new double[Dim];
        double[] negThree = new double[Dim], three = new double[Dim];
        Array.Fill(negFive, -5);
        Array.Fill(five, 5);
        Array.Fill(negThree, -3);
        Array.Fill(three, 3);
        return new(f, negFive, five, negThree, three);

    }



    static MinimaxProblem GetProblem(string name) {
        // 忽略大小写匹配，增强鲁棒性
        switch (name.ToUpperInvariant()) {
            case "F1": return F1();
            case "F2": return F2();
            case "F3": return F3();
            case "F4": return F4();
            case "F5": return F5();
            case "F6": return F6();
            case "F7": return F7();
            case "F8": return F8();
            case "F9": return F9();
            case "F10": return F10();
            case "F11": return F11();
            case "F12": return F12();
            default:
                throw new ArgumentException($"Unknown problem name: '{name}'. Available cases are F1-F12.");
        }
    }

}