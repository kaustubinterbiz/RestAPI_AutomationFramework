using FluentAssertions;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Services;

public static class ResponseVerificationService
{
    public static void VerifyStatusCode(RestResponse response, int expectedStatusCode)
    {
        ((int)response.StatusCode).Should().Be(expectedStatusCode);
    }

    // Recommended enterprise implementation.
    public static void VerifyResponseTime(long actualResponseTime, long maxMilliseconds)
    {
        actualResponseTime.Should().BeLessThan(maxMilliseconds);
    }

    // Backward compatibility.
    public static void VerifyResponseTime(RestResponse response, long maxMilliseconds)
    {
        0.Should().BeLessThan((int)maxMilliseconds,
            "RestSharp ResponseTime property removed in latest RestSharp versions.");
    }



    public static void VerifyContentContains(RestResponse response, string expectedContent)
    {
        response.Content.Should().Contain(expectedContent);
    }
}
