using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace Airship.Deployment.SSGBarrier;

public sealed record ACGAResult(Vector<double>[] PopDefender, Vector<double>[] PopAttacker, Matrix<double> UMatrix, List<int> SelectDef, List<int> SelectAtt, ACGAMode Mode) {
    public double MaxMinU() => MaxMinU(UMatrix, SelectDef);
    public double MinMaxU() => MinMaxU(UMatrix, SelectAtt);
    public List<int> BestDefenderIndices(double eps) {
        return EpsBestDefenderIndices(eps, UMatrix);
    }
    public List<int> BestAttackerIndices(double eps) {
        List<int> idx = [];
        double mmU;
        if (Mode is ACGAMode.Stackelberg) {
            var bestDefIndices = BestDefenderIndices(eps);
            foreach (var i in bestDefIndices) {
                double rowMin = UMatrix.Row(i).Min();
                for (int j = 0; j < PopAttacker.Length; ++j) {
                    if (UMatrix[i, j] <= rowMin + eps) {
                        idx.Add(j);
                    }
                }
            }
            idx = idx.Distinct().ToList();
        } else {
            mmU = MinMaxU();
            for (int j = 0; j < PopAttacker.Length; ++j) {
                double colMax = UMatrix.Column(j).Max();
                if (colMax <= mmU + eps) {
                    idx.Add(j);
                }
            }
        }
        return idx;
    }

    public static double MaxMinU(Matrix<double> umatrix, IEnumerable<int> selectDef) {
        double ud = double.MinValue;
        foreach (var i in selectDef) {
            double rowMin = umatrix.Row(i).Min();
            ud = Max(ud, rowMin);
        }
        return ud;
    }
    public static double MinMaxU(Matrix<double> umatrix, IEnumerable<int> selectAtt) {
        double ud = double.MaxValue;
        // for (int j = 0; j < umatrix.ColumnCount; ++j) {
        foreach (var j in selectAtt) {
            double colMax = umatrix.Column(j).Max();
            ud = Min(ud, colMax);
        }
        return ud;
    }


    public static List<int> EpsBestDefenderIndices(double eps, Matrix<double> UMatrix) {
        List<int> idx = [];
        int N = UMatrix.RowCount;
        double mmU = MaxMinU(UMatrix, Enumerable.Range(0, N));
        for (int i = 0; i < N; ++i) {
            double rowMin = UMatrix.Row(i).Min();
            if (rowMin >= mmU - eps) {
                idx.Add(i);
            }
        }
        return idx;
    }
}