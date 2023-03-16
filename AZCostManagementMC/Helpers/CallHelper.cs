using AZCostManagementMC.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AZCostManagementMC.Helpers
{
    public class CallHelper
    {
        public static async Task<string> GetStatus(string token, string uri,ILogger Log)
        {
            Log.LogInformation("Obteniendo el estado de la operación de exportación");
            var HttpsResponse = await ResponseHelper.GetResponse(token, uri);
            if (string.IsNullOrWhiteSpace(HttpsResponse))
            {
                Log.LogInformation("El sistema sigue procesando. Se debe esperar más");
                return uri;
            }
            else
            {
                return HttpsResponse;
            }
        }

        public static async Task CheckExport(CostManagementLog costManagementLog, StorageAccountSettings Storage, string token, string connectionString, ILogger log)
        {
            string json = costManagementLog.URL;

            while (json.StartsWith("https://"))
            {
                Console.WriteLine("Revisar avance de la exportación cada 20 segundos");
                Thread.Sleep(20000);
                json = await GetStatus(token, json, log);
            }
            if (json.StartsWith("https://"))
            {
                log.LogError("Se termino el tiempo de espera, debera volver a ejecutar el proceso");
                return;
            }
            else if (string.IsNullOrWhiteSpace(json))
            {
                log.LogError("No se recibio la respuesta esperada, por favor revise las configuraciones de su 'App Registration'");
                return;
            }

            var JSONObject = JsonConvert.DeserializeObject<CostDetails>(json);
            foreach (var url in JSONObject.Manifest.Blobs)
            {
                log.LogInformation("Copiando el archivo generado a un Blob Local");
                string ArchiveSufix = costManagementLog.Customer + "_" + costManagementLog.PeriodName;
                log.LogInformation($"Se mandara el archivo {ArchiveSufix}");
                string path = await StorageHelper.SavetoLocal(url.BlobLink, Storage,ArchiveSufix, log);
                UpdateParameters updateParameters = new()
                {
                    ID = costManagementLog.ID,
                    Status = 2,
                    URL = url.BlobLink,
                    Path = path
                };
                await DBHelper.UpdateLog(connectionString, updateParameters);
            }
        }

    }
}
