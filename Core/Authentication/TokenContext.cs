using TechTalk.SpecFlow;

namespace EnterpriseApiAutomationFramework.Core.Authentication;

/// <summary>Scenario-scoped token storage for login/logout flows.</summary>
public static class TokenContext
{
    public const string StoredAccessTokenKey = "StoredAccessToken";
    public const string LastResponseKey = "LastResponse";

    public static void StoreAccessToken(ScenarioContext context, string token)
    {
        context.Set(token, StoredAccessTokenKey);
        TokenManager.SetAccessToken(token, persistToConfig: false);
    }

    public static string GetStoredAccessToken(ScenarioContext context) =>
        context.Get<string>(StoredAccessTokenKey);

    public static void SetLastResponse(ScenarioContext context, RestSharp.RestResponse response) =>
        context.Set(response, LastResponseKey);

    public static RestSharp.RestResponse GetLastResponse(ScenarioContext context) =>
        context.Get<RestSharp.RestResponse>(LastResponseKey);
}
