namespace AzureFuns.Common
{
    using AzureFuns.Data.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization; 
    using System.IO; 

    public class EmployeeDataParser : IParser<RawEmployee>
    {
        private readonly JsonSerializerSettings _settings;

        public EmployeeDataParser()
        {
            var resolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            _settings = new JsonSerializerSettings
            {
                ContractResolver = resolver, 
                Formatting = Formatting.Indented
            };
        }

        public RawEmployee Parse(string input)
        {
            return JsonConvert.DeserializeObject<RawEmployee>(input, _settings);
        }

        public RawEmployee ReadAndParse(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var input = reader.ReadToEnd();
                return Parse(input);
            }
        }
    }
}
