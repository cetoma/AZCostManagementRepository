using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace AZCostManagementMC.Helpers
{
    public static class ConfigHelper
    {
        public static IConfigurationRoot BuildConfig(ExecutionContext context, ILogger log)
        {
            try
            {
                log.LogInformation("Obteniendo los valores de configuración");
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
                return config;
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error building config. Error: {ex}");
            }
            return null;
        }
    }
}
