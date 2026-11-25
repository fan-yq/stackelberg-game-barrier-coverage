using Aircraft.Core.ConvexHull;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using Vector2 = System.Numerics.Vector2;
namespace Aircraft.Core;

public static class TypeConventions {
    public static (Angle latitude, Angle longitude) PToLL(Point2D p) => (Angle.FromDegrees(p.X), Angle.FromDegrees(p.Y));
    public static (Angle latitude, Angle longitude) PToLL(Vector2 p) => (Angle.FromDegrees(p.X), Angle.FromDegrees(p.Y));

    public static float[][][] ConvexHullsToFloatArray(ConvexHullF[] hulls) {
        var hullsArray = new float[hulls.Length][][];
        for (int i = 0; i < hulls.Length; i++) {
            hullsArray[i] = hulls[i].ToArray();
        }
        return hullsArray;
    }

    public static float[][] ToJagArray(float[,] values) {
        int rows = values.GetLength(0);
        int cols = values.GetLength(1);
        var result = new float[rows][];

        for (int i = 0; i < rows; i++) {
            result[i] = new float[cols];
            for (int j = 0; j < cols; j++) {
                result[i][j] = values[i, j];
            }
        }
        return result;
    }

    public static double[][] PointsToArray(Point2D[] points) {
        int N = points.Length;
        var x = new double[N][];
        for (int i = 0; i < N; ++i) {
            x[i] = [points[i].X, points[i].Y];
        }
        return x;
    }

    public static double[][] VectorsToArray(Vector<double>[] vectors) {
        int N = vectors.Length;
        var x = new double[N][];
        for (int i = 0; i < N; ++i) {
            x[i] = vectors[i].ToArray();
        }
        return x;
    }

    public static double[][] MatrixToArray(Matrix<double> matrix) {
        int N = matrix.RowCount;
        var x = new double[N][];
        for (int i = 0; i < N; ++i) {
            x[i] = matrix.Row(i).ToArray();
        }
        return x;
    }
}


public static class AngleUtil {
    /// <summary>
    /// [-pi,pi]
    /// </summary>
    public static double NormalizeAngle(double a) {
        long modPsi = (long)(a / 2 / PI);
        a -= modPsi * PI * 2;
        if (a > PI) { a -= 2 * PI; }
        if (a < -PI) { a += 2 * PI; }
        return a;
    }

    public static double NormalizeAngleDegree(double a) {
        double a1 = NormalizeAngle(Angle.FromDegrees(a).Radians);
        return Angle.FromRadians(a1).Degrees;
    }

    public static double NearestAngle(double expect, double refAngle) {
        double delta = expect - refAngle;
        double clmapDelta = NormalizeAngle(delta);
        return refAngle + clmapDelta;
    }
}


public static class LogUtils {
    public static string WriteArray(IEnumerable<double> state) => string.Join(",", state);
    public static string WriteArrayG3(IEnumerable<double> state) => string.Join(",", state.Select(s => $"{s:G3}"));
    public static string WritePoints(IEnumerable<Point2D> points) => string.Join(",", points);
    public static string WritePointGeo2Ds(IEnumerable<PointGeo2D> points) => string.Join(",", points);


}