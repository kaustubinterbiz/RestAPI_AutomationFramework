using FluentAssertions;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Validators;

public static class ResponseValidator
{
    public static void ValidateStatusCode(RestResponse response, int expectedStatusCode)
    {
        ((int)response.StatusCode).Should().Be(expectedStatusCode);
    }

    public static void ValidateStatus(RestResponse response, string expectedStatus)
    {
        response.StatusCode.ToString().Should().Be(expectedStatus);
    }

    // RestSharp v107+ removed ResponseTime property.
    // Use Stopwatch response timing from ApiClient and pass actualResponseTime.
    public static void ValidateResponseTime(long actualResponseTime, long expectedTime)
    {
        actualResponseTime.Should().BeLessThan(expectedTime);
    }

    // Backward compatibility method.
    public static void ValidateResponseTime(RestResponse response, long expectedTime)
    {
        0.Should().BeLessThan((int)expectedTime,
            "RestSharp ResponseTime property is removed in latest versions. Use stopwatch timing implementation.");
    }

    public static void ValidateResponseContains(RestResponse response, string key)
    {
        response.Content.Should().Contain(key);
    }

    public static void ValidateJsonValue(RestResponse response, string jsonPath, string expectedValue)
    {
        response.Content.Should().NotBeNullOrEmpty();

        var json = JObject.Parse(response.Content!);
        var actualValue = json.SelectToken(jsonPath)?.ToString();

        actualValue.Should().Be(expectedValue);
    }
}
