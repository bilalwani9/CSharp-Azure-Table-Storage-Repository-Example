namespace AzureFuncs.BigTable.Example
{
    public interface ISettings
    {
        string FunctionStorageConnectionString { get; }

        string CosmosDbConnectionString { get; }
    }
}
