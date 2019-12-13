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
