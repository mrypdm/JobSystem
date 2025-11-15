using Microsoft.Extensions.Configuration;

namespace Shared.Contract.Extensions;

/// <summary>
/// Extensions for Configuration
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Get options from configuration
    /// </summary>
    public static TOptions GetOptions<TOptions>(this IConfiguration config, string section = null)
    {
        return config.GetSection(section ?? typeof(TOptions).Name).Get<TOptions>();
    }
}
