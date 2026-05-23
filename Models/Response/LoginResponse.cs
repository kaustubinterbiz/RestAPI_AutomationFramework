namespace EnterpriseApiAutomationFramework.Models.Response;

public class LoginResponse
{
    public string access_token { get; set; } = string.Empty;
    public string token_type { get; set; } = string.Empty;
    public string expires_in { get; set; }
}