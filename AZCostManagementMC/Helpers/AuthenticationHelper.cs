using AZCostManagementMC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace AZCostManagementMC.Helpers
{
    public class AuthenticationHelper
    {
        public static async Task<string> GetAccessToken(AzureAppReg AppReg,ILogger Log)
        {
            Log.LogInformation("Obteniendo token de autenticación");
            string Authority = $"https://login.windows.net/{AppReg.TenantID}/v2.0";
            var app = ConfidentialClientApplicationBuilder.Create(AppReg.ClientID)
                .WithClientSecret(AppReg.SecretValue)
                .WithAuthority(new Uri(Authority))
                .Build();
            var scopes = new[] { "https://management.azure.com/.default" };
            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }
    }
}
