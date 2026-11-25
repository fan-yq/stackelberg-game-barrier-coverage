using MathNet.Spatial.Euclidean;

namespace Aircraft.Core;


public static class DiskSampling {
    /// <summary>
    /// 使用同心圆映射法在单位圆盘内均匀采样
    /// </summary>
    /// <param name="random">随机数生成器</param>
    /// <returns>圆盘内的点坐标(x,y)</returns>
    public static Vector2D ConcentricSampleDisk(this Random random) {
        double u = random.NextDouble() * 2 - 1;
        double v = random.NextDouble() * 2 - 1;

        if (u == 0 && v == 0)
            return new(0, 0);

        // 核心变换：使用代数关系避免三角函数
        double r, cosTheta, sinTheta;

        if (u * u > v * v) {
            r = u;
            cosTheta = 1.0 / Math.Sqrt(1 + (v * v) / (u * u));
            sinTheta = (v / u) * cosTheta;
        } else {
            r = v;
            sinTheta = 1.0 / Math.Sqrt(1 + (u * u) / (v * v));
            cosTheta = (u / v) * sinTheta;
        }

        return new(r * cosTheta, r * sinTheta);
    }
}

public static class RbfFunctions {
    // 定义第一个径向基函数：高斯函数
    public static double Gaussian(double r, double epsilon = 1.0) {
        return Exp(-(epsilon * r) * (epsilon * r));
    }

    // 定义第二个径向基函数：多重二次曲面函数
    public static double MultiQuadratic(double r, double epsilon = 1.0) {
        return Sqrt(1 + (epsilon * r) * (epsilon * r));
    }
}