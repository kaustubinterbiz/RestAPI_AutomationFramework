namespace EnterpriseApiAutomationFramework.Core.Security.Reporting;

/// <summary>
/// ASP.NET Core remediation snippets keyed by failure class for developer action items.
/// </summary>
public static class SecurityRemediationCatalog
{
    private static readonly Dictionary<string, SecurityRemediationEntry> Entries =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["missing_auth"] = new SecurityRemediationEntry
            {
                Key = "missing_auth",
                Title = "Enforce authentication on protected endpoints",
                VulnerableCode = """
// Program.cs — JWT middleware registered AFTER MapControllers (wrong order)
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

// SessionController.cs — endpoint reachable without auth
[HttpGet("GetSessionInfo")]
public IActionResult GetSessionInfo()
{
    return Ok(_sessionService.GetCurrentUser());
}
""",
                SecuredCode = """
// Program.cs — correct middleware order
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SessionController.cs
[Authorize]
[HttpGet("GetSessionInfo")]
public IActionResult GetSessionInfo()
{
    return Ok(_sessionService.GetCurrentUser());
}
"""
            },
            ["invalid_jwt"] = new SecurityRemediationEntry
            {
                Key = "invalid_jwt",
                Title = "Validate JWT signature, issuer, audience, and lifetime",
                VulnerableCode = """
// Program.cs — token parsed but not validated
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false
        };
    });
""",
                SecuredCode = """
// Program.cs — full JWT validation (Azure AD B2C example)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["AzureAdB2C:Authority"];
        options.Audience = configuration["AzureAdB2C:ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
"""
            },
            ["rbac"] = new SecurityRemediationEntry
            {
                Key = "rbac",
                Title = "Enforce role-based access at controller or policy level",
                VulnerableCode = """
// ExistingUserController.cs — any authenticated user can call
[Authorize]
[HttpGet("ExistingUser")]
public IActionResult GetExistingUser(string emailId)
{
    return Ok(_userService.CheckExisting(emailId));
}
""",
                SecuredCode = """
// ExistingUserController.cs — restrict to privileged roles
[Authorize(Roles = "SuperAdmin,PlatformAdmin")]
[HttpGet("ExistingUser")]
public IActionResult GetExistingUser(string emailId)
{
    return Ok(_userService.CheckExisting(emailId));
}

// Or policy-based:
[Authorize(Policy = "CanViewCrossOrgUsers")]
[HttpGet("ExistingUser")]
public IActionResult GetExistingUser(string emailId) { ... }
"""
            },
            ["permission"] = new SecurityRemediationEntry
            {
                Key = "permission",
                Title = "Add resource-level permission checks beyond role membership",
                VulnerableCode = """
// Service layer trusts role name only
public bool CanDeleteTag(string memberId)
{
    return User.IsInRole("Organization Admin");
}
""",
                SecuredCode = """
// Custom authorization handler with permission matrix
public class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User.FindAll("permission").Select(c => c.Value);
        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

[Authorize(Policy = "DeleteTag")]
[HttpDelete("tags/{id}")]
public IActionResult DeleteTag(Guid id) { ... }
"""
            },
            ["valid_control"] = new SecurityRemediationEntry
            {
                Key = "valid_control",
                Title = "Positive control — no remediation required",
                VulnerableCode = "// N/A — this scenario verifies a valid token is accepted.",
                SecuredCode = "// Keep current JWT + [Authorize] configuration; monitor in CI."
            }
        };

    public static SecurityRemediationEntry Get(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key) && Entries.TryGetValue(key, out var entry))
            return entry;

        return Entries["invalid_jwt"];
    }

    public static IReadOnlyList<SecurityRemediationEntry> GetForFailedExecutions(
        IEnumerable<SecurityExecutionResult> executions) =>
        executions
            .Where(e => e.Status == SecurityTestStatus.Fail && e.RemediationKey != null)
            .Select(e => e.RemediationKey!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(Get)
            .ToList();
}
