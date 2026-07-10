using EnterpriseApiAutomationFramework.Core.Helpers;
using Reqnroll;
using RestSharp;

namespace EnterpriseApiAutomationFramework.Core.Authorization;

/// <summary>
/// Executes all enabled rows from AuthorizationMatrix.xlsx sequentially.
/// </summary>
public sealed class AuthorizationExecutor
{
    private readonly AuthorizationHelper _helper;

    public AuthorizationExecutor(AuthorizationHelper? helper = null) =>
        _helper = helper ?? new AuthorizationHelper();

    public async Task<RestResponse> ExecuteEndpointAccessAsync(
        ScenarioContext context,
        string endpointKey,
        string httpMethod,
        string role)
    {
        var entry = AuthorizationMatrixReader.FindEndpointAccessRow(endpointKey, httpMethod, role)
            ?? throw new InvalidOperationException(
                $"No EndpointAccess row found for endpoint '{endpointKey}', method '{httpMethod}', role '{role}'.");

        RoleProvider.ValidateRoleExists(role);
        var response = await _helper.ExecuteEndpointAccessAsync(context, entry);
        AuthorizationHelper.StoreLastResponse(context, response, entry.ExpectedStatus);
        return response;
    }

    public async Task ExecuteAllEndpointAccessRowsAsync(ScenarioContext context)
    {
        var rows = AuthorizationMatrixReader.GetEndpointAccessRows();
        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                $"No enabled rows in '{AuthorizationConstants.EndpointAccessSheet}' sheet.");
        }

        AuthorizationExecutionTracker.Begin(context);

        foreach (var entry in rows)
        {
            var label = $"{entry.Role} {entry.HttpMethod} {entry.EndpointKey}";
            try
            {
                RoleProvider.ValidateRoleExists(entry.Role);
                var response = await _helper.ExecuteEndpointAccessAsync(context, entry);
                var actual = (int)response.StatusCode;

                if (actual != entry.ExpectedStatus)
                {
                    AuthorizationExecutionTracker.RecordFailure(
                        context,
                        label,
                        actual,
                        entry.ExpectedStatus,
                        $"Expected {entry.ExpectedStatus}, got {actual}. Body: {response.Content}");
                    throw new InvalidOperationException(
                        $"{label}: expected {entry.ExpectedStatus}, actual {actual}.");
                }

                AuthorizationExecutionTracker.RecordSuccess(context, label, actual, entry.ExpectedStatus);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                AuthorizationExecutionTracker.RecordFailure(context, label, null, entry.ExpectedStatus, ex.Message);
                throw;
            }
        }
    }

    public async Task<RestResponse> ExecuteTokenScenarioAsync(
        ScenarioContext context,
        string scenarioType,
        string role)
    {
        var entry = AuthorizationMatrixReader.FindTokenScenario(scenarioType, role)
            ?? throw new InvalidOperationException(
                $"No TokenScenarios row found for scenario '{scenarioType}' and role '{role}'.");

        var response = await _helper.ExecuteTokenScenarioAsync(context, entry);
        AuthorizationHelper.StoreLastResponse(context, response, entry.ExpectedStatus);
        return response;
    }

    public async Task ExecuteAllTokenScenariosAsync(ScenarioContext context)
    {
        var rows = AuthorizationMatrixReader.GetTokenScenarioRows();
        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                $"No enabled rows in '{AuthorizationConstants.TokenScenariosSheet}' sheet.");
        }

        AuthorizationExecutionTracker.Begin(context);

        foreach (var entry in rows)
        {
            var label = $"{entry.ScenarioType}/{entry.Role}/{entry.EndpointKey}";
            try
            {
                if (!RoleProvider.IsGuestRole(entry.Role) && entry.Role != "-")
                    RoleProvider.ValidateRoleExists(entry.Role);

                var response = await _helper.ExecuteTokenScenarioAsync(context, entry);
                var actual = (int)response.StatusCode;

                if (actual != entry.ExpectedStatus)
                {
                    AuthorizationExecutionTracker.RecordFailure(
                        context,
                        label,
                        actual,
                        entry.ExpectedStatus,
                        $"Expected {entry.ExpectedStatus}, got {actual}. Body: {response.Content}");
                    throw new InvalidOperationException(
                        $"{label}: expected {entry.ExpectedStatus}, actual {actual}.");
                }

                AuthorizationExecutionTracker.RecordSuccess(context, label, actual, entry.ExpectedStatus);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                AuthorizationExecutionTracker.RecordFailure(context, label, null, entry.ExpectedStatus, ex.Message);
                throw;
            }
        }
    }
}
