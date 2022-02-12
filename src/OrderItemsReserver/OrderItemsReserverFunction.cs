using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using OrderItemsReserver.Models;

namespace OrderItemsReserver
{
    public static class Function1
    {
        // Can also fetch from App Settings or environment variable
        private static readonly string logicAppUri = @"https://prod-64.eastus.logic.azure.com:443/workflows/395587a9ecd54129a08b998275fd508c/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=3NzRMHHWWomQqqtCViKgkcpJ2kmupk2TtC3A-ulfaII";

        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("OrderItemsReserverFunction")]
        public static async Task RunAsync([ServiceBusTrigger("queue1", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            try
            {
                Order order = JsonConvert.DeserializeObject<Order>(myQueueItem);
                string id = order.Id;

                var connectionString = "DefaultEndpointsProtocol=https;AccountName=eshoponweb;AccountKey=5ksUKo/s8oTsCaMW5++yHx6TBGTnnLyg2qR9NVmA4B0viIMfbqBirHyOxx4ZsRnDBWKTfgygHfylyvbuliZNsg==;EndpointSuffix=core.windows.net";
                var fileContainerName = "sample-container";

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(fileContainerName);
                await blobContainer.CreateIfNotExistsAsync();
                CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference("Order-" + id + ".json");

                var optionsWithRetryPolicy = new BlobRequestOptions()
                {
                    RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(5), 3)
                };
                await blockBlob.UploadTextAsync(myQueueItem, null, optionsWithRetryPolicy, null);
            }
            catch (Exception)
            {
                log.LogError($"Error with message: {myQueueItem}");
                await SendEmail(myQueueItem, log);
            }
        }

        private static async Task SendEmail(string myQueueItem, ILogger log)
        {
            try
            {
                var response = await httpClient.PostAsync(logicAppUri, new StringContent(myQueueItem, Encoding.UTF8, "application/json"));
            }
            catch (Exception)
            {
                log.LogError($"Error with http request");
            }
        }
    }
}
