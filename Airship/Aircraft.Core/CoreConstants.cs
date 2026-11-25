namespace Aircraft.Core;

public static class CoreConstants {
    public const double M_Air = 28.9644E-3, M_He = 4.003E-3;
    public const double R = 8.31432;
    public const double RAir = R / M_Air; // m/(kg•K)
    public const double gamma = 1.4;
    public const double c_He = 5193; // 氦气比热容

    public const double g = 9.80665;  //Acceleration due to gravity   
    public const double EarthRadius = 6371.0E+3;
    public const double LightSpeed = 299792458;
}