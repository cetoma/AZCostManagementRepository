using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AZCostManagementMC.Helpers;
using AZCostManagementMC.Models;

namespace AZCostManagementMC
{
    public static class GetCosts
    {

        [FunctionName("GetCosts")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("Iniciando el llamado para la exportación de costos");
            var Storage = new StorageAccountSettings();
            var config = ConfigHelper.BuildConfig(context, log);

            Storage.Name = config["AccountName"].Trim();
            Storage.Key = config["AccountKey"].Trim();
            Storage.ContainerName = config["ContainerName"].Trim().ToLower();
            string ConnectionString = config["ConnectionStrings:AzureSQL"].Trim();
            
            CostManagementConfig costManagementConfig = await ReadRequestPropertiesAsync(req);
            log.LogInformation($"Se ha obtenido el periodo {costManagementConfig.Period}");
            var CurrentDate = DateHelper.GetDate().AddMonths(costManagementConfig.Period);

            string PeriodName = CurrentDate.ToString("yyyyMM");

            string token = await AuthenticationHelper.GetAccessToken(costManagementConfig.AppReg, log);
            var json = await CallExport(token, costManagementConfig.BillID, PeriodName);
            InsertParameters parameters = new()
            {
                PeriodName = PeriodName,
                Customer = costManagementConfig.Customer,
                URL = json
            };
            int Identity = await DBHelper.InsertLog(ConnectionString, parameters);
            var costManagementLog = new CostManagementLog() { 
                 ID = Identity
                ,URL = json
                ,Customer = costManagementConfig.Customer
                ,PeriodName = PeriodName
            };
            await CallHelper.CheckExport(costManagementLog, Storage, token, ConnectionString, log);
            
            string responseMessage = "Se ha ejecutado la operación de exportación";
            return new OkObjectResult(responseMessage);
        }

        private static async Task<string> CallExport(string token, string billingAccountId, string PeriodName)
        {
            Console.WriteLine("Se inicia la obtención de entidades");
            string URI = $"https://management.azure.com/providers/Microsoft.Billing/billingAccounts/{billingAccountId}/providers/Microsoft.CostManagement/generateCostDetailsReport?api-version=2022-05-01";
            var data = new
            {
                metric = "ActualCost",
                billingPeriod = $"{PeriodName}"
            };
            var body = JsonConvert.SerializeObject(data);
            var HttpsResponse = await ResponseHelper.PostBodyResponse(token, URI, body);
            if (string.IsNullOrWhiteSpace(HttpsResponse.Message))
            {
                Console.WriteLine("El sistema esta procesando. Se revisará el estado un poco más tarde.");
                return HttpsResponse.Location;
            }
            else
            {
                return HttpsResponse.Message;
            }
        }

        private static async Task<CostManagementConfig> ReadRequestPropertiesAsync(HttpRequest req)
        {
            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string Period = data?.Period;
            string BillID = data?.BillID;
            string CustomerValue = data?.Customer;
            string TenantIDValue = data?.TenantID;
            string ClientIDValue = data?.ClientID;
            string SecretValue = data?.SecretValue;
            if (!int.TryParse(Period, out int PeriodValue))
                PeriodValue = 0;
            //Verifica quue el Periodo no sea mayor a 3 meses por limitaciones de API 
            if (PeriodValue > 3)
                PeriodValue = 3;

            PeriodValue = -Math.Abs(PeriodValue);

            var CostManagementConfig = new CostManagementConfig()
            {
                Period = PeriodValue
                ,Customer = CustomerValue
                ,BillID = BillID
                ,AppReg = new AzureAppReg()
                {
                     TenantID = TenantIDValue
                    ,ClientID = ClientIDValue
                    ,SecretValue = SecretValue
                }
            };
            return CostManagementConfig;
        }
    }
}
