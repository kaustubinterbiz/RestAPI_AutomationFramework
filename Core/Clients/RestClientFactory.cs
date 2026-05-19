using RestSharp;
using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Clients;

public class RestClientFactory
{
    public RestClient GetClient()
    {
        var options = new RestClientOptions(ConfigReader.GetValue("BaseUrl"))
        {
            //MaxTimeout = int.Parse(ConfigReader.GetValue("Timeout"))
            Timeout = TimeSpan.FromMilliseconds(int.Parse(ConfigReader.GetValue("Timeout")))
        };

        return new RestClient(options);
    }
}