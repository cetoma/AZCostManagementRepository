using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AZCostManagementMC.Models;
using AZCostManagementMC.Helpers;

namespace AZCostManagementMC
{
    public static class CheckProgress
    {
        [FunctionName("CheckProgress")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("Revisando status de la exportación");
            var Storage = new StorageAccountSettings();
            var AppReg = new AzureAppReg();
            var config = ConfigHelper.BuildConfig(context, log);
            Storage.Name = config["AccountName"].Trim();
            Storage.Key = config["AccountKey"].Trim();
            Storage.ContainerName = config["ContainerName"].Trim().ToLower();
            string ConnectionString = config["ConnectionStrings:AzureSQL"].Trim();
            CostManagementConfig costManagementConfig = await ReadRequestPropertiesAsync(req);
            var costManagementLog = await DBHelper.GetLog(ConnectionString,costManagementConfig.Customer);

            if (costManagementLog == null || string.IsNullOrWhiteSpace(costManagementLog.Path))
                return new BadRequestObjectResult("No se ha encontrado el valor del archivo copiandose");
            bool finished;

            switch (costManagementLog.Status)
            {
                case 1:
                    string token = await AuthenticationHelper.GetAccessToken(AppReg, log);

                    await CallHelper.CheckExport(costManagementLog, Storage, token, ConnectionString, log);

                    finished = await StorageHelper.CheckStatus(Storage, costManagementLog.Path, log);
                    if (finished)
                        await DBHelper.CompleteLog(ConnectionString, costManagementLog.ID);
                    else
                        return new BadRequestObjectResult("Error en el proceso");
                    break;

                case 2:
                    finished = await StorageHelper.CheckStatus(Storage, costManagementLog.Path, log);

                    if (finished)
                        await DBHelper.CompleteLog(ConnectionString, costManagementLog.ID);
                    else
                        return new BadRequestObjectResult("Error en el proceso");
                    break;

                default:
                    log.LogInformation("No se encontró un proceso al cual hacer seguimiento");
                    break;
            }

            return new OkObjectResult("Se termina el proceso de revisión de copia");
        }

        private static async Task<CostManagementConfig> ReadRequestPropertiesAsync(HttpRequest req)
        {
            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string CustomerValue = data?.Customer;
            string TenantIDValue = data?.TenantID;
            string ClientIDValue = data?.ClientID;
            string SecretValue = data?.SecretValue;
   
            var CostManagementConfig = new CostManagementConfig()
            {
                Period = 0
                ,
                Customer = CustomerValue
                ,
                AppReg = new AzureAppReg()
                {
                    TenantID = TenantIDValue
                    ,
                    ClientID = ClientIDValue
                    ,
                    SecretValue = SecretValue
                }
            };
            return CostManagementConfig;
        }
    }
}
