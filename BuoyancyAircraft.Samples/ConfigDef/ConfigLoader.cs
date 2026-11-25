using DotNetEnv;
using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration;

namespace BuoyancyAircraft.Samples.ConfigDef;

public sealed class ConfigLoader {
    public static IConfiguration Load(string jsonFile = "") {


        var builder = new ConfigurationBuilder();
        builder.AddDotNetEnv(".env", LoadOptions.TraversePath());

        if (jsonFile != "") {
            builder.AddJsonFile(jsonFile);
        }

        return builder.Build();

    }
}