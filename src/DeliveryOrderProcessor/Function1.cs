using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using DeliveryOrderProcessor.Models;

namespace DeliveryOrderProcessor
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DeliveryOrderProcessor: C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Order order = JsonConvert.DeserializeObject<Order>(requestBody);
            string id = order.Id;

            string databaseName = "Orders";
            string containerName = "Items";
            string account = "https://eshoponweb.documents.azure.com:443/";
            string key = "01jkHso7acab4v8UkyhXPVaEgEvRKRLkjJJKi1oYZstRDVFn0Ydm435DElwLQoqg6nsPwg8DRChuXygY6eeuQQ==";
            CosmosClient cosmosClient = new CosmosClient(account, key);
            DatabaseResponse database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

            order.Total = order.GetTotal();

            Container cosmosContainer = cosmosClient.GetContainer(databaseName, containerName);
            await cosmosContainer.CreateItemAsync(order, new PartitionKey(id));

            return new OkObjectResult($"Order {id} sent");
        }
    }
}
