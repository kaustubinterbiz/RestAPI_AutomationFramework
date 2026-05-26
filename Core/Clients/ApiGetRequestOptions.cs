namespace EnterpriseApiAutomationFramework.Core.Clients;

/// <summary>
/// Optional GET parameters. Omit body/token to skip them.
/// Fail only when <see cref="SetBody"/> or <see cref="SetBearerToken"/> was called with null/empty.
/// </summary>
public sealed class ApiGetRequestOptions
{
    public bool BodyProvided { get; private set; }

    public object? Body { get; private set; }

    public bool BearerTokenProvided { get; private set; }

    public string? BearerToken { get; private set; }

    /// <summary>When true and no explicit token, uses <see cref="Authentication.TokenManager"/> if available.</summary>
    public bool UseCachedTokenWhenTokenNotProvided { get; set; } = true;

    public static ApiGetRequestOptions Create() => new();

    public ApiGetRequestOptions SetBody(object body)
    {
        BodyProvided = true;
        Body = body;
        return this;
    }

    public ApiGetRequestOptions SetBearerToken(string bearerToken)
    {
        BearerTokenProvided = true;
        BearerToken = bearerToken;
        return this;
    }

    internal void ValidateProvidedValues()
    {
        if (BodyProvided && Body is null)
        {
            throw new ArgumentNullException(nameof(Body), "GET body was provided but is null.");
        }

        if (BodyProvided && Body is string bodyText && string.IsNullOrWhiteSpace(bodyText))
        {
            throw new ArgumentException("GET body was provided but is empty.", nameof(Body));
        }

        if (BearerTokenProvided && string.IsNullOrWhiteSpace(BearerToken))
        {
            throw new ArgumentException(
                "Bearer token was provided but is null or empty.",
                nameof(BearerToken));
        }
    }
}
