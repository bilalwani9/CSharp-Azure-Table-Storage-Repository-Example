#  C# Azure Table Storage Repository Example, Azure Function(EVENTHUBTRIGGER) reading from Event Hub and  saving messages in persistent Azure Table Storage
 
This is a Sample Azure Function Project developed in C# .Net Core, the project contains EventHubTrigger Azure Function which reads messages from event hub and converts the raw Employee Object with the help of Parser and Mapper and converts the Employee Object to Azure Table TableEntity and finally saves it to Azure Table Storage.


## Prerequisites
To run this project, make sure that you have:

Azure subscription. If you don't have one, [create a free account](https://azure.microsoft.com/en-us/free/) before you begin.

1. Visual Studio 2019) or later.
2. .NET Standard SDK, version 2.0 or later.
3.  Already created Event Hub in Azure, if you haven't created please follow [Create Azure EventHub](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-create)
4.  Already created Azure Table Storage, if you haven't created please follow [Create Azure Table](https://docs.microsoft.com/en-us/azure/storage/tables/table-storage-quickstart-portal)


## Event Hub Trigger Azure Function
In this example EventHubListner is EventHubTrigger Azure Function which Listens to EventHub and IParser<RawEmployee> Parser parses the message and Mapper converts Raw Object to Employee Azure TableEntity Object and AzureTableRepository Saves it into Azure Table.

```
namespace AzureFuncs.BigTable.Example
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using AzureFuns.Common;
    using AzureFuns.Data.Models;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using static AzureFuns.Common.Constants;

    public static class EventHubListner
    {
        [FunctionName(nameof(EventHubListner))]
        public static async Task Run([EventHubTrigger(EventHubName, Connection = EventHubConnectionStringName,
            ConsumerGroup = ConsumerGroupName)] EventData[] events, ILogger log)
        {
            try
            {
                var container = IoCContainer.Create();
                var azureTableRepository = container.GetRequiredService<IAzureTableRepository>();
                var parser = container.GetRequiredService<IParser<RawEmployee>>();
                var mapper = container.GetRequiredService<IMapper<RawEmployee, Employee>>();

                foreach (EventData eventData in events)
                {
                    try
                    {
                        string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                        log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

                        var rawEmployee = parser.Parse(messageBody);

                        log.LogInformation($"DeserializeObject Raw employee Success Id: {rawEmployee.Id}");

                        var employee = mapper.Map(rawEmployee);

                        log.LogInformation($"Mapping Done successfully to employee Table employeeId: {employee.Id}");

                        var result = await azureTableRepository.AddAsync(AzureTableName, employee);

                        if (result != null)
                        {
                            log.LogInformation($"Record Add successfully to Azure Table");
                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Loop Error: {e.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Main Error: {ex.Message}");
            }
        }
    }
}

```
## Azure TableEntity

If you want to insert a row into an Azure Table Storage table the easiest way is to create a new class with the desired fields that inherits from TableEntity perform an insert operation with it using the storage client.

This works fine as long as long as the properties of your class are ones that are supported by the Table Storage client, if you have properties that aren’t supported then the row will insert but without the unsupported properties.

```
namespace AzureFuns.Data.Models
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;

    public class Employee : TableEntity
    {
        public Employee(string partitionKey, Guid guidId)
        {
            PartitionKey = partitionKey;
            RowKey = guidId.ToString();
            Id = guidId;
        }

        public Guid Id { get; private set; }

        public string FirstName { get; set; }

        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string MobileNo { get; set; }
        public string ICNumber { get; set; }
        public DateTime Dob { get; set; }
        public double Salary { get; set; }
        public string Address { get; set; }
    }
}

```

## Azure Table Repository

### Interface
```
namespace AzureFuns.Common
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IAzureTableRepository
    {
        Task<IEnumerable<T>> ReadAllAsync<T>(string tableName) where T : TableEntity, new();
        Task<IEnumerable<T>> QueryAsync<T>(string tableName, TableQuery<T> query) where T : TableEntity, new();
        Task<T> ReadAsync<T>(string tableName, string partitionKey, string rowKey) where T : TableEntity;
        Task<object> UpsertAsync(string tableName, TableEntity entity);
        Task<object> DeleteAsync(string tableName, TableEntity entity);
        Task<object> AddAsync(string tableName, TableEntity entity); 
        Task<object> UpdateAsync(string tableName, TableEntity entity);
    }
}

```

### Implementation

