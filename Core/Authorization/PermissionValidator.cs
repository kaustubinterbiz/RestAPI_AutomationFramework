using EnterpriseApiAutomationFramework.Core.Helpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Reqnroll;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Authorization;

/// <summary>
/// Validates permission matrix rules from Excel with soft assertions.
/// </summary>
public sealed class PermissionValidator
{
    private readonly AuthorizationHelper _helper;

    public PermissionValidator(AuthorizationHelper? helper = null) =>
        _helper = helper ?? new AuthorizationHelper();

    public async Task ValidateAllPermissionsAsync(ScenarioContext context)
    {
        var entries = AuthorizationMatrixReader.GetPermissionRows();
        if (entries.Count == 0)
        {
            throw new InvalidOperationException(
                $"No permission rows found in '{AuthorizationConstants.MatrixExcelFile}' sheet '{AuthorizationConstants.PermissionsSheet}'.");
        }

        AuthorizationExecutionTracker.Begin(context);
        using var scope = new AssertionScope();

        foreach (var entry in entries)
        {
            var label = $"{entry.Role}/{entry.Permission}/{entry.HttpMethod} {entry.EndpointKey}";
            try
            {
                var response = await _helper.ExecutePermissionCheckAsync(context, entry);
                var actual = (int)response.StatusCode;
                var expected = entry.ExpectedStatus;

                actual.Should().Be(expected,
                    because: $"role '{entry.Role}' permission '{entry.Permission}' on '{entry.EndpointKey}' should return {expected}");

                AuthorizationExecutionTracker.RecordSuccess(context, label, actual, expected);
            }
            catch (Exception ex)
            {
                AuthorizationExecutionTracker.RecordFailure(context, label, null, entry.ExpectedStatus, ex.Message);
                ex.Message.Should().BeEmpty(because: label);
            }
        }
    }

    public static void ValidateStatus(RestResponse response, int expectedStatus) =>
        AuthorizationHelper.ValidateStatusCode(response, expectedStatus);
}
