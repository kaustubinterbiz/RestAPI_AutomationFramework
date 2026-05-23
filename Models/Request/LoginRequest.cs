namespace EnterpriseApiAutomationFramework.Models.Request;

public class LoginRequest
{
    public string grant_type { get; set; } = "password";
    public string client_id { get; set; } = string.Empty;
    public string scope { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 ROPC token request fields for application/x-www-form-urlencoded body.
    /// </summary>
    public Dictionary<string, string> ToFormParameters()
    {
        return new Dictionary<string, string>
        {
            ["grant_type"] = grant_type,
            ["client_id"] = client_id,
            ["scope"] = scope,
            ["username"] = username,
            ["password"] = password
        };
    }
}