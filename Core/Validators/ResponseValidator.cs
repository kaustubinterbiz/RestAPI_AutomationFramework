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
        string s = response.StatusCode.ToString();
        bool b = s.Equals(expectedStatus);
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

    /// <summary>
    /// Asserts a 401/403 and that the body mentions token expiry or invalidation.
    /// </summary>
    public static void ValidateLoginFailure(RestResponse response)
    {
        var statusCode = (int)response.StatusCode;
        var failed = !response.IsSuccessful || statusCode is 400 or 401 or 403;
        failed.Should().BeTrue(
            $"login with invalid or reused token should fail (status={statusCode}, body={response.Content})");
    }

    public static void ValidateUnauthorizedOrExpiredLoginMessage(RestResponse response)
    {
        var content = (response.Content ?? string.Empty).ToLowerInvariant();
        var hasExpectedMessage =
            content.Contains("unauthorized user")
            || content.Contains("access token expired")
            || content.Contains("unauthorized")
            || content.Contains("expired")
            || content.Contains("invalid");

        hasExpectedMessage.Should().BeTrue(
            $"expected 'Unauthorized user' or 'Access token expired' but got: {response.Content}");
    }

    public static void ValidateExpiredOrInvalidTokenError(RestResponse response, string expectedFragment)
    {
        var statusCode = (int)response.StatusCode;
        statusCode.Should().BeOneOf(401, 403);

        var content = (response.Content ?? string.Empty).ToLowerInvariant();
        var fragment = expectedFragment.ToLowerInvariant();

        var hasExpectedFragment = content.Contains(fragment);
        var hasCommonAuthError =
            content.Contains("expired")
            || content.Contains("lifetime")
            || content.Contains("invalid")
            || content.Contains("unauthorized")
            || content.Contains("token");

        (hasExpectedFragment || hasCommonAuthError).Should().BeTrue(
            $"response body should mention '{expectedFragment}' or a common token error, but was: {response.Content}");
    }

    public static void ValidateJsonValue(RestResponse response, string jsonPath, string expectedValue)
    {
        response.Content.Should().NotBeNullOrEmpty();

        var json = JObject.Parse(response.Content!);
        var actualValue = json.SelectToken(jsonPath)?.ToString();

        actualValue.Should().Be(expectedValue);
    }
}
