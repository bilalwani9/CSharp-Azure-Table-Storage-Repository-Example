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
