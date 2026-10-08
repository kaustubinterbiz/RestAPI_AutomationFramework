using EnterpriseApiAutomationFramework.Core.Authorization;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

/// <summary>
/// In-memory cache for AuthorizationMatrix.xlsx to avoid repeated disk reads.
/// Loaded once per test run via <see cref="Load"/>.
/// </summary>
internal static class AuthorizationMatrixCache
{
    private static readonly object Gate = new();

    private static bool _loaded;
    private static List<Dictionary<string, string>> _endpointAccessRows = [];
    private static List<Dictionary<string, string>> _tokenScenarioRows = [];
    private static List<Dictionary<string, string>> _permissionRows = [];

    public static void Load()
    {
        lock (Gate)
        {
            if (_loaded)
                return;

            AuthorizationConfigBootstrap.EnsureAuthorizationMatrixWorkbook();

            _endpointAccessRows = ExcelReader.ReadSheet(
                AuthorizationConstants.MatrixExcelFile,
                AuthorizationConstants.EndpointAccessSheet);
            _tokenScenarioRows = ExcelReader.ReadSheet(
                AuthorizationConstants.MatrixExcelFile,
                AuthorizationConstants.TokenScenariosSheet);
            _permissionRows = ExcelReader.ReadSheet(
                AuthorizationConstants.MatrixExcelFile,
                AuthorizationConstants.PermissionsSheet);
            _loaded = true;
        }
    }

    public static IReadOnlyList<Dictionary<string, string>> GetEndpointAccessRows() =>
        GetRows(ref _loaded, _endpointAccessRows);

    public static IReadOnlyList<Dictionary<string, string>> GetTokenScenarioRows() =>
        GetRows(ref _loaded, _tokenScenarioRows);

    public static IReadOnlyList<Dictionary<string, string>> GetPermissionRows() =>
        GetRows(ref _loaded, _permissionRows);

    private static IReadOnlyList<Dictionary<string, string>> GetRows(
        ref bool loaded,
        List<Dictionary<string, string>> rows)
    {
        if (!loaded)
            Load();

        return rows;
    }
}
