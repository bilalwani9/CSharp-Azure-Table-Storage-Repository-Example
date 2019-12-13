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
