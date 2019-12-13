namespace AzureFuncs.BigTable.Example
{
    using System;

    public class Settings : ISettings
    {
        public string FunctionStorageConnectionString { get; set; }
        public string CosmosDbConnectionString { get; set; }
        public Settings(Func<string, string> getter)
        {
            FunctionStorageConnectionString = getter("AzureWebJobsStorage");
            CosmosDbConnectionString = getter("CosmosDbConnectionString");
        }
    }
}