```
namespace AzureFuns.Common
{
    using System.Collections.Concurrent;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class AzureTableRepository : IAzureTableRepository
    {
        private readonly CloudTableClient _cloudTableClient;
        private ConcurrentDictionary<string, CloudTable> _keyValuePairsTables;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableRepository" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public AzureTableRepository(string connectionString)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            _cloudTableClient = account.CreateCloudTableClient();

            _keyValuePairsTables = new ConcurrentDictionary<string, CloudTable>();
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns></returns>
        public async Task<T> ReadAsync<T>(string tableName, string partitionKey, string rowKey) where T : TableEntity
        {
            var table = await CreateTableIfNotExists(tableName).ConfigureAwait(false);

            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            TableResult result = await table.ExecuteAsync(retrieveOperation).ConfigureAwait(false);

            return result.Result as T;
        }

        public async Task<IEnumerable<T>> ReadAllAsync<T>(string tableName) where T : TableEntity, new()
        {
            var table = await CreateTableIfNotExists(tableName).ConfigureAwait(false);

            TableContinuationToken token = null;
            var entities = new List<T>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<T>(), token).ConfigureAwait(false);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return entities;
        }

        /// <summary>
        /// Gets entities by query. 
        /// Supports TakeCount parameter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(string tableName, TableQuery<T> query) where T : TableEntity, new()
        {
            var table = await CreateTableIfNotExists(tableName).ConfigureAwait(false);

            bool shouldConsiderTakeCount = query.TakeCount.HasValue;

            return shouldConsiderTakeCount ?
                await QueryAsyncWithTakeCount(table, query).ConfigureAwait(false) :
                await QueryAsync(table, query).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds the or update entity.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public async Task<object> UpsertAsync(string tableName, TableEntity entity)
        {
            var table = await CreateTableIfNotExists(tableName).ConfigureAwait(false);

            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);

            TableResult result = await table.ExecuteAsync(insertOrReplaceOperation).ConfigureAwait(false);

            return result.Result;
        }

        /// <summary>
        /// Deletes the entity.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public async Task<object> DeleteAsync(string tableName, TableEntity entity)
        {
            var table = await CreateTableIfNotExists(tableName).ConfigureAwait(false);

            TableOperation deleteOperation = TableOperation.Delete(entity);

            TableResult result = await table.ExecuteAsync(deleteOperation).ConfigureAwait(false);

            return result.Result;
        }

        /// <summary>
        /// Add the entity.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public async Task<object> AddAsync(string tableName, TableEntity entity)
        {
            var table = await CreateTableIfNotExists(tableName).ConfigureAwait(false);

            TableOperation insertOperation = TableOperation.Insert(entity);

            TableResult result = await table.ExecuteAsync(insertOperation).ConfigureAwait(false);

            return result.Result;
        }

      
        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public async Task<object> UpdateAsync(string tableName, TableEntity entity)
        {
            var table = await CreateTableIfNotExists(tableName).ConfigureAwait(false);

            TableOperation replaceOperation = TableOperation.Replace(entity);

            TableResult result = await table.ExecuteAsync(replaceOperation).ConfigureAwait(false);

            return result.Result;
        }

        /// <summary>
        /// Ensures existence of the table.
        /// </summary>
        private async Task<CloudTable> CreateTableIfNotExists(string tableName)
        {
            if (!_keyValuePairsTables.ContainsKey(tableName))
            {
                var table = _cloudTableClient.GetTableReference(tableName);
                await table.CreateIfNotExistsAsync().ConfigureAwait(false);
                _keyValuePairsTables[tableName] = table;
            }

            return _keyValuePairsTables[tableName];
        }

        /// <summary>
        /// Gets entities by query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        private async Task<IEnumerable<T>> QueryAsync<T>(CloudTable table, TableQuery<T> query)
            where T : class, ITableEntity, new()
        {
            var entities = new List<T>();

            TableContinuationToken token = null;
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return entities;
        }

        /// <summary>
        /// Get entities by query with TakeCount parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        private async Task<IEnumerable<T>> QueryAsyncWithTakeCount<T>(CloudTable table, TableQuery<T> query)
            where T : class, ITableEntity, new()
        {
            var entities = new List<T>();

            const int maxEntitiesPerQueryLimit = 1000;
            var totalTakeCount = query.TakeCount;
            var remainingRecordsToTake = query.TakeCount;

            TableContinuationToken token = null;
            do
            {
                query.TakeCount = remainingRecordsToTake >= maxEntitiesPerQueryLimit ? maxEntitiesPerQueryLimit : remainingRecordsToTake;
                remainingRecordsToTake -= query.TakeCount;

                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (entities.Count < totalTakeCount && token != null);

            return entities;
        }
    }
}


```


## Publish Azure Function From Visual Studio to Azure

Right Click On Project AzureFuns.EventHub.Example and Click on Publish Follow documentation on Microsoft [How to Publish Azure Function from Visual Studio](https://tutorials.visualstudio.com/first-azure-function/publish)

### Update Azure Function App Setting with below Key Values
        a) AzureWebJobsStorage
        b) CosmosDbConnectionString
        c) Update below as well in Constants.cs
        ```
           public const string EventHubName = "<EVENT HUB NAME>";
           public const string ConsumerGroupName = "<EVENT HUB CONSUMER GROUP NAME>";
           public const string EventHubConnectionStringName = "<EVENT HUB CONNECTION STRING NAME>"; 
        ```

