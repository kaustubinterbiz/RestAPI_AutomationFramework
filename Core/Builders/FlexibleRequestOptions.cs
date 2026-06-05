namespace EnterpriseApiAutomationFramework.Core.Builders;

public sealed class FlexibleRequestOptions
{
    public Dictionary<string, string>? Headers { get; init; }
    public Dictionary<string, string>? QueryParams { get; init; }
    public Dictionary<string, string>? UrlSegments { get; init; }
    public object? Body { get; init; }

    public bool AuthorizationRequired { get; init; } = true;
    public string? BearerToken { get; init; }
    public bool BearerTokenProvided { get; init; }
}