using AZCostManagementMC.Models;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AZCostManagementMC.Helpers
{
    public class StorageHelper
    {
        public static async Task<bool> CheckStatus(StorageAccountSettings Storage, string path, ILogger Log)
        {
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={Storage.Name};AccountKey={Storage.Key};EndpointSuffix=core.windows.net";
            string containerName = Storage.ContainerName;
            BlobContainerClient container = new(connectionString, containerName);
            bool ContainerExists = await container.ExistsAsync();
            bool IsCopying = true;
            if (ContainerExists)
            {
                BlobClient blob = container.GetBlobClient(path);
                while (IsCopying)
                {
                    BlobProperties destProperties = await blob.GetPropertiesAsync();
                    Log.LogInformation($"Copy status: {destProperties.CopyStatus}");
                    Log.LogInformation($"Copy progress: {destProperties.CopyProgress}");
                    IsCopying = destProperties.BlobCopyStatus == CopyStatus.Pending;
                    Thread.Sleep(5000);
                }
            }
            return !IsCopying;
        }

        public static async Task<string> SavetoLocal(string url, StorageAccountSettings storage, string ArchiveSuffix, ILogger Log)
        {
            Console.WriteLine("Copiando...");
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storage.Name};AccountKey={storage.Key};EndpointSuffix=core.windows.net";
            string containerName = storage.ContainerName;
            string path = $"AzureCost/Details{ArchiveSuffix}.csv";
            try
            {
                BlobContainerClient Destinycontainer = new(connectionString, containerName);
                Destinycontainer.CreateIfNotExists();
                
                BlobClient destBlob = Destinycontainer.GetBlobClient(path);
                Log.LogInformation("Iniciando la copia, esta copia puede tardar unos minutos...");
                await destBlob.StartCopyFromUriAsync(new Uri(url));
               
            }
            catch (RequestFailedException ex)
            {
                Log.LogError(ex.Message);
            }
            return path;
        }
    }
}
