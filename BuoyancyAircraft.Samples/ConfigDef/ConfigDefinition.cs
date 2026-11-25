using Microsoft.Extensions.Configuration;

namespace BuoyancyAircraft.Samples.ConfigDef;

public sealed class ConfigDefinition {
    public string OutputPath { get; set; } = "";

    public static ConfigDefinition Create(IConfiguration configuration) {
        ConfigDefinition configDefinition = new();
        configuration.Bind(configDefinition);
        return configDefinition;
    }
}