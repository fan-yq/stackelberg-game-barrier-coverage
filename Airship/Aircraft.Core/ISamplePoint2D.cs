using MathNet.Spatial.Euclidean;

namespace Aircraft.Core;

public interface ISamplePoint2D {
    public Point2D Sample(Random random);
}