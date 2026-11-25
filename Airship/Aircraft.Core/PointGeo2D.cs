using System.Numerics;
using System.Text.Json.Serialization;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;

namespace Aircraft.Core;

/// <summary>
/// 经纬度坐标
/// </summary>
/// <param name="Lat">纬度</param>
/// <param name="Lon">经度</param>
public readonly record struct PointGeo2D(double Lat, double Lon) {
    public const double RadiusRef = CoreConstants.EarthRadius;
    public PointGeo2D(Angle latitude, Angle longitude) : this(latitude.Degrees, longitude.Degrees) { }
    public PointGeo2D(Vector2 vector) : this(vector.X, vector.Y) { }
    public PointGeo2D(Point2D point) : this(point.X, point.Y) { }
    public readonly static PointGeo2D Zero = new(0, 0);
    [JsonIgnore] public Angle Latitude => Angle.FromDegrees(Lat);
    [JsonIgnore] public Angle Longitude => Angle.FromDegrees(Lon);
    [JsonIgnore] public Vector2 Vector2 => new((float)Lat, (float)Lon);

    public static double SphereDistance(PointGeo2D x, PointGeo2D y) {
        Angle dlat = x.Latitude - y.Latitude, dlon = x.Longitude - y.Longitude;
        double sindlat_2 = Sin(dlat.Radians / 2), sinlon_2 = Sin(dlon.Radians / 2);
        double a = sindlat_2 * sindlat_2 + Cos(Angle.FromDegrees(x.Lat).Radians) * Cos(Angle.FromDegrees(y.Lat).Radians) * sinlon_2 * sinlon_2;
        return 2 * Atan2(Sqrt(a), Sqrt(1 - a)) * RadiusRef;
    }

    public static double SphereAngle(PointGeo2D x, PointGeo2D y) {
        Angle dlat = x.Latitude - y.Latitude, dlon = x.Longitude - y.Longitude;
        double sindlat_2 = Sin(dlat.Radians / 2), sinlon_2 = Sin(dlon.Radians / 2);
        double a = sindlat_2 * sindlat_2 + Cos(Angle.FromDegrees(x.Lat).Radians) * Cos(Angle.FromDegrees(y.Lat).Radians) * sinlon_2 * sinlon_2;
        return 2 * Atan2(Sqrt(a), Sqrt(1 - a));
    }

    public override string ToString() {
        string lat = Lat >= 0 ? "N" : "S";
        string lon = Lon >= 0 ? "E" : "W";
        return $"{10 * Abs(Lat):0}{lat}{10 * Abs(Lon):0}{lon}";
    }
    /// <summary>
    /// 用于快速判断距离，角度(rad)的L2-norm
    /// </summary>
    public double DistanceL2_rad(PointGeo2D y) {
        Angle dlat = Latitude - y.Latitude, dlon = Longitude - y.Longitude;

        double dx = AngleUtil.NormalizeAngle(dlat.Radians);
        double dy = AngleUtil.NormalizeAngle(dlon.Radians);
        return new Vector2D(dx, dy).Length;
    }

    public Point2D ToPoint2D() => new(Lat, Lon);

    public static PointGeo2D operator +(PointGeo2D p0, Vector2D delta) => new(p0.Lat + delta.X, p0.Lon + delta.Y);
}

